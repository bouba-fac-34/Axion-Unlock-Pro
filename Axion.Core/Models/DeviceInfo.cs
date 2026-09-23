namespace Axion.Core.Models
{
    public class DeviceInfo
    {
        public string Brand { get; set; } = "Unknown";
        public string Model { get; set; } = "Unknown";
        public string Codename { get; set; } = "";
        public string Chipset { get; set; } = "";
        public string AndroidVersion { get; set; } = "";
        public string Serial { get; set; } = "";
        public string Imei { get; set; } = "";
        public ConnectionMode Mode { get; set; } = ConnectionMode.None;
        public bool IsAuthorized { get; set; }
        public int Battery { get; set; } = -1;
        public string Product { get; set; } = "";
        public string Hardware { get; set; } = "";
        public override string ToString() =>
            $"{Brand} {Model} | {Mode} | {Chipset} | Android {AndroidVersion}";
    }

    public enum ConnectionMode
    {
        None,
        ADB,
        Fastboot,
        Download,
        EDL,
        Preloader,
        Meta,
        Sideload,
        Unauthorized
    }

    public class UsbDevice
    {
        public string Name { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string Vid { get; set; } = "";
        public string Pid { get; set; } = "";
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Log { get; set; } = "";
        public TimeSpan Duration { get; set; }
    }
}
