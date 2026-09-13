using HarmonyLib;
using MelonLoader;
using Il2Cpp;
using Il2CppTLD.Interactions;
using System;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(FasterLootProgression.FasterLootProgressionMod), "FasterLootProgression", "1.2.0", "Andiron")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace FasterLootProgression
{
    public class FasterLootProgressionMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            // MelonLogger.Msg("=== FasterLootProgression Mod v1.0 ===");
            MelonLogger.Msg("Loot Search Skill System Loaded!");
            
            try
            {
                LootProgressionSkillManager.Initialize();
                
                var harmony = new HarmonyLib.Harmony("com.fasterlootprogression.mod");
                harmony.PatchAll(typeof(TimedHoldInteraction_BeginHold_Patch).Assembly);
                harmony.PatchAll(typeof(Container_ShowItemsAfterSearch_Patch).Assembly);
		LootProgressionUI.ApplyPatches(harmony);
                MelonLogger.Msg("[FasterLootProgression] Harmony patches applied!");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[FasterLootProgression] Failed to apply patches: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch: When container search completes, award skill points (only once per search)
    /// Awards 1 point for ANY container search (backpack, cabin, table, locker, etc.)
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.ShowItemsAfterSearch))]
    public class Container_ShowItemsAfterSearch_Patch
    {
        // Track which container instances we've already awarded points for in this frame
        private static HashSet<int> s_ProcessedContainers = new HashSet<int>();

        [HarmonyPrefix]
        public static void Prefix(Container __instance)
        {
            try
            {
                // Get unique identifier for this container instance
                int containerHash = __instance.GetHashCode();
                
                // Only award points once per container instance
                if (!s_ProcessedContainers.Contains(containerHash))
                {
                    s_ProcessedContainers.Add(containerHash);
                    
                    // Award 1 point for this container search
                    LootProgressionSkillManager.AwardSearchPoints(1);
                    
                    // Clear processed list on next frame to allow re-searching same container
                    if (s_ProcessedContainers.Count > 100)
                    {
                        s_ProcessedContainers.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[FasterLootProgression] Error awarding points: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch: Apply speed multiplier based on skill level when search begins
    /// </summary>
    [HarmonyPatch(typeof(Il2CppTLD.Interactions.TimedHoldInteraction), "BeginHold")]
    public class TimedHoldInteraction_BeginHold_Patch
    {
        private static int s_LastLoggedFrame = -1;

        [HarmonyPrefix]
        public static void Prefix(Il2CppTLD.Interactions.TimedHoldInteraction __instance)
        {
            try
            {
                float originalHoldTime = __instance.HoldTime;
                
                if (originalHoldTime > 0)
                {
                    int skillLevel = LootProgressionSkillManager.GetSkillLevel();
                    float multiplier = LootProgressionSkillManager.GetSpeedMultiplier();
                    float fasterHoldTime = originalHoldTime * multiplier;
                    
                    __instance.HoldTime = fasterHoldTime;
                    
                    // Only log once per frame to avoid duplicate messages
                    int currentFrame = UnityEngine.Time.frameCount;
                    if (currentFrame != s_LastLoggedFrame)
                    {
                        s_LastLoggedFrame = currentFrame;
                        
                        // Show level and speed bonus
                        if (skillLevel == 1)
                        {
                            MelonLogger.Msg($"[LootProgression] Level 1: {originalHoldTime:F1}s (no bonus)");
                        }
                        else
                        {
                            int speedBonus = (skillLevel - 1) * 25; // 25% per level after level 1
                            MelonLogger.Msg($"[LootProgression] Level {skillLevel}: {originalHoldTime:F1}s → {fasterHoldTime:F1}s (+{speedBonus}% faster)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[FasterLootProgression] Error in speed patch: {ex}");
            }
        }
    }
}