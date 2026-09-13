using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;

namespace FasterLootProgression
{
    public static class LootProgressionUI
    {
        internal const string INJECTED_OBJECT_NAME = "LootProgressionSkillItem_Injected";

        internal static SkillListItem InjectedItem;
        internal static bool InjectedSelected;

        internal static void OnInjectedRowClicked(Panel_Log panel)
        {
            try
            {
                InjectedSelected = true;

                // NOTE: we must NOT touch m_SkillListSelectedIndex here - setting it to
                var list = panel.m_SkillsDisplayList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var it = list[i];
                        if (it != null) it.SetSelected(false);
                    }
                }

                if (InjectedItem != null)
                {
                    InjectedItem.SetSelected(true);
                    if (IconSelected != null)
                        SetRowIcon(InjectedItem, IconSelected);
                }

                FillDetailPane(panel);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] OnInjectedRowClicked error: {ex}");
            }
        }

        internal static UnityEngine.Texture CachedMendingTexture;
        internal static UnityEngine.Texture LastSeenLargeTexture;

        internal static Texture2D IconNormal;
        internal static Texture2D IconSelected;

        private const int ICON_TARGET_SIZE = 52;

        internal static Texture2D LoadEmbeddedTexture(string resourceName)
        {
            try
            {
                var asm = typeof(LootProgressionUI).Assembly;
                using (var stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        MelonLogger.Error($"[LootProgressionUI] Embedded resource not found: '{resourceName}'. "
                            + "Available resources:");
                        foreach (var name in asm.GetManifestResourceNames())
                            MelonLogger.Error($"[LootProgressionUI]   - {name}");
                        return null;
                    }

                    byte[] pngBytes = new byte[stream.Length];
                    stream.Read(pngBytes, 0, pngBytes.Length);

                    if (!MiniPngDecoder.TryDecode(pngBytes, out int width, out int height, out Color32[] pixels))
                    {
                        MelonLogger.Error($"[LootProgressionUI] Failed to decode PNG '{resourceName}'.");
                        return null;
                    }

                    int outW = ICON_TARGET_SIZE, outH = ICON_TARGET_SIZE;
                    Color32[] outPixels = ResizeNearest(pixels, width, height, outW, outH);

                    var tex = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
                    tex.hideFlags = HideFlags.HideAndDontSave;
                    tex.SetPixels32(outPixels);
                    tex.Apply();

                    return tex;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] LoadEmbeddedTexture error ('{resourceName}'): {ex}");
                return null;
            }
        }

        private static Color32[] ResizeNearest(Color32[] src, int srcW, int srcH, int dstW, int dstH)
        {
            var dst = new Color32[dstW * dstH];
            for (int y = 0; y < dstH; y++)
            {
                int sy = y * srcH / dstH;
                for (int x = 0; x < dstW; x++)
                {
                    int sx = x * srcW / dstW;
                    dst[y * dstW + x] = src[sy * srcW + sx];
                }
            }
            return dst;
        }


        private static readonly string[] BenefitBoxes =
        {
            "Basic looting skill",
            "+25% faster looting",
            "+50% faster looting",
            "+75% faster looting",
            "+100% faster looting (instant)",
        };

        internal static void FillDetailPane(Panel_Log panel)
        {
            int level = Mathf.Clamp(CurrentLevel, 1, LootProgressionSkill.MAX_LEVEL);

            if (panel.m_SkillName != null)
                panel.m_SkillName.text = "LOOTING";
            if (panel.m_SkillLevelIconLargeLabel != null)
                panel.m_SkillLevelIconLargeLabel.text = level.ToString();
            if (panel.m_SkillLevelName != null)
                panel.m_SkillLevelName.text = TierNameForLevel(level);
            if (panel.m_SkillDescription != null)
                panel.m_SkillDescription.text = "Searching loot will update this skill.";

            if (panel.m_SkillImageLarge != null)
            {
                var tex = CachedMendingTexture != null ? CachedMendingTexture : LastSeenLargeTexture;
                if (tex != null)
                    panel.m_SkillImageLarge.mainTexture = tex;
            }

            FillBenefitBoxes(panel, level);
        }

        private const int BOX_SPRITE_W = 408;
        private const int BOX_SPRITE_H = 30;
        private const int BOX_LABEL_W = 400;
        private const int BOX_LABEL_H = 18;
        private const float LABEL_LOCAL_Y = -17f;
        private const float BOX_FIRST_OFFSET_Y = -8f;
        private const float BOX_STRIDE_Y = 33f;
        private static bool _boxAnchorCached;
        private static float _cachedBaseX;
        private static float _cachedBaseY;

        internal static void FillBenefitBoxes(Panel_Log panel, int count)
        {
            try
            {
                var lines = panel.m_SkillBenefitLines;
                if (lines == null) return;

                var prefab = panel.m_SkillBenefitPrefab;
                var startDummy = panel.m_SkillBenefitsStartDummy;

                if (count > BenefitBoxes.Length) count = BenefitBoxes.Length;
                if (count < 0) count = 0;

                Transform parent = null;
                if (lines.Count > 0 && lines[0] != null)
                    parent = lines[0].transform.parent;
                else if (prefab != null)
                    parent = prefab.transform.parent;

                GameObject cloneSource = (prefab != null) ? prefab
                    : (lines.Count > 0 ? lines[0] : null);

                int cap = Panel_Log.MAX_SKILL_BENEFITS;
                while (lines.Count < count && lines.Count < cap && cloneSource != null && parent != null)
                {
                    var extra = UnityEngine.Object.Instantiate(cloneSource, parent, false);
                    lines.Add(extra);
                }

                if (!_boxAnchorCached)
                {
                    _cachedBaseY = (startDummy != null)
                        ? startDummy.transform.localPosition.y + BOX_FIRST_OFFSET_Y
                        : -70f;
                    _cachedBaseX = (startDummy != null) ? startDummy.transform.localPosition.x : 179f;
                    _boxAnchorCached = true;
                }
                float baseY = _cachedBaseY;
                float baseX = _cachedBaseX;

                for (int i = 0; i < lines.Count; i++)
                {
                    var go = lines[i];
                    if (go == null) continue;

                    if (i < count && i < BenefitBoxes.Length)
                    {
                        go.SetActive(true);

                        var sprite = go.GetComponentInChildren<UISprite>(true);
                        if (sprite != null) { sprite.width = BOX_SPRITE_W; sprite.height = BOX_SPRITE_H; }

                        var label = go.GetComponentInChildren<UILabel>(true);
                        if (label != null)
                        {
                            label.width = BOX_LABEL_W;
                            label.height = BOX_LABEL_H;
                            label.text = BenefitBoxes[i];
                            label.alignment = NGUIText.Alignment.Center;
                            
                            var lt = label.transform;
                            lt.localPosition = new Vector3(0f, LABEL_LOCAL_Y, lt.localPosition.z);
                            
                            var uiWidget = label.GetComponent<UIWidget>();
                            if (uiWidget != null)
                                uiWidget.pivot = UIWidget.Pivot.Center;
                        }

                        go.transform.localPosition = new Vector3(baseX, baseY - (BOX_STRIDE_Y * i), 0f);
                    }
                    else
                    {
                        go.SetActive(false);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] FillBenefitBoxes error: {ex}");
            }
        }

        public static void ApplyPatches(HarmonyLib.Harmony harmony)
        {
            try
            {
                harmony.PatchAll(typeof(LootProgressionUI).Assembly);
                MelonLogger.Msg("[LootProgressionUI] Skills-tab UI patches applied!");

                IconNormal = LoadEmbeddedTexture("FasterLootProgression.Resources.two-arrows-up1.png");
                IconSelected = LoadEmbeddedTexture("FasterLootProgression.Resources.two-arrows-up2.png");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] Failed to apply UI patches: {ex}");
            }
        }


        internal static int CurrentLevel => LootProgressionSkillManager.GetSkillLevel();

        internal static int CurrentPoints => LootProgressionSkillManager.GetCurrentPoints();

        internal static float NormalizedProgress
        {
            get
            {
                int points = CurrentPoints;
                int level = CurrentLevel;

                int lower, upper;
                switch (level)
                {
                    case 1: lower = LootProgressionSkill.LEVEL_1_THRESHOLD; upper = LootProgressionSkill.LEVEL_2_THRESHOLD; break;
                    case 2: lower = LootProgressionSkill.LEVEL_2_THRESHOLD; upper = LootProgressionSkill.LEVEL_3_THRESHOLD; break;
                    case 3: lower = LootProgressionSkill.LEVEL_3_THRESHOLD; upper = LootProgressionSkill.LEVEL_4_THRESHOLD; break;
                    case 4: lower = LootProgressionSkill.LEVEL_4_THRESHOLD; upper = LootProgressionSkill.LEVEL_5_THRESHOLD; break;
                    default: return 1f;
                }

                int span = upper - lower;
                if (span <= 0) return 0f;
                return Mathf.Clamp01((points - lower) / (float)span);
            }
        }

        internal static UIAtlas InjectedIconAtlas;
        internal static UISprite InjectedIconSprite;
        private const string CUSTOM_ICON_SPRITE_NAME = "LootProgressionCustomIcon";

        private const int ICON_DISPLAY_SIZE = 26;

        internal static void EnsureCustomIconAtlas(SkillListItem item)
        {
            if (item == null || item.gameObject == null || InjectedIconAtlas != null)
                return;

            try
            {
                var sprites = item.gameObject.GetComponentsInChildren<UISprite>(true);
                if (sprites == null) return;

                UISprite iconSprite = null;
                foreach (var sp in sprites)
                {
                    if (sp != null && sp.gameObject != null && sp.gameObject.name == "Icon")
                    {
                        iconSprite = sp;
                        break;
                    }
                }
                if (iconSprite == null)
                {
                    MelonLogger.Error("[LootProgressionUI] Could not find row's \"Icon\" sprite.");
                    return;
                }

                int width = iconSprite.width;
                int height = iconSprite.height;

                var atlasGO = new GameObject("LootProgressionIconAtlas");
                atlasGO.transform.SetParent(iconSprite.transform, false);

                var atlas = atlasGO.AddComponent<UIAtlas>();
                var mat = new Material(iconSprite.material);
                atlas.spriteMaterial = mat;

                var spriteData = new UISpriteData();
                spriteData.name = CUSTOM_ICON_SPRITE_NAME;
                spriteData.x = 0;
                spriteData.y = 0;
                spriteData.width = width;
                spriteData.height = height;
                atlas.spriteList.Add(spriteData);

                iconSprite.atlas = atlas;
                iconSprite.spriteName = CUSTOM_ICON_SPRITE_NAME;
                iconSprite.pivot = UIWidget.Pivot.Center;
                iconSprite.width = ICON_DISPLAY_SIZE;
                iconSprite.height = ICON_DISPLAY_SIZE;

                InjectedIconAtlas = atlas;
                InjectedIconSprite = iconSprite;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] EnsureCustomIconAtlas error: {ex}");
            }
        }

        internal static void SetRowIcon(SkillListItem item, Texture2D tex)
        {
            if (item == null || tex == null)
                return;

            try
            {
                EnsureCustomIconAtlas(item);
                if (InjectedIconAtlas != null && InjectedIconAtlas.spriteMaterial != null)
                {
                    InjectedIconAtlas.spriteMaterial.mainTexture = tex;

                    if (InjectedIconAtlas.spriteList.Count > 0)
                    {
                        var sd = InjectedIconAtlas.spriteList[0];
                        sd.width = tex.width;
                        sd.height = tex.height;
                    }
                    if (InjectedIconSprite != null)
                        InjectedIconSprite.MarkAsChanged();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] SetRowIcon error: {ex}");
            }
        }

        internal static void PaintRow(SkillListItem item)
        {
            if (item == null) return;

            int level = CurrentLevel;
            bool maxed = level >= LootProgressionSkill.MAX_LEVEL;

            item.m_Skill = null;
            item.SetSkillLevel(level);

            try
            {
                var lvlLabel = item.m_SkillLevel;
                if (lvlLabel != null)
                    lvlLabel.text = level.ToString();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] level label override error: {ex}");
            }

            item.SetProgressBarName("Looting");
            item.SetSkillPoints(string.Empty);

            if (maxed)
            {
                item.EnableProgressBar(false);
            }
            else
            {
                item.EnableProgressBar(true);
                item.SetProgress(NormalizedProgress);
            }
        }

        private static readonly string[] FallbackTierNames =
            { "NOVICE", "INTERMEDIATE", "ADVANCED", "EXPERT", "MASTER" };

        internal static string TierNameForLevel(int level)
        {
            int idx = Mathf.Clamp(level - 1, 0, FallbackTierNames.Length - 1);

            try
            {
                var gm = GameManager.GetSkillsManager();
                if (gm != null)
                {
                    string native = gm.GetTierName(idx);
                    if (!string.IsNullOrEmpty(native))
                        return native;
                }
            }
            catch
            {
            }

            return FallbackTierNames[idx];
        }

        internal static bool IsInjected()
        {
            return InjectedItem != null && InjectedItem.gameObject != null;
        }

        internal static void RemoveInjectedRow(Panel_Log panel)
        {
            try
            {
                Transform parent = null;

                var list = panel.m_SkillsDisplayList;
                if (list != null && list.Count > 0)
                {
                    var first = list[0];
                    if (first != null && first.gameObject != null)
                        parent = first.gameObject.transform.parent;
                }

                if (parent == null && InjectedItem != null && InjectedItem.gameObject != null)
                    parent = InjectedItem.gameObject.transform.parent;

                if (parent != null)
                {
                    for (int c = parent.childCount - 1; c >= 0; c--)
                    {
                        var child = parent.GetChild(c);
                        if (child != null && child.gameObject != null
                            && child.gameObject.name == INJECTED_OBJECT_NAME)
                        {
                            UnityEngine.Object.Destroy(child.gameObject);
                        }
                    }
                }

                InjectedItem = null;
                InjectedSelected = false;
                InjectedIconAtlas = null;
                InjectedIconSprite = null;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] RemoveInjectedRow error: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Panel_Log), nameof(Panel_Log.Disable))]
    public static class Panel_Log_Disable_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Panel_Log __instance)
        {
            try
            {
                LootProgressionUI.RemoveInjectedRow(__instance);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] Disable postfix error: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Panel_Log), nameof(Panel_Log.BuildSkillsList))]
    public static class Panel_Log_BuildSkillsList_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Panel_Log __instance)
        {
            try
            {
                var list = __instance.m_SkillsDisplayList;
                if (list == null || list.Count == 0)
                    return;

                LootProgressionUI.RemoveInjectedRow(__instance);

                if (list.Count == 0)
                    return;

                SkillListItem template = list[list.Count - 1];
                if (template == null || template.gameObject == null)
                    return;

                GameObject templateGO = template.gameObject;
                Transform parent = templateGO.transform.parent;

                float spacing = 65f;
                if (list.Count >= 2)
                {
                    var a = list[list.Count - 2];
                    var b = list[list.Count - 1];
                    if (a != null && b != null && a.gameObject != null && b.gameObject != null)
                    {
                        float dy = a.gameObject.transform.localPosition.y
                                   - b.gameObject.transform.localPosition.y;
                        if (Mathf.Abs(dy) > 0.01f)
                            spacing = Mathf.Abs(dy);
                    }
                }
                Vector3 lastNativePos = template.gameObject.transform.localPosition;

                GameObject clone = UnityEngine.Object.Instantiate(templateGO, parent, false);
                clone.name = LootProgressionUI.INJECTED_OBJECT_NAME;

                SkillListItem item = clone.GetComponent<SkillListItem>();
                if (item == null)
                {
                    UnityEngine.Object.Destroy(clone);
                    return;
                }

                // CRITICAL: do NOT add our clone to m_SkillsDisplayList. Native's row
                item.m_Index = list.Count;
                LootProgressionUI.InjectedItem = item;
                LootProgressionUI.PaintRow(item);
                item.SetSelected(false);

                if (LootProgressionUI.IconNormal != null)
                    LootProgressionUI.SetRowIcon(item, LootProgressionUI.IconNormal);

                item.m_ClickedDelegate = (Action<int>)((int idx) =>
                {
                    try { LootProgressionUI.OnInjectedRowClicked(__instance); }
                    catch (Exception ex) { MelonLogger.Error($"[LootProgressionUI] click handler error: {ex}"); }
                });

                clone.SetActive(true);

                clone.transform.localPosition = new Vector3(
                    lastNativePos.x, lastNativePos.y - spacing, lastNativePos.z);
                clone.transform.SetAsLastSibling();

                if (LootProgressionUI.CachedMendingTexture == null)
                {
                    try
                    {
                        int mendingIndex = -1;
                        for (int mi = 0; mi < list.Count; mi++)
                        {
                            var it = list[mi];
                            if (it != null && it.m_Skill != null
                                && it.m_Skill.m_SkillType == SkillType.ClothingRepair)
                            {
                                mendingIndex = mi;
                                break;
                            }
                        }
                        if (mendingIndex >= 0)
                        {
                            int originalIndex = __instance.m_SkillListSelectedIndex;
                            __instance.m_SkillListSelectedIndex = mendingIndex;
                            __instance.RefreshSelectedSkillDescriptionView();
                            __instance.m_SkillListSelectedIndex = originalIndex;
                            __instance.RefreshSelectedSkillDescriptionView();
                        }
                    }
                    catch (Exception wex)
                    {
                        MelonLogger.Error($"[LootProgressionUI] Mending warmup error: {wex}");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] BuildSkillsList postfix error: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Panel_Log), nameof(Panel_Log.RefreshSkillsList))]
    public static class Panel_Log_RefreshSkillsList_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Panel_Log __instance)
        {
            try
            {
                if (!LootProgressionUI.IsInjected())
                    return;

                var item = LootProgressionUI.InjectedItem;

                if (item.gameObject != null && !item.gameObject.activeSelf)
                    item.gameObject.SetActive(true);

                LootProgressionUI.PaintRow(item);

                if (item.gameObject != null)
                    item.gameObject.transform.SetAsLastSibling();
                item.SetSelected(LootProgressionUI.InjectedSelected);

                if (LootProgressionUI.InjectedSelected && LootProgressionUI.IconSelected != null)
                    LootProgressionUI.SetRowIcon(item, LootProgressionUI.IconSelected);
                else if (!LootProgressionUI.InjectedSelected && LootProgressionUI.IconNormal != null)
                    LootProgressionUI.SetRowIcon(item, LootProgressionUI.IconNormal);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] RefreshSkillsList postfix error: {ex}");
            }
        }
    }


    [HarmonyPatch(typeof(Panel_Log), nameof(Panel_Log.RefreshSelectedSkillDescriptionView))]
    public static class Panel_Log_RefreshSelectedSkillDescriptionView_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Panel_Log __instance)
        {
            try
            {
                if (__instance.m_SkillImageLarge != null && __instance.m_SkillImageLarge.mainTexture != null)
                {
                    LootProgressionUI.LastSeenLargeTexture = __instance.m_SkillImageLarge.mainTexture;

                    var list = __instance.m_SkillsDisplayList;
                    int sel = __instance.m_SkillListSelectedIndex;
                    if (list != null && sel >= 0 && sel < list.Count)
                    {
                        var selItem = list[sel];
                        if (selItem != null && selItem.m_Skill != null
                            && selItem.m_Skill.m_SkillType == SkillType.ClothingRepair)
                        {
                            LootProgressionUI.CachedMendingTexture = __instance.m_SkillImageLarge.mainTexture;
                        }
                    }
                }

                if (LootProgressionUI.InjectedSelected)
                {
                    LootProgressionUI.InjectedSelected = false;
                    if (LootProgressionUI.IsInjected())
                    {
                        LootProgressionUI.InjectedItem.SetSelected(false);
                        if (LootProgressionUI.IconNormal != null)
                            LootProgressionUI.SetRowIcon(LootProgressionUI.InjectedItem, LootProgressionUI.IconNormal);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] RefreshSelectedSkillDescriptionView postfix error: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Panel_Log), nameof(Panel_Log.UpdateSkillListItemsColor))]
    public static class Panel_Log_UpdateSkillListItemsColor_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Panel_Log __instance)
        {
            try
            {
                if (!LootProgressionUI.IsInjected())
                    return;

                LootProgressionUI.InjectedItem.SetSelected(LootProgressionUI.InjectedSelected);

                if (LootProgressionUI.InjectedSelected)
                {
                    var list = __instance.m_SkillsDisplayList;
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var it = list[i];
                            if (it != null) it.SetSelected(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgressionUI] UpdateSkillListItemsColor postfix error: {ex}");
            }
        }
    }

    internal static class MiniPngDecoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static bool TryDecode(byte[] png, out int width, out int height, out Color32[] pixels)
        {
            width = 0; height = 0; pixels = null;

            try
            {
                if (png == null || png.Length < 8) return false;
                for (int i = 0; i < 8; i++)
                    if (png[i] != Signature[i]) { MelonLogger.Error("[MiniPngDecoder] Bad PNG signature."); return false; }

                int pos = 8;
                int bitDepth = 0, colorType = 0, interlace = 0;
                byte[] palette = null;
                byte[] trns = null;
                using (var idat = new MemoryStream())
                {
                    while (pos + 8 <= png.Length)
                    {
                        int len = ReadBE32(png, pos); pos += 4;
                        string type = System.Text.Encoding.ASCII.GetString(png, pos, 4); pos += 4;
                        int dataStart = pos;

                        if (type == "IHDR")
                        {
                            width = ReadBE32(png, dataStart);
                            height = ReadBE32(png, dataStart + 4);
                            bitDepth = png[dataStart + 8];
                            colorType = png[dataStart + 9];
                            interlace = png[dataStart + 12];
                        }
                        else if (type == "PLTE")
                        {
                            palette = new byte[len];
                            Array.Copy(png, dataStart, palette, 0, len);
                        }
                        else if (type == "tRNS")
                        {
                            trns = new byte[len];
                            Array.Copy(png, dataStart, trns, 0, len);
                        }
                        else if (type == "IDAT")
                        {
                            idat.Write(png, dataStart, len);
                        }
                        else if (type == "IEND")
                        {
                            break;
                        }

                        pos += len;
                        pos += 4;
                    }

                    if (width <= 0 || height <= 0)
                    {
                        MelonLogger.Error("[MiniPngDecoder] Missing/invalid IHDR.");
                        return false;
                    }
                    if (interlace != 0)
                    {
                        MelonLogger.Error("[MiniPngDecoder] Interlaced PNGs not supported.");
                        return false;
                    }

                    int channels;
                    switch (colorType)
                    {
                        case 0: channels = 1; break;
                        case 2: channels = 3; break;
                        case 3: channels = 1; break;
                        case 4: channels = 2; break;
                        case 6: channels = 4; break;
                        default:
                            MelonLogger.Error($"[MiniPngDecoder] Unsupported color type {colorType}.");
                            return false;
                    }

                    bool subByteAllowed = (colorType == 0 || colorType == 3);
                    bool validDepth = bitDepth == 8
                        || (subByteAllowed && (bitDepth == 1 || bitDepth == 2 || bitDepth == 4));
                    if (!validDepth)
                    {
                        MelonLogger.Error($"[MiniPngDecoder] Unsupported bit depth {bitDepth} "
                            + $"for color type {colorType}.");
                        return false;
                    }
                    if (colorType == 3 && palette == null)
                    {
                        MelonLogger.Error("[MiniPngDecoder] Palette color type but no PLTE chunk found.");
                        return false;
                    }

                    int bpp = Math.Max(1, (bitDepth * channels) / 8);
                    int stride = (width * bitDepth * channels + 7) / 8;
                    byte[] raw = new byte[(stride + 1) * height];

                    idat.Position = 0;
                    using (var z = new ZLibStream(idat, CompressionMode.Decompress))
                    {
                        int off = 0;
                        while (off < raw.Length)
                        {
                            int read = z.Read(raw, off, raw.Length - off);
                            if (read <= 0) break;
                            off += read;
                        }
                    }

                    byte[] outPix = new byte[stride * height];
                    byte[] prevRow = new byte[stride];
                    int rawPos = 0;
                    for (int y = 0; y < height; y++)
                    {
                        int filterType = raw[rawPos]; rawPos += 1;
                        byte[] curRow = new byte[stride];
                        Array.Copy(raw, rawPos, curRow, 0, stride);
                        rawPos += stride;

                        for (int x = 0; x < stride; x++)
                        {
                            int a = x >= bpp ? curRow[x - bpp] : 0;
                            int b = prevRow[x];
                            int c = x >= bpp ? prevRow[x - bpp] : 0;
                            int recon;
                            switch (filterType)
                            {
                                case 0: recon = curRow[x]; break;
                                case 1: recon = curRow[x] + a; break;
                                case 2: recon = curRow[x] + b; break;
                                case 3: recon = curRow[x] + ((a + b) / 2); break;
                                case 4: recon = curRow[x] + PaethPredictor(a, b, c); break;
                                default:
                                    MelonLogger.Error($"[MiniPngDecoder] Unknown filter type {filterType}.");
                                    return false;
                            }
                            curRow[x] = (byte)(recon & 0xFF);
                        }

                        Array.Copy(curRow, 0, outPix, y * stride, stride);
                        prevRow = curRow;
                    }

                    pixels = new Color32[width * height];
                    for (int y = 0; y < height; y++)
                    {
                        int srcRow = y * stride;
                        int dstRow = (height - 1 - y) * width;
                        for (int x = 0; x < width; x++)
                        {
                            byte r, g, b, a;

                            if (bitDepth == 8)
                            {
                                int si = srcRow + x * bpp;
                                switch (colorType)
                                {
                                    case 0: r = g = b = outPix[si]; a = 255; break;
                                    case 2: r = outPix[si]; g = outPix[si + 1]; b = outPix[si + 2]; a = 255; break;
                                    case 3:
                                        {
                                            int idx = outPix[si];
                                            r = palette[idx * 3];
                                            g = palette[idx * 3 + 1];
                                            b = palette[idx * 3 + 2];
                                            a = (trns != null && idx < trns.Length) ? trns[idx] : (byte)255;
                                            break;
                                        }
                                    case 4: r = g = b = outPix[si]; a = outPix[si + 1]; break;
                                    case 6: r = outPix[si]; g = outPix[si + 1]; b = outPix[si + 2]; a = outPix[si + 3]; break;
                                    default: r = g = b = 0; a = 255; break;
                                }
                            }
                            else
                            {
                                int sample = ExtractSample(outPix, srcRow, x, bitDepth);
                                if (colorType == 3)
                                {
                                    r = palette[sample * 3];
                                    g = palette[sample * 3 + 1];
                                    b = palette[sample * 3 + 2];
                                    a = (trns != null && sample < trns.Length) ? trns[sample] : (byte)255;
                                }
                                else
                                {
                                    int maxVal = (1 << bitDepth) - 1;
                                    byte v = (byte)(sample * 255 / maxVal);
                                    r = g = b = v; a = 255;
                                }
                            }

                            pixels[dstRow + x] = new Color32(r, g, b, a);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MiniPngDecoder] Decode error: {ex}");
                return false;
            }
        }

        private static int ExtractSample(byte[] outPix, int rowStart, int x, int bitDepth)
        {
            int bitOffset = x * bitDepth;
            int byteIndex = rowStart + bitOffset / 8;
            int bitInByte = bitOffset % 8;
            int shift = 8 - bitDepth - bitInByte;
            int mask = (1 << bitDepth) - 1;
            return (outPix[byteIndex] >> shift) & mask;
        }

        private static int ReadBE32(byte[] buf, int offset)
        {
            return (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];
        }

        private static int PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }
    }
}