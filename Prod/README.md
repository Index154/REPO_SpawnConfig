# SpawnConfig
This mod allows you to change several things about the enemy spawning behavior in the game. It is intended to be used for modpack balance fine-tuning. Only the host needs to have it installed.
Small warning: Taking advantage of this mod requires you to have a basic understanding of json file syntax. In return it offers a very high degree of configurability!

Here's a list of what's made possible by the mod:
- You can modify or remove the vanilla enemy groups and add your own custom groups
- Support for custom enemies added by other mods!
- You can change how many enemy groups spawn based on the level number and based on the amount of players in the lobby. You can also add multiple possible group count configurations for the same level for some randomness / variety
- You can assign weights to fine-tune how likely each enemy group is to appear
- You can control whether a group should only be able to appear before or after a certain level number
- You can give enemy groups different spawn weights for each type of level (for example you could have the Headman only spawnable in the Manor)
- An enemy group can be set to not allow any other groups to spawn if it happens to be selected. Use this if you crafted something truly devious and grand
- You can add extra variety to a group by configuring a chance for it to sometimes be bigger or smaller by a configurable range
- You can also disable enemy spawning entirely


## Getting started
1. These important files and folders will be generated in your BepInEx config folder after launching the game:
    - `Index154.SpawnConfig.cfg`  =>  The global config of the mod which is comparatively simple. Contains descriptions for each available setting. Please take a look at it!
    - Subfolder `SpawnConfig`:
        - `SpawnGroups.json`  =>  Edit this file to modify, add and remove enemy groups
        - `GroupsPerLevel.json`  =>  Edit this file to change how many enemy groups spawn per level
        - Subfolder `Defaults`:
            - `SpawnGroups-Readonly.json`  =>  This file just exists as a reference and is not meant to be edited! It contains the configs for all groups the game has loaded before your custom config was applied. Enemy groups added by other mods will be found in here as well. The file is overwritten on every game launch
            - `GroupsPerLevel-Readonly.json`  =>  This file just exists as a reference and is not meant to be edited! It contains the vanilla groups per level configs. The file is overwritten on every game launch

2. [PLEASE READ THROUGH THE MOD'S WIKI HERE](https://github.com/Index154/REPO_SpawnConfig/wiki)

3. Make your edits in the files `SpawnConfig\SpawnGroups.json` and `SpawnConfig\GroupsPerLevel.json`. As long as you read the wiki and follow the example of the vanilla entries you'll be fine. Changes will take effect only after restarting the game

4. Have fun!


## Known incompatibilities
- SpawnConfigPlus by onecoolsnowmanMods
- TierSpawnConfig by Dev1A3
- MoneyBag Valuable by Oooppp (No clue why this one is problematic. I tried to contact the creator but have not received a reply)
- Most likely any other mod that makes major changes to how enemy spawning works in the game


## For mod developers
- As long as you register your custom enemies properly with REPOLib they will be supported by SpawnConfig
- SpawnConfig FULLY replaces the functions `AmountSetup()` and `PickEnemies()` in `EnemyDirector.cs`!


## What else?
- If something doesn't work as expected then take a look at the BepInEx log output. Most issues will come with a corresponding error message in the log to help with troubleshooting
- Report issues, request features, check known issues and check planned features on GitHub: https://github.com/Index154/REPO_SpawnConfig