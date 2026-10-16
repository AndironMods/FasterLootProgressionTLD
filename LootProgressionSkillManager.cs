using MelonLoader;
using ModData;
using System;
using System.IO;

namespace FasterLootProgression
{
    /// <summary>
    /// Global manager for the Looting skill. Progress is stored per save slot via ModData.
    /// </summary>
    public static class LootProgressionSkillManager
    {
        private static readonly ModDataManager s_ModData = new ModDataManager("FasterLootProgression", false);

        private static LootProgressionSkill s_PlayerSkill = new LootProgressionSkill();
        private static string s_LegacyFilePath = "";
        private static bool s_PersistWarned;

        public static LootProgressionSkill PlayerSkill => s_PlayerSkill;

        public static void Initialize()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            s_LegacyFilePath = Path.Combine(basePath, "TheLongDark", "Mods", "FasterLootProgression", "skilldata.json");
        }

        /// <summary>
        /// Called after a save slot was loaded or created: replaces the in-memory skill with the slot's data.
        /// </summary>
        internal static void OnSaveSlotEntered()
        {
            try
            {
                s_PlayerSkill = new LootProgressionSkill();
                s_PersistWarned = false;

                string json = s_ModData.Load();
                if (!string.IsNullOrEmpty(json))
                {
                    s_PlayerSkill = LootProgressionSkill.LoadFromJson(json);
                    LogLoaded();
                    return;
                }

                TryMigrateLegacyData();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error loading skill data: {ex}");
            }
        }

        // The pre-1.3.0 global skilldata.json moves into the first save slot that has no data yet.
        private static void TryMigrateLegacyData()
        {
            if (!File.Exists(s_LegacyFilePath))
                return;

            s_PlayerSkill = LootProgressionSkill.LoadFromJson(File.ReadAllText(s_LegacyFilePath));

            // No active ModData slot yet (e.g. main menu): stay silent and retry on the next slot.
            if (!s_ModData.Save(s_PlayerSkill.SaveToJson()))
                return;

            File.Move(s_LegacyFilePath, s_LegacyFilePath + ".migrated", true);
            MelonLogger.Msg($"Imported old global save into this save slot");
            LogLoaded();
        }

        private static void LogLoaded()
        {
            MelonLogger.Msg($"Loaded from save: {s_PlayerSkill.CurrentPoints} points (Level {s_PlayerSkill.CurrentLevel})");
        }

        private static void Persist()
        {
            if (s_ModData.Save(s_PlayerSkill.SaveToJson()) || s_PersistWarned)
                return;

            s_PersistWarned = true;
            MelonLogger.Warning("ModData has no active save slot (autosave/quicksave?). Progress is not stored for this session.");
        }

        /// <summary>
        /// Award points for a completed container search
        /// </summary>
        public static void AwardSearchPoints(int pointValue)
        {
            int newLevel = s_PlayerSkill.AddSearchPoints(pointValue);
            Persist();

            if (newLevel > 0)
            {
                MelonLogger.Msg($"Looting skill reached level {newLevel}");
                LootLevelUpNotifier.ShowLevelUp(newLevel);
            }
        }

        public static int GetSkillLevel()
        {
            return s_PlayerSkill.CurrentLevel;
        }

        public static float GetSpeedMultiplier()
        {
            return s_PlayerSkill.GetSpeedMultiplier();
        }

        public static int GetCurrentPoints()
        {
            return s_PlayerSkill.CurrentPoints;
        }
    }
}
