# NugLabs CLI

`nug-labs` is a cross‑platform command‑line tool for quickly looking up cannabis strains from the NugLabs strain catalogue.

It is designed to work entirely from a **local JSON snapshot** while periodically refreshing from the Strain Data API at **`https://strains.nuglabs.co`**.

## What it does

- **Search by strain name** or **AKA** (alias), using exact, case‑insensitive matching.
- Prints a human‑friendly summary:
  - Name, type, AKAs
  - Top/positive/negative effects
  - THC %, flavors, terpenes, “helps with”
  - Rating and a concise description
- Maintains a **local cache** of the full strain list (in `assets/data.json`).
- On startup, runs a **background refresh**:
  - If the last successful fetch is older than 12 hours
  - Calls `GET https://strains.nuglabs.co/api/v1/strains`
  - Overwrites the local `data.json` snapshot

## Usage

After building or downloading the CLI, run:

```bash
nug-labs "Mimosa"
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
nug-labs "Purple Mimosa"
```

which will resolve to the same Mimosa entry via its alias.

## Where the data comes from

The CLI ships with an embedded `assets/data.json` file containing the full strain list. On startup it:

1. Loads `assets/data.json` into memory.
2. Starts a background task:
   - If the last refresh is older than 12 hours, fetches `GET https://strains.nuglabs.co/api/v1/strains`.
   - Writes the new snapshot back to `assets/data.json`.
3. All interactive searches use the in‑memory snapshot, so there is **no per‑query API latency**.

