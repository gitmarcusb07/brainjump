using System.Collections;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Central gameplay orchestrator for BrainJump.
    /// Owns the high-level flow state machine (Menu -> Playing -> Paused / Failed / Complete)
    /// and coordinates the other managers (questions, timer, UI, audio, progression, player).
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
            Complete
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
        [Tooltip("Seconds the player has to answer a single question.")]
        [SerializeField] private float questionTimeLimit = 10f;
        [Tooltip("Delay between a correct answer and the next question / level complete screen.")]
        [SerializeField] private float nextQuestionDelay = 1.4f;
        [Tooltip("Delay between a failed answer and the fail screen (lets the explosion play).")]
        [SerializeField] private float failDelay = 0.6f;

        /// <summary>Current flow state.</summary>
        public GameState State { get; private set; } = GameState.Menu;

        /// <summary>Raised with the new state whenever <see cref="State"/> changes.</summary>
        public event System.Action<GameState> OnStateChanged;

        /// <summary>Correct answers in the current level (0 when progression is not wired).</summary>
        public int CorrectCount => progression != null ? progression.CorrectCount : 0;

        /// <summary>Questions required to complete one level.</summary>
        public int QuestionsTotal => GameProgression.QuestionsPerLevel;

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
        }

        /// <summary>Logs a warning for each essential reference that is still missing.</summary>
        private void WarnMissingReferences()
        {
            WarnIfNull(questionManager, nameof(questionManager));
            WarnIfNull(gameTimer, nameof(gameTimer));
            WarnIfNull(uiManager, nameof(uiManager));
            WarnIfNull(progression, nameof(progression));
            WarnIfNull(player, nameof(player));
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
            Transform spawn)
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

        /// <summary>Starts a fresh run from the menu / results screen.</summary>
        public void StartGame()
        {
            Time.timeScale = 1f;

            if (progression != null) progression.ResetProgress();

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

            if (questionManager != null) questionManager.NewQuestion();
            if (gameTimer != null) gameTimer.StartTimer(questionTimeLimit);

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

                platform.ShowCorrect();
                if (audioManager != null) audioManager.PlayCorrect();
                if (characterAnimator != null) characterAnimator.OnCelebrate();

                if (uiManager != null)
                {
                    uiManager.SetScore(CorrectCount);
                    uiManager.SetProgress(CorrectCount, QuestionsTotal);
                }

                // Either finish the level or move on to the next question after a short beat.
                bool levelComplete = progression != null && progression.IsLevelComplete;
                System.Action followUp = levelComplete ? (System.Action)CompleteLevel : NextQuestion;
                StartCoroutine(DelayedAction(nextQuestionDelay, followUp));
            }
            else
            {
                // Wrong-answer feedback (red flash, explosion, fail panel) is fully handled by
                // FailQuestion so the sequence stays identical for timeouts and falls.
                FailQuestion("Wrong answer!");
            }
        }

        /// <summary>Timer callback - the player did not answer in time.</summary>
        public void OnTimerTimeout()
        {
            if (State != GameState.Playing) return;
            FailQuestion("Time ran out!");
        }

        /// <summary>Player fell off the platform course.</summary>
        public void OnPlayerFell()
        {
            if (State != GameState.Playing) return;
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
        /// Shared failure path: freezes the player, plays the explosion and shows the fail panel
        /// after <see cref="failDelay"/> seconds. The mistake is counted exactly once here.
        /// </summary>
        private void FailQuestion(string reason)
        {
            SetState(GameState.Failed);

            if (gameTimer != null) gameTimer.StopTimer();
            if (progression != null) progression.RegisterMistake();

            if (audioManager != null)
            {
                audioManager.PlayWrong();
                audioManager.PlayExplosion();
            }

            if (characterAnimator != null) characterAnimator.OnExplode();
            if (player != null) player.Freeze(true);

            StartCoroutine(DelayedAction(failDelay, () =>
            {
                if (uiManager != null) uiManager.ShowFail(reason);
            }));
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
            if (gameTimer != null) gameTimer.StartTimer(questionTimeLimit);

            PlayClickSound();
        }

        /// <summary>Advances to the next question of the current level.</summary>
        public void NextQuestion()
        {
            if (questionManager != null) questionManager.NewQuestion();
            if (gameTimer != null) gameTimer.StartTimer(questionTimeLimit);

            if (uiManager != null) uiManager.SetProgress(CorrectCount, QuestionsTotal);
        }

        /// <summary>Level finished: stops the clock, freezes the player and shows the results.</summary>
        private void CompleteLevel()
        {
            SetState(GameState.Complete);

            if (gameTimer != null) gameTimer.StopTimer();
            if (player != null) player.Freeze(true);
            if (audioManager != null) audioManager.PlayWin();

            if (uiManager != null)
            {
                uiManager.ShowComplete(CorrectCount, progression != null ? progression.MistakeCount : 0);
            }
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

        /// <summary>Restarts the whole level from scratch (used by the results / fail screen).</summary>
        public void RestartLevel()
        {
            Time.timeScale = 1f;
            StartGame();
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
