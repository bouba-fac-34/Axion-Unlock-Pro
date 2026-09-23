# Axion Unlock Pro

Professional Windows desktop tool for MDM / FRP / screen-lock / network unlock operations across major Android brands.

## Supported

| Brand | FRP | MDM | Screen Lock | Notes |
|-------|-----|-----|-------------|-------|
| Samsung | Yes | Knox Guard / MDM | Yes | ADB + Download mode |
| Tecno / Infinix / Itel | Yes | Yes | Yes | Transsion |
| Xiaomi / Redmi / Poco | ADB | — | — | EDL path skeleton |
| Oppo / Realme | ADB | — | — | |
| Huawei / Honor | ADB | — | — | |
| Motorola | ADB | — | — | |
| MediaTek Generic | Yes | — | — | Preloader / DA |
| Qualcomm EDL | Yes | — | — | Sahara + Firehose |

## Requirements

- Windows 10/11 x64
- .NET 8 SDK
- USB drivers (Samsung, MediaTek, Qualcomm)
- `adb.exe` + `fastboot.exe` placed in `bin/` folder (platform-tools)

## Build

```bash
dotnet restore
dotnet build -c Release
dotnet run --project AxionUnlockPro
```

## Structure

```
AxionUnlockPro/
├── AxionUnlockPro/          # WPF UI
├── Axion.Core/              # Detection + protocols
│   ├── DeviceManager/
│   ├── Protocols/
│   │   ├── Samsung/
│   │   ├── Transsion/
│   │   ├── MTK/
│   │   └── Qualcomm/
│   └── Utils/
├── Resources/bin/           # adb, fastboot
└── Database/
```

## Usage

1. Install .NET 8 runtime/SDK
2. Copy official platform-tools `adb.exe` / `fastboot.exe` into `AxionUnlockPro/bin/` (or next to the exe)
3. Install brand USB drivers
4. Run the app
5. Connect device → select brand → run operation

## Notes

- ADB methods require USB debugging authorized
- Download / EDL / Preloader modes need correct drivers and (for production) native protocol libraries
- IMEI write and full network unlock require additional auth tokens / engineering tools
- This is a functional base for technical research and recovery workflows

## License

Private – Axion
