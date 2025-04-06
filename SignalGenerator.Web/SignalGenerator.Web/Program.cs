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

// بارگذاری تنظیمات ProtocolConfigs از appsettings.json
builder.Services.Configure<ProtocolConfigs>(
    builder.Configuration.GetSection("ProtocolConfigs")); // 👈 این خط کلیدیه

// تنظیمات سرویس‌ها
await ConfigureServicesAsync(builder);

var app = builder.Build();

// پیکربندی میدل‌ویر و راه‌اندازی اپلیکیشن
ConfigureMiddleware(app);

app.Run();

// ----------------------------------------------------
// متدهای ماژولار برای خوانایی و نگهداری بهتر کد
// ----------------------------------------------------

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

async Task ConfigureServicesAsync(WebApplicationBuilder builder)
{
    var loggerService = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerService>();

    builder.Services.AddHttpClient();

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

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }
}

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

    builder.Services.AddScoped<ProtocolConfigProvider>(); // 👈 مهم
    builder.Services.AddScoped<ProtocolFactory>(sp =>
    {
        var configProvider = sp.GetRequiredService<ProtocolConfigProvider>();
        var loggerService = sp.GetRequiredService<ILoggerService>();
        return new ProtocolFactory(sp, configProvider);
    });
}

void RegisterDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        throw new Exception("Database connection string is missing!");

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

void RegisterIdentity(WebApplicationBuilder builder)
{
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
}

void RegisterProtocols(WebApplicationBuilder builder)
{
    var config = builder.Configuration;

    RegisterProtocol<Http_Protocol>(builder, config, "ProtocolConfigs:Http:BasePath", "/api");
    RegisterProtocol<ModbusProtocol>(builder, config, "ProtocolConfigs:Modbus:IpAddress", "127.0.0.1");
    RegisterProtocol<SignalRProtocol>(builder, config, "ProtocolConfigs:SignalR:HubUrl", "http://localhost:5000/signalhub");
}

void RegisterProtocol<TProtocol>(WebApplicationBuilder builder, IConfiguration config, string configKey, string defaultValue)
    where TProtocol : class, IProtocolCommunication
{
    builder.Services.AddScoped<IProtocolCommunication>(sp =>
    {
        var logger = sp.GetRequiredService<ILoggerService>();
        var protocolUrl = config.GetValue<string>(configKey) ?? defaultValue;

        if (typeof(TProtocol) == typeof(ModbusProtocol))
        {
            var ipAddress = config.GetValue<string>("ProtocolConfigs:Modbus:IpAddress");
            var port = config.GetValue<int>("ProtocolConfigs:Modbus:Port");

            return new ModbusProtocol(ipAddress, port, logger);
        }

        return ActivatorUtilities.CreateInstance<TProtocol>(sp, protocolUrl, logger);
    });
}

void RegisterSignalR(WebApplicationBuilder builder)
{
    var config = builder.Configuration;
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.MaximumReceiveMessageSize = config.GetValue<long>("ProtocolConfigs:SignalR:MaxMessageSize", 512000);
    }).AddMessagePackProtocol();

    builder.Services.AddScoped<SignalProcessorService>();
    builder.Services.AddServerSideBlazor();
}

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

    // ✅ این خط اضافه شده: مسیر SignalR Hub
    app.MapHub<SignalHub>("/signalhub"); // حتما namespace رو هم اضافه کن
}


async Task LogInitializationStep(ILoggerService loggerService, string message)
{
    await loggerService.LogAsync(message);
}
