# 🚀KSA AircraftHUD v0.2.1

Kitten Space Agency Version: `ksa_v2026.7.6.4939`


KSA: [https://ahwoo.com/app/100000/kitten-space-agency](https://ahwoo.com/app/100000/kitten-space-agency)

Forum: [https://forums.ahwoo.com/threads/aircraft-hud.618/](https://forums.ahwoo.com/threads/aircraft-hud.618/)


### Dependencies

* [https://github.com/StarMapLoader/StarMap/releases](https://github.com/StarMapLoader/StarMap/releases)
* [https://github.com/MrJeranimo/ModMenu/releases](https://github.com/MrJeranimo/ModMenu/releases)
* [.NET 10 SDK/Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* Visual Studio 2026

### Installation

* Download the latest release of StarMap, follow the instructions to install until you are able to launch KSA from StarMap.exe
* Download the latest release of ModMenu
* Unzip both ModMenu and AircraftHUD into `Documents/My Games/Kitten Space Agency/mods/` (persistent)
* Open manifest.toml and add the lines below to enable both mods
* Always launch KSA using StarMap.Loader.exe!


##### manifest.toml

```
[[mods]]
id = "ModMenu"
enabled = true

[[mods]]
id = "AircraftHUD"
enabled = true
```

### Credits

🚀Massive thanks to RocketWerkz, Dean Hall and all of the devs for making KSA possible!

Also, thanks to the many community members who helped and contributed to making this mod possible in one way or another, including:

* Lexi for making the original [KSAModLoader](https://github.com/cheese3660/KsaLoader)
* KlaasWhite for making and maintaining StarMap
* MrJeranimo for making ModMenu
* Dejvid for making the [Avionics](https://github.com/DavidK0/Avionics) mod, invaluable for initial code reference
* tom_is_unlucky, Pimiento and others for helping to solve some issues