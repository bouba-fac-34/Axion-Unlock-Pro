using System.Management;
using System.Text.RegularExpressions;
using Axion.Core.Models;
using Axion.Core.Utils;

namespace Axion.Core.DeviceManager
{
    public class DeviceDetectionService
    {
        public event Action<DeviceInfo>? DeviceConnected;
        public event Action? DeviceDisconnected;
        public event Action<string>? LogMessage;

        private CancellationTokenSource? _cts;
        private DeviceInfo? _lastDevice;
        private readonly string _adbPath;
        private readonly string _fastbootPath;

        public DeviceDetectionService(string toolsPath = "bin")
        {
            _adbPath = Path.Combine(toolsPath, "adb.exe");
            _fastbootPath = Path.Combine(toolsPath, "fastboot.exe");
        }

        public void StartMonitoring()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(_cts.Token));
        }

        public void StopMonitoring() => _cts?.Cancel();

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var device = await DetectAsync();
                    if (device != null && device.Serial != _lastDevice?.Serial)
                    {
                        _lastDevice = device;
                        DeviceConnected?.Invoke(device);
                        Log($"Device connected: {device}");
                    }
                    else if (device == null && _lastDevice != null)
                    {
                        _lastDevice = null;
                        DeviceDisconnected?.Invoke();
                        Log("Device disconnected");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Detection error: {ex.Message}");
                }
                await Task.Delay(2000, token);
            }
        }

        public async Task<DeviceInfo?> DetectAsync()
        {
            // 1. ADB
            var adbOut = await ProcessRunner.RunAsync(_adbPath, "devices -l");
            var adbMatch = Regex.Match(adbOut, @"^(\S+)\s+(device|unauthorized|offline)", RegexOptions.Multiline);
            if (adbMatch.Success)
            {
                var serial = adbMatch.Groups[1].Value;
                var state = adbMatch.Groups[2].Value;
                if (state == "unauthorized")
                    return new DeviceInfo { Serial = serial, Mode = ConnectionMode.Unauthorized, Brand = "Unknown" };

                if (state == "device")
                {
                    var props = await GetAdbProperties(serial);
                    return BuildFromProps(props, serial, ConnectionMode.ADB);
                }
            }

            // 2. Fastboot
            var fbOut = await ProcessRunner.RunAsync(_fastbootPath, "devices");
            var fbMatch = Regex.Match(fbOut, @"^(\S+)\s+fastboot", RegexOptions.Multiline);
            if (fbMatch.Success)
            {
                var serial = fbMatch.Groups[1].Value;
                var product = await ProcessRunner.RunAsync(_fastbootPath, $"-s {serial} getvar product");
                return new DeviceInfo
                {
                    Serial = serial,
                    Mode = ConnectionMode.Fastboot,
                    Product = ExtractFastbootVar(product, "product"),
                    Brand = GuessBrandFromProduct(ExtractFastbootVar(product, "product"))
                };
            }

            // 3. USB VID/PID scan
            var usbDevices = GetUsbDevices();
            foreach (var usb in usbDevices)
            {
                if (IsSamsungDownload(usb))
                    return new DeviceInfo { Brand = "Samsung", Mode = ConnectionMode.Download, Model = "Download Mode" };
                if (IsQualcommEdl(usb))
                    return new DeviceInfo { Brand = "Qualcomm", Mode = ConnectionMode.EDL, Model = "EDL 9008" };
                if (IsMtkPreloader(usb))
                    return new DeviceInfo { Brand = "MediaTek", Mode = ConnectionMode.Preloader, Model = "Preloader" };
                if (IsTranssion(usb))
                    return new DeviceInfo { Brand = "Transsion", Mode = ConnectionMode.ADB, Model = "Transsion Device" };
            }

            return null;
        }

        private async Task<Dictionary<string, string>> GetAdbProperties(string serial)
        {
            var output = await ProcessRunner.RunAsync(_adbPath, $"-s {serial} shell getprop");
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(output, @"\[([^\]]+)\]:\s*\[([^\]]*)\]"))
                dict[m.Groups[1].Value] = m.Groups[2].Value;
            return dict;
        }

        private DeviceInfo BuildFromProps(Dictionary<string, string> props, string serial, ConnectionMode mode)
        {
            string Get(string key) => props.TryGetValue(key, out var v) ? v : "";
            var brand = Get("ro.product.brand");
            var model = Get("ro.product.model");
            var device = Get("ro.product.device");
            var hardware = Get("ro.hardware");
            var android = Get("ro.build.version.release");
            var chipset = GuessChipset(hardware, Get("ro.board.platform"));

            int.TryParse(Get("ro.boot.battery") ?? "-1", out var battery);

            return new DeviceInfo
            {
                Brand = string.IsNullOrEmpty(brand) ? "Unknown" : brand,
                Model = string.IsNullOrEmpty(model) ? device : model,
                Codename = device,
                Chipset = chipset,
                AndroidVersion = android,
                Serial = serial,
                Mode = mode,
                IsAuthorized = true,
                Battery = battery,
                Product = Get("ro.product.name"),
                Hardware = hardware
            };
        }

        private string GuessChipset(string hardware, string platform)
        {
            if (hardware.Contains("qcom", StringComparison.OrdinalIgnoreCase) || platform.Contains("msm") || platform.Contains("sm"))
                return "Qualcomm";
            if (hardware.Contains("mt", StringComparison.OrdinalIgnoreCase) || platform.StartsWith("mt", StringComparison.OrdinalIgnoreCase))
                return "MediaTek";
            if (hardware.Contains("exynos", StringComparison.OrdinalIgnoreCase) || platform.Contains("universal"))
                return "Exynos";
            if (hardware.Contains("kirin", StringComparison.OrdinalIgnoreCase) || platform.Contains("hi"))
                return "Kirin";
            if (hardware.Contains("unisoc") || hardware.Contains("spreadtrum") || platform.Contains("ums"))
                return "Unisoc";
            return platform.Length > 0 ? platform : hardware;
        }

        private string GuessBrandFromProduct(string product)
        {
            product = product.ToLowerInvariant();
            if (product.Contains("samsung") || product.StartsWith("sm-") || product.StartsWith("gt-")) return "Samsung";
            if (product.Contains("tecno") || product.Contains("infinix") || product.Contains("itel")) return "Transsion";
            if (product.Contains("xiaomi") || product.Contains("redmi") || product.Contains("poco")) return "Xiaomi";
            if (product.Contains("oppo") || product.Contains("realme") || product.Contains("oneplus")) return "Oppo";
            if (product.Contains("huawei") || product.Contains("honor")) return "Huawei";
            if (product.Contains("motorola") || product.Contains("moto")) return "Motorola";
            return "Unknown";
        }

        private string ExtractFastbootVar(string output, string key)
        {
            var m = Regex.Match(output, $@"{key}:\s*(\S+)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : "";
        }

        private List<UsbDevice> GetUsbDevices()
        {
            var list = new List<UsbDevice>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, DeviceID FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var id = obj["DeviceID"]?.ToString() ?? "";
                    var name = obj["Name"]?.ToString() ?? "";
                    var vid = "";
                    var pid = "";
                    var vidMatch = Regex.Match(id, @"VID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    var pidMatch = Regex.Match(id, @"PID_([0-9A-F]{4})", RegexOptions.IgnoreCase);
                    if (vidMatch.Success) vid = vidMatch.Groups[1].Value.ToUpper();
                    if (pidMatch.Success) pid = pidMatch.Groups[1].Value.ToUpper();
                    list.Add(new UsbDevice { Name = name, DeviceId = id, Vid = vid, Pid = pid });
                }
            }
            catch { }
            return list;
        }

        private bool IsSamsungDownload(UsbDevice d) =>
            d.Vid == "04E8" && (d.Name.Contains("Gadget", StringComparison.OrdinalIgnoreCase) ||
                                d.Name.Contains("Samsung", StringComparison.OrdinalIgnoreCase) ||
                                d.Pid == "685D" || d.Pid == "6860");

        private bool IsQualcommEdl(UsbDevice d) =>
            d.Vid == "05C6" && (d.Name.Contains("QDLoader", StringComparison.OrdinalIgnoreCase) ||
                                d.Name.Contains("9008", StringComparison.OrdinalIgnoreCase) ||
                                d.Pid == "9008");

        private bool IsMtkPreloader(UsbDevice d) =>
            d.Vid == "0E8D" && (d.Name.Contains("PreLoader", StringComparison.OrdinalIgnoreCase) ||
                                d.Name.Contains("MediaTek", StringComparison.OrdinalIgnoreCase) ||
                                d.Pid == "2000" || d.Pid == "0003");

        private bool IsTranssion(UsbDevice d) =>
            d.Vid == "0E8D" || d.Name.Contains("TECNO", StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains("Infinix", StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains("Itel", StringComparison.OrdinalIgnoreCase);

        private void Log(string msg) => LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {msg}");
    }
}
