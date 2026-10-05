# 🔧 Idle Workshop — Mobile Idle Tycoon

> A mobile idle tycoon built in Unity 6: customers drop off items, workers repair them across unlockable workshops, and the economy keeps running while the player is away. Playable prototype, in active development.

![Engine](https://img.shields.io/badge/Engine-Unity%206-black?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-purple)
![Platform](https://img.shields.io/badge/Platform-Mobile-green?logo=android)
![Status](https://img.shields.io/badge/Status-In%20development-yellow)

> **About this repository.** Public copy of an in-progress project, published with the team's permission so the code can be reviewed. The full commit history is preserved.

---

## 🎮 Overview

The player runs a repair workshop. Customers arrive at reception, leave an item and wait; workers carry it to a workstation, repair it and deliver the result back. Coins come in continuously and are spent on upgrades that widen the loop: faster workers, bigger carts, new rooms and new workstations.

As an idle game, the central design problem is that **progress has to continue when nobody is playing** — which is what drives the offline earnings, the save system and the data-driven upgrade architecture below.

---

## ⚙️ Core Systems

### 💰 Economy & Offline Earnings
- `EconomyManager` as the single source of truth for currency, with `CoinSource` emitters at each income point
- `EarningsRateMeter` computes the live coins-per-second rate shown to the player
- `OfflineEarnings` grants the progress accumulated while the game was closed, configured through a ScriptableObject and surfaced in a return-to-game popup
- `CurrencyFormatter` for idle-style number shortening (K, M, B…)

### 💾 Save System
A full custom save layer instead of scattered `PlayerPrefs` calls:
- `ISaveableElement` — anything persistent implements it and registers itself
- `SaveRegistry`, `WorkerRegistry` and `WorkStationRegistry` — track live objects and rebuild them on load
- `SaveIdentity` — stable ids so objects survive scene reloads
- `SaveManager`, `SaveData`, `SaveFile` and `SaveFormat` — serialization, versioning and disk I/O

### ⬆️ Upgrade & Unlock System
Data-driven, so new upgrades don't require touching existing code:
- `UpgradeableBase` plus the `IUpgradeable`, `IUpgradePreview`, `IUnlockable` and `IHiddenUntilBought` interfaces
- One `UpgradeData` ScriptableObject per upgrade family: workers, idle workers, carts, cart handling, work desks, reception, reception desks and decoration
- `UpgradeZone` and `RoomUpgradeElement` group upgrades by room
- **Ghost previews** (`UpgradeGhost`, `GhostPulse`, `DecorationReveal`) show what an upgrade will look like before the player pays for it
- `CoinBonusRegistry` aggregates multiplicative bonuses coming from every source

### 👷 Workers & Workshop Simulation
- `WorkerBase` shared by `Worker`, `IdleWorker` and `CartWorker`
- `WorkStation`, `WorkTable`, `Workshop` and `WorkshopOutput` drive the repair pipeline
- `Cart`, `Sack`, `ItemStack` and `IItemContainer` handle item transport and capacity
- Custom navigation with `WorkshopNavGraph`, `PathSmoothing` and `NavObstacle`, instead of relying on NavMesh
- `SortingOrders` keeps 2D depth consistent as workers and items move between rooms

### 🧍 Customers & Reception
- `CustomerManager` spawns customers against a `WaitingArea` with limited capacity
- `ReceptionDesk`, `PickupDesk` and `Receptionist` split drop-off and collection
- `ServiceSpeed` as the upgradeable throughput of the reception loop

### 🚀 Boot & Architecture
- `GameBootstrap` with `BootOrder` and `BootPhase`: deterministic, ordered initialization, so saved data loads before the systems that depend on it
- `InputManager` and `TapHandler` on the new Input System, driven by `IdleInputActions`
- `AudioManager` and `ButtonSoundManager`, with a `NoClickSound` opt-out marker

### 🧰 Custom Editor Tooling
- `SaveAudit` — inspects what is actually being persisted and flags objects missing a save identity
- `UpgradeAudit` — validates upgrade data for gaps and inconsistent costs
- `Taller1Builder` — scene builder for workshop layouts
- `LayerSetup` — project layer configuration

---

## 🛠️ Tech Stack

| Category | Technology |
|----------|------------|
| Engine | Unity 6 (6000.4) |
| Language | C# |
| Input | Unity Input System |
| Data | ScriptableObjects |
| Rendering | Universal Render Pipeline (URP) |
| UI | Unity UI + TextMeshPro |
| Platform | Mobile (Android) |

---

## 🚧 Status

Playable prototype under active development. The economy, save, upgrade, worker and reception loops are implemented; content, balancing, art and monetization are still in progress.

---

## 👤 Author

**Darío Calderón Tornero** — Gameplay Programmer (Unity & Unreal Engine 5)
[Portfolio](https://dariogamedev.com) · [LinkedIn](https://www.linkedin.com/in/dariocalderontornero/) · [GitHub](https://github.com/DarioCalderonTornero)

---

## 📄 License

This project is not open source. All rights reserved.
