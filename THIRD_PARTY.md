# Third-party tools Axion Unlock Pro integrates with

| Component | Project | License | Role |
|-----------|---------|---------|------|
| EDL / Firehose CLI | [bkerler/edl](https://github.com/bkerler/edl) | GPLv3 | Qualcomm Sahara + Firehose |
| MTK DA / BROM | [bkerler/mtkclient](https://github.com/bkerler/mtkclient) | GPLv3 | MediaTek flash / FRP |
| Samsung Odin protocol (C#) | [Alephgsm/SharpOdinClient](https://github.com/Alephgsm/SharpOdinClient) | GPL-3.0 | Download-mode native client |
| Samsung Firehose pack | [Alephgsm/SAMSUNG-EDL-Loaders](https://github.com/Alephgsm/SAMSUNG-EDL-Loaders) | see repo | Programmer binaries |
| EDL loaders (multi) | [bkerler/Loaders](https://github.com/bkerler/Loaders) | see repo | Extra programmers |

GPLv3 tools: if you ship a binary that **links** or **distributes** modified edl/mtkclient, follow GPLv3 obligations. Axion currently **shells out** to `edl` / `mtk` as external processes (not linked).

SharpOdinClient: add as project/NuGet reference for full Odin automation; until then OdinService documents the path and can launch external Odin3.exe.
