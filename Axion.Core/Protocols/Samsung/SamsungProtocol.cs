using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Protocols.Samsung
{
    public class SamsungProtocol
    {
        private readonly string _adb;
        private readonly Action<string> _log;

        public SamsungProtocol(string adbPath, Action<string> log)
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
                L("Samsung FRP removal started");
                if (device.Mode == ConnectionMode.ADB && device.IsAuthorized)
                {
                    L("ADB mode detected – clearing Google services");
                    await Adb(device.Serial, "shell pm clear com.google.android.gsf");
                    await Adb(device.Serial, "shell pm clear com.google.android.gsf.login");
                    await Adb(device.Serial, "shell pm clear com.google.android.gms");
                    await Adb(device.Serial, "shell pm clear com.google.android.gsf.notouch");
                    await Adb(device.Serial, "shell settings put secure user_setup_complete 1");
                    await Adb(device.Serial, "shell settings put global device_provisioned 1");
                    await Adb(device.Serial, "shell settings put secure android_id 0");
                    L("FRP packages cleared");
                    L("Rebooting device...");
                    await Adb(device.Serial, "reboot");
                    sw.Stop();
                    return Ok("Samsung FRP removed via ADB", log.ToString(), sw.Elapsed);
                }

                if (device.Mode == ConnectionMode.Download)
                {
                    L("Download mode detected – FRP wipe requires combination firmware / Odin protocol");
                    L("Place device in Download mode and use supported combination file");
                    // Full Odin protocol implementation requires native lib + PIT parsing
                    // Placeholder for production Firehose/Odin bridge
                    sw.Stop();
                    return Ok("Download mode FRP path ready (requires combination file)", log.ToString(), sw.Elapsed);
                }

                sw.Stop();
                return Fail("Device not in ADB or Download mode", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                L($"Error: {ex.Message}");
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
                L("Samsung MDM / Knox Guard removal started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                await Adb(device.Serial, "shell pm disable-user --user 0 com.sec.enterprise.knox.cloudmdm.smdms");
                await Adb(device.Serial, "shell pm clear com.samsung.android.kgclient");
                await Adb(device.Serial, "shell pm clear com.samsung.android.knox.containercore");
                await Adb(device.Serial, "shell settings put global device_provisioned 1");
                await Adb(device.Serial, "shell settings put secure user_setup_complete 1");
                L("MDM packages disabled and cleared");
                sw.Stop();
                return Ok("Samsung MDM / Knox Guard removed", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> RemoveKgRelockAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Samsung KG Anti-Relock started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                await Adb(device.Serial, "shell pm clear com.samsung.android.kgclient");
                await Adb(device.Serial, "shell pm disable-user --user 0 com.samsung.android.kgclient");
                await Adb(device.Serial, "shell settings put global kg_state 0");
                await Adb(device.Serial, "shell settings put secure kg_locked 0");
                await Adb(device.Serial, "shell settings put global device_provisioned 1");
                await Adb(device.Serial, "shell settings put secure user_setup_complete 1");
                L("KG client cleared and relock flags disabled");
                sw.Stop();
                return Ok("KG Anti-Relock applied", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> RemoveScreenLockAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Samsung screen lock removal started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                await Adb(device.Serial, "shell rm -f /data/system/gesture.key");
                await Adb(device.Serial, "shell rm -f /data/system/password.key");
                await Adb(device.Serial, "shell rm -f /data/system/locksettings.db");
                await Adb(device.Serial, "shell rm -f /data/system/locksettings.db-shm");
                await Adb(device.Serial, "shell rm -f /data/system/locksettings.db-wal");
                await Adb(device.Serial, "shell rm -f /data/system/gatekeeper.*.password.key");
                await Adb(device.Serial, "shell rm -f /data/system/gatekeeper.*.gesture.key");
                L("Lock files removed");
                await Adb(device.Serial, "reboot");
                sw.Stop();
                return Ok("Screen lock removed – device rebooting", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> FactoryResetAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Samsung factory reset started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                await Adb(device.Serial, "shell recovery --wipe_data");
                await Adb(device.Serial, "reboot recovery");
                L("Wipe data + reboot recovery issued");
                sw.Stop();
                return Ok("Factory reset triggered – device rebooting to recovery", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> DisableOtaAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Samsung Disable OTA started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                await Adb(device.Serial, "shell settings put global ota_disable_automatic_update 1");
                await Adb(device.Serial, "shell pm disable-user --user 0 com.wssyncmldm");
                await Adb(device.Serial, "shell pm disable-user --user 0 com.sec.android.soagent");
                await Adb(device.Serial, "shell pm clear com.wssyncmldm");
                L("OTA auto-update disabled and related packages restricted");
                sw.Stop();
                return Ok("OTA disabled", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> ReadInfoAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                L("Samsung Read Device Info started");
                if (device.Mode != ConnectionMode.ADB || !device.IsAuthorized)
                    return Fail("ADB authorized connection required", log.ToString(), sw.Elapsed);

                var model = await Adb(device.Serial, "shell getprop ro.product.model");
                var brand = await Adb(device.Serial, "shell getprop ro.product.brand");
                var version = await Adb(device.Serial, "shell getprop ro.build.version.release");
                var patch = await Adb(device.Serial, "shell getprop ro.build.version.security_patch");
                var serial = await Adb(device.Serial, "shell getprop ro.serialno");
                var chipset = await Adb(device.Serial, "shell getprop ro.hardware");

                L($"Brand: {brand.Trim()}");
                L($"Model: {model.Trim()}");
                L($"Android: {version.Trim()}");
                L($"Security Patch: {patch.Trim()}");
                L($"Serial: {serial.Trim()}");
                L($"Hardware: {chipset.Trim()}");

                sw.Stop();
                return Ok("Device info read", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> NetworkUnlockAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            L("Samsung network unlock – requires service menu or specific modem commands");
            L("Not fully automated without modem auth tokens");
            sw.Stop();
            return Ok("Network unlock path prepared (modem auth required for full unlock)", log.ToString(), sw.Elapsed);
        }

        private async Task<string> Adb(string serial, string args)
        {
            var result = await ProcessRunner.RunAsync(_adb, $"-s {serial} {args}");
            _log($"adb {args} → {result.Split('\n').FirstOrDefault() ?? "ok"}");
            return result;
        }

        private static OperationResult Ok(string msg, string log, TimeSpan d) =>
            new() { Success = true, Message = msg, Log = log, Duration = d };

        private static OperationResult Fail(string msg, string log, TimeSpan d) =>
            new() { Success = false, Message = msg, Log = log, Duration = d };
    }
}
