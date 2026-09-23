using System.Text.RegularExpressions;

namespace Axion.Core.Services
{
    /// <summary>
    /// Resolves Firehose / DA programmer paths from Resources/programmers.
    /// </summary>
    public static class ProgrammerResolver
    {
        private static string Root =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "programmers");

        public static string? ResolveSamsungFirehose(string model, string? chipsetHint = null)
        {
            model = NormalizeModel(model);
            if (string.IsNullOrEmpty(model)) return null;

            // 1. exact model folder
            var modelDir = Path.Combine(Root, "samsung", model);
            if (Directory.Exists(modelDir))
            {
                var hit = Directory.GetFiles(modelDir, "*.elf")
                    .Concat(Directory.GetFiles(modelDir, "*.mbn"))
                    .OrderByDescending(f => new FileInfo(f).Length)
                    .FirstOrDefault();
                if (hit != null) return hit;
            }

            // 2. generic by chipset hint
            var genericDir = Path.Combine(Root, "samsung", "generic");
            if (Directory.Exists(genericDir) && !string.IsNullOrEmpty(chipsetHint))
            {
                var key = MapChipsetToGeneric(chipsetHint);
                if (key != null)
                {
                    var g = Directory.GetFiles(genericDir, $"*{key}*")
                        .FirstOrDefault();
                    if (g != null) return g;
                }
            }

            // 3. any generic
            if (Directory.Exists(genericDir))
            {
                return Directory.GetFiles(genericDir, "*.elf").FirstOrDefault()
                    ?? Directory.GetFiles(genericDir, "*.mbn").FirstOrDefault();
            }

            return null;
        }

        public static string? ResolveMtkDa(string chipset)
        {
            var dir = Path.Combine(Root, "mtk");
            if (!Directory.Exists(dir)) return null;

            chipset = chipset.ToUpperInvariant();
            var hit = Directory.GetFiles(dir, "*.bin")
                .Concat(Directory.GetFiles(dir, "*.da"))
                .FirstOrDefault(f => Path.GetFileName(f).Contains(chipset, StringComparison.OrdinalIgnoreCase));
            return hit ?? Directory.GetFiles(dir).FirstOrDefault();
        }

        public static IEnumerable<string> ListSamsungModels()
        {
            var dir = Path.Combine(Root, "samsung");
            if (!Directory.Exists(dir)) yield break;
            foreach (var d in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(d);
                if (!name.Equals("generic", StringComparison.OrdinalIgnoreCase))
                    yield return name;
            }
        }

        private static string NormalizeModel(string model)
        {
            model = model.Trim().ToUpperInvariant();
            // SM-A526U / SMA526U / A526U → SM-A526U style when possible
            if (Regex.IsMatch(model, @"^SM[A-Z]?\d"))
            {
                if (!model.StartsWith("SM-"))
                    model = "SM-" + model.Replace("SM", "");
            }
            return model;
        }

        private static string? MapChipsetToGeneric(string chipset)
        {
            chipset = chipset.ToLowerInvariant();
            if (chipset.Contains("460") || chipset.Contains("sm4250")) return "smd460";
            if (chipset.Contains("680") || chipset.Contains("sm6225")) return "smd680";
            if (chipset.Contains("720") || chipset.Contains("sm7125")) return "smd720g";
            if (chipset.Contains("778") || chipset.Contains("sm7325")) return "smd778";
            if (chipset.Contains("855") || chipset.Contains("sm8150")) return "smd855";
            return null;
        }
    }
}
