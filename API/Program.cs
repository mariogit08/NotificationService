using Application;
using Application.Services;
using Polly;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var config = builder.Configuration;
var rateLimitOptions = config.GetSection("RateLimitOptions");
builder.Services.Configure<RateLimitOptions>(rateLimitOptions);
builder.Services.AddSingleton<Gateway>();
builder.Services.AddSingleton<BackgroundQueue<Notification>>();
builder.Services.AddSingleton<INotificationService, NotificationServiceImpl>();
builder.Services.AddHostedService<NotificationProcessingService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();




