using Microsoft.AspNetCore.SignalR;
using SignalGenerator.Data.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using SignalGenerator.Core.Models;

namespace SignalGenerator.Web
{
    public class SignalHub : Hub
    {
        private readonly ILogger<SignalHub> _logger;
        private static readonly ConcurrentDictionary<string, string> _userGroups = new();
        private static readonly ConcurrentDictionary<string, DateTime> _lastSignalTimes = new();

        public SignalHub(ILogger<SignalHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for client {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
                _userGroups.TryRemove(Context.ConnectionId, out _);
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync for client {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

        public async Task SendSignal(SignalData signal)
        {
            try
            {
                if (signal == null)
                {
                    _logger.LogWarning("Attempted to send null signal from client {ConnectionId}", Context.ConnectionId);
                    return;
                }

                _lastSignalTimes.AddOrUpdate(Context.ConnectionId, DateTime.UtcNow, (_, __) => DateTime.UtcNow);

                await Clients.All.SendAsync("ReceiveSignal", signal);
                _logger.LogInformation("Signal sent to all clients from {ConnectionId}", Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signal from client {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

        public async Task SendSignalToClient(string connectionId, SignalData signal)
        {
            try
            {
                if (signal == null)
                {
                    _logger.LogWarning("Attempted to send null signal to client {TargetConnectionId}", connectionId);
                    return;
                }

                await Clients.Client(connectionId).SendAsync("ReceiveSignal", signal);
                _logger.LogInformation("Signal sent to client {TargetConnectionId} from {SourceConnectionId}", connectionId, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signal to client {TargetConnectionId} from {SourceConnectionId}", connectionId, Context.ConnectionId);
                throw;
            }
        }

        public async Task SendSignalToGroup(string groupName, SignalData signal)
        {
            try
            {
                if (signal == null)
                {
                    _logger.LogWarning("Attempted to send null signal to group {GroupName}", groupName);
                    return;
                }

                await Clients.Group(groupName).SendAsync("ReceiveSignal", signal);
                _logger.LogInformation("Signal sent to group {GroupName} from {ConnectionId}", groupName, Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending signal to group {GroupName} from {ConnectionId}", groupName, Context.ConnectionId);
                throw;
            }
        }

        public async Task AddToGroup(string groupName)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _userGroups.AddOrUpdate(Context.ConnectionId, groupName, (_, __) => groupName);
                _logger.LogInformation("Client {ConnectionId} added to group {GroupName}", Context.ConnectionId, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding client {ConnectionId} to group {GroupName}", Context.ConnectionId, groupName);
                throw;
            }
        }

        public async Task RemoveFromGroup(string groupName)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                _userGroups.TryRemove(Context.ConnectionId, out _);
                _logger.LogInformation("Client {ConnectionId} removed from group {GroupName}", Context.ConnectionId, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing client {ConnectionId} from group {GroupName}", Context.ConnectionId, groupName);
                throw;
            }
        }
    }
}
