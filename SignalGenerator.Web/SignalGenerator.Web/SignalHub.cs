using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalGenerator.Data.Models;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;

namespace SignalGenerator.Web
{
    public class SignalHub : Hub
    {
        private readonly ILogger<SignalHub> _logger;

        // ذخیره گروه‌های کاربران به‌صورت ConcurrentDictionary
        private static readonly ConcurrentDictionary<string, string> _userGroups = new();

        // سازنده برای ثبت Logger
        public SignalHub(ILogger<SignalHub> logger)
        {
            _logger = logger;
        }

        // وقتی یک کلاینت به سرور متصل می‌شود
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        // وقتی یک کلاینت از سرور قطع می‌شود
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            _userGroups.TryRemove(Context.ConnectionId, out _); // حذف از گروه‌ها
            await base.OnDisconnectedAsync(exception);
        }

        // ارسال سیگنال به تمامی کلاینت‌ها
        public async Task SendSignal(SignalData signal)
        {
            if (signal == null)
            {
                _logger.LogWarning($"Null signal received from {Context.ConnectionId}");
                return;
            }

            try
            {
                // ارسال سیگنال به تمامی کلاینت‌ها
                await Clients.All.SendAsync("ReceiveSignals", signal);
                _logger.LogInformation($"Signal sent to all clients from {Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending signal to clients: {ex.Message}", ex);
            }
        }

        // ارسال سیگنال به گروه خاصی از کاربران
        public async Task SendSignalToGroup(string groupName, SignalData signal)
        {
            if (signal == null)
            {
                _logger.LogWarning($"Null signal received for group {groupName} from {Context.ConnectionId}");
                return;
            }

            try
            {
                // ارسال سیگنال به گروه خاص
                await Clients.Group(groupName).SendAsync("ReceiveSignals", signal);
                _logger.LogInformation($"Signal sent to group {groupName} from {Context.ConnectionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending signal to group {groupName}: {ex.Message}", ex);
            }
        }

        // اضافه کردن کلاینت به گروه خاص
        public async Task AddToGroup(string groupName)
        {
            try
            {
                // اضافه کردن به گروه
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _userGroups[Context.ConnectionId] = groupName;  // ذخیره گروه در دیکشنری
                _logger.LogInformation($"Client {Context.ConnectionId} added to group {groupName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding client {Context.ConnectionId} to group {groupName}: {ex.Message}", ex);
            }
        }

        // حذف کلاینت از گروه
        public async Task RemoveFromGroup(string groupName)
        {
            try
            {
                // حذف از گروه
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                _userGroups.TryRemove(Context.ConnectionId, out _); // حذف از دیکشنری
                _logger.LogInformation($"Client {Context.ConnectionId} removed from group {groupName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing client {Context.ConnectionId} from group {groupName}: {ex.Message}", ex);
            }
        }

        // متد برای دریافت سیگنال‌ها از سرور و ارسال به کلاینت‌ها
        public async Task ReceiveSignalFromServer(SignalData signal)
        {
            if (signal == null)
            {
                _logger.LogWarning($"Null signal received from server.");
                return;
            }

            try
            {
                // ارسال سیگنال دریافتی به تمامی کلاینت‌ها
                await Clients.All.SendAsync("ReceiveSignals", signal);
                _logger.LogInformation($"Signal received from server and sent to all clients.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing signal from server: {ex.Message}", ex);
            }
        }
    }
}
