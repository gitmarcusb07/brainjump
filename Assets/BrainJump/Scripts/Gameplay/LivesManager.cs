using System;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Tracks the player's remaining hearts (lives) and notifies listeners when they change.
    /// </summary>
    public class LivesManager : MonoBehaviour
    {
        /// <summary>
        /// The maximum number of hearts the player can hold.
        /// </summary>
        public const int MaxHearts = 3;

        /// <summary>
        /// The active LivesManager instance, or null if none exists.
        /// </summary>
        public static LivesManager Instance { get; private set; }

        /// <summary>
        /// The current number of hearts.
        /// </summary>
        public int Hearts { get; private set; }

        /// <summary>
        /// Invoked with the new heart count whenever the hearts change.
        /// </summary>
        public event Action<int> OnHeartsChanged;

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
        /// Restores hearts to <see cref="MaxHearts"/> and notifies listeners.
        /// </summary>
        public void ResetLives()
        {
            Hearts = MaxHearts;
            OnHeartsChanged?.Invoke(Hearts);
        }

        /// <summary>
        /// Removes one heart. The count never drops below zero.
        /// </summary>
        /// <returns>True if no hearts remain (game over); otherwise false.</returns>
        public bool LoseHeart()
        {
            Hearts = Mathf.Max(0, Hearts - 1);
            OnHeartsChanged?.Invoke(Hearts);
            return Hearts <= 0;
        }

        /// <summary>
        /// Adds one heart. The count never goes above <see cref="MaxHearts"/>.
        /// </summary>
        public void GainHeart()
        {
            Hearts = Mathf.Min(MaxHearts, Hearts + 1);
            OnHeartsChanged?.Invoke(Hearts);
        }
    }
}
