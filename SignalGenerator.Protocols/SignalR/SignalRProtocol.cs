using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SignalGenerator.Core.Models;
using SignalGenerator.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;


namespace SignalGenerator.Protocols.SignalR
{
    public class SignalRProtocol : IProtocolCommunication
    {
        private readonly HubConnection _connection;

        // سازنده برای ایجاد ارتباط SignalR
        public SignalRProtocol(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();
        }

        // متد برای دریافت سیگنال‌ها از SignalR
        public async Task<List<SignalData>> ReceiveSignalsAsync(SignalConfig config)
        {
            try
            {
                // اتصال به SignalR Hub
                await _connection.StartAsync();

                // ارسال درخواست دریافت سیگنال‌ها
                await _connection.SendAsync("RequestSignals", config.SignalCount);

                // دریافت سیگنال‌ها از SignalR
                var signals = new List<SignalData>();
                _connection.On<List<SignalData>>("ReceiveSignals", (receivedSignals) =>
                {
                    signals = receivedSignals;
                });

                // منتظر ماندن برای دریافت سیگنال‌ها
                await Task.Delay(5000); // به عنوان مثال منتظر 5 ثانیه

                return signals;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReceiveSignalsAsync: {ex.Message}");
                return new List<SignalData>();
            }
            finally
            {
                // قطع ارتباط بعد از اتمام
                await _connection.StopAsync();
            }
        }

        // متد برای ارسال سیگنال‌ها به SignalR
        public async Task<bool> SendSignalsAsync(List<SignalData> signalData)
        {
            try
            {
                // اتصال به SignalR Hub
                await _connection.StartAsync();

                // ارسال سیگنال‌ها به SignalR
                await _connection.SendAsync("SendSignals", signalData);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendSignalsAsync: {ex.Message}");
                return false;
            }
            finally
            {
                // قطع ارتباط بعد از اتمام
                await _connection.StopAsync();
            }
        }

        // متد برای نظارت بر وضعیت سیگنال‌ها (اگر مورد نیاز است)
        public async Task<bool> MonitorStatusAsync()
        {
            try
            {
                // اتصال به SignalR Hub
                await _connection.StartAsync();

                // ارسال درخواست وضعیت سیگنال‌ها
                var status = await _connection.InvokeAsync<bool>("MonitorStatus");

                return status;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MonitorStatusAsync: {ex.Message}");
                return false;
            }
            finally
            {
                // قطع ارتباط بعد از اتمام
                await _connection.StopAsync();
            }
        }
    }
}


