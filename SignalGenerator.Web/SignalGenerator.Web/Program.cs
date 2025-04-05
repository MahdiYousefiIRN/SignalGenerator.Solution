using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SignalGenerator.Data.Data;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.Services;
using SignalGenerator.Data.SignalProtocol;
using SignalGenerator.Helpers;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.Modbus;
using SignalGenerator.Web;
using SignalGenerator.Web.Data.Interface;
using SignalGenerator.Web.Data.Services;
using SignalGenerator.Web.Interfaces;
using SignalGenerator.Web.Services;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// تنظیمات LoggerHelper
ConfigureLoggerHelper(builder);

// تنظیمات سرویس‌ها
await ConfigureServicesAsync(builder);

var app = builder.Build();

// پیکربندی میدل‌ویر و راه‌اندازی اپلیکیشن
ConfigureMiddleware(app);

app.Run();

// 🛠️ متدهای ماژولار برای خوانایی و نگهداری بهتر کد
// ----------------------------------------------------

// تنظیمات LoggerHelper (لاگ‌گیری کنسول و فایل)
void ConfigureLoggerHelper(WebApplicationBuilder builder)
{
    builder.Services.AddSingleton<ILoggerService, LoggerHelper>(provider =>
    {
        var loggerHelper = new LoggerHelper(
            logFilePath: "logs.json",
            source: "SignalGeneratorApp",
            minLogLevel: SignalGenerator.Helpers.LogLevel.Info
        );
        loggerHelper.EnableConsoleLogging();
        loggerHelper.EnableFileLogging();
        return loggerHelper;
    });
}

// تنظیمات سرویس‌های مختلف
async Task ConfigureServicesAsync(WebApplicationBuilder builder)
{
    var loggerService = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerService>();

    // ثبت IHttpClientFactory
    builder.Services.AddHttpClient();  // ثبت IHttpClientFactory

    // لاگ‌گیری مرحله‌ای
    await LogInitializationStep(loggerService, "📌 Initializing core services...");
    RegisterCoreServices(builder);

    await LogInitializationStep(loggerService, "📌 Setting up the database...");
    RegisterDatabase(builder);

    await LogInitializationStep(loggerService, "📌 Configuring Identity...");
    RegisterIdentity(builder);

    await LogInitializationStep(loggerService, "📌 Initializing communication protocols...");
    RegisterProtocols(builder);

    await LogInitializationStep(loggerService, "📌 Setting up SignalR services...");
    RegisterSignalR(builder);

    await LogInitializationStep(loggerService, "📌 Configuring CORS policy...");
    RegisterCors(builder);

    await LogInitializationStep(loggerService, "📌 Configuring response compression...");
    RegisterResponseCompression(builder);

    // اضافه کردن Swagger برای محیط توسعه
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }
}

// ثبت سرویس‌های اصلی
void RegisterCoreServices(WebApplicationBuilder builder)
{
    builder.Services.AddRazorPages();
    builder.Services.AddControllersWithViews();

    builder.Services.AddScoped<ISignalTestingService, SignalTestingService>();
    builder.Services.AddScoped<ISignalProcessorService, SignalProcessorService>();
    builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
    builder.Services.AddScoped<IDataExportService, DataExportService>();
    builder.Services.AddScoped<IProtocolDataStore, ProtocolDataStore>();
    builder.Services.AddScoped<AppState>();
    builder.Services.AddScoped<ISignalDataService, SignalDataService>();

    builder.Services.AddScoped<SignalGenerator.Protocols.ProtocolFactory>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var loggerService = sp.GetRequiredService<ILoggerService>();
        return new SignalGenerator.Protocols.ProtocolFactory(sp, config);
    });
}

// تنظیمات پایگاه داده
void RegisterDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        throw new Exception("Database connection string is missing!");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// تنظیمات Identity
void RegisterIdentity(WebApplicationBuilder builder)
{
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
}

// تنظیمات پروتکل‌ها
void RegisterProtocols(WebApplicationBuilder builder)
{
    var config = builder.Configuration;

    RegisterProtocol<Http_Protocol>(builder, config, "HttpProtocol:BaseUrl", "http://localhost:5000");
    RegisterProtocol<ModbusProtocol>(builder, config, "ProtocolSettings:Modbus:DefaultIp", "127.0.0.1");
    RegisterProtocol<SignalRProtocol>(builder, config, "ProtocolSettings:SignalR:HubUrl", "http://localhost:5000/signalhub");
}

// ثبت پروتکل‌ها به صورت مشترک
// ثبت پروتکل‌ها به صورت مشترک
void RegisterProtocol<TProtocol>(WebApplicationBuilder builder, IConfiguration config, string configKey, string defaultValue)
    where TProtocol : class, IProtocolCommunication
{
    builder.Services.AddScoped<IProtocolCommunication>(sp =>
    {
        var logger = sp.GetRequiredService<ILoggerService>();
        var protocolUrl = config.GetValue<string>(configKey) ?? defaultValue;

        // برای ModbusProtocol که به پارامترهای خاص نیاز دارد
        if (typeof(TProtocol) == typeof(ModbusProtocol))
        {
            var ipAddress = config.GetValue<string>("ProtocolSettings:Modbus:IpAddress");
            var port = config.GetValue<int>("ProtocolSettings:Modbus:Port");

            // ایجاد و بازگشت نمونه ModbusProtocol با پارامترهای لازم
            return new ModbusProtocol(ipAddress, port, logger);
        }

        // برای دیگر پروتکل‌ها از ActivatorUtilities استفاده می‌کنیم.
        return ActivatorUtilities.CreateInstance<TProtocol>(sp, protocolUrl, logger);
    });
}

// تنظیمات SignalR
void RegisterSignalR(WebApplicationBuilder builder)
{
    var config = builder.Configuration;
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = config.GetValue<long>("SignalR:MaxMessageSize", 512000);
    }).AddMessagePackProtocol();

    builder.Services.AddScoped<SignalProcessorService>();
    builder.Services.AddServerSideBlazor();
}

// تنظیمات CORS
void RegisterCors(WebApplicationBuilder builder)
{
    var allowedOrigins = builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:5000" };

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
}

// تنظیمات فشرده‌سازی پاسخ‌ها
void RegisterResponseCompression(WebApplicationBuilder builder)
{
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
}

// پیکربندی میدل‌ویر
void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("SignalRPolicy");
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();
    app.MapBlazorHub();
}

// متد کمک برای لاگ‌گیری مرحله‌ای
async Task LogInitializationStep(ILoggerService loggerService, string message)
{
    await loggerService.LogAsync(message);
}
