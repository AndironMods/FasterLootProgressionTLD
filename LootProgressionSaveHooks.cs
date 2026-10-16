using System;
using HarmonyLib;
using Il2Cpp;

namespace FasterLootProgression
{
    // ModData builds its per-slot cache in a prefix of these methods, so our postfixes can read it safely.

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), new Type[] { typeof(string), typeof(int) })]
    public static class SaveSlot_LoadByName_Patch
    {
        [HarmonyPostfix]
        public static void Postfix() => LootProgressionSkillManager.OnSaveSlotEntered();
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), new Type[] { typeof(SaveSlotInfo) })]
    public static class SaveSlot_LoadByInfo_Patch
    {
        [HarmonyPostfix]
        public static void Postfix() => LootProgressionSkillManager.OnSaveSlotEntered();
    }

    [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.CreateSlot), new Type[] { typeof(string), typeof(SaveSlotType), typeof(uint), typeof(Episode) })]
    public static class SaveSlot_Create_Patch
    {
        [HarmonyPostfix]
        public static void Postfix() => LootProgressionSkillManager.OnSaveSlotEntered();
    }
}
