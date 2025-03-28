using Microsoft.EntityFrameworkCore;

using SignalGenerator.Core.Interfaces;
using SignalGenerator.Core.Services;
using SignalGenerator.Core.Data;

var builder = WebApplication.CreateBuilder(args);

// اضافه کردن DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// اضافه کردن سرویس‌های مربوط به دیتا
builder.Services.AddScoped<IProtocolDataStore, SqlSignalDataStore>();
builder.Services.AddScoped<SignalProcessorService>();

// اضافه کردن MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// پیکربندی میدل‌ورها
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
