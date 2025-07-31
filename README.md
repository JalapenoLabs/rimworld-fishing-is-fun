# Rimworld Fishing Is Fun

Have you ever wondered why fishing in RimWorld doesn't count as recreation?

How can you go fishing for hours and not feel any joy from it??

This is a mod designed for Rimworld, 1.6 and the Odyssey DLC.

This is intended to make the Odyssey DLC’s fishing activity count as recreation and grant a mood buff after a long fishing session.

# Features:
1. ***Recreation Need Gain***: While the pawn is actively fishing, their recreation (joy) need increases steadily (similar to playing chess or other joy activities).
2. ***Mood Buff***: If the pawn spent at least 1 in-game hour (2,500 ticks) fishing continuously, they receive a +3 "Pleasant Fishing Trip" mood thought lasting 6 hours.
3. ***Configurable***: You can enable/disable the mood buff or recreation, and you can tweak the buff reward time in the mod settings.

# Building & compiling from the source:
This was compiled with Dotnet 9.0+ make sure you have it installed or a later version.

Clone this repository into your `steamapps/common/RimWorld/Mods` folder.

### Using the command line:

```shell
make
```

Or if you don't have `make` installed, you can run:

```shell
dotnet build .vscode
```

### Using Visual Studio Code:
Open this repository in Visual Studio Code, and use Ctrl+Shift+B to build the assemblies for the mod.
