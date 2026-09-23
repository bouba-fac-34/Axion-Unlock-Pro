using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Protocols.Oppo
{
    public class OppoProtocol
    {
        private readonly string _adb;
        private readonly Action<string> _log;
        public OppoProtocol(string adb, Action<string> log) { _adb = adb; _log = log; }

        public async Task<OperationResult> RemoveFrpAsync(DeviceInfo d)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }
            L("Oppo/Realme FRP + Demo");
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized)
                return Fail("ADB required", log.ToString(), sw.Elapsed);
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.google.android.gsf");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.google.android.gsf.login");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.google.android.gms");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.coloros.phonemanager");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell settings put secure user_setup_complete 1");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell settings put global device_provisioned 1");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm disable-user --user 0 com.oppo.daydreamvideo");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.oppo.daydreamvideo");
            L("FRP + Demo cleared");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} reboot");
            sw.Stop();
            return Ok("Oppo/Realme FRP + Demo removed", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> RemoveMdmAsync(DeviceInfo d)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }
            L("Oppo MDM / PayJoy style");
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized)
                return Fail("ADB required", log.ToString(), sw.Elapsed);
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm uninstall -k --user 0 com.oppo.mdm");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.heytap.market");
            L("MDM packages cleared");
            sw.Stop();
            return Ok("Oppo MDM removed", log.ToString(), sw.Elapsed);
        }

        private static OperationResult Ok(string m, string l, TimeSpan d) => new() { Success = true, Message = m, Log = l, Duration = d };
        private static OperationResult Fail(string m, string l, TimeSpan d) => new() { Success = false, Message = m, Log = l, Duration = d };
    }
}
