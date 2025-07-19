using BepInEx.Configuration;

namespace SpawnConfig;

public class ConfigManager {
    internal ConfigEntry<bool> preventSpawns = null!;
    internal ConfigEntry<bool> addMissingGroups = null!;
    internal ConfigEntry<double> repeatMultiplier = null!;
    internal ConfigEntry<bool> ignoreInvalidGroups = null!;
    internal ConfigEntry<int> groupCountMultiplier = null!;
    //internal ConfigEntry<double> enemyCountMultiplier = null!;
    internal void Setup(ConfigFile configFile) {
        preventSpawns = configFile.Bind("General", "Prevent enemy spawning", false, new ConfigDescription("Prevent enemy spawning entirely, turning the game into a no-stakes gathering simulator or for when you want to test something in peace"));

        addMissingGroups = configFile.Bind("General", "Re-add missing groups", false, new ConfigDescription("Whether the mod should update your custom SpawnGroups config at launch by adding all loaded enemy groups that are missing from it"));

        repeatMultiplier = configFile.Bind("General", "Repeat spawn weight multiplier", 0.5, new ConfigDescription("All three weights of an enemy group will be multiplied by this value for the current level after having been selected once. Reduces the chance of encountering multiple copies of a group in the same level. Set to 1.0 to \"disable\""));

        ignoreInvalidGroups = configFile.Bind("General", "Ignore groups with invalid spawnObjects", true, new ConfigDescription("If set to true, any group containing a single invalid spawn object will be ignored completely. If set to false, only the individual spawn object will be ignored and the group can still spawn as long as it contains at least one valid enemy"));

        groupCountMultiplier = configFile.Bind("General", "Group count multiplier", 1, new ConfigDescription("The amount of enemy groups spawned each level is multiplied by this number. This will apply on top of what you configure in `GroupsPerLevel.json`", new AcceptableValueRange<int>(1, 10), System.Array.Empty<object>()));

        //enemyCountMultiplier = configFile.Bind("General", "Enemy Count Multiplier", 1.0, new ConfigDescription("The amount of individual enemies per group is multiplied by this number. Warning: Some vanilla groups consist of as many as 10 small enemies. Setting this higher than 10 is not recommended", new AcceptableValueRange<double>(1.0, 100.0), Array.Empty<object>()));
    }
}