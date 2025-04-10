using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.SignalR;
using SignalGenerator.Data.Models;

public class SignalHub : Hub
{
    private readonly ILogger<SignalHub> _logger;

    public SignalHub(ILogger<SignalHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    // ارسال سیگنال به تمامی کلاینت‌ها
    public async Task SendSignal(SignalData signal)
    {
        if (signal == null)
        {
            _logger.LogWarning($"Null signal received from {Context.ConnectionId}");
            await Clients.Caller.SendAsync("Error", "Received null signal.");
            return;
        }

        try
        {
            await Clients.All.SendAsync("ReceiveSignals", signal);
            _logger.LogInformation($"Signal sent to all clients from {Context.ConnectionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending signal to clients: {ex.Message}", ex);
        }
    }
}
