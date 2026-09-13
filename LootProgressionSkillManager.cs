using MelonLoader;
using System;
using System.IO;

namespace FasterLootProgression
{
    /// <summary>
    /// Global manager for Loot Search skill
    /// Handles data persistence to JSON file
    /// </summary>
    public static class LootProgressionSkillManager
    {
        private static LootProgressionSkill s_PlayerSkill = new LootProgressionSkill();
        private static bool s_Initialized = false;
        private static string s_SaveFilePath = "";

        public static LootProgressionSkill PlayerSkill => s_PlayerSkill;

        public static void Initialize()
        {
            if (s_Initialized)
                return;

            s_Initialized = true;
            
            // Save file in The Long Dark data directory
            string basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string savePath = Path.Combine(basePath, "TheLongDark", "Mods", "FasterLootProgression");
            
            MelonLogger.Msg($"[LootProgression] Save path: {savePath}");
            
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                MelonLogger.Msg($"[LootProgression] Created save directory");
            }
            
            s_SaveFilePath = Path.Combine(savePath, "skilldata.json");
            MelonLogger.Msg($"[LootProgression] Save file: {s_SaveFilePath}");
            MelonLogger.Msg($"[LootProgression] Edit skilldata.json to set starting points (0-500)");
            
            // Load existing skill data from file
            LoadSkillData();
            
            MelonLogger.Msg("[LootProgressionSkillManager] Initialized");
            MelonLogger.Msg($"[LootProgression] Current: Level {s_PlayerSkill.CurrentLevel}, {s_PlayerSkill.CurrentPoints}/500 points");
        }

        /// <summary>
        /// Load skill data from file
        /// </summary>
        private static void LoadSkillData()
        {
            if (File.Exists(s_SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(s_SaveFilePath);
                    s_PlayerSkill = LootProgressionSkill.LoadFromJson(json);
                    MelonLogger.Msg($"[LootProgression] Loaded from file: {s_PlayerSkill.CurrentPoints} points (Level {s_PlayerSkill.CurrentLevel})");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[LootProgression] Error loading skill file: {ex}");
                    s_PlayerSkill = new LootProgressionSkill();
                }
            }
            else
            {
                // First run - create initial file with 0 points
                s_PlayerSkill = new LootProgressionSkill();
                SaveSkillData();
                MelonLogger.Msg($"[LootProgression] Created new skilldata.json (starting at Level 1)");
            }
        }

        /// <summary>
        /// Save skill data to file
        /// </summary>
        public static void SaveSkillData()
        {
            if (!s_Initialized || string.IsNullOrEmpty(s_SaveFilePath))
                return;

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(s_SaveFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    MelonLogger.Msg($"[LootProgression] Created directory: {directory}");
                }

                // Write the file
                string json = s_PlayerSkill.SaveToJson();
                File.WriteAllText(s_SaveFilePath, json);
                //MelonLogger.Msg($"[LootProgression] Saved: Level {s_PlayerSkill.CurrentLevel}, {s_PlayerSkill.CurrentPoints} points to {s_SaveFilePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgression] Error saving skill file: {ex}");
            }
        }

        /// <summary>
        /// Award points for opening a container
        /// </summary>
        public static void AwardSearchPoints(int pointValue)
        {
            if (!s_Initialized)
                Initialize();

            int newLevel = s_PlayerSkill.AddSearchPoints(pointValue);
            
            if (newLevel > 0)
            {
                // Level up occurred - always show
                MelonLogger.Msg($"[LootProgression] ★ LEVEL UP! {pointValue}pt → Level {newLevel} ★ ({s_PlayerSkill.CurrentPoints}/500 points)");
                LootLevelUpNotifier.ShowLevelUp(newLevel);
            }
            else if (s_PlayerSkill.CurrentLevel < LootProgressionSkill.MAX_LEVEL)
            {
                // Normal search - only show if not at max level
                MelonLogger.Msg($"[LootProgression] +{pointValue}pt ({s_PlayerSkill.CurrentPoints}/500)");
            }
            // else: At max level - silent (still tracking points, just no console spam)
            
            SaveSkillData();
        }

        /// <summary>
        /// Get current skill level
        /// </summary>
        public static int GetSkillLevel()
        {
            return s_PlayerSkill.CurrentLevel;
        }

        /// <summary>
        /// Get speed multiplier for current level
        /// </summary>
        public static float GetSpeedMultiplier()
        {
            return s_PlayerSkill.GetSpeedMultiplier();
        }

        /// <summary>
        /// Get current points
        /// </summary>
        public static int GetCurrentPoints()
        {
            return s_PlayerSkill.CurrentPoints;
        }

        /// <summary>
        /// Reset skill (for testing)
        /// </summary>
        public static void ResetSkill()
        {
            s_PlayerSkill = new LootProgressionSkill();
            SaveSkillData();
            MelonLogger.Msg("[LootProgressionSkillManager] Skill reset");
        }
    }
}