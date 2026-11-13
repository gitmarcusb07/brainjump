using System.Collections;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Central gameplay orchestrator for BrainJump.
    /// Owns the high-level flow state machine
    /// (Menu / LevelSelect -> Playing -> Paused / Failed / Complete / GameOver)
    /// and coordinates the other managers (questions, timer, UI, audio, progression, player,
    /// score, lives, coins, power-ups and effects).
    ///
    /// Single-scene prototype singleton: the first instance wins, duplicates are destroyed and
    /// no DontDestroyOnLoad is used. Every serialized reference is null-guarded so a partially
    /// wired scene never throws, it only warns.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>High-level flow state of a BrainJump level.</summary>
        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            Failed,
            Complete,
            /// <summary>The level select screen is open.</summary>
            LevelSelect,
            /// <summary>All hearts were lost; the run is over.</summary>
            GameOver
        }

        /// <summary>The single active instance (null when no GameManager exists).</summary>
        public static GameManager Instance { get; private set; }

        [Header("Managers")]
        [SerializeField] private QuestionManager questionManager;
        [SerializeField] private GameTimer gameTimer;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private GameProgression progression;

        [Header("Player & Camera")]
        [SerializeField] private PlayerController player;
        [SerializeField] private CharacterAnimator characterAnimator;
        [SerializeField] private CameraFollow cameraFollow;
        [Tooltip("Where the player is placed at the start of a run and on every retry.")]
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Timing")]
        [Tooltip("Seconds the player has to answer a single question (serialized fallback; per-level limits are computed from the level index).")]
        [SerializeField] private float questionTimeLimit = 10f;
        [Tooltip("Delay between a correct answer and the next question / level complete screen.")]
        [SerializeField] private float nextQuestionDelay = 1.4f;
        [Tooltip("Delay between a failed answer and the fail screen (lets the explosion play).")]
        [SerializeField] private float failDelay = 0.6f;

        [Header("Meta Managers")]
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private LivesManager livesManager;
        [SerializeField] private CoinManager coinManager;
        [SerializeField] private PowerUpManager powerUpManager;
        [SerializeField] private EffectsManager effectsManager;
        [SerializeField] private LevelSelectUI levelSelectUI;

        /// <summary>Current flow state.</summary>
        public GameState State { get; private set; } = GameState.Menu;

        /// <summary>Raised with the new state whenever <see cref="State"/> changes.</summary>
        public event System.Action<GameState> OnStateChanged;

        /// <summary>Correct answers in the current level (0 when progression is not wired).</summary>
        public int CorrectCount => progression != null ? progression.CorrectCount : 0;

        /// <summary>Questions required to complete one level.</summary>
        public int QuestionsTotal => GameProgression.QuestionsPerLevel;

        /// <summary>Zero-based world index of the current level (0 when progression is not wired).</summary>
        public int CurrentWorld => progression != null ? progression.World : 0;

        /// <summary>Zero-based level index inside the current world (0 when progression is not wired).</summary>
        public int CurrentLevel => progression != null ? progression.Level : 0;

        /// <summary>
        /// Seconds per question for the current level: starts at 10s and shrinks by 0.25s per
        /// level, never dropping below 6s.
        /// </summary>
        private float LevelTimeLimit => Mathf.Max(6f, 10f - (CurrentWorld * 10 + CurrentLevel) * 0.25f);

        /// <summary>
        /// Position the player is placed at. Falls back to the player's current position and
        /// finally to the world origin so a missing spawn point can never teleport to (NaN).
        /// </summary>
        private Vector3 SpawnPosition
        {
            get
            {
                if (playerSpawnPoint != null) return playerSpawnPoint.position;
                if (player != null) return player.transform.position;
                return Vector3.zero;
            }
        }

        // ---------------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------------

        private void Awake()
        {
            // Keep the first instance; destroy any duplicate that slips into the scene.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate instance detected - destroying the duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            CacheMissingReferences();
            WarnMissingReferences();

            // A frozen clock must never survive a scene load / domain reload.
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (gameTimer != null)
            {
                gameTimer.OnTimeout += OnTimerTimeout;
                gameTimer.OnTick += OnTimerTick; // Drives the HUD countdown.
            }

            if (powerUpManager != null) powerUpManager.OnPowerUpGranted += OnPowerUpGranted;

            SetState(GameState.Menu);

            if (uiManager != null)
            {
                uiManager.ShowMenu();
            }
        }

        private void OnDestroy()
        {
            if (gameTimer != null)
            {
                gameTimer.OnTimeout -= OnTimerTimeout;
                gameTimer.OnTick -= OnTimerTick;
            }

            if (powerUpManager != null) powerUpManager.OnPowerUpGranted -= OnPowerUpGranted;

            // Safety net: never leave the game (or the editor) with a frozen clock after teardown.
            Time.timeScale = 1f;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ---------------------------------------------------------------------
        // Reference wiring
        // ---------------------------------------------------------------------

        /// <summary>
        /// Resolves any reference that was not assigned in the inspector. Serialized references
        /// always win; these lookups are a pure fallback so the scene still works when built
        /// procedurally (SceneBuilder).
        /// </summary>
        private void CacheMissingReferences()
        {
            if (questionManager == null) questionManager = GetComponentInChildren<QuestionManager>(true);
            if (questionManager == null) questionManager = FindFirstObjectByType<QuestionManager>();

            if (gameTimer == null) gameTimer = GetComponentInChildren<GameTimer>(true);
            if (gameTimer == null) gameTimer = FindFirstObjectByType<GameTimer>();

            if (uiManager == null) uiManager = GetComponentInChildren<UIManager>(true);
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();

            if (progression == null) progression = GetComponentInChildren<GameProgression>(true);
            if (progression == null) progression = FindFirstObjectByType<GameProgression>();

            if (player == null) player = FindFirstObjectByType<PlayerController>();

            if (characterAnimator == null && player != null)
            {
                characterAnimator = player.GetComponentInChildren<CharacterAnimator>(true);
            }
            if (characterAnimator == null) characterAnimator = FindFirstObjectByType<CharacterAnimator>();

            if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();

            // AudioManager exposes a static accessor, so it can be resolved without a scene search.
            if (audioManager == null) audioManager = AudioManager.Instance;

            if (scoreManager == null) scoreManager = GetComponentInChildren<ScoreManager>(true);
            if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();

            if (livesManager == null) livesManager = GetComponentInChildren<LivesManager>(true);
            if (livesManager == null) livesManager = FindFirstObjectByType<LivesManager>();

            if (coinManager == null) coinManager = GetComponentInChildren<CoinManager>(true);
            if (coinManager == null) coinManager = FindFirstObjectByType<CoinManager>();

            if (powerUpManager == null) powerUpManager = GetComponentInChildren<PowerUpManager>(true);
            if (powerUpManager == null) powerUpManager = FindFirstObjectByType<PowerUpManager>();

            if (effectsManager == null) effectsManager = GetComponentInChildren<EffectsManager>(true);
            if (effectsManager == null) effectsManager = FindFirstObjectByType<EffectsManager>();

            if (levelSelectUI == null) levelSelectUI = GetComponentInChildren<LevelSelectUI>(true);
            if (levelSelectUI == null) levelSelectUI = FindFirstObjectByType<LevelSelectUI>();
        }

        /// <summary>Logs a warning for each essential reference that is still missing.</summary>
        private void WarnMissingReferences()
        {
            WarnIfNull(questionManager, nameof(questionManager));
            WarnIfNull(gameTimer, nameof(gameTimer));
            WarnIfNull(uiManager, nameof(uiManager));
            WarnIfNull(progression, nameof(progression));
            WarnIfNull(player, nameof(player));
            WarnIfNull(scoreManager, nameof(scoreManager));
            WarnIfNull(livesManager, nameof(livesManager));
            WarnIfNull(coinManager, nameof(coinManager));
            WarnIfNull(powerUpManager, nameof(powerUpManager));
            WarnIfNull(effectsManager, nameof(effectsManager));
            // levelSelectUI is optional in the prototype scene - no warning.
        }

        private void WarnIfNull(UnityEngine.Object reference, string fieldName)
        {
            if (reference == null)
            {
                Debug.LogWarning(
                    $"[GameManager] Required reference '{fieldName}' is missing. " +
                    "Assign it in the Inspector or call WireReferences().", this);
            }
        }

        /// <summary>
        /// Assigns the manager references from code (used by the SceneBuilder right after the
        /// managers are created). Only non-null arguments overwrite existing values, so this can
        /// be called repeatedly and still respect manual inspector assignments.
        /// </summary>
        public void WireReferences(
            QuestionManager qm,
            GameTimer gt,
            UIManager ui,
            AudioManager am,
            GameProgression gp,
            PlayerController pc,
            CharacterAnimator ca,
            CameraFollow cam,
            Transform spawn,
            ScoreManager sm,
            LivesManager lm,
            CoinManager cm,
            PowerUpManager pum,
            EffectsManager em,
            LevelSelectUI lsu)
        {
            if (qm != null) questionManager = qm;
            if (gt != null) gameTimer = gt;
            if (ui != null) uiManager = ui;
            if (am != null) audioManager = am;
            if (gp != null) progression = gp;
            if (pc != null) player = pc;
            if (ca != null) characterAnimator = ca;
            if (cam != null) cameraFollow = cam;
            if (spawn != null) playerSpawnPoint = spawn;
            if (sm != null) scoreManager = sm;
            if (lm != null) livesManager = lm;
            if (cm != null) coinManager = cm;
            if (pum != null) powerUpManager = pum;
            if (em != null) effectsManager = em;
            if (lsu != null) levelSelectUI = lsu;
        }

        // ---------------------------------------------------------------------
        // State machine
        // ---------------------------------------------------------------------

        private void SetState(GameState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        // ---------------------------------------------------------------------
        // Run flow
        // ---------------------------------------------------------------------

        /// <summary>Starts a fresh run at the highest unlocked level (menu / results screen).</summary>
        public void StartGame()
        {
            int levelIndex = SaveManager.HighestUnlockedLevel;
            StartLevel(levelIndex / 10, levelIndex % 10);
        }

        /// <summary>
        /// Starts the given level from scratch: resets progression, score, lives, coins and
        /// power-ups, respawns the player and starts the first question with the level's timer.
        /// </summary>
        /// <param name="world">Zero-based world index.</param>
        /// <param name="level">Zero-based level index inside the world.</param>
        public void StartLevel(int world, int level)
        {
            Time.timeScale = 1f;

            if (progression != null)
            {
                progression.ResetProgress();
                progression.SetLevel(world, level);
            }

            if (scoreManager != null) scoreManager.ResetScore();
            if (livesManager != null) livesManager.ResetLives();
            if (coinManager != null) coinManager.ResetCoins();
            if (powerUpManager != null) powerUpManager.ResetPowerUps();

            if (player != null)
            {
                player.ResetToSpawn(SpawnPosition);
                player.Freeze(false);
            }

            if (cameraFollow != null) cameraFollow.SnapToTarget();
            if (characterAnimator != null) characterAnimator.ResetCharacter();

            if (uiManager != null)
            {
                uiManager.ShowHUD();
                uiManager.SetScore(0);
                uiManager.SetProgress(0, QuestionsTotal);
            }

            SetState(GameState.Playing);

            if (questionManager != null)
            {
                questionManager.SetLevel(world, level);
                questionManager.NewQuestion();

                // Moving platforms from World 2 onward (world index >= 1): slow sideways drift.
                if (questionManager.AnswerPlatforms != null)
                {
                    bool moving = world >= 1;
                    float amplitude = world >= 2 ? 0.8f : 0.5f;
                    float speed = world >= 2 ? 1.1f : 0.7f;

                    foreach (AnswerPlatform platform in questionManager.AnswerPlatforms)
                    {
                        if (platform != null) platform.SetMoving(moving, amplitude, speed);
                    }
                }
            }

            if (gameTimer != null) gameTimer.StartTimer(LevelTimeLimit);

            PlayClickSound();
        }

        /// <summary>
        /// Starts the next level after the current one (used by the level complete screen).
        /// On the final level, opens the level select screen instead.
        /// </summary>
        public void NextLevel()
        {
            int nextIndex = CurrentWorld * 10 + CurrentLevel + 1;

            if (nextIndex >= SaveManager.TotalLevels)
            {
                OpenLevelSelect();
                return;
            }

            StartLevel(nextIndex / 10, nextIndex % 10);
        }

        /// <summary>
        /// Opens the level select screen. Only allowed from the menu, the level complete screen
        /// or the game over screen.
        /// </summary>
        public void OpenLevelSelect()
        {
            if (State != GameState.Menu && State != GameState.Complete && State != GameState.GameOver) return;

            SetState(GameState.LevelSelect);

            if (levelSelectUI != null) levelSelectUI.Build();
            if (uiManager != null) uiManager.ShowLevelSelect();

            PlayClickSound();
        }

        /// <summary>
        /// Called by an <see cref="AnswerPlatform"/> when the player touches it.
        /// Resolves the answer, plays the matching feedback and schedules the follow-up.
        /// </summary>
        public void ValidateAnswer(AnswerPlatform platform)
        {
            if (State != GameState.Playing || platform == null) return;

            if (gameTimer != null) gameTimer.StopTimer();

            if (platform.IsCorrect)
            {
                if (progression != null) progression.RegisterCorrect();

                if (scoreManager != null) scoreManager.RegisterCorrect();

                if (effectsManager != null)
                {
                    effectsManager.PlayConfetti(platform.transform.position);
                    effectsManager.FovKick(6f);
                }

                platform.ShowCorrect();
                if (audioManager != null) audioManager.PlayCorrect();
                if (characterAnimator != null) characterAnimator.OnCelebrate();

                if (uiManager != null)
                {
                    uiManager.SetScore(scoreManager != null ? scoreManager.Score : CorrectCount);
                    uiManager.SetProgress(CorrectCount, QuestionsTotal);
                }

                // Either finish the level or move on to the next question after a short beat.
                bool levelComplete = progression != null && progression.IsLevelComplete;
                System.Action followUp = levelComplete ? (System.Action)CompleteLevel : NextQuestion;
                StartCoroutine(DelayedAction(nextQuestionDelay, followUp));
            }
            else
            {
                // A shield absorbs the mistake entirely.
                if (powerUpManager != null && powerUpManager.ConsumeShield())
                {
                    ShieldSaved(platform);
                    return;
                }

                // Wrong-answer feedback (red flash, explosion, fail panel) is fully handled by
                // FailQuestion so the sequence stays identical for timeouts and falls.
                FailQuestion("Wrong answer!");
            }
        }

        /// <summary>Timer callback - the player did not answer in time.</summary>
        public void OnTimerTimeout()
        {
            if (State != GameState.Playing) return;

            if (powerUpManager != null && powerUpManager.ConsumeShield())
            {
                ShieldSaved(null);
                return;
            }

            FailQuestion("Time ran out!");
        }

        /// <summary>Player fell off the platform course.</summary>
        public void OnPlayerFell()
        {
            if (State != GameState.Playing) return;

            if (powerUpManager != null && powerUpManager.ConsumeShield())
            {
                ShieldSaved(null);
                return;
            }

            FailQuestion("You fell off!");
        }

        /// <summary>
        /// Reserved hook for future landing-based rules. Intentionally empty: the landing sound is
        /// already played by <see cref="PlayerController"/>, so re-playing it here would double it.
        /// Safe to call at any time.
        /// </summary>
        public void OnPlayerLanded()
        {
            // No-op by design (see summary).
        }

        /// <summary>
        /// A shield absorbed a failure: shows feedback and retries the question after a short beat
        /// without costing a heart or a mistake.
        /// </summary>
        private void ShieldSaved(AnswerPlatform platform)
        {
            // Prevent a second timeout / fall from firing during the short retry beat.
            if (gameTimer != null) gameTimer.StopTimer();
            if (player != null) player.Freeze(true);

            if (platform != null) platform.ShowWrong();
            if (audioManager != null) audioManager.PlayPowerUp();

            if (effectsManager != null && player != null)
            {
                effectsManager.ShowPopup(
                    "Shield saved you!",
                    player.transform.position + Vector3.up * 2f,
                    new Color(0.298f, 0.588f, 0.439f));
            }

            StartCoroutine(DelayedAction(1.0f, RestartQuestion));
        }

        /// <summary>
        /// Shared failure path: freezes the player, plays the dizzy animation and shows the fail panel
        /// (or the explosion and the game over screen when the last heart is lost) after <see cref="failDelay"/>
        /// seconds. The mistake is counted exactly once here.
        /// </summary>
        private void FailQuestion(string reason)
        {
            SetState(GameState.Failed);

            if (gameTimer != null) gameTimer.StopTimer();
            if (progression != null) progression.RegisterMistake();
            if (scoreManager != null) scoreManager.RegisterMistake();

            bool gameOver = livesManager != null && livesManager.LoseHeart();

            if (audioManager != null)
            {
                audioManager.PlayWrong();
                if (gameOver) audioManager.PlayExplosion();
            }

            if (gameOver)
            {
                if (characterAnimator != null) characterAnimator.OnExplode();
            }
            else if (characterAnimator != null)
            {
                characterAnimator.OnDizzy();
            }
            if (player != null) player.Freeze(true);

            if (gameOver)
            {
                if (effectsManager != null && player != null) effectsManager.PlayExplosion(player.transform.position);
                StartCoroutine(DelayedAction(failDelay, GameOver));
            }
            else
            {
                StartCoroutine(DelayedAction(failDelay, () =>
                {
                    if (uiManager != null) uiManager.ShowFail(reason);
                }));
            }
        }

        /// <summary>All hearts lost: ends the run, stores the best score and shows game over.</summary>
        private void GameOver()
        {
            SetState(GameState.GameOver);

            if (gameTimer != null) gameTimer.StopTimer();
            if (player != null) player.Freeze(true);
            if (audioManager != null) audioManager.PlayGameOver();
            if (characterAnimator != null) characterAnimator.OnExplode();

            int score = scoreManager != null ? scoreManager.Score : 0;
            SaveManager.SetBestScore(score);

            if (uiManager != null) uiManager.ShowGameOver(score, SaveManager.BestScore);
        }

        /// <summary>
        /// Retries after a failure. A brand-new question is generated for the same slot, which is
        /// acceptable for the prototype and is communicated in the UI as "Retry".
        /// </summary>
        public void RestartQuestion()
        {
            Time.timeScale = 1f;

            if (player != null)
            {
                player.ResetToSpawn(SpawnPosition);
                player.Freeze(false);
            }

            if (cameraFollow != null) cameraFollow.SnapToTarget();
            if (characterAnimator != null) characterAnimator.ResetCharacter();

            if (uiManager != null) uiManager.ShowHUD();

            SetState(GameState.Playing);

            if (questionManager != null) questionManager.NewQuestion();
            if (gameTimer != null) gameTimer.StartTimer(LevelTimeLimit);

            PlayClickSound();
        }

        /// <summary>Advances to the next question of the current level.</summary>
        public void NextQuestion()
        {
            if (questionManager != null) questionManager.NewQuestion();
            if (gameTimer != null) gameTimer.StartTimer(LevelTimeLimit);

            if (uiManager != null) uiManager.SetProgress(CorrectCount, QuestionsTotal);
        }

        /// <summary>
        /// Level finished: stops the clock, freezes the player, banks coins, saves stars / best
        /// score, unlocks the next level and shows the results.
        /// </summary>
        private void CompleteLevel()
        {
            SetState(GameState.Complete);

            if (gameTimer != null) gameTimer.StopTimer();
            if (player != null) player.Freeze(true);
            if (audioManager != null) audioManager.PlayWin();

            int score = scoreManager != null ? scoreManager.Score : 0;
            int coins = coinManager != null ? coinManager.CoinsThisLevel : 0;
            if (coinManager != null) coinManager.BankCoins();

            int mistakes = progression != null ? progression.MistakeCount : 0;
            int stars = mistakes == 0 ? 3 : (mistakes == 1 ? 2 : 1);

            int levelIndex = CurrentWorld * 10 + CurrentLevel;
            SaveManager.SetStars(levelIndex, stars);
            SaveManager.SetBestScore(score);
            SaveManager.UnlockLevel(levelIndex + 1);

            if (uiManager != null) uiManager.ShowComplete(score, coins, stars, mistakes);
        }

        // ---------------------------------------------------------------------
        // Power-ups
        // ---------------------------------------------------------------------

        private void OnPowerUpGranted(PowerUpType type)
        {
            if (type == PowerUpType.ExtraTime && gameTimer != null) gameTimer.AddTime(5f);
        }

        // ---------------------------------------------------------------------
        // Pause / restart / navigation
        // ---------------------------------------------------------------------

        /// <summary>Pauses an active round (freezes the clock via Time.timeScale).</summary>
        public void PauseGame()
        {
            if (State != GameState.Playing) return;

            SetState(GameState.Paused);
            Time.timeScale = 0f;

            if (uiManager != null) uiManager.ShowPause();
        }

        /// <summary>Resumes a paused round.</summary>
        public void ResumeGame()
        {
            if (State != GameState.Paused) return;

            Time.timeScale = 1f;
            SetState(GameState.Playing);

            if (uiManager != null) uiManager.ShowHUD();
        }

        /// <summary>Restarts the current level from scratch (used by the results / fail screen).</summary>
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            StartLevel(CurrentWorld, CurrentLevel);
        }

        /// <summary>Abandons the current round and returns to the main menu.</summary>
        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            SetState(GameState.Menu);

            if (gameTimer != null) gameTimer.StopTimer();
            if (questionManager != null) questionManager.ClearPlatforms();

            if (player != null)
            {
                player.ResetToSpawn(SpawnPosition);
                player.Freeze(true);
            }

            if (characterAnimator != null) characterAnimator.ResetCharacter();
            if (uiManager != null) uiManager.ShowMenu();
        }

        /// <summary>Plays the UI click sound. Wire this to UI buttons from the inspector / code.</summary>
        public void PlayClickSound()
        {
            if (audioManager != null) audioManager.PlayClick();
        }

        // ---------------------------------------------------------------------
        // Coroutine helpers
        // ---------------------------------------------------------------------

        /// <summary>
        /// Runs <paramref name="action"/> after <paramref name="delay"/> seconds of real time.
        /// The flow state is captured when the coroutine is scheduled and re-checked before the
        /// action runs, so a delayed follow-up is silently dropped when the player quit to the
        /// menu, restarted or otherwise changed state in the meantime.
        /// </summary>
        private System.Collections.IEnumerator DelayedAction(float delay, System.Action action)
        {
            GameState stateAtSchedule = State;

            // Realtime delay: keeps working even if Time.timeScale is 0 (e.g. paused).
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            if (action == null) yield break;
            if (State != stateAtSchedule) yield break; // State changed while waiting - drop it.

            action();
        }

        // ---------------------------------------------------------------------
        // Timer -> HUD
        // ---------------------------------------------------------------------

        private void OnTimerTick(float remaining)
        {
            if (uiManager != null)
            {
                uiManager.SetTimer(remaining, questionTimeLimit);
            }
        }
    }
}
