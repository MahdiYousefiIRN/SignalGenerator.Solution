using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Logging;
using SignalGenerator.Data.Data;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Services;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.Modbus;
using SignalGenerator.Web.SignalHub;
using SignalGenerator.Data.Models;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// ✨ Service Registration
// -------------------------

// Razor Pages فقط
builder.Services.AddRazorPages();

// Configure Logging
builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Protocol Services
var httpProtocolBaseUrl = builder.Configuration["HttpProtocol:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddTransient<IProtocolCommunication, Http_Protocol>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Http_Protocol>>();
    return new Http_Protocol(httpProtocolBaseUrl, logger);
});

var modbusIp = builder.Configuration["Modbus:IpAddress"] ?? "127.0.0.1";
var modbusPort = int.Parse(builder.Configuration["Modbus:Port"] ?? "502");
builder.Services.AddTransient<IProtocolCommunication, ModbusProtocol>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ModbusProtocol>>();
    return new ModbusProtocol(modbusIp, modbusPort, logger);
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = builder.Configuration.GetValue<long>("SignalR:MaxMessageSize", 512000);
})
.AddMessagePackProtocol();

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Project Services
builder.Services.AddScoped<IProtocolDataStore, SqlSignalDataStore>();
builder.Services.AddScoped<SignalProcessorService>();
builder.Services.AddScoped<IDataExportService, DataExportService>();
builder.Services.AddScoped<ISignalTestingService, SignalTestingService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
builder.Services.AddScoped<ISystemEvaluationService, SystemEvaluationService>();

// -------------------------
// 🚀 Build Application
// -------------------------
var app = builder.Build();

// -------------------------
// ✨ Middleware
// -------------------------

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCompression();
app.UseRouting();
app.UseCors("SignalRPolicy");

// -------------------------
// 📌 Endpoints (Only Razor Pages)
// -------------------------
app.UseAuthorization();
app.MapRazorPages();
app.MapHub<SignalHub>("/signalHub");

// اگر مسیر ناشناخته باشد، به `/Home/Index` هدایت شود
app.MapFallbackToPage("/Home/Index");

// -------------------------
// 🏁 Run Application
// -------------------------
app.Run();
