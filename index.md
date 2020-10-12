---
layout: page
title: Home
---

RE CVX SRT is a speedrun tool for [Resident Evil: Code: Veronica](https://en.wikipedia.org/wiki/Resident_Evil_%E2%80%93_Code:_Veronica){:target="_blank" rel="noopener"} that works with [PCSX2](https://pcsx2.net/){:target="_blank" rel="noopener"} and [RPCS3](https://rpcs3.net/){:target="_blank" rel="noopener"} emulators.

*Last Update: 2020-10-11 ([Changelog](changelog.html))*.

## Installation

2. Download and extract the latest version of **[SRT Host](https://www.neonblu.com/SRT/){:target="_blank" rel="noopener"}**.
3. Download the latest **[{{ site.product_name }} Plugin Pack {{ site.github.latest_release.name }}]({{ site.github.html_url }}/releases/download/{{ site.github.latest_release.tag_name }}/{{ site.github.html_url }}-plugin-pack_{{ site.github.latest_release.name }}.zip)**.
3. Extract the Plugin Pack contents to SRT Host **plugins** folder.
5. Run **SRTHost64.exe** and start Resident Evil: Code: Veronica using a [supported emulator](#support).

## Features

<img align="right" width="456" height="463" src="{{ site.url }}/{{ site.github.name }}/assets/srt_window_detailed.png" alt="{{ site.product_name }} main window">

* Enemy health.
* Player health.
* Poison status.
* Gassed status (Poisoned by Nosferatus gas attack).
* Inventory display.
* Equipped weapon.
* In-game timer.
* Retires used.
* Saves used.
* F.A.S. used.
* Customizable interface.
* JSON HTTP Server via **[SRTPluginUIJSON](https://github.com/Squirrelies/SRTPluginUIJSON/){:target="_blank" rel="noopener"}**.

### Planned

* Rank/score tracking.
* DirectX overlay.

## Support

The following emulators and game releases are currently supported.

### [RPCS3](https://rpcs3.net/){:target="_blank" rel="noopener"}

PlayStation 3 releases:

  - [**NPJB00135**] [JP] BioHazard Code: Veronica Kanzenban
  - [**NPUB30467**] [US] Resident Evil Code: Veronica X HD
  - [**NPEB00553**] [EU] Resident Evil Code: Veronica X

### [PCSX2](https://pcsx2.net/){:target="_blank" rel="noopener"}

PlayStation 2 releases:

  - [**SLPM-650.22**] [JP] BioHazard Code: Veronica Kanzenban 
  - [**SLUS-201.84**] [US] Resident Evil Code: Veronica X
  - [**SLES-503.06**] [EU] Resident Evil Code: Veronica X
