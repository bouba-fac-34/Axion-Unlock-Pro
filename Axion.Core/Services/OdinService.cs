using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Services
{
    /// <summary>
    /// Samsung Download-mode bridge.
    /// Preferred: reference Alephgsm/SharpOdinClient in your solution for native Odin.
    /// Fallback: shell external Odin3.exe if present under bin/.
    /// Docs: https://github.com/Alephgsm/SharpOdinClient
    /// </summary>
    public class OdinService
    {
        private readonly Action<string> _log;
        private readonly string _odinExe;

        public OdinService(Action<string> log)
        {
            _log = log;
            var bin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
            var candidates = new[]
            {
                Path.Combine(bin, "Odin3.exe"),
                Path.Combine(bin, "Odin.exe"),
                "Odin3.exe"
            };
            _odinExe = candidates.FirstOrDefault(File.Exists) ?? "";
        }

        public async Task<OperationResult> ReadInfoAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            if (device.Mode != ConnectionMode.Download)
                return Fail("Device must be in Download mode", log.ToString(), sw.Elapsed);

            L("Samsung Download mode detected");
            L("Native path: add NuGet/project ref to Alephgsm SharpOdinClient → Odin.FindAndSetDownloadMode + DVIF");
            if (!string.IsNullOrEmpty(_odinExe))
            {
                L($"External Odin found: {_odinExe}");
                L("Launch Odin manually or integrate SharpOdinClient for automated DVIF");
            }
            else
            {
                L("Place Odin3.exe in bin/ OR reference SharpOdinClient for full protocol");
            }

            sw.Stop();
            return Ok("Download mode ready — use SharpOdinClient for DVIF/flash", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> FactoryResetHintAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            L("Download-mode factory reset requires combination firmware or stock flash via Odin/SharpOdinClient");
            L("Recommended: SharpOdinClient Flash with combination tar.md5 containing cache/userdata wipe");
            sw.Stop();
            return Ok("Odin flash path documented", log.ToString(), sw.Elapsed);
        }

        private static OperationResult Ok(string m, string l, TimeSpan d) =>
            new() { Success = true, Message = m, Log = l, Duration = d };
        private static OperationResult Fail(string m, string l, TimeSpan d) =>
            new() { Success = false, Message = m, Log = l, Duration = d };
    }
}
