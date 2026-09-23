# Firehose / DA Programmers

Place Qualcomm Firehose (`.elf` / `.mbn`) and MediaTek DA binaries here.

## Samsung Qualcomm (EDL)

Source: https://github.com/Alephgsm/SAMSUNG-EDL-Loaders

```
# clone once
git clone --depth 1 https://github.com/Alephgsm/SAMSUNG-EDL-Loaders.git _tmp_loaders

# copy generic platform loaders
mkdir -p Resources/programmers/samsung/generic
cp "_tmp_loaders/Generic Samsung Firehose/"*.elf Resources/programmers/samsung/generic/

# copy per-model (example)
mkdir -p Resources/programmers/samsung/SM-A057F
cp "_tmp_loaders/Samsung Galaxy A05s (SM-A057F)/firehose/"*.elf Resources/programmers/samsung/SM-A057F/

# cleanup
rm -rf _tmp_loaders
```

Naming convention expected by Axion:
- `Resources/programmers/samsung/{MODEL}/*.elf`  e.g. SM-A526U
- `Resources/programmers/samsung/generic/prog_firehose_ddr_smd*.elf`

## MediaTek DA

Place DA files under:
```
Resources/programmers/mtk/
  MT6765_DA.bin
  MT6789_DA.bin
  ...
```

Or point `EdlService` / `MtkService` at an external SP Flash Tool / mtkclient install.

## edl CLI (bkerler)

Recommended for real EDL work:
```
pip install edl
# or clone https://github.com/bkerler/edl
```

Axion will call `edl` if present on PATH or under `bin/edl`.
