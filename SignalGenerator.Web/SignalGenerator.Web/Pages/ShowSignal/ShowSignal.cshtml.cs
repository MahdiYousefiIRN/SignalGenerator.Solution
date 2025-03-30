using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR.Client;
using SignalGenerator.Data.Models;

public class ShowSignalModel : PageModel
{
    private readonly HubConnection _hubConnection;

    // لیست برای نگهداری داده‌های سیگنال‌ها
    public List<SignalData> Signals { get; set; } = new List<SignalData>();
    public string ErrorMessage { get; set; }

    public ShowSignalModel()
    {
        // ارتباط SignalR با URL مشخص شده
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/signalhub")
            .Build();
    }

    // متد برای دریافت سیگنال‌ها
    public async Task OnGetAsync()
    {
        await GetSignalsAsync();
    }

    // متد برای دریافت سیگنال‌ها از SignalR
    public async Task GetSignalsAsync()
    {
        try
        {
            await _hubConnection.StartAsync();

            // دریافت سیگنال‌ها از SignalR
            _hubConnection.On<List<SignalData>>("ReceiveSignals", (receivedSignals) =>
            {
                Signals = receivedSignals;
            });

            await _hubConnection.SendAsync("RequestSignals");  // درخواست سیگنال‌ها
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }

    // متد برای قطع اتصال از SignalR
    public async Task OnPostDisconnectAsync()
    {
        await _hubConnection.StopAsync();
    }
}
