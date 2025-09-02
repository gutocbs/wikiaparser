# Genshin Wiki Parser

> Extracts structured JSON from Genshin Impact Fandom database dump into tidy, chunked datasets.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet\&logoColor=white)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-29ab87.svg)](#license)

[![Release](https://img.shields.io/github/v/release/gutocbs/wikiaparser)](https://github.com/gutocbs/wikiaparser/releases)

---

## Table of contents

* [Overview](#overview)
* [What it parses](#what-it-parses)
* [How it decides the type](#how-it-decides-the-type)
* [Output format](#output-format)
* [Quick start](#quick-start)
* [Build & publish](#build--publish)
* [Config](#config)
* [Chunking / splitting big outputs](#chunking--splitting-big-outputs)
* [Extending (add a new parser)](#extending-add-a-new-parser)
* [FAQ](#faq)
* [Project layout](#project-layout)
* [License](#license)

---

## Overview

This worker ingests wiki “pages” (XML with `#text` holding wikitext), detects the entity type (weapon, artifact, NPC, etc.), parses to strong-typed DTOs, and writes JSON files.
Large categories are **sharded** into many small files to keep search/indexing easier.

> \[!TIP]
> Designed for bulk runs (tens of thousands of pages) and incremental re-parses. The sinks avoid duplicating objects and support alphabetical or size-based splits.

---

## What it parses

* **Characters** (core info + Lore + Quotes)
* **Weapons**
* **Artifacts** (pieces + set)
* **NPCs** (including in-page **Dialogue**)
* **Enemies**
* **Factions**
* **Books / Book Collections**
* **Locations / Points of Interest**
* **Items** (incl. Quest Items)
* **Furnishings**
* **Quests**

---

## How it decides the type

The parser checks wikitext markers inside the `#text` field:

| Type       | Key markers (examples)                                      |
| ---------- | ----------------------------------------------------------- |
| Character  | `{{Character Infobox`, `{{VO/Story`, `{{Combat VO`          |
| Weapon     | `{{Weapon Infobox`                                          |
| Artifact   | `{{Artifact Infobox`                                        |
| NPC        | `{{Character Infobox` + `==Dialogue==`/`{{Dialogue Start}}` |
| Enemy      | `{{Enemy Infobox`                                           |
| Faction    | `{{Faction Infobox`                                         |
| Book       | `{{Book Collection Infobox` / `{{Book Infobox`              |
| Location   | `{{Location Infobox`                                        |
| Item       | `{{Item Infobox`                                            |
| Furnishing | `{{Furnishing Infobox`                                      |
| Quest      | `{{Quest Infobox`                                           |

---

## Output format

Each parsed object becomes a compact JSON. Collections can **split** deterministically:

```
out/
  characters/
    A/
      Albedo.json
    B/
      Baizhu.json
    ...
  weapons/
    favonius/
      Favonius-Sword.json
      Favonius-Greatsword.json
  artifacts/
    long-nights-oath/
      pieces/
        Flower-of-Life.json
        ...
      set.json
  npcs/
    C/
      Caterpillar.json
  ...
```

* **Stable file names** 
* **Alphabetical** folders by first letter (or series/set where it makes sense)
* Optional **size caps** (e.g., after N objects per file)

---

## Quick start

### Prerequirements

* [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)

### Run (framework-dependent)

```bash
dotnet run -c Release --project src/Worker/Worker.csproj -- \
  --input "/path/to/pages/*.xml" \
  --out "./out"
```

---

## Build & publish

```bash
# Windows x64
dotnet publish src/Worker/Worker.csproj -c Release -f net8.0 -r win-x64 -o out/win-x64

# Linux x64
dotnet publish src/Worker/Worker.csproj -c Release -f net8.0 -r linux-x64 -o out/linux-x64
```

---

## Chunking / splitting big outputs

The default sink (**SharedArraySink**) writes per-collection and splits by **alphabet** or **object count**:

* **Alphabetical**: `A/`, `B/`, …, based on `Title` (fast lookup, stable).
* **Size-based**: start a new file after **N** objects.

> This keeps files small enough for quick greps and avoids giant monoliths.

---

## Legal

Genshin Impact and related content © COGNOSPHERE/HoYoverse.

Data extracted here is for educational and research purposes.
