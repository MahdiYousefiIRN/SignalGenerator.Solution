using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SignalGenerator.Data.Data;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Services;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.Modbus;
using SignalGenerator.Protocols.SignalR;
using SignalGenerator.Data.Models;
using System.IO.Compression;
using SignalGenerator.Web;
using SignalGenerator.Web.Services;
using SignalGenerator.Web.Data.Interface;
using SignalGenerator.Web.Data.Services;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// -------------------------
// ✨ Service Registration
// -------------------------

// Razor Pages + MVC Controllers
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

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

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// -------------------------
// 🌐 Register Protocol Services
// -------------------------

// تنظیم پروتکل HTTP
var httpProtocolBaseUrl = builder.Configuration["HttpProtocol:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddScoped<IProtocolCommunication, Http_Protocol>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Http_Protocol>>();
    return new Http_Protocol(httpProtocolBaseUrl, logger);
});

// تنظیم پروتکل Modbus
var modbusSettings = builder.Configuration.GetSection("ProtocolSettings:Modbus");
var modbusIp = modbusSettings["DefaultIp"] ?? "127.0.0.1";
var modbusPort = int.Parse(modbusSettings["DefaultPort"] ?? "502");

builder.Services.AddScoped<IProtocolCommunication, ModbusProtocol>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ModbusProtocol>>();
    return new ModbusProtocol(modbusIp, modbusPort, logger);
});

// تنظیم پروتکل SignalR
builder.Services.AddScoped<SignalRProtocol>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SignalRProtocol>>();
    var connectionString = builder.Configuration["SignalR:ConnectionString"];
    return new SignalRProtocol(connectionString, logger);
});

// -------------------------
// 🛠️ SignalR Configuration
// -------------------------
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = builder.Configuration.GetValue<long>("SignalR:MaxMessageSize", 512000);
})
.AddMessagePackProtocol();
builder.Services.AddServerSideBlazor();

// -------------------------
// 📦 Response Compression
// -------------------------
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal;
    });
}

// -------------------------
// 🔒 CORS Configuration
// -------------------------
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

// -------------------------
// 📡 Project Services
// -------------------------
builder.Services.AddScoped<IProtocolDataStore, SqlSignalDataStore>();
builder.Services.AddScoped<SignalProcessorService>();
builder.Services.AddScoped<IDataExportService, DataExportService>();
builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
builder.Services.AddScoped<ISystemEvaluationService, SystemEvaluationService>();
builder.Services.AddScoped<ISignalTestingService, SignalTestingService>(); // ثبت ISignalTestingService
builder.Services.AddScoped<AppState>(); // ثبت AppState در DI
builder.Services.AddScoped<ISignalDataService, SignalDataService>();

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
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (builder.Configuration.GetValue<bool>("Security:RequireHttps"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseRouting();
app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

// -------------------------
// 📌 Endpoints (Controllers + Razor Pages)
// -------------------------
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");  // مهم برای مسیر‌یابی Blazor
app.MapControllers();
app.MapHub<SignalHub>("/signalHub");

// 📌 تنظیم مسیر پیش‌فرض به HomeController
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// -------------------------
// 🏁 Run Application
// -------------------------
app.Run();
