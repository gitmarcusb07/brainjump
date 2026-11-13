using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Static save system for BrainJump. All player progress and settings are persisted via PlayerPrefs.
    /// The game has 3 worlds with 10 levels each (30 levels total). Level indices are 0-based across
    /// all worlds: world = levelIndex / 10, levelInWorld = levelIndex % 10.
    /// </summary>
    public static class SaveManager
    {
        // ------------------------------------------------------------------
        // Structure constants
        // ------------------------------------------------------------------

        /// <summary>
        /// Number of worlds in the game.
        /// </summary>
        public const int Worlds = 3;

        /// <summary>
        /// Number of levels contained in each world.
        /// </summary>
        public const int LevelsPerWorld = 10;

        /// <summary>
        /// Total number of levels across all worlds.
        /// </summary>
        public const int TotalLevels = 30;

        /// <summary>
        /// Maximum number of stars that can be earned on a single level.
        /// </summary>
        public const int MaxStarsPerLevel = 3;

        // ------------------------------------------------------------------
        // PlayerPrefs keys
        // ------------------------------------------------------------------

        private const string KeyUnlockedLevel = "BJ_UnlockedLevel";
        private const string KeyStarsFormat = "BJ_Stars_{0}";
        private const string KeyTotalCoins = "BJ_TotalCoins";
        private const string KeyBestScore = "BJ_BestScore";
        private const string KeyMusicOn = "BJ_MusicOn";
        private const string KeySfxOn = "BJ_SfxOn";

        // ------------------------------------------------------------------
        // World info
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the display name of the given world.
        /// 0 = "Addition Meadows", 1 = "Subtraction Hills", 2 = "Multiplication Peaks";
        /// any other value returns "BrainJump".
        /// </summary>
        /// <param name="world">0-based world index.</param>
        /// <returns>The world's display name.</returns>
        public static string WorldName(int world)
        {
            switch (world)
            {
                case 0: return "Addition Meadows";
                case 1: return "Subtraction Hills";
                case 2: return "Multiplication Peaks";
                default: return "BrainJump";
            }
        }

        /// <summary>
        /// Returns the maths operator symbol associated with the given world.
        /// 0 = "+", 1 = "-", 2 = "x"; any other value returns "?".
        /// </summary>
        /// <param name="world">0-based world index.</param>
        /// <returns>The world's operator symbol.</returns>
        public static string WorldSymbol(int world)
        {
            switch (world)
            {
                case 0: return "+";
                case 1: return "-";
                case 2: return "x";
                default: return "?";
            }
        }

        // ------------------------------------------------------------------
        // Progress
        // ------------------------------------------------------------------

        /// <summary>
        /// The highest unlocked level as a 0-based index. Defaults to 0 (level 1 is unlocked from the start).
        /// Always within 0..TotalLevels-1.
        /// </summary>
        public static int HighestUnlockedLevel
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(KeyUnlockedLevel, 0), 0, TotalLevels - 1); }
        }

        /// <summary>
        /// Returns whether the given level is unlocked. Negative indices are never unlocked,
        /// level 0 is always unlocked, otherwise the level is unlocked if it is at or below
        /// <see cref="HighestUnlockedLevel"/>.
        /// </summary>
        /// <param name="levelIndex">0-based level index across all worlds.</param>
        /// <returns>True if the level is playable.</returns>
        public static bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex < 0) return false;
            if (levelIndex == 0) return true;
            return levelIndex <= HighestUnlockedLevel;
        }

        /// <summary>
        /// Unlocks the given level. Only ever raises <see cref="HighestUnlockedLevel"/>, never lowers it.
        /// The index is clamped to 0..TotalLevels-1. Saves immediately.
        /// </summary>
        /// <param name="levelIndex">0-based level index to unlock.</param>
        public static void UnlockLevel(int levelIndex)
        {
            int clamped = Mathf.Clamp(levelIndex, 0, TotalLevels - 1);
            int current = HighestUnlockedLevel;
            if (clamped > current)
            {
                PlayerPrefs.SetInt(KeyUnlockedLevel, clamped);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Returns the best star rating (0..3) earned on the given level. Defaults to 0;
        /// out-of-range indices return 0.
        /// </summary>
        /// <param name="levelIndex">0-based level index across all worlds.</param>
        /// <returns>Stars earned, 0..MaxStarsPerLevel.</returns>
        public static int GetStars(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= TotalLevels) return 0;
            return Mathf.Clamp(PlayerPrefs.GetInt(StarsKey(levelIndex), 0), 0, MaxStarsPerLevel);
        }

        /// <summary>
        /// Records a star rating for the given level. The value is clamped to 0..3 and is only
        /// stored if it is higher than the currently stored rating. Out-of-range indices are ignored.
        /// Saves immediately when a new value is written.
        /// </summary>
        /// <param name="levelIndex">0-based level index across all worlds.</param>
        /// <param name="stars">Stars earned on this attempt.</param>
        public static void SetStars(int levelIndex, int stars)
        {
            if (levelIndex < 0 || levelIndex >= TotalLevels)
            {
                Debug.LogWarning("[SaveManager] SetStars called with out-of-range level index: " + levelIndex);
                return;
            }

            int clamped = Mathf.Clamp(stars, 0, MaxStarsPerLevel);
            if (clamped > GetStars(levelIndex))
            {
                PlayerPrefs.SetInt(StarsKey(levelIndex), clamped);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// The sum of stars earned across all 30 levels.
        /// </summary>
        public static int TotalStars
        {
            get
            {
                int total = 0;
                for (int i = 0; i < TotalLevels; i++)
                {
                    total += GetStars(i);
                }
                return total;
            }
        }

        /// <summary>
        /// Total coins collected by the player. Defaults to 0.
        /// </summary>
        public static int TotalCoins
        {
            get { return Mathf.Max(0, PlayerPrefs.GetInt(KeyTotalCoins, 0)); }
        }

        /// <summary>
        /// Adds coins to the player's total. Amounts of zero or less are ignored. Saves immediately.
        /// </summary>
        /// <param name="amount">Number of coins to add.</param>
        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;

            int current = TotalCoins;
            int newTotal = current > int.MaxValue - amount ? int.MaxValue : current + amount;
            PlayerPrefs.SetInt(KeyTotalCoins, newTotal);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// The player's best score. Defaults to 0.
        /// </summary>
        public static int BestScore
        {
            get { return PlayerPrefs.GetInt(KeyBestScore, 0); }
        }

        /// <summary>
        /// Records a score, storing it only if it is higher than the current best. Saves immediately
        /// when a new best is written.
        /// </summary>
        /// <param name="score">Score achieved.</param>
        public static void SetBestScore(int score)
        {
            if (score > BestScore)
            {
                PlayerPrefs.SetInt(KeyBestScore, score);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Resets all gameplay progress: highest unlocked level to 0, all stars to 0, coins to 0 and
        /// best score to 0. Settings (music and SFX) are not affected. Saves immediately.
        /// </summary>
        public static void ResetAllProgress()
        {
            PlayerPrefs.SetInt(KeyUnlockedLevel, 0);
            for (int i = 0; i < TotalLevels; i++)
            {
                PlayerPrefs.SetInt(StarsKey(i), 0);
            }
            PlayerPrefs.SetInt(KeyTotalCoins, 0);
            PlayerPrefs.SetInt(KeyBestScore, 0);
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] All progress reset.");
        }

        // ------------------------------------------------------------------
        // Settings
        // ------------------------------------------------------------------

        /// <summary>
        /// Whether background music is enabled. Defaults to true. Setting the value saves immediately.
        /// </summary>
        public static bool MusicEnabled
        {
            get { return PlayerPrefs.GetInt(KeyMusicOn, 1) != 0; }
            set
            {
                PlayerPrefs.SetInt(KeyMusicOn, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Whether sound effects are enabled. Defaults to true. Setting the value saves immediately.
        /// </summary>
        public static bool SfxEnabled
        {
            get { return PlayerPrefs.GetInt(KeySfxOn, 1) != 0; }
            set
            {
                PlayerPrefs.SetInt(KeySfxOn, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Builds the PlayerPrefs key used to store stars for a level.
        /// </summary>
        /// <param name="levelIndex">0-based level index.</param>
        /// <returns>The PlayerPrefs key.</returns>
        private static string StarsKey(int levelIndex)
        {
            return string.Format(KeyStarsFormat, levelIndex);
        }
    }
}
