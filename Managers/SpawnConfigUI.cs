using UnityEngine;
using SpawnConfig;
using System.Collections.Generic;
using SpawnConfig.ExtendedClasses;
using System.Linq;
using static SpawnConfig.ListManager;
using SpawnConfig.Managers;

namespace SpawnConfig.Managers {
    [ExecuteAlways]
    public class SpawnConfigUI : MonoBehaviour {
        private bool showUI = false;
        private Rect windowRect = new Rect(20, 20, 560, 600);
        public static SpawnConfigUI? Instance { get; private set; }
        
        private GUIStyle? titleStyle;
        private GUIStyle? labelStyle;
        private GUIStyle? boxStyle;
        private GUIStyle? buttonStyle;
        private GUIStyle? sliderStyle;
        private bool stylesInitialized = false;
        
        private int currentTab = 0;
        private string[] tabNames = new string[] { "General", "Groups", "Manual Spawn", "Advanced" };
        
        private Vector2 groupsScrollPosition = Vector2.zero;
        private Vector2 manualSpawnScrollPosition = Vector2.zero;
        
        private string searchFilter = "";
        private string spawnSearchFilter = "";
        private List<string> filteredGroups = new List<string>();
        private List<string> filteredSpawnGroups = new List<string>();
        
        private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();
        private string selectedGroupForSpawn = "";
        private Color selectedGroupColor = Color.green;

        private bool directoryWasAvailable = false;
        private float directoryCheckTimer = 0f;
        private float directoryCheckInterval = 1f;

        void Awake() {
            SpawnConfig.Logger.LogInfo("SpawnConfigUI.Awake called");
            Instance = this;
        }

        void Start() {
            SpawnConfig.Logger.LogInfo($"SpawnConfigUI.Start called - Toggle key is {SpawnConfig.configManager.menuToggleKey.Value}");
            RefreshFilteredGroups();
            RefreshSpawnGroups();
        }

        void OnEnable() {
            SpawnConfig.Logger.LogInfo("SpawnConfigUI enabled");
        }

        void InitStyles() {
            if (Application.isPlaying && !stylesInitialized) {
                SpawnConfig.Logger.LogInfo("Initializing UI styles");
                stylesInitialized = true;
                
                titleStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                
                labelStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 14
                };
                
                boxStyle = new GUIStyle(GUI.skin.box) {
                    padding = new RectOffset(10, 10, 10, 10)
                };
                
                buttonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 14
                };
                
                sliderStyle = new GUIStyle(GUI.skin.horizontalSlider) {
                    fixedHeight = 15
                };
            }
        }

        void Update() {
            if (Input.GetKeyDown(SpawnConfig.configManager.menuToggleKey.Value)) {
                showUI = !showUI;
                SpawnConfig.Logger.LogInfo("Menu toggled: " + (showUI ? "ON" : "OFF"));
                
                Cursor.visible = showUI;
                Cursor.lockState = showUI ? CursorLockMode.None : CursorLockMode.Locked;
            }
            
            if (Input.GetKeyDown(SpawnConfig.configManager.quickSpawnKey.Value) && !string.IsNullOrEmpty(selectedGroupForSpawn)) {
                SpawnHelper.SpawnEnemyGroup(selectedGroupForSpawn);
                SpawnConfig.Logger.LogInfo($"Quick spawn: {selectedGroupForSpawn}");
            }

            directoryCheckTimer += Time.deltaTime;
            if (directoryCheckTimer >= directoryCheckInterval) {
                directoryCheckTimer = 0f;
                bool directorAvailable = SpawnHelper.TryFindEnemyDirector();
                
                if (directorAvailable != directoryWasAvailable) {
                    SpawnConfig.Logger.LogInfo("EnemyDirector availability changed: " + (directorAvailable ? "AVAILABLE" : "NOT AVAILABLE"));
                    directoryWasAvailable = directorAvailable;
                }
            }
        }

        void OnGUI() {
            if (!showUI) return;
            
            if (!stylesInitialized) {
                InitStyles();
            }
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            GUI.contentColor = Color.white;
            windowRect = GUILayout.Window(1111, windowRect, DrawWindow, "SpawnConfig v" + SpawnConfig.configVersion);
        }
        
        private void RefreshFilteredGroups() {
            filteredGroups.Clear();
            
            if (extendedSetups == null || extendedSetups.Count == 0) {
                SpawnConfig.Logger.LogWarning("No enemy groups found!");
                return;
            }
            
            string searchLower = searchFilter.ToLowerInvariant();
            foreach (var group in extendedSetups.Values) {
                if (string.IsNullOrEmpty(searchFilter) || 
                    group.name.ToLowerInvariant().Contains(searchLower)) {
                    filteredGroups.Add(group.name);
                }
            }
            
            filteredGroups.Sort();
        }
        
        private void RefreshSpawnGroups() {
            filteredSpawnGroups.Clear();
            
            if (extendedSetups == null || extendedSetups.Count == 0) {
                return;
            }
            
            string searchLower = spawnSearchFilter.ToLowerInvariant();
            foreach (var group in extendedSetups.Values) {
                if (string.IsNullOrEmpty(spawnSearchFilter) || 
                    group.name.ToLowerInvariant().Contains(searchLower)) {
                    filteredSpawnGroups.Add(group.name);
                }
            }
            
            filteredSpawnGroups.Sort();
            
            if (string.IsNullOrEmpty(selectedGroupForSpawn) && filteredSpawnGroups.Count > 0) {
                selectedGroupForSpawn = filteredSpawnGroups[0];
            }
        }

        void DrawWindow(int windowID) {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++) {
                if (GUILayout.Toggle(currentTab == i, tabNames[i], "Button", GUILayout.Height(30))) {
                    if (currentTab != i) {
                        currentTab = i;
                        if (i == 1) RefreshFilteredGroups();
                        if (i == 2) RefreshSpawnGroups();
                    }
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            switch (currentTab) {
                case 0:
                    DrawGeneralTab();
                    break;
                case 1:
                    DrawGroupsTab();
                    break;
                case 2:
                    DrawManualSpawnTab();
                    break;
                case 3:
                    DrawAdvancedTab();
                    break;
            }
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Config", GUILayout.Height(30))) {
                SpawnConfig.ReadAndUpdateJSON();
                RefreshFilteredGroups();
                RefreshSpawnGroups();
                SpawnConfig.Logger.LogInfo("Configuration reloaded");
            }
            
            if (GUILayout.Button("Save", GUILayout.Height(30))) {
                SpawnConfig.Conf.Save();
                SpawnConfig.Logger.LogInfo("Configuration saved");
            }
            
            if (GUILayout.Button("Close", GUILayout.Height(30))) {
                showUI = false;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            GUILayout.EndHorizontal();
            
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 30));
        }
        
        private void DrawGeneralTab() {
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Global Enemy Configuration", titleStyle != null ? titleStyle : GUI.skin.label);
            GUILayout.Space(10);
            
            GUILayout.Label("Repeat Multiplier: " + SpawnConfig.configManager.repeatMultiplier.Value.ToString("0.00"));
            float rate = (float)SpawnConfig.configManager.repeatMultiplier.Value;
            rate = GUILayout.HorizontalSlider(rate, 0.1f, 5f);
            if (rate != (float)SpawnConfig.configManager.repeatMultiplier.Value) {
                SpawnConfig.configManager.repeatMultiplier.Value = rate;
                SpawnConfig.Logger.LogInfo("Repeat multiplier changed: " + rate);
            }
            
            GUILayout.Space(15);
            
            GUILayout.Label("Global Spawn Multiplier: " + SpawnConfig.configManager.globalSpawnMultiplier.Value.ToString("0.00"));
            float globalRate = (float)SpawnConfig.configManager.globalSpawnMultiplier.Value;
            globalRate = GUILayout.HorizontalSlider(globalRate, 0.1f, 5f);
            if (globalRate != (float)SpawnConfig.configManager.globalSpawnMultiplier.Value) {
                SpawnConfig.configManager.globalSpawnMultiplier.Value = globalRate;
                SpawnConfig.Logger.LogInfo("Global multiplier changed: " + globalRate);
            }
            
            GUILayout.Space(15);
            
            bool preventSpawns = GUILayout.Toggle(SpawnConfig.configManager.preventSpawns.Value, 
                " Completely disable enemy spawning");
            if (preventSpawns != SpawnConfig.configManager.preventSpawns.Value) {
                SpawnConfig.configManager.preventSpawns.Value = preventSpawns;
                SpawnConfig.Logger.LogInfo("Spawn prevention: " + preventSpawns);
            }
            
            GUILayout.Space(15);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All Groups", GUILayout.Height(30), GUILayout.Width(200))) {
                SpawnHelper.EnableAllEnemyGroups(1.0f);
                SpawnConfig.Logger.LogInfo("All groups enabled");
            }
            
            if (GUILayout.Button("Disable All Groups", GUILayout.Height(30), GUILayout.Width(200))) {
                SpawnHelper.DisableAllEnemyGroups();
                SpawnConfig.Logger.LogInfo("All groups disabled");
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            GUILayout.Label("Commands:", labelStyle != null ? labelStyle : GUI.skin.label);
            GUILayout.Label($"• Menu key: {SpawnConfig.configManager.menuToggleKey.Value}");
            GUILayout.Label($"• Quick spawn key: {SpawnConfig.configManager.quickSpawnKey.Value}");
            GUILayout.Label($"• Escape: close menu");
            
            GUILayout.EndVertical();
        }
        
        private void DrawGroupsTab() {
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Enemy Group Configuration", titleStyle != null ? titleStyle : GUI.skin.label);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(70));
            string newSearch = GUILayout.TextField(searchFilter, GUILayout.ExpandWidth(true));
            if (newSearch != searchFilter) {
                searchFilter = newSearch;
                RefreshFilteredGroups();
            }
            if (GUILayout.Button("×", GUILayout.Width(25)) && !string.IsNullOrEmpty(searchFilter)) {
                searchFilter = "";
                RefreshFilteredGroups();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.Label($"Groups ({filteredGroups.Count}/{extendedSetups?.Count ?? 0}):");
            
            groupsScrollPosition = GUILayout.BeginScrollView(groupsScrollPosition, GUI.skin.box, GUILayout.ExpandHeight(true));
            
            if (filteredGroups.Count == 0) {
                GUILayout.Label("No groups found.");
            } else {
                foreach (string groupName in filteredGroups) {
                    if (!extendedSetups.TryGetValue(groupName, out ExtendedEnemySetup group)) continue;
                    
                    if (!groupFoldouts.ContainsKey(groupName)) {
                        groupFoldouts[groupName] = false;
                    }
                    
                    GUILayout.BeginVertical(GUI.skin.box);
                    
                    GUILayout.BeginHorizontal();
                    GUI.contentColor = (groupName == selectedGroupForSpawn) ? selectedGroupColor : Color.white;
                    groupFoldouts[groupName] = EditorGUILayout.Foldout(groupFoldouts[groupName], groupName);
                    GUI.contentColor = Color.white;
                    
                    if (GUILayout.Button("👁️", GUILayout.Width(25))) {
                        selectedGroupForSpawn = groupName;
                        SpawnConfig.Logger.LogInfo($"Group selected for spawn: {groupName}");
                    }
                    
                    GUILayout.EndHorizontal();
                    
                    if (groupFoldouts[groupName]) {
                        GUILayout.Space(5);
                        
                        if (group.spawnObjects.Count > 0) {
                            GUILayout.Label("Enemies in this group:");
                            foreach (string enemyName in group.spawnObjects) {
                                GUILayout.Label($"• {enemyName}");
                            }
                            GUILayout.Space(5);
                        }
                        
                        GUILayout.Label("Weights by difficulty level:");
                        
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Diff 1 (easy):", GUILayout.Width(100));
                        float diff1 = group.difficulty1Weight;
                        diff1 = GUILayout.HorizontalSlider(diff1, 0f, 10f);
                        GUILayout.Label(diff1.ToString("0.0"), GUILayout.Width(35));
                        GUILayout.EndHorizontal();
                        if (diff1 != group.difficulty1Weight) {
                            group.difficulty1Weight = diff1;
                            SpawnConfig.Logger.LogInfo($"Modified {groupName} diff1: {diff1}");
                        }
                        
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Diff 2 (medium):", GUILayout.Width(100));
                        float diff2 = group.difficulty2Weight;
                        diff2 = GUILayout.HorizontalSlider(diff2, 0f, 10f);
                        GUILayout.Label(diff2.ToString("0.0"), GUILayout.Width(35));
                        GUILayout.EndHorizontal();
                        if (diff2 != group.difficulty2Weight) {
                            group.difficulty2Weight = diff2;
                            SpawnConfig.Logger.LogInfo($"Modified {groupName} diff2: {diff2}");
                        }
                        
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Diff 3 (hard):", GUILayout.Width(100));
                        float diff3 = group.difficulty3Weight;
                        diff3 = GUILayout.HorizontalSlider(diff3, 0f, 10f);
                        GUILayout.Label(diff3.ToString("0.0"), GUILayout.Width(35));
                        GUILayout.EndHorizontal();
                        if (diff3 != group.difficulty3Weight) {
                            group.difficulty3Weight = diff3;
                            SpawnConfig.Logger.LogInfo($"Modified {groupName} diff3: {diff3}");
                        }
                        
                        GUILayout.Space(5);
                        
                        bool exclusiveGroup = GUILayout.Toggle(group.thisGroupOnly, " This group only");
                        if (exclusiveGroup != group.thisGroupOnly) {
                            group.thisGroupOnly = exclusiveGroup;
                            SpawnConfig.Logger.LogInfo($"Modified {groupName} exclusive: {exclusiveGroup}");
                        }
                        
                        GUILayout.Space(10);
                        
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Spawn", GUILayout.Height(25), GUILayout.Width(120))) {
                            if (SpawnHelper.SpawnEnemyGroup(groupName)) {
                                GUIUtility.ExitGUI();
                            }
                        }
                        if (GUILayout.Button("Disable", GUILayout.Height(25), GUILayout.Width(120))) {
                            group.difficulty1Weight = 0;
                            group.difficulty2Weight = 0;
                            group.difficulty3Weight = 0;
                        }
                        GUILayout.EndHorizontal();
                        
                        GUILayout.Space(5);
                    }
                    
                    GUILayout.EndVertical();
                }
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
        }
        
        private void DrawManualSpawnTab() {
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Manual Enemy Spawning", titleStyle != null ? titleStyle : GUI.skin.label);
            
            bool directorAvailable = SpawnHelper.currentEnemyDirector != null;
            GUI.backgroundColor = directorAvailable ? new Color(0.1f, 0.6f, 0.1f) : new Color(0.6f, 0.1f, 0.1f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Enemy Director: {(directorAvailable ? "AVAILABLE" : "NOT AVAILABLE")}", 
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            
            if (!directorAvailable) {
                GUILayout.Label("Tips to enable the enemy director:", GUI.skin.label);
                GUILayout.Label("1. Make sure you are in-game (in a level)");
                GUILayout.Label("2. Try starting a new game");
                GUILayout.Label("3. Wait for enemies to start appearing naturally");
                
                if (GUILayout.Button("Search for director now", GUILayout.Height(30))) {
                    bool found = SpawnHelper.TryFindEnemyDirector();
                    SpawnConfig.Logger.LogInfo(found ? "Director found!" : "Director not found");
                }
            }
            GUILayout.EndVertical();
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            
            GUILayout.Space(10);
            
            if (!directorAvailable) {
                GUILayout.Label("Manual spawning is not available without enemy director");
                GUILayout.EndVertical();
                return;
            }
            
            GUILayout.Label("Select an enemy group and spawn it near the player.");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(70));
            string newSpawnSearch = GUILayout.TextField(spawnSearchFilter, GUILayout.ExpandWidth(true));
            if (newSpawnSearch != spawnSearchFilter) {
                spawnSearchFilter = newSpawnSearch;
                RefreshSpawnGroups();
            }
            
            if (GUILayout.Button("×", GUILayout.Width(25)) && !string.IsNullOrEmpty(spawnSearchFilter)) {
                spawnSearchFilter = "";
                RefreshSpawnGroups();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.contentColor = selectedGroupColor;
            GUILayout.Label("Selected group: " + selectedGroupForSpawn);
            GUI.contentColor = Color.white;
            
            if (!string.IsNullOrEmpty(selectedGroupForSpawn) && extendedSetups.TryGetValue(selectedGroupForSpawn, out ExtendedEnemySetup selectedGroup)) {
                GUILayout.Label($"Contains {selectedGroup.spawnObjects.Count} enemies");
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("SPAWN NOW", GUILayout.Height(40))) {
                    if (SpawnHelper.SpawnEnemyGroup(selectedGroupForSpawn)) {
                        GUIUtility.ExitGUI();
                    }
                }
                
                if (GUILayout.Button("Quick spawn: " + SpawnConfig.configManager.quickSpawnKey.Value, GUILayout.Height(40))) {
                    SpawnConfig.configManager.quickSpawnKey.Value = KeyCode.F9;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(10);
            
            GUILayout.Label($"Available groups ({filteredSpawnGroups.Count}):");
            
            manualSpawnScrollPosition = GUILayout.BeginScrollView(manualSpawnScrollPosition, GUI.skin.box, GUILayout.Height(250));
            
            foreach (string groupName in filteredSpawnGroups) {
                GUILayout.BeginHorizontal(GUI.skin.box);
                
                GUI.contentColor = (groupName == selectedGroupForSpawn) ? selectedGroupColor : Color.white;
                if (GUILayout.Button(groupName, GUILayout.ExpandWidth(true), GUILayout.Height(30))) {
                    selectedGroupForSpawn = groupName;
                }
                GUI.contentColor = Color.white;
                
                if (GUILayout.Button("Spawn", GUILayout.Width(60), GUILayout.Height(30))) {
                    if (SpawnHelper.SpawnEnemyGroup(groupName)) {
                        GUIUtility.ExitGUI();
                    }
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
        }
        
        private void DrawAdvancedTab() {
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Advanced Options", titleStyle != null ? titleStyle : GUI.skin.label);
            GUILayout.Space(10);
            
            bool addMissing = GUILayout.Toggle(SpawnConfig.configManager.addMissingGroups.Value, 
                " Automatically add missing groups at startup");
            if (addMissing != SpawnConfig.configManager.addMissingGroups.Value) {
                SpawnConfig.configManager.addMissingGroups.Value = addMissing;
            }
            
            bool ignoreInvalid = GUILayout.Toggle(SpawnConfig.configManager.ignoreInvalidGroups.Value, 
                " Ignore groups with invalid spawn objects");
            if (ignoreInvalid != SpawnConfig.configManager.ignoreInvalidGroups.Value) {
                SpawnConfig.configManager.ignoreInvalidGroups.Value = ignoreInvalid;
            }
            
            GUILayout.Space(15);
            
            GUILayout.Label("Debug Information:");
            if (SpawnHelper.currentEnemyDirector != null) {
                GUILayout.Label("• Enemy Director: Active");
                GUILayout.Label($"• Loaded groups: {extendedSetups?.Count ?? 0}");
                GUILayout.Label($"• Active enemies: {SpawnHelper.currentEnemyDirector.enemyList?.Count ?? 0}");
            } else {
                GUILayout.Label("• Enemy Director: Not available");
            }
            
            GUILayout.Space(15);
            
            GUILayout.Label("Configuration file paths:");
            GUILayout.Label("• " + SpawnConfig.spawnGroupsCfg);
            
            GUILayout.EndVertical();
        }
    }
    
    public static class EditorGUILayout
    {
        private static Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        
        public static bool Foldout(bool foldout, string content)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(foldout ? "▼ " : "► ", GUILayout.Width(25)))
            {
                foldout = !foldout;
            }
            if (GUILayout.Button(content, GUI.skin.label, GUILayout.ExpandWidth(true)))
            {
                foldout = !foldout;
            }
            GUILayout.EndHorizontal();
            return foldout;
        }
    }
}