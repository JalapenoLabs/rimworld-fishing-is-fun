# Rimworld Fishing Is Fun

Have you ever wondered why fishing in RimWorld doesn't count as recreation?

How can you go fishing for hours and not feel any joy from it??

This is a mod designed for Rimworld, 1.6 and the Odyssey DLC.

This is intended to make the Odyssey DLC’s fishing activity count as recreation and grant a mood buff after a long fishing session.

# Features:
1. ***Recreation Need Gain***: While the pawn is actively fishing, their recreation (joy) need increases steadily (similar to playing chess or other joy activities).
2. ***Mood Buff***: If the pawn spent at least 1 in-game hour (2,500 ticks) fishing continuously, they receive a +3 "Pleasant Fishing Trip" mood thought lasting 6 hours.
3. ***Configurable***: You can enable/disable the mood buff or recreation, and you can tweak the buff reward time in the mod settings.

# Building from source

This mod is part of the [rimworld-mods monorepo](https://github.com/JalapenoLabs/rimworld-mods), which provides the shared RimWorld DLLs and build tooling. **You must clone the monorepo — not this repository directly — in order to build.**

### Prerequisites
- [.NET 9.0+](https://dotnet.microsoft.com/download)
- [Mage](https://magefile.org/) — `go install github.com/magefile/mage/mage@latest`

### Steps

```shell
git clone --recurse-submodules https://github.com/JalapenoLabs/rimworld-mods
cd rimworld-mods
mage build fishing-is-fun
```

Or with Make:

```shell
make -C mods/fishing-is-fun
```

# Steam URL:
https://steamcommunity.com/sharedfiles/filedetails/?id=3538562620&searchtext=

# Repository:
View the source code, contribute, or report issues on GitHub:
https://github.com/JalapenoLabs/rimworld-fishing-is-fun

# License:
This mod is licensed under the [MIT License](https://opensource.org/licenses/MIT)

# Author:
Alex Navarro
alex@jalapenolabs.io

Website: https://www.jalapenolabs.io/
Discord: https://www.jalapenolabs.io/discord
Patreon: https://www.jalapenolabs.io/patreon
