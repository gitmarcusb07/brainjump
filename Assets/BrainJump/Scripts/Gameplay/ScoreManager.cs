using System;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Tracks the player's score and combo of consecutive correct answers.
    /// Exposes events so UI elements can react to score, combo and point gains.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        /// <summary>
        /// The active ScoreManager instance, or null if none exists.
        /// </summary>
        public static ScoreManager Instance { get; private set; }

        /// <summary>
        /// The current total score.
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// The number of consecutive correct answers. This is 0 when there is no combo.
        /// </summary>
        public int Combo { get; private set; }

        /// <summary>
        /// The current score multiplier, derived from the combo and capped at x5.
        /// A combo of 0 or 1 gives x1, a combo of 2 gives x2, and so on.
        /// </summary>
        public int Multiplier => Mathf.Clamp(Combo, 1, 5);

        /// <summary>
        /// Invoked with the new total score whenever the score changes.
        /// </summary>
        public event Action<int> OnScoreChanged;

        /// <summary>
        /// Invoked with the new combo value whenever the combo changes.
        /// </summary>
        public event Action<int> OnComboChanged;

        /// <summary>
        /// Invoked with the number of points just gained, for example to show a "+100" popup.
        /// </summary>
        public event Action<int> OnPointsGained;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Resets the score and combo to zero and notifies listeners.
        /// </summary>
        public void ResetScore()
        {
            Score = 0;
            Combo = 0;
            OnScoreChanged?.Invoke(0);
            OnComboChanged?.Invoke(0);
        }

        /// <summary>
        /// Registers a correct answer. This increments the combo and awards 100 points
        /// multiplied by the current multiplier.
        /// </summary>
        /// <returns>The number of points awarded.</returns>
        public int RegisterCorrect()
        {
            Combo++;
            int points = 100 * Multiplier;
            Score += points;
            OnScoreChanged?.Invoke(Score);
            OnComboChanged?.Invoke(Combo);
            OnPointsGained?.Invoke(points);
            return points;
        }

        /// <summary>
        /// Registers a wrong answer. This resets the combo to zero.
        /// </summary>
        public void RegisterMistake()
        {
            Combo = 0;
            OnComboChanged?.Invoke(0);
        }

        /// <summary>
        /// Adds bonus points to the score, for example from coins.
        /// Values of zero or less are ignored.
        /// </summary>
        /// <param name="points">The bonus points to add.</param>
        public void AddBonus(int points)
        {
            if (points <= 0)
            {
                return;
            }

            Score += points;
            OnScoreChanged?.Invoke(Score);
            OnPointsGained?.Invoke(points);
        }
    }
}
