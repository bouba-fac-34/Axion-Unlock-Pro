using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.Services
{
    /// <n>
    /// Bridges to bkerler/edl (Python) or falls back to structured log when tool missing.
    /// Real Firehose work requires: device in 9008 + matching programmer .elf/.mbn.
    /// </summary>
    public class EdlService
    {
        private readonly Action<string> _log;
        private readonly string _edlExe;

        public EdlService(Action<string> log, string? edlPath = null)
        {
            _log = log;
            _edlExe = ResolveEdl(edlPath);
        }

        public async Task<OperationResult> WipeFrpAsync(DeviceInfo device, string? programmerPath = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            try
            {
                if (device.Mode != ConnectionMode.EDL)
                    return Fail("Device must be in EDL (9008)", log.ToString(), sw.Elapsed);

                programmerPath ??= ProgrammerResolver.ResolveSamsungFirehose(device.Model, device.Chipset)
                    ?? ProgrammerResolver.ResolveSamsungFirehose(device.Product, device.Chipset);

                if (string.IsNullOrEmpty(programmerPath) || !File.Exists(programmerPath))
                {
                    L("No Firehose programmer found under Resources/programmers/samsung/");
                    L("Clone Alephgsm/SAMSUNG-EDL-Loaders and copy .elf/.mbn into Resources/programmers/samsung/{MODEL}/");
                    return Fail("Firehose programmer missing", log.ToString(), sw.Elapsed);
                }

                L($"Programmer: {programmerPath}");

                if (string.IsNullOrEmpty(_edlExe))
                {
                    L("edl CLI not found on PATH or bin/edl");
                    L("Install: pip install edl   OR place edl binary under bin/");
                    L("Manual command once installed:");
                    L($"  edl --loader \"{programmerPath}\" w frp");
                    L($"  edl --loader \"{programmerPath}\" e frp");
                    return Fail("edl tool not installed", log.ToString(), sw.Elapsed);
                }

                L("Sahara + Firehose via edl…");
                // try common FRP partition names
                var partitions = new[] { "frp", "persistent", "config" };
                foreach (var p in partitions)
                {
                    L($"Erasing partition: {p}");
                    var r = await ProcessRunner.RunAsync(_edlExe,
                        $"--loader \"{programmerPath}\" e {p}", 120000);
                    L(r);
                    if (!r.Contains("ERROR", StringComparison.OrdinalIgnoreCase) &&
                        !r.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase))
                    {
                        sw.Stop();
                        return Ok($"EDL wipe ok ({p})", log.ToString(), sw.Elapsed);
                    }
                }

                sw.Stop();
                return Fail("EDL wipe failed for known FRP partitions", log.ToString(), sw.Elapsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Fail(ex.Message, log.ToString(), sw.Elapsed);
            }
        }

        public async Task<OperationResult> IdentifyAsync(DeviceInfo device, string? programmerPath = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var log = new System.Text.StringBuilder();
            void L(string m) { log.AppendLine(m); _log(m); }

            if (device.Mode != ConnectionMode.EDL)
                return Fail("EDL required", log.ToString(), sw.Elapsed);

            programmerPath ??= ProgrammerResolver.ResolveSamsungFirehose(device.Model, device.Chipset);
            if (string.IsNullOrEmpty(programmerPath))
                return Fail("No programmer", log.ToString(), sw.Elapsed);

            if (string.IsNullOrEmpty(_edlExe))
            {
                L($"Would run: edl --loader \"{programmerPath}\" printgpt");
                return Fail("edl not installed", log.ToString(), sw.Elapsed);
            }

            var r = await ProcessRunner.RunAsync(_edlExe,
                $"--loader \"{programmerPath}\" printgpt", 90000);
            L(r);
            sw.Stop();
            return Ok("Identify done", log.ToString(), sw.Elapsed);
        }

        private static string ResolveEdl(string? preferred)
        {
            if (!string.IsNullOrEmpty(preferred) && File.Exists(preferred)) return preferred;
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "edl.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "edl"),
                "edl",
                "edl.exe"
            };
            foreach (var c in candidates)
            {
                try
                {
                    if (c.Contains(Path.DirectorySeparatorChar) || c.Contains('/'))
                    {
                        if (File.Exists(c)) return c;
                    }
                    else
                    {
                        // PATH lookup via where/which is environment-dependent; return name and let Process start
                        return c;
                    }
                }
                catch { }
            }
            return "";
        }

        private static OperationResult Ok(string m, string l, TimeSpan d) =>
            new() { Success = true, Message = m, Log = l, Duration = d };
        private static OperationResult Fail(string m, string l, TimeSpan d) =>
            new() { Success = false, Message = m, Log = l, Duration = d };
    }
}
