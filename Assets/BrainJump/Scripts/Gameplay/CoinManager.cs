using System;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Tracks the coins collected during the current level.
    /// Awards bonus score for each coin and banks the coins to the save data when the level is completed.
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        /// <summary>
        /// The active CoinManager instance, or null if none exists.
        /// </summary>
        public static CoinManager Instance { get; private set; }

        /// <summary>
        /// The number of coins collected in the current level.
        /// </summary>
        public int CoinsThisLevel { get; private set; }

        /// <summary>
        /// Invoked with the new <see cref="CoinsThisLevel"/> value whenever it changes.
        /// </summary>
        public event Action<int> OnCoinsChanged;

        /// <summary>
        /// Invoked with the coin's world position when a coin is collected, for sparkle effects.
        /// </summary>
        public event Action<Vector3> OnCoinCollected;

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
        /// Resets the level coin counter to zero and notifies listeners.
        /// </summary>
        public void ResetCoins()
        {
            CoinsThisLevel = 0;
            OnCoinsChanged?.Invoke(0);
        }

        /// <summary>
        /// Registers a collected coin. This increments the counter, awards 10 bonus points,
        /// plays the coin sound and notifies listeners.
        /// </summary>
        /// <param name="position">The world position of the collected coin.</param>
        public void CollectCoin(Vector3 position)
        {
            CoinsThisLevel++;
            ScoreManager.Instance?.AddBonus(10);
            AudioManager.Instance?.PlayCoin();
            OnCoinsChanged?.Invoke(CoinsThisLevel);
            OnCoinCollected?.Invoke(position);
        }

        /// <summary>
        /// Adds the coins collected this level to the saved total.
        /// Call this when the level is completed. It does not reset the level counter.
        /// </summary>
        public void BankCoins()
        {
            if (CoinsThisLevel <= 0)
            {
                return;
            }

            SaveManager.AddCoins(CoinsThisLevel);
        }
    }
}
