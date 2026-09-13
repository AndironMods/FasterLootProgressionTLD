using System;
using MelonLoader;

namespace FasterLootProgression
{
    /// <summary>
    /// Tracks Loot Search skill progression
    /// Level 1: 0-199 points - 0% faster (no bonus)
    /// Level 2: 200-299 points - 25% faster
    /// Level 3: 300-399 points - 50% faster
    /// Level 4: 400-499 points - 75% faster
    /// Level 5: 500 points - 100% faster (instant)
    /// </summary>
    public class LootProgressionSkill
    {
        public const int LEVEL_1_THRESHOLD = 0;
        public const int LEVEL_2_THRESHOLD = 200;
        public const int LEVEL_3_THRESHOLD = 300;
        public const int LEVEL_4_THRESHOLD = 400;
        public const int LEVEL_5_THRESHOLD = 500;
        public const int MAX_POINTS = 500;
        public const int MAX_LEVEL = 5;

        private int m_CurrentPoints = 0;
        private int m_CurrentLevel = 1;

        public int CurrentPoints
        {
            get { return m_CurrentPoints; }
            set 
            { 
                // Points are allowed to keep growing past MAX_POINTS (500) - searching
                // still legitimately increases the raw total forever. The LEVEL and
                // speed bonus are what stay hard-capped at 500 (see UpdateLevel()
                // below, whose final "else" branch already treats any value >= 500
                // as Level 5/instant, regardless of how far past 500 it is).
                // Only guard against negative values from bad/corrupt save data.
                m_CurrentPoints = Math.Max(0, value);
                UpdateLevel(); // Recalculate level when points change
            }
        }

        public int CurrentLevel
        {
            get { return m_CurrentLevel; }
            private set { m_CurrentLevel = Math.Max(1, Math.Min(value, MAX_LEVEL)); }
        }

        /// <summary>
        /// Add points from loot search
        /// </summary>
        public int AddSearchPoints(int points)
        {
            int oldLevel = CurrentLevel;
            CurrentPoints += points;
            UpdateLevel();
            
            return oldLevel < CurrentLevel ? CurrentLevel : 0; // Return new level if leveled up, else 0
        }

        /// <summary>
        /// Calculate level based on current points
        /// </summary>
        private void UpdateLevel()
        {
            if (CurrentPoints < LEVEL_2_THRESHOLD)
                CurrentLevel = 1;
            else if (CurrentPoints < LEVEL_3_THRESHOLD)
                CurrentLevel = 2;
            else if (CurrentPoints < LEVEL_4_THRESHOLD)
                CurrentLevel = 3;
            else if (CurrentPoints < LEVEL_5_THRESHOLD)
                CurrentLevel = 4;
            else
                CurrentLevel = 5;
        }

        /// <summary>
        /// Get speed multiplier based on current level
        /// Level 1: 1.00x (no bonus)
        /// Level 2: 0.75x (25% faster)
        /// Level 3: 0.50x (50% faster)
        /// Level 4: 0.25x (75% faster)
        /// Level 5: 0.00x (100% faster = instant)
        /// </summary>
        public float GetSpeedMultiplier()
        {
            return CurrentLevel switch
            {
                1 => 1.0f,   // 0% faster
                2 => 0.75f,  // 25% faster
                3 => 0.5f,   // 50% faster
                4 => 0.25f,  // 75% faster
                5 => 0.0f,   // 100% faster (instant)
                _ => 1.0f
            };
        }

        /// <summary>
        /// Load from JSON string
        /// </summary>
        public static LootProgressionSkill LoadFromJson(string json)
        {
            try
            {
                var skill = new LootProgressionSkill();
                
                if (!string.IsNullOrEmpty(json) && json.Contains("points"))
                {
                    int start = json.IndexOf("points") + 8;
                    int end = json.IndexOf("}", start);
                    if (end == -1) end = json.IndexOf(",", start);
                    if (end == -1) end = json.Length;
                    
                    string pointsStr = json.Substring(start, end - start).Trim(' ', '"', ':');
                    
                    if (int.TryParse(pointsStr, out int points))
                    {
                        skill.CurrentPoints = points;
                        skill.UpdateLevel();
                    }
                }
                
                return skill;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LootProgression] Error loading skill: {ex}");
                return new LootProgressionSkill();
            }
        }

        /// <summary>
        /// Save to JSON string
        /// </summary>
        public string SaveToJson()
        {
            return $"{{\"points\":{CurrentPoints}}}";
        }
    }
}