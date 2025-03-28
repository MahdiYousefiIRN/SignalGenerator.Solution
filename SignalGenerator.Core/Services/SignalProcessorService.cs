using SignalGenerator.Core.Interfaces;
using SignalGenerator.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalGenerator.Core.Services
{
    public class SignalProcessorService
    {
        // سازنده حذف می‌شود زیرا به جای آن پروتکل‌ها را به متدها ارسال می‌کنیم
        public SignalProcessorService() { }

        // متد برای شروع تولید سیگنال‌ها (ارسال و دریافت)
        public async Task StartSignalGeneration(SignalConfig config, IProtocolCommunication protocolCommunication)
        {
            // دریافت سیگنال‌ها از طریق پروتکل
            var signalData = await protocolCommunication.ReceiveSignalsAsync(config);

            // ارسال سیگنال‌ها به پروتکل
            await protocolCommunication.SendSignalsAsync(signalData);
        }

        // متد جدید برای دریافت سیگنال‌ها
        public async Task<List<SignalData>> GetSignalsAsync(SignalConfig config, IProtocolCommunication protocolCommunication)
        {
            // دریافت سیگنال‌ها از طریق پروتکل
            return await protocolCommunication.ReceiveSignalsAsync(config);
        }

        // متد برای ارسال سیگنال‌ها
        public async Task<bool> SendSignalsAsync(List<SignalData> signalData, IProtocolCommunication protocolCommunication)
        {
            // ارسال سیگنال‌ها به پروتکل
            return await protocolCommunication.SendSignalsAsync(signalData);
        }

        // متد برای نظارت بر وضعیت سیگنال‌ها
        public async Task<bool> MonitorSignalStatus(IProtocolCommunication protocolCommunication)
        {
            // نظارت بر وضعیت از طریق پروتکل
            return await protocolCommunication.MonitorStatusAsync();
        }
       
    }
}
