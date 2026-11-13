using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Tracks correct answers, mistakes, and the current difficulty level.
    /// </summary>
    public class GameProgression : MonoBehaviour
    {
        public const int QuestionsPerLevel = 10;

        public int CorrectCount { get; private set; }
        public int MistakeCount { get; private set; }

        public int QuestionsCompleted => CorrectCount + MistakeCount;

        /// <summary>
        /// Difficulty derived from the current world and level, clamped to 1..5.
        /// World 0 maps to 1..3, World 1 to 2..4, World 2 to 3..5.
        /// </summary>
        public int Difficulty => Mathf.Clamp(1 + World + Level / 4, 1, 5);

        public bool IsLevelComplete => CorrectCount >= QuestionsPerLevel;

        /// <summary>
        /// Current 0-based world index (0..2).
        /// </summary>
        public int World { get; private set; }

        /// <summary>
        /// Current 0-based level index within the world (0..9).
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// Sets the current world and level, clamped to their valid ranges.
        /// </summary>
        /// <param name="world">0-based world index, clamped to 0..2.</param>
        /// <param name="level">0-based level index within the world, clamped to 0..9.</param>
        public void SetLevel(int world, int level)
        {
            World = Mathf.Clamp(world, 0, 2);
            Level = Mathf.Clamp(level, 0, 9);
        }

        /// <summary>
        /// Records a correct answer. Difficulty is derived from the current level.
        /// </summary>
        public void RegisterCorrect()
        {
            CorrectCount++;
        }

        /// <summary>
        /// Records an incorrect answer without lowering difficulty.
        /// </summary>
        public void RegisterMistake()
        {
            MistakeCount++;
        }

        /// <summary>
        /// Clears all run progress and restores the opening world and level.
        /// </summary>
        public void ResetProgress()
        {
            CorrectCount = 0;
            MistakeCount = 0;
            World = 0;
            Level = 0;
        }
    }
}
