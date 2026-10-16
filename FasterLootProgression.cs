using HarmonyLib;
using MelonLoader;
using Il2Cpp;
using Il2CppTLD.Interactions;
using System;
using System.Collections.Generic;
using UnityEngine;

[assembly: MelonInfo(typeof(FasterLootProgression.FasterLootProgressionMod), "FasterLootProgression", FasterLootProgression.BuildInfo.Version, "Andiron")]
[assembly: MelonGame("Hinterland", "TheLongDark")]
[assembly: MelonAdditionalDependencies("ModData")]

namespace FasterLootProgression
{
    internal static class BuildInfo
    {
        public const string Version = "1.3.0";
    }

    public class FasterLootProgressionMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            // All [HarmonyPatch] classes in this assembly are applied by MelonLoader itself.
            // Calling PatchAll manually would stack every patch a second/third time.
            LootProgressionSkillManager.Initialize();
            LootProgressionUI.LoadIcons();
            LoggerInstance.Msg("*** LootProgression Mod initialized ***");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ContainerHoldPatch.ResetState();
            ContainerSearchPatch.ResetState();
        }
    }

    /// <summary>
    /// Awards 1 skill point per completed container search (once per container instance and scene).
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.ShowItemsAfterSearch))]
    public static class ContainerSearchPatch
    {
        private static readonly HashSet<int> s_Awarded = new HashSet<int>();

        internal static void ResetState() => s_Awarded.Clear();

        [HarmonyPrefix]
        public static void Prefix(Container __instance)
        {
            try
            {
                if (s_Awarded.Add(__instance.GetInstanceID()))
                    LootProgressionSkillManager.AwardSearchPoints(1);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error awarding points: {ex}");
            }
        }
    }

    /// <summary>
    /// Shortens the hold time of looting interactions (containers, safes, locks, rock caches)
    /// according to the skill level. TimedHoldInteraction is also the base of doors, loading
    /// zones, harvesting, repairs etc., so the patch bails out for everything else.
    /// </summary>
    [HarmonyPatch(typeof(TimedHoldInteraction), "BeginHold")]
    public static class ContainerHoldPatch
    {
        private const float MinHoldTime = 0.05f;

        // BeginHold can run several times on the same instance (cancel + retry), so remember the
        // untouched value per instance and always scale that, never our own previous result.
        private static readonly Dictionary<int, (float original, float applied)> s_State =
            new Dictionary<int, (float original, float applied)>();

        internal static void ResetState() => s_State.Clear();

        [HarmonyPrefix]
        public static void Prefix(TimedHoldInteraction __instance)
        {
            try
            {
                // LockedInteraction is the base of containers, safes and simple locks.
                if (__instance.TryCast<LockedInteraction>() == null && __instance.TryCast<RockCacheInteraction>() == null)
                    return;

                float current = __instance.HoldTime;
                int id = __instance.GetInstanceID();

                float original = current;
                if (s_State.TryGetValue(id, out var state) && Mathf.Approximately(current, state.applied))
                    original = state.original;

                if (original <= 0f)
                    return;

                float multiplier = LootProgressionSkillManager.GetSpeedMultiplier();
                float target = Mathf.Min(original, Mathf.Max(original * multiplier, MinHoldTime));

                s_State[id] = (original, target);
                if (!Mathf.Approximately(current, target))
                    __instance.HoldTime = target;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in speed patch: {ex}");
            }
        }
    }
}
