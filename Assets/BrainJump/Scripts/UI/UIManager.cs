using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainJump
{
    /// <summary>
    /// Owns all canvas panels and HUD elements. Visual updates only: button
    /// click listeners are wired by the SceneBuilder, not by this component.
    /// All references are optional and every public method is null-safe so it
    /// can be called immediately after SceneBuilder construction.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject failPanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject levelSelectPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("HUD Text")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI heartsText;
        [SerializeField] private TextMeshProUGUI comboText;

        [Header("Panel Text")]
        [SerializeField] private TextMeshProUGUI failReasonText;
        [SerializeField] private TextMeshProUGUI completeScoreText;
        [SerializeField] private TextMeshProUGUI completeTitleText;
        [SerializeField] private TextMeshProUGUI menuTitleText;
        [SerializeField] private TextMeshProUGUI completeStarsText;
        [SerializeField] private TextMeshProUGUI gameOverScoreText;
        [SerializeField] private TextMeshProUGUI gameOverTitleText;
        [SerializeField] private TextMeshProUGUI musicToggleLabel;
        [SerializeField] private TextMeshProUGUI sfxToggleLabel;

        [Header("Timer")]
        [SerializeField] private Image timerFill;

        [Tooltip("Timer text colour when three seconds or fewer remain. Default Coral #E98270.")]
        [SerializeField] private Color lowTimeColor = new Color(0.9137255f, 0.50980395f, 0.4392157f, 1f);

        [Tooltip("Timer text colour during normal play. Default Warm Off-white #F7F4EC.")]
        [SerializeField] private Color normalTimeColor = new Color(0.96862745f, 0.95686275f, 0.9254902f, 1f);

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button menuButton;

        private const string MenuTitle = "BrainJump";
        private const string CompleteTitle = "Level Complete!";

        private bool _settingsFromPause;

        private void Awake()
        {
            ApplySafeArea();
        }

        private void Start()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += SetScore;
                ScoreManager.Instance.OnComboChanged += SetCombo;
            }

            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinsChanged += SetCoins;
            }

            if (LivesManager.Instance != null)
            {
                LivesManager.Instance.OnHeartsChanged += SetHearts;
            }
        }

        // ---------------------------------------------------------------------
        // Screen state
        // ---------------------------------------------------------------------

        public void ShowMenu()
        {
            HideAllPanels();
            SetPanelActive(menuPanel, true);

            if (menuTitleText != null)
            {
                menuTitleText.text = MenuTitle;
            }
        }

        public void ShowHUD()
        {
            HideAllPanels();
            SetPanelActive(hudPanel, true);
        }

        public void ShowFail(string reason)
        {
            HideAllPanels();
            SetPanelActive(failPanel, true);

            if (failReasonText != null)
            {
                failReasonText.text = reason ?? string.Empty;
            }
        }

        /// <summary>
        /// Shows the level complete panel with the final score, coins collected,
        /// stars earned (0-3) and the number of mistakes made.
        /// </summary>
        /// <param name="score">Points scored in the level.</param>
        /// <param name="coins">Coins collected in the level.</param>
        /// <param name="stars">Stars earned, out of three.</param>
        /// <param name="mistakes">Number of mistakes made.</param>
        public void ShowComplete(int score, int coins, int stars, int mistakes)
        {
            HideAllPanels();
            SetPanelActive(completePanel, true);

            if (completeTitleText != null)
            {
                completeTitleText.text = CompleteTitle;
            }

            if (completeScoreText != null)
            {
                completeScoreText.text = $"Score: {score}  •  Coins: {coins}";
            }

            if (completeStarsText != null)
            {
                completeStarsText.text = $"Stars: {stars}/3"
                    + (mistakes == 0
                        ? "  Perfect!"
                        : mistakes == 1
                            ? "  1 mistake"
                            : $"  {mistakes} mistakes");
            }
        }

        /// <summary>
        /// Pause is an overlay on top of the HUD, so the HUD remains visible.
        /// </summary>
        public void ShowPause()
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(failPanel, false);
            SetPanelActive(completePanel, false);
            SetPanelActive(levelSelectPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(gameOverPanel, false);
            SetPaused(true);
        }

        /// <summary>
        /// Convenience helper for toggling the pause overlay independently.
        /// </summary>
        public void SetPaused(bool paused)
        {
            SetPanelActive(pausePanel, paused);
        }

        private void HideAllPanels()
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(hudPanel, false);
            SetPanelActive(failPanel, false);
            SetPanelActive(completePanel, false);
            SetPanelActive(pausePanel, false);
            SetPanelActive(levelSelectPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(gameOverPanel, false);
        }

        /// <summary>
        /// Hides every other panel and shows the level select panel.
        /// </summary>
        public void ShowLevelSelect()
        {
            HideAllPanels();
            SetPanelActive(levelSelectPanel, true);
        }

        /// <summary>
        /// Shows the settings panel, remembering whether it was opened from the
        /// pause overlay so that <see cref="HideSettings"/> can return there.
        /// </summary>
        public void ShowSettings()
        {
            _settingsFromPause = pausePanel != null && pausePanel.activeSelf;
            HideAllPanels();
            SetPanelActive(settingsPanel, true);
            UpdateToggleLabels();
        }

        /// <summary>
        /// Hides the settings panel and returns to the pause overlay or the
        /// main menu, depending on where settings were opened from.
        /// </summary>
        public void HideSettings()
        {
            SetPanelActive(settingsPanel, false);

            if (_settingsFromPause)
            {
                ShowPause();
            }
            else
            {
                ShowMenu();
            }
        }

        /// <summary>
        /// Hides every other panel and shows the game over panel with the final
        /// score and the best score.
        /// </summary>
        /// <param name="score">Score achieved in this run.</param>
        /// <param name="bestScore">Best score recorded so far.</param>
        public void ShowGameOver(int score, int bestScore)
        {
            HideAllPanels();
            SetPanelActive(gameOverPanel, true);

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = "Game Over";
            }

            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = $"Score: {score}  •  Best: {bestScore}";
            }
        }

        // ---------------------------------------------------------------------
        // HUD updates
        // ---------------------------------------------------------------------

        public void SetQuestion(string text)
        {
            if (questionText != null)
            {
                questionText.text = text;
            }
        }

        public void SetTimer(float remaining, float duration)
        {
            float clampedRemaining = Mathf.Max(0f, remaining);

            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(clampedRemaining).ToString() + "s";
                timerText.color = clampedRemaining <= 3f ? lowTimeColor : normalTimeColor;
            }

            if (timerFill != null)
            {
                timerFill.fillAmount = duration > 0f
                    ? Mathf.Clamp01(clampedRemaining / duration)
                    : 0f;
            }
        }

        public void SetProgress(int current, int total)
        {
            if (progressText != null)
            {
                int shown = Mathf.Clamp(current + 1, 1, total);
                progressText.text = $"Question {shown} / {total}";
            }
        }

        /// <summary>
        /// Updates the HUD score label with the current points total.
        /// </summary>
        /// <param name="points">Current points total.</param>
        public void SetScore(int points)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {points}";
            }
        }

        /// <summary>
        /// Updates the HUD coins label.
        /// </summary>
        /// <param name="coins">Current coin count.</param>
        public void SetCoins(int coins)
        {
            if (coinsText != null)
            {
                coinsText.text = $"Coins: {coins}";
            }
        }

        /// <summary>
        /// Updates the HUD lives label.
        /// </summary>
        /// <param name="hearts">Current number of hearts.</param>
        public void SetHearts(int hearts)
        {
            if (heartsText != null)
            {
                heartsText.text = $"Lives: {hearts}";
            }
        }

        /// <summary>
        /// Updates the HUD combo label. The label is cleared when the combo is
        /// below two.
        /// </summary>
        /// <param name="combo">Current combo count.</param>
        public void SetCombo(int combo)
        {
            if (comboText != null)
            {
                comboText.text = combo >= 2 ? $"COMBO x{combo}" : string.Empty;
            }
        }

        /// <summary>
        /// Updates the HUD level label using zero-based world and level indices.
        /// </summary>
        /// <param name="world">Zero-based world index.</param>
        /// <param name="level">Zero-based level index within the world.</param>
        public void SetLevelLabel(int world, int level)
        {
            if (levelText != null)
            {
                levelText.text = $"World {world + 1} · Level {level + 1}";
            }
        }

        // ---------------------------------------------------------------------
        // Settings
        // ---------------------------------------------------------------------

        /// <summary>
        /// Toggles background music on or off, persists the setting and
        /// refreshes the settings labels.
        /// </summary>
        public void ToggleMusic()
        {
            bool enabled = !SaveManager.MusicEnabled;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicEnabled(enabled);
            }
            else
            {
                SaveManager.MusicEnabled = enabled;
            }

            UpdateToggleLabels();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
            }
        }

        /// <summary>
        /// Toggles sound effects on or off, persists the setting and refreshes
        /// the settings labels.
        /// </summary>
        public void ToggleSfx()
        {
            bool enabled = !SaveManager.SfxEnabled;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxEnabled(enabled);
            }
            else
            {
                SaveManager.SfxEnabled = enabled;
            }

            UpdateToggleLabels();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
            }
        }

        /// <summary>
        /// Clears all saved progress and refreshes the settings labels.
        /// </summary>
        public void ResetProgress()
        {
            SaveManager.ResetAllProgress();
            UpdateToggleLabels();
        }

        private void UpdateToggleLabels()
        {
            if (musicToggleLabel != null)
            {
                musicToggleLabel.text = "Music: " + (SaveManager.MusicEnabled ? "ON" : "OFF");
            }

            if (sfxToggleLabel != null)
            {
                sfxToggleLabel.text = "SFX: " + (SaveManager.SfxEnabled ? "ON" : "OFF");
            }
        }

        // ---------------------------------------------------------------------
        // SceneBuilder wiring
        // ---------------------------------------------------------------------

        /// <summary>
        /// Assigns references created by the SceneBuilder. Null arguments leave
        /// the existing inspector-assigned reference untouched.
        /// </summary>
        public void WireReferences(
            GameObject menu,
            GameObject hud,
            GameObject fail,
            GameObject complete,
            GameObject pause,
            TextMeshProUGUI question,
            TextMeshProUGUI timer,
            TextMeshProUGUI level,
            TextMeshProUGUI progress,
            TextMeshProUGUI score,
            TextMeshProUGUI failReason,
            TextMeshProUGUI completeScore,
            TextMeshProUGUI completeTitle,
            TextMeshProUGUI menuTitle,
            Image fill)
        {
            if (menu != null) menuPanel = menu;
            if (hud != null) hudPanel = hud;
            if (fail != null) failPanel = fail;
            if (complete != null) completePanel = complete;
            if (pause != null) pausePanel = pause;

            if (question != null) questionText = question;
            if (timer != null) timerText = timer;
            if (level != null) levelText = level;
            if (progress != null) progressText = progress;
            if (score != null) scoreText = score;

            if (failReason != null) failReasonText = failReason;
            if (completeScore != null) completeScoreText = completeScore;
            if (completeTitle != null) completeTitleText = completeTitle;
            if (menuTitle != null) menuTitleText = menuTitle;

            if (fill != null) timerFill = fill;
        }

        /// <summary>
        /// Assigns the additional panels and labels created by the SceneBuilder
        /// (level select, settings, game over, coins, hearts, combo, stars and
        /// settings toggles). Null arguments leave the existing
        /// inspector-assigned reference untouched.
        /// </summary>
        public void WireExtraPanels(
            GameObject levelSelect,
            GameObject settings,
            GameObject gameOver,
            TextMeshProUGUI coins,
            TextMeshProUGUI hearts,
            TextMeshProUGUI combo,
            TextMeshProUGUI completeStars,
            TextMeshProUGUI gameOverScore,
            TextMeshProUGUI gameOverTitle,
            TextMeshProUGUI musicLabel,
            TextMeshProUGUI sfxLabel)
        {
            if (levelSelect != null) levelSelectPanel = levelSelect;
            if (settings != null) settingsPanel = settings;
            if (gameOver != null) gameOverPanel = gameOver;

            if (coins != null) coinsText = coins;
            if (hearts != null) heartsText = hearts;
            if (combo != null) comboText = combo;

            if (completeStars != null) completeStarsText = completeStars;
            if (gameOverScore != null) gameOverScoreText = gameOverScore;
            if (gameOverTitle != null) gameOverTitleText = gameOverTitle;
            if (musicLabel != null) musicToggleLabel = musicLabel;
            if (sfxLabel != null) sfxToggleLabel = sfxLabel;
        }

        // ---------------------------------------------------------------------
        // Safe area
        // ---------------------------------------------------------------------

        private void ApplySafeArea()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                return;
            }

            RectTransform safeAreaRect = FindChildRectTransform(canvasRect, "SafeArea");
            if (safeAreaRect == null)
            {
                return;
            }

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2 anchorMin = new Vector2(
                safeArea.xMin / Screen.width,
                safeArea.yMin / Screen.height);
            Vector2 anchorMax = new Vector2(
                safeArea.xMax / Screen.width,
                safeArea.yMax / Screen.height);

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
        }

        private static RectTransform FindChildRectTransform(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name == childName)
                {
                    return child as RectTransform;
                }

                RectTransform nested = FindChildRectTransform(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private static void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null && panel.activeSelf != active)
            {
                panel.SetActive(active);
            }
        }
    }
}
