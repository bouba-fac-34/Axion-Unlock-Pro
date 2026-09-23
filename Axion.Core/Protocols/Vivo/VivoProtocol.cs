using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Protocols.Vivo
{
    public class VivoProtocol
    {
        private readonly string _adb;
        private readonly Action<string> _log;

        public VivoProtocol(string adb, Action<string> log) { _adb = adb; _log = log; }

        public async Task<OperationResult> RemoveFrpAsync(DeviceInfo d)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }
            L("Vivo FRP");
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized)
                return Fail("ADB required", log.ToString(), sw.Elapsed);
            await Adb(d.Serial, "shell pm clear com.google.android.gsf");
            await Adb(d.Serial, "shell pm clear com.google.android.gsf.login");
            await Adb(d.Serial, "shell pm clear com.google.android.gms");
            await Adb(d.Serial, "shell pm clear com.bbk.account");
            await Adb(d.Serial, "shell settings put secure user_setup_complete 1");
            await Adb(d.Serial, "shell settings put global device_provisioned 1");
            await Adb(d.Serial, "reboot");
            sw.Stop();
            return Ok("Vivo FRP cleared", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> DisableOtaAsync(DeviceInfo d)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized)
                return Fail("ADB required", log.ToString(), sw.Elapsed);
            await Adb(d.Serial, "shell settings put global ota_disable_automatic_update 1");
            L("OTA disabled");
            sw.Stop();
            return Ok("Vivo OTA disabled", log.ToString(), sw.Elapsed);
        }

        private async Task<string> Adb(string serial, string args) =>
            await ProcessRunner.RunAsync(_adb, $"-s {serial} {args}");

        private static OperationResult Ok(string m, string l, TimeSpan d) => new() { Success = true, Message = m, Log = l, Duration = d };
        private static OperationResult Fail(string m, string l, TimeSpan d) => new() { Success = false, Message = m, Log = l, Duration = d };
    }
}
