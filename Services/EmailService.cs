using Azure.Communication.Email;
using BuoySystem.Models;
using System.Text;

namespace BuoySystem.Services
{
    public interface IEmailService
    {
        Task<BuoyCommandResponse> SendBuoyCommandAsync(BuoyCommandRequest request);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailClient _emailClient;
        private readonly string _senderEmail;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            var connectionString = configuration.GetConnectionString("AzureCommunicationServices");
            _senderEmail = configuration["EmailSettings:SenderEmail"] ?? "donotreply@your-verified-domain.azurecomm.net";
            _logger = logger;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Azure Communication Services connection string is not configured");
            }

            _emailClient = new EmailClient(connectionString);
        }

        public async Task<BuoyCommandResponse> SendBuoyCommandAsync(BuoyCommandRequest request)
        {
            var transactionId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Processing buoy command for IMEI: {Imei}, Transaction: {TransactionId}", 
                    request.Imei, transactionId);

                // Generate .sbd file content
                var sbdContent = GenerateSbdContent(request.Command);
                var base64Content = Convert.ToBase64String(sbdContent);

                // Create email message
                var emailMessage = new EmailMessage(
                    senderAddress: _senderEmail,
                    content: new EmailContent($"{request.Imei}")
                    {
                        PlainText = $"Attached is the command for IMEI {request.Imei}.\n\nCommand: {request.Command}",
                        Html = $@"
                            <html>
                                <body>
                                    <h3> {request.Imei}</h3>
                                    <p><strong>Command:</strong> {request.Command}</p>
                                    <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                                    <p><strong>Transaction ID:</strong> {transactionId}</p>
                                </body>
                            </html>"
                    },
                    recipients: new EmailRecipients(new List<EmailAddress>
                    {
                        new EmailAddress(request.RecipientEmail, request.RecipientDisplayName ?? "Command Receiver")
                    }));

                // Add attachment
                var attachment = new EmailAttachment(
                    name: "command.sbd",
                    contentType: "application/octet-stream",
                    content: BinaryData.FromBytes(sbdContent));

                emailMessage.Attachments.Add(attachment);

                // Send email
                var sendResult = await _emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);

                if (sendResult.HasCompleted && sendResult.Value.Status == EmailSendStatus.Succeeded)
                {
                    _logger.LogInformation("Email sent successfully for transaction: {TransactionId}", transactionId);
                    
                    return new BuoyCommandResponse
                    {
                        Success = true,
                        Message = "Command sent successfully!",
                        TransactionId = transactionId
                    };
                }
                else
                {
                    var errorMsg = $"Email sending failed. Status: {sendResult.Value.Status}";
                    _logger.LogError("Email sending failed for transaction: {TransactionId}. Status: {Status}", 
                        transactionId, sendResult.Value.Status);
                    
                    return new BuoyCommandResponse
                    {
                        Success = false,
                        Message = "Failed to send command",
                        ErrorDetails = errorMsg,
                        TransactionId = transactionId
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending buoy command for transaction: {TransactionId}", transactionId);
                
                return new BuoyCommandResponse
                {
                    Success = false,
                    Message = "An error occurred while sending the command",
                    ErrorDetails = ex.Message,
                    TransactionId = transactionId
                };
            }
        }

        private static byte[] GenerateSbdContent(string command)
        {
            // Convert command to bytes using UTF-8 encoding
            return Encoding.UTF8.GetBytes(command);
        }
    }
}