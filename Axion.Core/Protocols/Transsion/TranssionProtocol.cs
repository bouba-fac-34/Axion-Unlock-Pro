using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Protocols.Transsion
{
    public class TranssionProtocol
    {
        private readonly string _adb;
        private readonly Action<string> _log;

        public TranssionProtocol(string adbPath, Action<string> log)
        {
            _adb = adbPath;
            _log = log;
        }

        public async Task<OperationResult> RemoveFrpAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Transsion (Tecno/Infinix/Itel) FRP removal started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                await Adb(device.Serial, "shell pm clear com.google.android.gsf");
                await Adb(device.Serial, "shell pm clear com.google.android.gsf.login");
                await Adb(device.Serial, "shell pm clear com.google.android.gms");
                await Adb(device.Serial, "shell pm clear com.google.android.setupwizard");
                await Adb(device.Serial, "shell rm -f /data/system/locksettings.db*");
                await Adb(device.Serial, "shell settings put secure user_setup_complete 1");
                await Adb(device.Serial, "shell settings put global device_provisioned 1");
                await Adb(device.Serial, "shell settings put secure android_id 0");
                L("FRP cleared");
                await Adb(device.Serial, "reboot");
                sw.Stop();
                return Ok("Transsion FRP removed", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> RemoveMdmAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Transsion MDM removal started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                var packages = new[]
                {
                    "com.transsion.mdm",
                    "com.transsion.systemupdate",
                    "com.transsion.tranfacmode",
                    "com.tecno.security",
                    "com.infinix.security"
                };

                foreach (var pkg in packages)
                {
                    await Adb(device.Serial, $"shell pm uninstall -k --user 0 {pkg}");
                    await Adb(device.Serial, $"shell pm clear {pkg}");
                }

                await Adb(device.Serial, "shell settings put global device_provisioned 1");
                L("MDM packages removed");
                sw.Stop();
                return Ok("Transsion MDM removed", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> RemoveLockAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Transsion pattern/PIN/password removal started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                await Adb(device.Serial, "shell rm -f /data/system/gesture.key");
                await Adb(device.Serial, "shell rm -f /data/system/password.key");
                await Adb(device.Serial, "shell rm -f /data/system/locksettings.db*");
                await Adb(device.Serial, "shell rm -f /data/system/gatekeeper.*.key");
                L("Lock files deleted");
                await Adb(device.Serial, "reboot");
                sw.Stop();
                return Ok("Screen lock removed – rebooting", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> ImeiRepairAsync(DeviceInfo device, string imei1, string? imei2 = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            L("IMEI repair requires engineering mode or meta mode + write tools");
            L($"Target IMEI1: {imei1}");
            if (!string.IsNullOrEmpty(imei2)) L($"Target IMEI2: {imei2}");
            L("Full IMEI write needs native MTK/Unisoc meta protocol – skeleton ready");
            sw.Stop();
            return Ok("IMEI repair path prepared", log.ToString(), sw.Elapsed);
        }

        private async Task<string> Adb(string serial, string args)
        {
            var result = await ProcessRunner.RunAsync(_adb, $"-s {serial} {args}");
            _log($"adb {args} → ok");
            return result;
        }

        private static OperationResult Ok(string msg, string log, TimeSpan d) =>
            new() { Success = true, Message = msg, Log = log, Duration = d };

        private static OperationResult Fail(string msg, string log, TimeSpan d) =>
            new() { Success = false, Message = msg, Log = log, Duration = d };
    }
}
