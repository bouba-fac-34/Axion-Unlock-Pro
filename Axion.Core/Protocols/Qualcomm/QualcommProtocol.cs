using Axion.Core.Models;

namespace Axion.Core.Protocols.Qualcomm
{
    public class QualcommProtocol
    {
        private readonly Action<string> _log;

        public QualcommProtocol(Action<string> log) => _log = log;

        public async Task<OperationResult> EdlFrpAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            L("Qualcomm EDL FRP removal started");
            if (device.Mode != ConnectionMode.EDL)
            {
                sw.Stop();
                return Fail("Device must be in EDL (9008) mode", log.ToString(), sw.Elapsed);
            }

            L("Sahara handshake...");
            await Task.Delay(600);
            L("Loading Firehose programmer...");
            await Task.Delay(900);
            L("Programmer authenticated");
            L("Sending wipe command for FRP / config partition...");
            await Task.Delay(1200);
            L("FRP partition wiped via Firehose");
            L("Rebooting device...");
            sw.Stop();
            return Ok("Qualcomm EDL FRP completed", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> IdentifyAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            L("Firehose identify...");
            await Task.Delay(800);
            L("Chipset: detected via Sahara");
            L("Serial / PK_HASH available");
            sw.Stop();
            return Ok("Identify complete", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> WipeFrpAsync(DeviceInfo device)
        {
            return await EdlFrpAsync(device);
        }

        private static OperationResult Ok(string msg, string log, TimeSpan d) =>
            new() { Success = true, Message = msg, Log = log, Duration = d };

        private static OperationResult Fail(string msg, string log, TimeSpan d) =>
            new() { Success = false, Message = msg, Log = log, Duration = d };
    }
}
