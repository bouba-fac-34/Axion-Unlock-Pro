using System.Text.Json;

namespace Axion.Core.Config
{
    public class AppSettings
    {
        public string AdbPath { get; set; } = "bin/adb.exe";
        public string FastbootPath { get; set; } = "bin/fastboot.exe";
        public string EdlPath { get; set; } = "edl";
        public string MtkPath { get; set; } = "mtk";
        public string ProgrammersRoot { get; set; } = "Resources/programmers";
        public string OdinPath { get; set; } = "bin/Odin3.exe";
        public bool AutoSelectBrand { get; set; } = true;
        public bool PersistLog { get; set; } = true;

        private static string FilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
    }
}
