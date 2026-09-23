using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Axion.Core.DeviceManager;
using Axion.Core.Models;
using Axion.Core.Protocols.Samsung;
using Axion.Core.Protocols.Transsion;
using Axion.Core.Protocols.MTK;
using Axion.Core.Protocols.Qualcomm;

namespace AxionUnlockPro
{
    public partial class MainWindow : Window
    {
        private readonly DeviceDetectionService _detector;
        private DeviceInfo? _currentDevice;
        private string _selectedBrand = "";
        private readonly string _adbPath;
        private bool _busy;

        public MainWindow()
        {
            InitializeComponent();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var bin = System.IO.Path.Combine(baseDir, "bin");
            _adbPath = System.IO.Path.Combine(bin, "adb.exe");
            if (!System.IO.File.Exists(_adbPath))
                _adbPath = "adb.exe"; // fallback to PATH

            _detector = new DeviceDetectionService(bin);
            _detector.DeviceConnected += OnDeviceConnected;
            _detector.DeviceDisconnected += OnDeviceDisconnected;
            _detector.LogMessage += msg => Dispatcher.Invoke(() => AppendLog(msg));

            Loaded += (_, _) =>
            {
                AppendLog("Axion Unlock Pro started");
                AppendLog("Place adb.exe + fastboot.exe in bin/ folder for full detection");
                _detector.StartMonitoring();
            };
            Closed += (_, _) => _detector.StopMonitoring();
        }

        private void OnDeviceConnected(DeviceInfo device)
        {
            Dispatcher.Invoke(() =>
            {
                _currentDevice = device;
                txtDeviceStatus.Text = $"{device.Brand} {device.Model}";
                txtDeviceDetails.Text = $"{device.Mode} | {device.Chipset} | Android {device.AndroidVersion} | S/N: {device.Serial}";
                statusDot.Fill = device.Mode == ConnectionMode.Unauthorized
                    ? new SolidColorBrush(Color.FromRgb(255, 170, 0))
                    : new SolidColorBrush(Color.FromRgb(0, 200, 83));
                txtSession.Text = $"Connected: {device}";
            });
        }

        private void OnDeviceDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                _currentDevice = null;
                txtDeviceStatus.Text = "No device connected";
                txtDeviceDetails.Text = "Connect a phone via USB";
                statusDot.Fill = new SolidColorBrush(Color.FromRgb(255, 68, 68));
                txtSession.Text = "Device disconnected";
            });
        }

        private void Brand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string brand)
            {
                _selectedBrand = brand;
                txtSession.Text = $"Selected: {brand}";
                BuildOperations(brand);
                AppendLog($"Brand selected: {brand}");
            }
        }

        private void BuildOperations(string brand)
        {
            opsPanel.Children.Clear();
            var ops = brand switch
            {
                "Samsung" => new[]
                {
                    ("Remove FRP", (Func<Task>)(() => RunSamsungFrp())),
                    ("Remove MDM / Knox", (Func<Task>)(() => RunSamsungMdm())),
                    ("Remove Screen Lock", (Func<Task>)(() => RunSamsungLock())),
                    ("Network Unlock", (Func<Task>)(() => RunSamsungNetwork()))
                },
                "Transsion" => new[]
                {
                    ("Remove FRP", (Func<Task>)(() => RunTranssionFrp())),
                    ("Remove MDM", (Func<Task>)(() => RunTranssionMdm())),
                    ("Remove Pattern/PIN", (Func<Task>)(() => RunTranssionLock())),
                    ("IMEI Repair", (Func<Task>)(() => RunTranssionImei()))
                },
                "MTK" => new[]
                {
                    ("FRP Bypass", (Func<Task>)(() => RunMtkFrp())),
                    ("Format + FRP", (Func<Task>)(() => RunMtkFormat())),
                    ("Read GPT", (Func<Task>)(() => RunMtkGpt()))
                },
                "Qualcomm" => new[]
                {
                    ("EDL FRP", (Func<Task>)(() => RunQcFrp())),
                    ("Identify", (Func<Task>)(() => RunQcIdentify())),
                    ("Wipe FRP", (Func<Task>)(() => RunQcWipe()))
                },
                "Xiaomi" => new[]
                {
                    ("Remove FRP (ADB)", (Func<Task>)(() => RunGenericFrp("Xiaomi"))),
                    ("Info", (Func<Task>)(() => Task.Run(() => AppendLog("Xiaomi: use EDL + auth for locked devices"))))
                },
                "Oppo" => new[]
                {
                    ("Remove FRP (ADB)", (Func<Task>)(() => RunGenericFrp("Oppo")))
                },
                "Huawei" => new[]
                {
                    ("Remove FRP (ADB)", (Func<Task>)(() => RunGenericFrp("Huawei")))
                },
                "Motorola" => new[]
                {
                    ("Remove FRP (ADB)", (Func<Task>)(() => RunGenericFrp("Motorola")))
                },
                _ => new[]
                {
                    ("Generic FRP (ADB)", (Func<Task>)(() => RunGenericFrp("Generic")))
                }
            };

            foreach (var (title, action) in ops)
            {
                var b = new Button
                {
                    Content = title,
                    Style = (Style)FindResource("OpBtn"),
                    MinWidth = 160,
                    MinHeight = 48
                };
                b.Click += async (_, _) =>
                {
                    if (_busy) return;
                    _busy = true;
                    b.IsEnabled = false;
                    try { await action(); }
                    finally
                    {
                        _busy = false;
                        b.IsEnabled = true;
                    }
                };
                opsPanel.Children.Add(b);
            }
        }

        private async Task RunSamsungFrp()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new SamsungProtocol(_adbPath, AppendLog);
            var r = await proto.RemoveFrpAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunSamsungMdm()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new SamsungProtocol(_adbPath, AppendLog);
            var r = await proto.RemoveMdmAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunSamsungLock()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new SamsungProtocol(_adbPath, AppendLog);
            var r = await proto.RemoveScreenLockAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunSamsungNetwork()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new SamsungProtocol(_adbPath, AppendLog);
            var r = await proto.NetworkUnlockAsync(_currentDevice);
            AppendLog(r.Message);
        }

        private async Task RunTranssionFrp()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new TranssionProtocol(_adbPath, AppendLog);
            var r = await proto.RemoveFrpAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunTranssionMdm()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new TranssionProtocol(_adbPath, AppendLog);
            var r = await proto.RemoveMdmAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunTranssionLock()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new TranssionProtocol(_adbPath, AppendLog);
            var r = await proto.RemoveLockAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunTranssionImei()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new TranssionProtocol(_adbPath, AppendLog);
            var r = await proto.ImeiRepairAsync(_currentDevice, "000000000000000");
            AppendLog(r.Message);
        }

        private async Task RunMtkFrp()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new MtkProtocol(AppendLog);
            var r = await proto.RemoveFrpAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunMtkFormat()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new MtkProtocol(AppendLog);
            var r = await proto.FormatFrpAsync(_currentDevice);
            AppendLog(r.Message);
        }

        private async Task RunMtkGpt()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new MtkProtocol(AppendLog);
            var r = await proto.ReadGptAsync(_currentDevice);
            AppendLog(r.Message);
        }

        private async Task RunQcFrp()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new QualcommProtocol(AppendLog);
            var r = await proto.EdlFrpAsync(_currentDevice);
            AppendLog(r.Success ? $"OK: {r.Message}" : $"FAIL: {r.Message}");
        }

        private async Task RunQcIdentify()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new QualcommProtocol(AppendLog);
            var r = await proto.IdentifyAsync(_currentDevice);
            AppendLog(r.Message);
        }

        private async Task RunQcWipe()
        {
            if (_currentDevice == null) { AppendLog("No device"); return; }
            var proto = new QualcommProtocol(AppendLog);
            var r = await proto.WipeFrpAsync(_currentDevice);
            AppendLog(r.Message);
        }

        private async Task RunGenericFrp(string brand)
        {
            if (_currentDevice == null || _currentDevice.Mode != ConnectionMode.ADB)
            {
                AppendLog("ADB authorized device required");
                return;
            }
            AppendLog($"{brand} generic FRP via ADB...");
            var serial = _currentDevice.Serial;
            await Axion.Core.Utils.ProcessRunner.RunAsync(_adbPath, $"-s {serial} shell pm clear com.google.android.gsf");
            await Axion.Core.Utils.ProcessRunner.RunAsync(_adbPath, $"-s {serial} shell pm clear com.google.android.gsf.login");
            await Axion.Core.Utils.ProcessRunner.RunAsync(_adbPath, $"-s {serial} shell pm clear com.google.android.gms");
            await Axion.Core.Utils.ProcessRunner.RunAsync(_adbPath, $"-s {serial} shell settings put secure user_setup_complete 1");
            AppendLog("Generic FRP packages cleared – reboot device");
        }

        private void AppendLog(string msg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendLog(msg));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            txtLog.ScrollToEnd();
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e) => txtLog.Clear();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
