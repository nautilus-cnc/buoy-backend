using Microsoft.AspNetCore.Mvc;
using BuoySystem.Models;
using BuoySystem.Services;
using System.ComponentModel.DataAnnotations;

namespace BuoySystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BuoyController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<BuoyController> _logger;

        public BuoyController(IEmailService emailService, ILogger<BuoyController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Sends a command to a buoy via email with .sbd file attachment
        /// </summary>
        /// <param name="request">The buoy command request containing IMEI, command, and recipient details</param>
        /// <returns>Response indicating success or failure of the operation</returns>
        [HttpPost("send-command")]
        [ProducesResponseType(typeof(BuoyCommandResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BuoyCommandResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BuoyCommandResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BuoyCommandResponse>> SendCommand([FromBody] BuoyCommandRequest request)
        {
            try
            {
                _logger.LogInformation("Received command request for IMEI: {Imei}", request.Imei);

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();

                    var errorResponse = new BuoyCommandResponse
                    {
                        Success = false,
                        Message = "Validation failed",
                        ErrorDetails = string.Join("; ", errors)
                    };

                    return BadRequest(errorResponse);
                }

                // Send the command
                var result = await _emailService.SendBuoyCommandAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing command request for IMEI: {Imei}", request.Imei);

                var errorResponse = new BuoyCommandResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    ErrorDetails = ex.Message
                };

                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Health check endpoint to verify the API is running
        /// </summary>
        /// <returns>Simple health status</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<object> HealthCheck()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "Buoy Command API"
            });
        }

        /// <summary>
        /// Get API information and available endpoints
        /// </summary>
        /// <returns>API information</returns>
        [HttpGet("info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<object> GetApiInfo()
        {
            return Ok(new
            {
                Name = "Buoy Command API",
                Version = "1.0.0",
                Description = "API for sending commands to remote buoy systems via email",
                Endpoints = new[]
                {
                    new { Method = "POST", Path = "/api/buoy/send-command", Description = "Send command to buoy" },
                    new { Method = "GET", Path = "/api/buoy/health", Description = "Health check" },
                    new { Method = "GET", Path = "/api/buoy/info", Description = "API information" }
                },
                Timestamp = DateTime.UtcNow
            });
        }
    }
}