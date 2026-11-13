using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// The kinds of power-up that the player can collect during a run.
    /// </summary>
    public enum PowerUpType
    {
        /// <summary>Adds extra seconds to the question timer. The GameManager applies the bonus.</summary>
        ExtraTime,

        /// <summary>Allows one extra jump while airborne.</summary>
        DoubleJump,

        /// <summary>Protects the player from one wrong-answer penalty.</summary>
        Shield
    }

    /// <summary>
    /// Stores the player's active power-ups and grants or consumes them.
    /// Also decides when a power-up should spawn and which type it should be.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        private const float SpawnChance = 0.25f;
        private const int PowerUpTypeCount = 3;

        /// <summary>
        /// The active PowerUpManager instance. This may be null.
        /// </summary>
        public static PowerUpManager Instance { get; private set; }

        /// <summary>
        /// True while the player has an unused shield.
        /// </summary>
        public bool HasShield { get; private set; }

        /// <summary>
        /// True while the player has an unused double jump.
        /// </summary>
        public bool DoubleJumpAvailable { get; private set; }

        /// <summary>
        /// Raised whenever a power-up is granted to the player.
        /// </summary>
        public event System.Action<PowerUpType> OnPowerUpGranted;

        /// <summary>
        /// Raised whenever a stored power-up is used up.
        /// </summary>
        public event System.Action<PowerUpType> OnPowerUpConsumed;

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
        /// Clears all stored power-ups, for example at the start of a new run.
        /// </summary>
        public void ResetPowerUps()
        {
            HasShield = false;
            DoubleJumpAvailable = false;
        }

        /// <summary>
        /// Grants a power-up to the player, plays the power-up sound and raises <see cref="OnPowerUpGranted"/>.
        /// </summary>
        /// <param name="type">The type of power-up to grant.</param>
        public void GrantPowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Shield:
                    HasShield = true;
                    break;
                case PowerUpType.DoubleJump:
                    DoubleJumpAvailable = true;
                    break;
                case PowerUpType.ExtraTime:
                    // No stored state. The GameManager adds the extra time when it receives the event.
                    break;
            }

            AudioManager.Instance?.PlayPowerUp();
            OnPowerUpGranted?.Invoke(type);
        }

        /// <summary>
        /// Uses up the shield if the player has one.
        /// </summary>
        /// <returns>True if a shield was consumed. Otherwise false.</returns>
        public bool ConsumeShield()
        {
            if (!HasShield)
            {
                return false;
            }

            HasShield = false;
            OnPowerUpConsumed?.Invoke(PowerUpType.Shield);
            return true;
        }

        /// <summary>
        /// Uses up the double jump if the player has one.
        /// </summary>
        /// <returns>True if a double jump was consumed. Otherwise false.</returns>
        public bool ConsumeDoubleJump()
        {
            if (!DoubleJumpAvailable)
            {
                return false;
            }

            DoubleJumpAvailable = false;
            OnPowerUpConsumed?.Invoke(PowerUpType.DoubleJump);
            return true;
        }

        /// <summary>
        /// Rolls whether a power-up should spawn for the current question. There is a 25% chance.
        /// </summary>
        /// <returns>True if a power-up should spawn.</returns>
        public bool ShouldSpawnPowerUp()
        {
            return UnityEngine.Random.value < SpawnChance;
        }

        /// <summary>
        /// Picks a random power-up type. Each type has the same chance.
        /// </summary>
        /// <returns>The randomly chosen power-up type.</returns>
        public PowerUpType RollPowerUpType()
        {
            return (PowerUpType)UnityEngine.Random.Range(0, PowerUpTypeCount);
        }
    }
}
