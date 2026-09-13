using MelonLoader;
using UnityEngine;
using Il2Cpp;

namespace FasterLootProgression
{
    /// <summary>
    /// Shows a level-up notification by calling the game's native SkillNotify.MaybeShowLevelUp()
    /// This uses the same system that real skills use - diamond + level + skill name + tier.
    /// </summary>
    public static class LootLevelUpNotifier
    {
        public static void ShowLevelUp(int newLevel)
        {
            // MelonLogger.Msg($"[LootProgression] ShowLevelUp({newLevel}) called");

            try
            {
                // Find the SkillNotify singleton (lives on the HUD)
                var skillNotify = Object.FindObjectOfType<SkillNotify>();
                if (skillNotify == null)
                {
                    MelonLogger.Error("[LootProgression] SkillNotify not found in scene");
                    return;
                }

                // Get the localized tier name (0-4 index for levels 1-5)
                var skillsManager = Object.FindObjectOfType<SkillsManager>();
                if (skillsManager == null)
                {
                    MelonLogger.Error("[LootProgression] SkillsManager not found in scene");
                    return;
                }
                
                string tierName = skillsManager.GetTierName(newLevel - 1);
                
                // Call the real game's skill level-up notification system
                // This displays the diamond + level + skill name + tier UI
                skillNotify.MaybeShowLevelUp(
                    spriteName: "Loot",                // Sprite name for the skill icon
                    header: "LOOTING",             // Skill name
                    footer: tierName,                  // Tier name (NOVICE, SKILLED, EXPERT, etc.)
                    tier: newLevel                 // Levels 1-5)
                );

                MelonLogger.Msg($"[LootProgression] Displayed skill level-up notification (level {newLevel}, tier '{tierName}')");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[LootProgression] Error showing level-up: {ex}");
            }
        }
    }
}