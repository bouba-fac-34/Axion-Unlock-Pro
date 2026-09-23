using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Protocols.Xiaomi
{
    public class XiaomiProtocol
    {
        private readonly string _adb;
        private readonly string _fb;
        private readonly Action<string> _log;

        public XiaomiProtocol(string adb, string fastboot, Action<string> log)
        {
            _adb = adb; _fb = fastboot; _log = log;
        }

        public async Task<OperationResult> RemoveFrpAsync(DeviceInfo d)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }
            L("Xiaomi FRP removal");
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized)
                return Fail("ADB authorized required", log.ToString(), sw.Elapsed);
            await Adb(d.Serial, "shell pm clear com.google.android.gsf");
            await Adb(d.Serial, "shell pm clear com.google.android.gsf.login");
            await Adb(d.Serial, "shell pm clear com.google.android.gms");
            await Adb(d.Serial, "shell pm clear com.xiaomi.finddevice");
            await Adb(d.Serial, "shell pm clear com.miui.cloudservice");
            await Adb(d.Serial, "shell settings put secure user_setup_complete 1");
            await Adb(d.Serial, "shell settings put global device_provisioned 1");
            await Adb(d.Serial, "reboot");
            sw.Stop();
            return Ok("Xiaomi FRP + Mi Account packages cleared", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> RemoveMiAccountAsync(DeviceInfo d)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }
            L("Mi Account / Find Device removal");
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized)
                return Fail("ADB required", log.ToString(), sw.Elapsed);
            await Adb(d.Serial, "shell pm clear com.xiaomi.finddevice");
            await Adb(d.Serial, "shell pm clear com.miui.cloudservice");
            await Adb(d.Serial, "shell pm clear com.xiaomi.account");
            await Adb(d.Serial, "shell pm disable-user --user 0 com.xiaomi.finddevice");
            L("Mi Account services cleared");
            sw.Stop();
            return Ok("Mi Account removed", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> FastbootToEdlAsync(DeviceInfo d)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }
            L("Fastboot to EDL");
            if (d.Mode != ConnectionMode.Fastboot)
                return Fail("Device must be in Fastboot", log.ToString(), sw.Elapsed);
            await ProcessRunner.RunAsync(_fb, $"-s {d.Serial} oem edl");
            await ProcessRunner.RunAsync(_fb, $"-s {d.Serial} reboot-edl");
            L("EDL command sent");
            sw.Stop();
            return Ok("Reboot to EDL issued", log.ToString(), sw.Elapsed);
        }

        private async Task<string> Adb(string serial, string args) =>
            await ProcessRunner.RunAsync(_adb, $"-s {serial} {args}");

        private static OperationResult Ok(string msg, string log, TimeSpan d) =>
            new() { Success = true, Message = msg, Log = log, Duration = d };
        private static OperationResult Fail(string msg, string log, TimeSpan d) =>
            new() { Success = false, Message = msg, Log = log, Duration = d };
    }
}
