namespace SpawnConfig.ExtendedClasses;
public class Explanations
{

    public static string groupsPerLevelExplanation = @"[
  {
    ""level"": 1,
    ""possibleGroupCounts"": [
      {""counts"":[2,0,1],""weight"":1}
    ]
  },
  {
    ""level"": 4,
    ""possibleGroupCounts"": [
      {""counts"":[1,2,2],""weight"":8},
      {""counts"":[1,0,3],""weight"":6},
      {""counts"":[3,3,2],""weight"":1}
    ]
  }
]

Above you can see a valid example of a GroupsPerLevel config featuring custom settings for levels 1 and 4. Each level in the file must have at least one entry in the possibleGroupCounts list. The ""counts"" property of each of these entries contains the number of enemy groups to spawn for the three difficulty tiers (in ascending order). For example, the entry for level 1 here has two difficulty 1 groups, zero difficulty two groups and one difficulty 3 group.
When you enter a level the game will randomly pick one of the possibleGroupCounts that is configured for it. Entries with a higher weight are more likely to be selected.

Further info:
- The levels must be configured in ascending order
- If you don't configure anything for a level then the game will simply use the settings of the previous level. In the above example levels 2 and 3 would both automatically end up having the same possible group counts as level 1
- The entry for level 1 is required!";

}