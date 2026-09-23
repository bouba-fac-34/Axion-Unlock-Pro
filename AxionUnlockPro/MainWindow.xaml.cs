using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Axion.Core.Config;
using Axion.Core.DeviceManager;
using Axion.Core.Models;
using Axion.Core.Protocols.Samsung;
using Axion.Core.Protocols.Transsion;
using Axion.Core.Protocols.Xiaomi;
using Axion.Core.Protocols.Oppo;
using Axion.Core.Protocols.MTK;
using Axion.Core.Protocols.Qualcomm;
using Axion.Core.Protocols.Huawei;
using Axion.Core.Protocols.Vivo;
using Axion.Core.Protocols.Motorola;
using Axion.Core.Services;
using Axion.Core.Utils;

namespace AxionUnlockPro
{
    public partial class MainWindow : Window
    {
        private readonly DeviceDetectionService _detector;
        private DeviceInfo? _device;
        private readonly AppSettings _settings;
        private string _adb;
        private string _fb;
        private bool _busy;

        public MainWindow()
        {
            InitializeComponent();
            _settings = AppSettings.Load();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var bin = System.IO.Path.Combine(baseDir, "bin");
            _adb = ResolveTool(_settings.AdbPath, System.IO.Path.Combine(bin, "adb.exe"), "adb.exe");
            _fb = ResolveTool(_settings.FastbootPath, System.IO.Path.Combine(bin, "fastboot.exe"), "fastboot.exe");

            _detector = new DeviceDetectionService(bin);
            _detector.DeviceConnected += d => Dispatcher.Invoke(() => OnDeviceConnected(d));
            _detector.DeviceDisconnected += () => Dispatcher.Invoke(OnDeviceDisconnected);
            _detector.LogMessage += m => Dispatcher.Invoke(() => AppendLog(m));

            Loaded += (_, _) =>
            {
                AppendLog("Axion Unlock Pro v2.1 ready");
                AppendLog("Tools: adb/fastboot in bin/ · edl + mtkclient via pip · programmers via scripts/fetch-programmers.ps1");
                AppendLog("Odin: reference Alephgsm/SharpOdinClient or place Odin3.exe in bin/");
                _detector.StartMonitoring();
            };
            Closed += (_, _) => _detector.StopMonitoring();
        }

        private static string ResolveTool(string preferred, string fallback, string pathName)
        {
            if (!string.IsNullOrWhiteSpace(preferred) && System.IO.File.Exists(preferred)) return preferred;
            if (System.IO.File.Exists(fallback)) return fallback;
            return pathName;
        }

        private void OnDeviceConnected(DeviceInfo d)
        {
            _device = d;
            txtDeviceStatus.Text = $"{d.Brand} {d.Model}";
            txtDeviceDetails.Text = $"{d.Mode} | {d.Chipset} | Android {d.AndroidVersion} | Patch {d.SecurityPatch} | {d.Serial}";
            statusDot.Fill = d.Mode == ConnectionMode.Unauthorized
                ? new SolidColorBrush(Color.FromRgb(255, 171, 0))
                : new SolidColorBrush(Color.FromRgb(0, 200, 83));
            txtSession.Text = $"Live: {d}";

            if (_settings.AutoSelectBrand)
            {
                var brand = MapBrand(d);
                if (!string.IsNullOrEmpty(brand))
                {
                    AppendLog($"Auto-selected brand: {brand}");
                    BuildOps(brand);
                    txtSession.Text = $"Brand: {brand} · {d}";
                }
            }
        }

        private void OnDeviceDisconnected()
        {
            _device = null;
            txtDeviceStatus.Text = "No device connected";
            txtDeviceDetails.Text = "Connect phone · USB debugging ON · authorize PC";
            statusDot.Fill = new SolidColorBrush(Color.FromRgb(255, 82, 82));
            txtSession.Text = "Disconnected";
            SetOpsEnabled(true);
        }

        private static string MapBrand(DeviceInfo d)
        {
            var b = (d.Brand ?? "").ToLowerInvariant();
            if (b.Contains("samsung")) return "Samsung";
            if (b.Contains("tecno") || b.Contains("infinix") || b.Contains("itel") || b.Contains("transsion")) return "Transsion";
            if (b.Contains("xiaomi") || b.Contains("redmi") || b.Contains("poco")) return "Xiaomi";
            if (b.Contains("oppo") || b.Contains("realme") || b.Contains("oneplus")) return "Oppo";
            if (b.Contains("huawei") || b.Contains("honor")) return "Huawei";
            if (b.Contains("motorola") || b.Contains("moto")) return "Motorola";
            if (b.Contains("vivo")) return "Vivo";
            if (d.Mode == ConnectionMode.EDL) return "Qualcomm";
            if (d.Mode == ConnectionMode.Preloader) return "MTK";
            if (d.Mode == ConnectionMode.Download) return "Samsung";
            return "Universal";
        }

        private void Brand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string brand) return;
            txtSession.Text = $"Brand: {brand}";
            AppendLog($"Selected {brand}");
            BuildOps(brand);
        }

        private void BuildOps(string brand)
        {
            opsPanel.Children.Clear();
            var list = brand switch
            {
                "Samsung" => new (string, Func<Task>)[]
                {
                    ("Remove FRP", () => RunResult(async () => await new SamsungProtocol(_adb, AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Remove MDM / KG", () => RunResult(async () => await new SamsungProtocol(_adb, AppendLog).RemoveMdmAsync(NeedDevice()))),
                    ("KG Anti-Relock", () => RunResult(async () => await new SamsungProtocol(_adb, AppendLog).RemoveKgRelockAsync(NeedDevice()))),
                    ("Remove Screen Lock", () => RunResult(async () => await new SamsungProtocol(_adb, AppendLog).RemoveScreenLockAsync(NeedDevice()))),
                    ("Factory Reset", () => RunResult(async () => await new SamsungProtocol(_adb, AppendLog).FactoryResetAsync(NeedDevice()))),
                    ("Disable OTA", () => RunResult(async () => await new SamsungProtocol(_adb, AppendLog).DisableOtaAsync(NeedDevice()))),
                    ("Read Device Info", () => RunResult(async () => await new SamsungProtocol(_adb, AppendLog).ReadInfoAsync(NeedDevice()))),
                    ("Download Info (Odin)", () => RunResult(async () => await new OdinService(AppendLog).ReadInfoAsync(NeedDevice())))
                },
                "Transsion" => new (string, Func<Task>)[]
                {
                    ("Remove FRP", () => RunResult(async () => await new TranssionProtocol(_adb, AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Remove MDM", () => RunResult(async () => await new TranssionProtocol(_adb, AppendLog).RemoveMdmAsync(NeedDevice()))),
                    ("Remove Pattern/PIN", () => RunResult(async () => await new TranssionProtocol(_adb, AppendLog).RemoveLockAsync(NeedDevice()))),
                    ("Disable OTA (anti-relock)", () => RunResult(async () => await new TranssionProtocol(_adb, AppendLog).DisableOtaAsync(NeedDevice())))
                },
                "Xiaomi" => new (string, Func<Task>)[]
                {
                    ("Remove FRP", () => RunResult(async () => await new XiaomiProtocol(_adb, _fb, AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Remove Mi Account", () => RunResult(async () => await new XiaomiProtocol(_adb, _fb, AppendLog).RemoveMiAccountAsync(NeedDevice()))),
                    ("Fastboot to EDL", () => RunResult(async () => await new XiaomiProtocol(_adb, _fb, AppendLog).FastbootToEdlAsync(NeedDevice())))
                },
                "Oppo" => new (string, Func<Task>)[]
                {
                    ("Remove FRP + Demo", () => RunResult(async () => await new OppoProtocol(_adb, AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Remove MDM", () => RunResult(async () => await new OppoProtocol(_adb, AppendLog).RemoveMdmAsync(NeedDevice())))
                },
                "Huawei" => new (string, Func<Task>)[]
                {
                    ("Remove FRP", () => RunResult(async () => await new HuaweiProtocol(_adb, AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Disable OTA", () => RunResult(async () => await new HuaweiProtocol(_adb, AppendLog).DisableOtaAsync(NeedDevice())))
                },
                "Vivo" => new (string, Func<Task>)[]
                {
                    ("Remove FRP", () => RunResult(async () => await new VivoProtocol(_adb, AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Disable OTA", () => RunResult(async () => await new VivoProtocol(_adb, AppendLog).DisableOtaAsync(NeedDevice())))
                },
                "Motorola" => new (string, Func<Task>)[]
                {
                    ("Remove FRP", () => RunResult(async () => await new MotorolaProtocol(_adb, AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Disable OTA", () => RunResult(async () => await new MotorolaProtocol(_adb, AppendLog).DisableOtaAsync(NeedDevice())))
                },
                "MTK" => new (string, Func<Task>)[]
                {
                    ("FRP (DA/BROM)", () => RunResult(async () => await new MtkProtocol(AppendLog).RemoveFrpAsync(NeedDevice()))),
                    ("Format + FRP", () => RunResult(async () => await new MtkProtocol(AppendLog).FormatFrpAsync(NeedDevice()))),
                    ("Read GPT", () => RunResult(async () => await new MtkProtocol(AppendLog).ReadGptAsync(NeedDevice())))
                },
                "Qualcomm" => new (string, Func<Task>)[]
                {
                    ("EDL FRP", () => RunResult(async () => await new QualcommProtocol(AppendLog).EdlFrpAsync(NeedDevice()))),
                    ("Identify", () => RunResult(async () => await new QualcommProtocol(AppendLog).IdentifyAsync(NeedDevice())))
                },
                "SPD" or "Universal" => new (string, Func<Task>)[]
                {
                    ("Universal FRP (ADB)", () => Run(GenericFrp)),
                    ("Disable OTA", () => Run(GenericDisableOta)),
                    ("Factory Reset (ADB)", () => Run(GenericFactoryReset))
                },
                _ => Array.Empty<(string, Func<Task>)>()
            };

            foreach (var (title, action) in list)
            {
                var b = new Button { Content = title, Style = (Style)FindResource("OpBtn"), MinWidth = 150, MinHeight = 46, IsEnabled = !_busy };
                b.Click += async (_, _) => await action();
                opsPanel.Children.Add(b);
            }
        }

        private void SetOpsEnabled(bool enabled)
        {
            foreach (var child in opsPanel.Children)
                if (child is Button btn) btn.IsEnabled = enabled;
        }

        private void SetProgress(bool on, double value = 0)
        {
            progressBar.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            progressBar.Value = value;
            if (on && value < 5) progressBar.IsIndeterminate = true;
            else progressBar.IsIndeterminate = false;
        }

        private DeviceInfo NeedDevice()
        {
            if (_device == null) throw new InvalidOperationException("No device connected");
            return _device;
        }

        private async Task Run(Func<Task> work)
        {
            if (_busy) return;
            _busy = true;
            SetOpsEnabled(false);
            SetProgress(true, 10);
            try { await work(); }
            catch (Exception ex) { AppendLog($"FAIL: {ex.Message}"); }
            finally
            {
                _busy = false;
                SetOpsEnabled(true);
                SetProgress(false);
            }
        }

        private async Task RunResult(Func<Task<OperationResult>> work)
        {
            if (_busy) return;
            _busy = true;
            SetOpsEnabled(false);
            SetProgress(true, 15);
            try
            {
                var r = await work();
                AppendLog(r.Success ? $"OK · {r.Message} ({r.Duration.TotalSeconds:0.0}s)" : $"FAIL · {r.Message}");
            }
            catch (Exception ex) { AppendLog($"FAIL: {ex.Message}"); }
            finally
            {
                _busy = false;
                SetOpsEnabled(true);
                SetProgress(false);
            }
        }

        private async Task GenericFrp()
        {
            var d = NeedDevice();
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized) { AppendLog("ADB authorized required"); return; }
            AppendLog("Universal FRP…");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.google.android.gsf");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.google.android.gsf.login");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell pm clear com.google.android.gms");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell settings put secure user_setup_complete 1");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell settings put global device_provisioned 1");
            AppendLog("FRP flags cleared – reboot device");
        }

        private async Task GenericDisableOta()
        {
            var d = NeedDevice();
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized) { AppendLog("ADB required"); return; }
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell settings put global ota_disable_automatic_update 1");
            AppendLog("OTA auto-update disabled");
        }

        private async Task GenericFactoryReset()
        {
            var d = NeedDevice();
            if (d.Mode != ConnectionMode.ADB || !d.IsAuthorized) { AppendLog("ADB required"); return; }
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} shell recovery --wipe_data");
            await ProcessRunner.RunAsync(_adb, $"-s {d.Serial} reboot recovery");
            AppendLog("Factory reset triggered");
        }

        private void AppendLog(string msg)
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => AppendLog(msg)); return; }
            var line = $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
            txtLog.AppendText(line);
            txtLog.ScrollToEnd();
            if (_settings.PersistLog)
            {
                try
                {
                    var dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.txt"), line);
                }
                catch { }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsWindow(_settings);
            if (dlg.ShowDialog() == true)
            {
                _settings.Save();
                _adb = ResolveTool(_settings.AdbPath, _adb, "adb.exe");
                _fb = ResolveTool(_settings.FastbootPath, _fb, "fastboot.exe");
                AppendLog("Settings saved");
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e) => txtLog.Clear();
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
