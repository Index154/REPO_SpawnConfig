using System.Collections.Generic;
using static SpawnConfig.ListManager;

namespace SpawnConfig.ExtendedClasses;

public class ExtendedGroupCounts
{

    public int level = 1;
    public List<GroupCountEntry> possibleGroupCounts = [];

    public ExtendedGroupCounts(int i)
    {
        level = levelNumbers[i];
        possibleGroupCounts.Add(new GroupCountEntry(i));
    }

}

public class GroupCountEntry
{
    public List<int> counts = [];
    public int weight = 1;
    public int minPlayerCount = 1;
    public int maxPlayerCount = 100;

    public GroupCountEntry(int i)
    {
        counts = [difficulty1Counts[i], difficulty2Counts[i], difficulty3Counts[i]];
    }
}