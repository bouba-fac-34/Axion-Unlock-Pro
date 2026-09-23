using System.Diagnostics;
using System.Text;

namespace Axion.Core.Utils
{
    public static class ProcessRunner
    {
        public static async Task<string> RunAsync(string fileName, string arguments, int timeoutMs = 60000)
        {
            if (!File.Exists(fileName) && !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // try without path
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                using var process = new Process { StartInfo = psi };
                var output = new StringBuilder();
                var error = new StringBuilder();

                process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var exited = await Task.Run(() => process.WaitForExit(timeoutMs));
                if (!exited)
                {
                    try { process.Kill(true); } catch { }
                    return $"TIMEOUT after {timeoutMs}ms";
                }

                var result = output.ToString();
                if (error.Length > 0)
                    result += "\n" + error;
                return result.Trim();
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }
    }
}
