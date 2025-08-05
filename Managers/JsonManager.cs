using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using SpawnConfig.ExtendedClasses;

namespace SpawnConfig;

public class JsonManager
{

    public static List<ExtendedGroupCounts> GetEGCListFromJSON(string path)
    {
        List<ExtendedGroupCounts> temp = [];
        if (File.Exists(path))
        {
            string readFile = File.ReadAllText(path);
            if (readFile != null && readFile != "")
            {
                temp = JsonConvert.DeserializeObject<List<ExtendedGroupCounts>>(readFile);
            }
        }
        return temp;
    }

    public static List<ExtendedEnemySetup> GetEESListFromJSON(string path)
    {
        List<ExtendedEnemySetup> temp = [];
        if (File.Exists(path))
        {
            string readFile = File.ReadAllText(path);
            if (readFile != null && readFile != "")
            {
                temp = JsonConvert.DeserializeObject<List<ExtendedEnemySetup>>(readFile);
                if (!readFile.Contains("allowDuplicates"))
                {
                    SpawnConfig.missingProperties = true;
                }
            }
        }
        return temp;
    }

    public static string EESToJSON(List<ExtendedEnemySetup> eesList)
    {

        StringBuilder json = new();
        StringWriter sw = new(json);
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.Indented;
            writer.WriteStartArray();

            foreach (ExtendedEnemySetup ees in eesList)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("name");
                writer.WriteValue(ees.name);
                writer.WritePropertyName("levelRangeCondition");
                writer.WriteValue(ees.levelRangeCondition);
                writer.WritePropertyName("minLevel");
                writer.WriteValue(ees.minLevel);
                writer.WritePropertyName("maxLevel");
                writer.WriteValue(ees.maxLevel);
                writer.WritePropertyName("runsPlayed");
                writer.WriteValue(ees.runsPlayed);
                writer.WritePropertyName("spawnObjects");
                writer.WriteStartArray();
                foreach (string s in ees.spawnObjects)
                {
                    writer.WriteValue(s);
                }
                writer.WriteEndArray();
                writer.WritePropertyName("difficulty1Weight");
                writer.WriteValue(ees.difficulty1Weight);
                writer.WritePropertyName("difficulty2Weight");
                writer.WriteValue(ees.difficulty2Weight);
                writer.WritePropertyName("difficulty3Weight");
                writer.WriteValue(ees.difficulty3Weight);
                writer.WritePropertyName("thisGroupOnly");
                writer.WriteValue(ees.thisGroupOnly);
                writer.WritePropertyName("allowDuplicates");
                writer.WriteValue(ees.allowDuplicates);
                writer.WritePropertyName("alterAmountChance");
                writer.WriteValue(ees.alterAmountChance);
                writer.WritePropertyName("alterAmountMin");
                writer.WriteValue(ees.alterAmountMin);
                writer.WritePropertyName("alterAmountMax");
                writer.WriteValue(ees.alterAmountMax);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        return json.ToString();
    }

    public static string GroupCountsToJSON(List<ExtendedGroupCounts> gcList)
    {

        StringBuilder json = new();
        StringWriter sw = new(json);
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            writer.Formatting = Formatting.Indented;
            writer.WriteStartArray();

            foreach (ExtendedGroupCounts groupCounts in gcList)
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("level");
                writer.WriteValue(groupCounts.level);
                writer.WritePropertyName("possibleGroupCounts");
                writer.WriteStartArray();
                foreach (GroupCountEntry groupCountEntry in groupCounts.possibleGroupCounts)
                {
                    writer.WriteStartObject();
                    writer.Formatting = Formatting.None;
                    writer.WritePropertyName("counts");
                    writer.WriteStartArray();
                    writer.WriteValue(groupCountEntry.counts[0]);
                    writer.WriteValue(groupCountEntry.counts[1]);
                    writer.WriteValue(groupCountEntry.counts[2]);
                    writer.WriteEndArray();
                    writer.WritePropertyName("weight");
                    writer.WriteValue(groupCountEntry.weight);
                    writer.WriteEndObject();
                }
                writer.Formatting = Formatting.Indented;
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        return json.ToString();
    }

}