using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static SpawnConfig.ListManager;

namespace SpawnConfig.ExtendedClasses;

public class ExtendedEnemySetup {

    public string name = "Nameless";
    public bool levelsCompletedCondition = false;   // Legacy support
    public int levelsCompletedMax = 10;             // Legacy support
    public int levelsCompletedMin = 0;              // Legacy support
    public bool levelRangeCondition = false;
    public int minLevel = 0;
    public int maxLevel = 0;
    public int runsPlayed = 0;
    public List<string> spawnObjects = [];
    public float difficulty1Weight = 0.0f;
    public float difficulty2Weight = 0.0f;
    public float difficulty3Weight = 0.0f;
    public Dictionary<string, float> levelWeightMultipliers = [];
    public bool thisGroupOnly = false;
    public bool allowDuplicates = true;
    public double alterAmountChance = 0.0;
    public int alterAmountMin = 0;
    public int alterAmountMax = 0;
    public ExtendedEnemySetup() {}
    
    public ExtendedEnemySetup(EnemySetup enemySetup, int difficulty)
    {
        name = enemySetup.name;
        if (enemySetup.levelsCompletedCondition)
        {
            levelRangeCondition = true;
            minLevel = enemySetup.levelsCompletedMin + 1;
            maxLevel = enemySetup.levelsCompletedMax + 1;
        }
        if (maxLevel == 1) maxLevel = 0;     // If maxLevel is 0 the game ignores the maxLevel check (in vanilla too)
        runsPlayed = enemySetup.runsPlayed;
        spawnObjects = enemySetup.spawnObjects.Where(obj => !obj.PrefabName.Contains("Director")).Select(obj => obj.PrefabName).ToList();
        float baseWeight = 100.0f;
        if ((bool)enemySetup.rarityPreset)
        {
            // Adjust weights of groups based on the defined rarity preset (estimative)
            // The vanilla rarities are so fricked up that none of the possible imitations make sense across the board
            baseWeight = (float)System.Math.Round((-2.08f * (100 - enemySetup.rarityPreset.chance)) + 98.45f);  // Calculation that kiiind of emulates some of the values from vanilla...
            // Manual adjustments to make things less weird
            if (baseWeight > 100f) baseWeight = 100f;
            if (baseWeight == 98f) baseWeight = 100f;
            if (baseWeight <= 15f && baseWeight > 2f) baseWeight = 5f;
            if (baseWeight < 2f) baseWeight = 2f;
            SpawnConfig.Logger.LogDebug(name + " (rarity) = " + enemySetup.rarityPreset.chance);
        }
        difficulty1Weight = (difficulty == 1) ? baseWeight : 0.0f;
        difficulty2Weight = (difficulty == 2) ? baseWeight : 0.0f;
        difficulty3Weight = (difficulty == 3) ? baseWeight : 0.0f;

        foreach(string levelName in levelNames){
            if(!levelWeightMultipliers.ContainsKey(levelName)) levelWeightMultipliers.Add(levelName, 1.0f);
        }
    }
    
    public EnemySetup GetEnemySetup()
    {
        EnemySetup es = ScriptableObject.CreateInstance<EnemySetup>();
        es.name = name;
        es.spawnObjects = [];
        es.levelsCompletedCondition = levelRangeCondition;
        es.levelsCompletedMin = minLevel - 1;
        es.levelsCompletedMax = maxLevel - 1;
        es.runsPlayed = runsPlayed;

        foreach (string objName in spawnObjects){
            es.spawnObjects.Add(spawnObjectsDict[objName]);
        }
        return es;
    }

    public float GetWeight(int difficulty, List<EnemySetup> enemyList, string currentLevelName)
    {
        float weight = difficulty1Weight;
        if (difficulty == 2) weight = difficulty2Weight;
        else if (difficulty == 3) weight = difficulty3Weight;

        if(levelWeightMultipliers.ContainsKey(currentLevelName)){
            weight = (float)(weight * levelWeightMultipliers[currentLevelName]);
        }

        if (enemyList.Select(obj => obj.name).ToList().Contains(name)) {
            weight = (float)(weight * SpawnConfig.configManager.repeatMultiplier.Value);
            if (!allowDuplicates) weight = 0;
        }
        if (weight < 0.1) weight = 0;
        return weight;
    }

    public void UpdateWithDefaults(ExtendedEnemySetup defaultSetup)
    {
        PropertyInfo[] properties = defaultSetup.GetType().GetProperties();

        foreach (PropertyInfo property in properties)
        {
            object defaultValue = property.GetValue(defaultSetup);
            object customValue = property.GetValue(this);
            object newDefaultValue = property.GetValue(extendedSetups[defaultSetup.name]);

            if (defaultValue == customValue && newDefaultValue != defaultValue)
            {
                SpawnConfig.Logger.LogInfo("Updating unmodified property " + property + ": " + defaultValue + " => " + newDefaultValue);
                property.SetValue(this, newDefaultValue);
            }
        }
    }

    public bool Update()
    {
        bool changedSomething = false;

        // Migrate legacy values
        if (this.levelsCompletedCondition)
        {
            this.levelRangeCondition = true;
            this.minLevel = this.levelsCompletedMin;
            this.maxLevel = this.levelsCompletedMax;
            this.levelsCompletedCondition = false;
            changedSomething = true;
        }
        if (!this.levelRangeCondition && this.maxLevel == 10)
        {
            this.maxLevel = 0;
            changedSomething = true;
        }

        // Remove unused levels if enabled in config
        if (SpawnConfig.configManager.removeUnloadedLevelWeights.Value)
        {
            Dictionary<string, float> newTemp = [];
            bool removedEntry = false;
            foreach (KeyValuePair<string, float> kvp in levelWeightMultipliers)
            {
                if (!levelNames.Contains(kvp.Key)) {
                    changedSomething = true;
                    removedEntry = true;
                }else{
                    newTemp.Add(kvp.Key, kvp.Value);
                }
            }
            if (removedEntry) levelWeightMultipliers = newTemp;
        }
        
        // Add all loaded levels to the config
        foreach(string levelName in levelNames){
            if (!levelWeightMultipliers.ContainsKey(levelName))
            {
                levelWeightMultipliers.Add(levelName, 1.0f);
                changedSomething = true;
            }
        }

        return changedSomething;
    }
}