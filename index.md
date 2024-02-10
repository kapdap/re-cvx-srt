---
layout: page
title: Home
---
{{ site.product_name }} is a speedrun tool for [{{ site.game_name }}](https://en.wikipedia.org/wiki/Resident_Evil_%E2%80%93_Code:_Veronica){:target="_blank" rel="noopener"} that works with [RPCS3](https://rpcs3.net/){:target="_blank" rel="noopener"}, [PCSX2](https://pcsx2.net/){:target="_blank" rel="noopener"} and [Dolphin](https://dolphin-emu.org/) emulators.

{% if site.posts.size > 0 %}
*Last Update: {% assign post = site.posts | first %} {{ post.date | date: "%F" }}*.
{% endif %}

## Quick Start

1. Download and run **[SRTPluginManager](https://github.com/SpeedrunTooling/SRTPluginManager/releases){:target="_blank" rel="noopener"}**.
3. Select **User Interfaces** then **{{ site.game_name }} X (WPF)** and click **Install**.
2. Select **SRT Host** then **{{ site.game_name }} X** and click **Install**.
4. Click **Start SRT**.

The SRT window will automatically display when a [supported emulator](#support) is detect and running ***{{ site.game_name }}***.

## Features

<img align="right" width="456" height="463" src="assets/srt_window_detailed.png" alt="{{ site.product_name }} main window">

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
* DirectX overlay.
* Customizable interface.

## Support

The following emulators and game releases are currently supported.

### [RPCS3](https://rpcs3.net/){:target="_blank" rel="noopener"}

PlayStation 3 releases:

  - [**NPJB00135**] [JP] BioHazard: Code: Veronica Kanzenban
  - [**NPUB30467**] [US] {{ site.game_name }} X HD
  - [**NPEB00553**] [EU] {{ site.game_name }} X

### [PCSX2](https://pcsx2.net/){:target="_blank" rel="noopener"}

PlayStation 2 releases:

  - [**SLPM-650.22**] [JP] BioHazard: Code: Veronica Kanzenban 
  - [**SLUS-201.84**] [US] {{ site.game_name }} X
  - [**SLES-503.06**] [EU] {{ site.game_name }} X

### [Dolphin](https://dolphin-emu.org/){:target="_blank" rel="noopener"}

GameCube releases:

  - [**GCDJ08**] [JP] BioHazard Code: Veronica Kanzenban 
  - [**GCDE08**] [US] {{ site.game_name }} X
  - [**GCDP08**] [EU] {{ site.game_name }} X