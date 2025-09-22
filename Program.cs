using BuoySystem.Services;
using BuoySystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the email service
builder.Services.AddScoped<IEmailService, EmailService>();

// CORS policy: allow local dev and your Azure Static Web App
const string CorsPolicy = "FrontendDev";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, p => p
        .WithOrigins(
            "http://localhost:3000",
            "https://zealous-water-0d99f0a0f.2.azurestaticapps.net" // ← your SWA origin
        )
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthorization();

app.MapControllers();

app.Run();
