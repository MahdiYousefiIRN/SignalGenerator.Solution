using Microsoft.Extensions.Options;
using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;

public class ProtocolConfigProvider
{
    private readonly Dictionary<string, IProtocolConfig> _protocolConfigs;

    public ProtocolConfigProvider(IOptions<ProtocolConfigs> options)
    {
        var settings = options.Value;

        // اطمینان از اینکه مقادیر به درستی پر شده‌اند
        if (settings == null)
        {
            throw new Exception("پیکربندی پروتکل‌ها به درستی بارگذاری نشده است.");
        }

        _protocolConfigs = new Dictionary<string, IProtocolConfig>(StringComparer.OrdinalIgnoreCase)
{
    { "modbus", settings.Modbus },
    { "http", settings.Http },
    { "signalr", settings.SignalR } // ✅ اصلاح شد: "signalr" ➜ "signalr"
};

    }

    public IProtocolConfig GetConfig(string protocolType)
    {
        if (_protocolConfigs.TryGetValue(protocolType, out var config))
            return config;

        throw new ArgumentException($"تنظیمات مربوط به پروتکل '{protocolType}' یافت نشد.");
    }
}
