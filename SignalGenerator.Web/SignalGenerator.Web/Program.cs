using Microsoft.EntityFrameworkCore;
using SignalGenerator.Core.Data;
using SignalGenerator.Core.Interfaces;
using SignalGenerator.Core.Services;
using SignalGenerator.Protocols.Modbus;
using SignalGenerator.Protocols.Http;
using SignalGenerator.Protocols.SignalR;
using SignalGenerator.Web;
using SignalGenerator.Web.SignalHub;

var builder = WebApplication.CreateBuilder(args);

// پیکربندی سرویس‌ها
builder.Services.AddControllersWithViews(); // برای پشتیبانی از صفحات ویو (MVC)

// پیکربندی DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// پیکربندی پروتکل‌ها به صورت داینامیک
builder.Services.AddScoped<IProtocolCommunication>(serviceProvider =>
{
    var protocolType = builder.Configuration.GetValue<string>("ProtocolType"); // می‌توان از ورودی‌ها یا تنظیمات استفاده کرد
    return protocolType switch
    {
        "modbus" => new ModbusProtocol("192.168.1.100", 502),
        "http" => new Http_Protocol("http://localhost:5000"),
        "signalar" => new SignalRProtocol("http://localhost:5000/signalhub"),
        _ => throw new ArgumentException("Invalid protocol type.")
    };
});

// پیکربندی ذخیره‌سازی داده‌ها
builder.Services.AddScoped<IProtocolDataStore, SqlSignalDataStore>(); // ذخیره‌سازی داده‌ها در دیتابیس
builder.Services.AddScoped<SignalProcessorService>(); // سرویس پردازش سیگنال‌ها

// پیکربندی SignalR
builder.Services.AddSignalR(); // برای پشتیبانی از SignalR

var app = builder.Build();

// پیکربندی مسیریابی
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseStaticFiles(); // برای سرویس‌دهی به فایل‌های استاتیک مثل CSS, JS

// پیکربندی مسیرهای کنترلر و SignalR
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // روتینگ برای صفحه اصلی (Home)

// اگر به SignalController نیاز دارید
app.MapControllerRoute(
    name: "signal",
    pattern: "Signal/{action=Index}/{id?}", // تنظیم روتینگ برای SignalController
    defaults: new { controller = "Signal", action = "Index" });

app.MapControllers(); // برای API ها
app.MapHub<SignalHub>("/signalHub"); // مسیر SignalR

app.Run();
