using SignalGenerator.Data.Interfaces;
using SignalGenerator.Data.Models;
using SignalGenerator.Protocols.SignalR; // اضافه کردن استفاده از SignalR
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalGenerator.Data.Services
{
    public class SignalProcessorService
    {
        private readonly SignalRProtocol _signalRProtocol;

        public SignalProcessorService(SignalRProtocol signalRProtocol)
        {
            _signalRProtocol = signalRProtocol;
        }

        // متد برای شروع تولید سیگنال‌ها (ارسال و دریافت)
        public async Task StartSignalGeneration(SignalConfig config, IProtocolCommunication protocolCommunication)
        {
            // دریافت سیگنال‌ها از پروتکل
            var signalData = await protocolCommunication.ReceiveSignalsAsync(config);

            // ارسال سیگنال‌ها به پروتکل
            await protocolCommunication.SendSignalsAsync(signalData);

            // ارسال سیگنال‌ها به SignalR
            await _signalRProtocol.SendSignalsAsync(signalData);
        }

        // متد جدید برای دریافت سیگنال‌ها
        public async Task<List<SignalData>> GetSignalsAsync(SignalConfig config, IProtocolCommunication protocolCommunication)
        {
            // دریافت سیگنال‌ها از پروتکل
            var signals = await protocolCommunication.ReceiveSignalsAsync(config);

            // ارسال سیگنال‌ها به SignalR
            await _signalRProtocol.SendSignalsAsync(signals);

            return signals;
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
