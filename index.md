---
layout: page
title: Home
---

A SRT (Speedrun Tool) for Resident Evil: Code: Veronica that works with PCSX2 and RPCS3 emulators.

*Last Update: 2020-09-30 ([changelog](changelog.html))*.

## Installation

1. Download and install any missing **[prerequisite](/re-cvx-srt/downloads.html#Prerequisite)** software.
2. Download and extract the latest version of **[SRT Host 64-bit](https://www.neonblu.com/SRT/){:target="_blank" rel="noopener"}**.
3. Download the latest **[RE CVX SRT Provider](https://github.com/kapdap/re-cvx-srt-provider/releases/download/{{ site.provider_version }}/re-cvx-srt-provider_v{{ site.provider_version }}.zip)** and **[RE CVX SRT UI WPF](https://github.com/kapdap/re-cvx-srt-ui-wpf/releases/download/{{ site.interface_version }}/re-cvx-srt-ui-wpf_v{{ site.interface_version }}.zip)** plugins.
4. Extract plugin contents to the SRT Host **plugins** folder.
5. Run **SRTHost.exe** and start Resident Evil: Code: Veronica using a supported emulator.

## Features

* Enemy health.
* Player health.
* Poison status.
* Gassed status (Poisoned by Nosferatus gas attack).
* Inventory display.
* Equipped weapon.
* In-game timer.
* Retires used.
* Saves used.
* F.A.S used.
* Rank/score calculations.
* JSON HTTP Server via **[SRTPluginUIJSON](https://github.com/Squirrelies/SRTPluginUIJSON/){:target="_blank" rel="noopener"}**.

### Planned

* DirectX overlay.

## Support

The following emulators and game releases are currently supported.

### [RPCS3](https://rpcs3.net/)

PlayStation 3 releases:

  - [**NPJB00135**] [JP] BioHazard Code: Veronica Kanzenban
  - [**NPUB30467**] [US] Resident Evil Code: Veronica X HD
  - [**NPEB00553**] [EU] Resident Evil Code: Veronica X

### [PCSX2](https://pcsx2.net/)

PlayStation 2 releases:

  - [**SLPM-650.22**] [JP] BioHazard Code: Veronica Kanzenban 
  - [**SLUS-201.84**] [US] Resident Evil Code: Veronica X
  - [**SLES-503.06**] [EU] Resident Evil Code: Veronica X
