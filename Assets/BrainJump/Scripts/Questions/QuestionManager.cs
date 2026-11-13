using System;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Generates questions, distributes their options across the three answer
    /// platforms, and tracks the previous question to avoid repeats.
    /// </summary>
    public class QuestionManager : MonoBehaviour
    {
        private const int OptionCount = 3;

        [Header("Scene References")]
        [SerializeField] private AnswerPlatform[] answerPlatforms;
        [SerializeField] private GameProgression gameProgression;
        [SerializeField] private UIManager uiManager;

        [Header("Power-Ups & Coins")]
        [SerializeField] private PowerUpManager powerUpManager;
        [SerializeField] private Coin[] coins;
        [SerializeField] private PowerUpPickup powerUpPickup;

        private string _previousQuestionText = string.Empty;

        private PowerUpPickup _activePowerUp;
        private int _currentWorld;
        private int _currentLevel;
        private bool _levelSet;

        public AnswerPlatform[] AnswerPlatforms => answerPlatforms;

        /// <summary>
        /// Assigns scene references (used by the SceneBuilder). Inspector
        /// assignment works too — this only fills what is still null.
        /// </summary>
        public void Wire(AnswerPlatform[] platforms, GameProgression progression, UIManager ui)
        {
            if (platforms != null) answerPlatforms = platforms;
            if (progression != null) gameProgression = progression;
            if (ui != null) uiManager = ui;
        }

        /// <summary>
        /// Assigns power-up and coin references (used by the SceneBuilder).
        /// Only non-null arguments overwrite the current values.
        /// </summary>
        /// <param name="pum">Power-up manager used to roll spawns and types.</param>
        /// <param name="cm">Coin manager reference.</param>
        /// <param name="coinPool">Coins respawned on every new question.</param>
        /// <param name="pickup">Pooled power-up pickup placed above a platform.</param>
        public void WirePowerUps(PowerUpManager pum, CoinManager cm, Coin[] coinPool, PowerUpPickup pickup)
        {
            if (pum != null) powerUpManager = pum;
            if (coinPool != null) coins = coinPool;
            if (pickup != null) powerUpPickup = pickup;
        }

        /// <summary>
        /// Sets the current world (0..2) and level (0..9) used for question
        /// generation, and forwards it to GameProgression when available.
        /// </summary>
        /// <param name="world">0-based world index, clamped to 0..2.</param>
        /// <param name="level">0-based level index within the world, clamped to 0..9.</param>
        public void SetLevel(int world, int level)
        {
            _currentWorld = Mathf.Clamp(world, 0, 2);
            _currentLevel = Mathf.Clamp(level, 0, 9);
            _levelSet = true;

            if (gameProgression != null)
            {
                gameProgression?.SetLevel(_currentWorld, _currentLevel);
            }
        }

        public QuestionData CurrentQuestion { get; private set; }

        public event System.Action<QuestionData> OnQuestionChanged;

        /// <summary>
        /// Difficulty is sourced from GameProgression when available and
        /// falls back to the opening difficulty otherwise.
        /// </summary>
        public int Difficulty
        {
            get
            {
                if (gameProgression == null)
                {
                    gameProgression = GetComponent<GameProgression>();
                }

                return gameProgression != null
                    ? Mathf.Clamp(gameProgression.Difficulty, 1, 5)
                    : 1;
            }
        }

        private void Awake()
        {
            // Serialized references are preferred. These fallbacks support the
            // common setup where progression and UI live on the same object.
            if (gameProgression == null)
            {
                gameProgression = GetComponent<GameProgression>();
            }

            if (uiManager == null)
            {
                uiManager = GetComponent<UIManager>();
            }
        }

        /// <summary>
        /// Generates and applies a new question only while gameplay is active.
        /// Safe to call from other game states.
        /// </summary>
        public void NewQuestion()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.GameState.Playing)
            {
                return;
            }

            QuestionData question = _levelSet
                ? QuestionGenerator.GenerateForLevel(_currentWorld, _currentLevel, _previousQuestionText)
                : QuestionGenerator.Generate(Difficulty, _previousQuestionText);
            if (question == null || question.options == null)
            {
                return;
            }

            CurrentQuestion = question;
            _previousQuestionText = question.questionText;

            ApplyOptionsToPlatforms(question);

            RespawnCoins();
            MaybeSpawnPowerUp();

            if (uiManager != null)
            {
                uiManager.SetQuestion(question.questionText);
            }

            OnQuestionChanged?.Invoke(question);
        }

        /// <summary>
        /// Resets every platform to a neutral state, e.g. when returning to menu.
        /// </summary>
        public void ClearPlatforms()
        {
            if (answerPlatforms == null)
            {
                return;
            }

            for (int i = 0; i < answerPlatforms.Length; i++)
            {
                if (answerPlatforms[i] != null)
                {
                    answerPlatforms[i].SetAnswer(0, false);
                }
            }

            if (_activePowerUp != null)
            {
                _activePowerUp.gameObject.SetActive(false);
            }
        }

        private void ApplyOptionsToPlatforms(QuestionData question)
        {
            if (answerPlatforms == null)
            {
                return;
            }

            for (int i = 0; i < OptionCount; i++)
            {
                if (i >= answerPlatforms.Length || answerPlatforms[i] == null)
                {
                    continue;
                }

                int optionValue = i < question.options.Length ? question.options[i] : 0;
                bool isCorrect = i == question.correctIndex;
                answerPlatforms[i].SetAnswer(optionValue, isCorrect);
            }
        }

        private void RespawnCoins()
        {
            if (coins == null)
            {
                return;
            }

            for (int i = 0; i < coins.Length; i++)
            {
                Coin coin = coins[i];
                if (coin != null)
                {
                    coin.ResetCoin();
                }
            }
        }

        private void MaybeSpawnPowerUp()
        {
            if (_activePowerUp != null && _activePowerUp.gameObject.activeSelf)
            {
                return;
            }

            if (powerUpManager == null || answerPlatforms == null || answerPlatforms.Length == 0)
            {
                return;
            }

            if (!powerUpManager.ShouldSpawnPowerUp())
            {
                return;
            }

            if (powerUpPickup == null)
            {
                return;
            }

            // Fully qualified: both System and UnityEngine define Random.
            int index = UnityEngine.Random.Range(0, answerPlatforms.Length);
            if (answerPlatforms[index] == null)
            {
                return;
            }

            _activePowerUp = powerUpPickup;
            PowerUpType type = powerUpManager.RollPowerUpType();
            _activePowerUp.Setup(type);
            _activePowerUp.transform.position = answerPlatforms[index].transform.position + Vector3.up * 1.6f;
            _activePowerUp.ResetPickup();
        }
    }
}
