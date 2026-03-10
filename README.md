# NugLabs CLI

`nuglabs` is a cross‑platform command‑line tool for quickly looking up cannabis strains from the NugLabs strain catalogue.

It is designed to work entirely from a **local JSON snapshot** while periodically refreshing from the Strain Data API at **`https://strains.nuglabs.co`**.

## Install (macOS, Linux, Windows)

Download the zip for your platform from [Releases](https://github.com/nug-labs/cli/releases), then:

**macOS (Apple Silicon):**
```bash
curl -L https://github.com/nug-labs/cli/releases/download/v1.0.4/nuglabs-macos-arm64.zip -o nuglabs.zip && unzip -o nuglabs.zip && chmod +x nuglabs && sudo mv nuglabs /usr/local/bin/ && rm nuglabs.zip && echo "Installed! Run: nuglabs mimosa"
```

**macOS (Intel):**
```bash
curl -L https://github.com/nug-labs/cli/releases/download/v1.0.4/nuglabs-macos-x64.zip -o nuglabs.zip && unzip -o nuglabs.zip && chmod +x nuglabs && sudo mv nuglabs /usr/local/bin/ && rm nuglabs.zip && echo "Installed! Run: nuglabs mimosa"
```

**Linux (x64):**
```bash
curl -L https://github.com/nug-labs/cli/releases/download/v1.0.4/nuglabs-linux-x64.zip -o nuglabs.zip && unzip -o nuglabs.zip && chmod +x nuglabs && sudo mv nuglabs /usr/local/bin/ && rm nuglabs.zip && echo "Installed! Run: nuglabs mimosa"
```

**Windows:** download `nuglabs-windows-x64.zip`, unzip, and add the folder to your `PATH` (or run `nuglabs.exe` from that folder).

Replace `v1.0.3` with the latest release tag if needed.

## What it does

- **Search by strain name** or **AKA** (alias), using exact, case‑insensitive matching.
- Prints a human‑friendly summary:
  - Name, type, AKAs
  - Top/positive/negative effects
  - THC %, flavors, terpenes, “helps with”
  - Rating and a concise description
- Maintains a **local cache** of the full strain list in `~/.nuglabs/data.json` (user directory, so it works when the binary is in `/usr/local/bin` or Program Files).
- On startup, runs a **background refresh**:
  - If the last successful fetch is older than 12 hours
  - Calls `GET https://strains.nuglabs.co/api/v1/strains`
  - Overwrites the local `data.json` snapshot

## Usage

After building or downloading the CLI, run:

```bash
nuglabs "Mimosa"
```

Example output:

```text
name: Mimosa
type: Hybrid
category: Hybrid
akas: Purple Mimosa, Mimosas
parents: Purple Punch, Clementine
children: Kings Juice, Garlic Cocktail
thc: 19%
description: Mimosa is a hybrid strain created by crossing Clementine with Purple Punch, offering happy, uplifting, and motivating effects in small doses.
top_effect: Energetic
positive_effects: Focused, Energetic, Uplifted
negative_effects: Anxious, Dry Mouth, Headache
flavors: Citrus
detailed_terpenes: Myrcene, Pinene, Caryophyllene
helps_with: Anxiety, Stress, Depression
rating: 4.43
grow_notes: Those growing Mimosa with standard or feminized seeds should expect to pay $10–15 per seed...
```

You can also search by AKA:

```bash
nuglabs "Purple Mimosa"
```

which will resolve to the same Mimosa entry via its alias.

## Where the data comes from

The CLI ships with an embedded strain list. On first run it writes that snapshot to **`~/.nuglabs/data.json`** (so the cache lives in your home directory and works even when the binary is installed in `/usr/local/bin` or Program Files). On startup it:

1. If `~/.nuglabs/data.json` is missing, creates it from the embedded snapshot.
2. Loads the cache into memory.
3. Starts a background task: if the last refresh is older than 12 hours, fetches `GET https://strains.nuglabs.co/api/v1/strains` and overwrites `~/.nuglabs/data.json`.
4. All interactive searches use the in‑memory snapshot, so there is **no per‑query API latency**.

