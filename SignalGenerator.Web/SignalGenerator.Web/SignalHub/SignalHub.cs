using Microsoft.AspNetCore.SignalR;
using SignalGenerator.Core.Models;

namespace SignalGenerator.Web.SignalHub
{
    public class SignalHub : Hub
    {
        // متد برای ارسال سیگنال به تمام کلاینت‌ها
        public async Task SendSignal(SignalData signal)
        {
            // ارسال سیگنال به تمامی کلاینت‌ها
            await Clients.All.SendAsync("ReceiveSignal", signal);
        }

        // متد برای ارسال سیگنال به یک کلاینت خاص
        public async Task SendSignalToClient(string connectionId, SignalData signal)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveSignal", signal);
        }

        // متد برای ارسال سیگنال به گروهی از کلاینت‌ها
        public async Task SendSignalToGroup(string groupName, SignalData signal)
        {
            await Clients.Group(groupName).SendAsync("ReceiveSignal", signal);
        }

        // متد برای اضافه کردن کلاینت‌ها به گروه
        public async Task AddToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // متد برای حذف کلاینت‌ها از گروه
        public async Task RemoveFromGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
