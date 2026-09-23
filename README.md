# Axion Unlock Pro v2.0

Professional Windows desktop tool for FRP, MDM, Knox Guard, screen lock, Demo, Mi Account, anti-relock, and related service operations.

## What's new in v2

- Samsung: FRP, MDM/KG, KG anti-relock, screen lock, factory reset, disable OTA, device info
- Transsion (Tecno/Infinix/Itel): FRP, MDM, lock, disable OTA
- Xiaomi: FRP, Mi Account, Fastboot to EDL
- Oppo/Realme: FRP + Demo, MDM
- MediaTek: DA/BROM FRP, format+FRP, GPT
- Qualcomm: EDL FRP, identify
- Universal ADB FRP / OTA / reset for Huawei, Motorola, Vivo, SPD
- Dark pro UI, live detection, session log

## Build (Rider / Visual Studio / CLI)

```bash
dotnet restore
dotnet build -c Release
dotnet run --project AxionUnlockPro
```

## Runtime requirements

1. .NET 8
2. bin/adb.exe + bin/fastboot.exe (+ AdbWinApi.dll, AdbWinUsbApi.dll) next to the exe
3. Brand USB drivers (Samsung, MTK VCOM, Qualcomm 9008)

## Notes

- ADB paths are fully functional against authorized devices.
- EDL / Preloader / BROM paths are structured for Firehose/DA loaders.
- After MDM/KG removal always run Disable OTA / Anti-relock to reduce re-lock risk.
