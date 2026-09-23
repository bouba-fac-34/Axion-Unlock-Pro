using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Services
{
    /// <summary>
    /// Bridges to mtkclient / SP Flash Tool style DA workflow.
    /// Real Preloader work needs DA binary + USB VCOM.
    /// </summary>
    public class MtkService
    {
        private readonly Action<string> _log;
        private readonly string _mtkExe;

        public MtkService(Action<string> log, string? mtkPath = null)
        {
            _log = log;
            _mtkExe = ResolveMtk(mtkPath);
        }

        public async Task<OperationResult> WipeFrpAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                if (device.Mode != ConnectionMode.Preloader && device.Mode != ConnectionMode.EDL)
                    return Fail("Connect in Preloader / BROM", log.ToString(), sw.Elapsed);

                var da = ProgrammerResolver.ResolveMtkDa(device.Chipset);
                if (!string.IsNullOrEmpty(da))
                    L($"DA: {da}");
                else
                    L("No local DA under Resources/programmers/mtk — mtkclient will use built-in if available");

                if (string.IsNullOrEmpty(_mtkExe))
                {
                    L("mtkclient not found. Install: pip install mtkclient");
                    L("Or place mtk under bin/");
                    L("Manual: mtk e frp");
                    return Fail("mtkclient not installed", log.ToString(), sw.Elapsed);
                }

                L("BROM / Preloader handshake via mtkclient…");
                var r = await ProcessRunner.RunAsync(_mtkExe, "e frp", 180000);
                L(r);
                if (r.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
                    return Fail("mtk e frp failed", log.ToString(), sw.Elapsed);

                sw.Stop();
                return Ok("MTK FRP erased", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> FormatFrpAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            if (string.IsNullOrEmpty(_mtkExe))
            {
                L("mtkclient missing — cannot format");
                return Fail("mtkclient not installed", log.ToString(), sw.Elapsed);
            }

            L("Erasing userdata + frp…");
            var r1 = await ProcessRunner.RunAsync(_mtkExe, "e userdata", 180000);
            L(r1);
            var r2 = await ProcessRunner.RunAsync(_mtkExe, "e frp", 120000);
            L(r2);
            sw.Stop();
            return Ok("Format + FRP done", log.ToString(), sw.Elapsed);
        }

        public async Task<OperationResult> ReadGptAsync(DeviceInfo device)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            if (string.IsNullOrEmpty(_mtkExe))
                return Fail("mtkclient not installed", log.ToString(), sw.Elapsed);

            var r = await ProcessRunner.RunAsync(_mtkExe, "printgpt", 90000);
            L(r);
            sw.Stop();
            return Ok("GPT printed", log.ToString(), sw.Elapsed);
        }

        private static string ResolveMtk(string? preferred)
        {
            if (!string.IsNullOrEmpty(preferred) && File.Exists(preferred)) return preferred;
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mtk.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "mtk"),
                "mtk",
                "mtk.exe"
            };
            foreach (var c in candidates)
            {
                if (c.Contains(Path.DirectorySeparatorChar) && File.Exists(c)) return c;
                if (!c.Contains(Path.DirectorySeparatorChar)) return c;
            }
            return "";
        }

        private static OperationResult Ok(string m, string l, TimeSpan d) =>
            new() { Success = true, Message = m, Log = l, Duration = d };
        private static OperationResult Fail(string m, string l, TimeSpan d) =>
            new() { Success = false, Message = m, Log = l, Duration = d };
    }
}
