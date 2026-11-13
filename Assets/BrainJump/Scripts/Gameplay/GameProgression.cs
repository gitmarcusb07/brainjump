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

        public int Difficulty { get; private set; } = 1;

        public bool IsLevelComplete => CorrectCount >= QuestionsPerLevel;

        /// <summary>
        /// Records a correct answer and raises difficulty every two correct answers.
        /// </summary>
        public void RegisterCorrect()
        {
            CorrectCount++;
            Difficulty = Mathf.Clamp(1 + CorrectCount / 2, 1, 5);
        }

        /// <summary>
        /// Records an incorrect answer without lowering difficulty.
        /// </summary>
        public void RegisterMistake()
        {
            MistakeCount++;
        }

        /// <summary>
        /// Clears all run progress and restores the opening difficulty.
        /// </summary>
        public void ResetProgress()
        {
            CorrectCount = 0;
            MistakeCount = 0;
            Difficulty = 1;
        }
    }
}
