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

// Configure Logger Helper (for logging information, errors, etc.)
ConfigureLoggerHelper(builder);

// Load ProtocolConfigs from appsettings.json
builder.Services.Configure<ProtocolConfigs>(
    builder.Configuration.GetSection("ProtocolConfigs")); // 👈 This line is crucial for loading protocol configurations

// Configure services (dependencies)
await ConfigureServicesAsync(builder);

var app = builder.Build();

// Configure middleware and application startup
ConfigureMiddleware(app);

app.Run();

// ----------------------------------------------------
// Modular Methods for Improved Readability and Maintenance
// ----------------------------------------------------

// Configures the logger service
void ConfigureLoggerHelper(WebApplicationBuilder builder)
{
    builder.Services.AddSingleton<ILoggerService, LoggerHelper>(provider =>
    {
        var loggerHelper = new LoggerHelper(
            logDirectory: "Logs",          // Log directory location
            source: "SignalGeneratorApp",  // Source name for logs
            minLogLevel: SignalGenerator.Helpers.LogLevel.Info // Minimum log level (info level)
        );
        loggerHelper.EnableConsoleLogging(); // Enable console logging
        loggerHelper.EnableFileLogging();   // Enable file logging
        return loggerHelper;
    });

}

// Configures services and dependencies asynchronously
async Task ConfigureServicesAsync(WebApplicationBuilder builder)
{
    var loggerService = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerService>();

    builder.Services.AddHttpClient(); // Register HTTP client for external requests

    // Log initialization steps
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

    // Add Swagger and API Explorer in development environment
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }
}

// Registers core services such as SignalProcessor, SignalTesting, and others
void RegisterCoreServices(WebApplicationBuilder builder)
{
    builder.Services.AddRazorPages();   // Add Razor pages support
    builder.Services.AddControllersWithViews(); // Add MVC controllers with views

    // Register scoped services for dependency injection
    builder.Services.AddScoped<ISignalTestingService, SignalTestingService>();
    builder.Services.AddScoped<ISignalProcessorService, SignalProcessorService>();
    builder.Services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
    builder.Services.AddScoped<IDataExportService, DataExportService>();
    builder.Services.AddScoped<IProtocolDataStore, ProtocolDataStore>();
    builder.Services.AddScoped<AppState>();  // AppState for managing application state
    builder.Services.AddScoped<ISignalDataService, SignalDataService>();

    builder.Services.AddScoped<ProtocolConfigProvider>(); // Protocol configuration provider
    builder.Services.AddScoped<ProtocolFactory>(sp =>
    {
        var configProvider = sp.GetRequiredService<ProtocolConfigProvider>();
        var loggerService = sp.GetRequiredService<ILoggerService>();
        return new ProtocolFactory(sp, configProvider);  // Factory for creating protocol instances
    });
}

// Registers the database context with connection string
void RegisterDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
        throw new Exception("Database connection string is missing!"); // Ensure the connection string is provided

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString)); // Configure SQL Server context
}

// Configures Identity for user management and authentication
void RegisterIdentity(WebApplicationBuilder builder)
{
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders(); // Adds token providers for account verification, password reset, etc.
}

// Registers communication protocols (HTTP, Modbus, SignalR)
void RegisterProtocols(WebApplicationBuilder builder)
{
    var config = builder.Configuration;

    // Register each protocol using a helper method
    RegisterProtocol<Http_Protocol>(builder, config, "ProtocolConfigs:Http:BasePath", "/api");
    RegisterProtocol<ModbusProtocol>(builder, config, "ProtocolConfigs:Modbus:IpAddress", "127.0.0.1");
    RegisterProtocol<SignalRProtocol>(builder, config, "ProtocolConfigs:SignalR:HubUrl", "https://localhost:7001/signalhub");
}

// Helper method to register a specific protocol
void RegisterProtocol<TProtocol>(WebApplicationBuilder builder, IConfiguration config, string configKey, string defaultValue)
    where TProtocol : class, IProtocolCommunication
{
    builder.Services.AddScoped<IProtocolCommunication>(sp =>
    {
        var logger = sp.GetRequiredService<ILoggerService>();
        var protocolUrl = config.GetValue<string>(configKey) ?? defaultValue;  // Fetch protocol URL from config

        if (typeof(TProtocol) == typeof(ModbusProtocol))
        {
            var ipAddress = config.GetValue<string>("ProtocolConfigs:Modbus:IpAddress");
            var port = config.GetValue<int>("ProtocolConfigs:Modbus:Port");

            return new ModbusProtocol(ipAddress, port, logger); // Return ModbusProtocol instance
        }

        return ActivatorUtilities.CreateInstance<TProtocol>(sp, protocolUrl, logger); // Instantiate other protocols dynamically
    });
}

// Configures SignalR services
void RegisterSignalR(WebApplicationBuilder builder)
{
    var config = builder.Configuration;
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();  // Enable detailed errors in development
        options.MaximumReceiveMessageSize = config.GetValue<long>("ProtocolConfigs:SignalR:MaxMessageSize", 512000);  // Set max message size
    }).AddMessagePackProtocol();  // Add MessagePack protocol for SignalR

    builder.Services.AddScoped<SignalProcessorService>();  // Register SignalProcessorService
    builder.Services.AddServerSideBlazor();  // Add Blazor server-side support
}

// Configures CORS (Cross-Origin Resource Sharing) policy
void RegisterCors(WebApplicationBuilder builder)
{
    var allowedOrigins = builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:5000" };  // Default to localhost if no origins are specified

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SignalRPolicy", policy =>
        {
            policy.WithOrigins(allowedOrigins)  // Allow requests from the configured origins
                  .AllowAnyMethod()  // Allow any HTTP method (GET, POST, etc.)
                  .AllowAnyHeader()  // Allow any headers
                  .AllowCredentials();  // Allow credentials in requests
        });
    });
}

// Registers response compression (using Gzip for production environment)
void RegisterResponseCompression(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsDevelopment())  // Only enable compression in non-development environments
    {
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;  // Enable compression for HTTPS traffic
            options.Providers.Add<GzipCompressionProvider>();  // Use Gzip compression
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;  // Set the optimal compression level
        });
    }
}

// Configures middleware and routes for the application
void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();  // Show developer exception page in development mode
        app.UseSwagger();  // Enable Swagger for API documentation
        app.UseSwaggerUI();  // Use Swagger UI
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");  // Custom error page for production
        app.UseHsts();  // Enable HTTP Strict Transport Security (HSTS)
    }

    app.UseHttpsRedirection();  // Redirect HTTP to HTTPS
    app.UseStaticFiles();  // Serve static files (images, CSS, JS, etc.)
    app.UseRouting();  // Enable routing for the app
    app.UseCors("SignalRPolicy");  // Apply the SignalR CORS policy
    app.UseAuthorization();  // Enable authorization (required for controllers and Razor pages)

    // Configure routes for controllers, Razor pages, and Blazor
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapRazorPages();
    app.MapBlazorHub();  // Map the Blazor Hub route

    // ✅ Add the SignalR Hub route
    app.MapHub<SignalHub>("/signalhub");  // Define the SignalR Hub route
}

// Logs the initialization step messages asynchronously
async Task LogInitializationStep(ILoggerService loggerService, string message)
{
    await loggerService.LogAsync(message);  // Log the message asynchronously
}
