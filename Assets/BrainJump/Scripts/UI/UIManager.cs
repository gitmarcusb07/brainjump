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

        [Header("HUD Text")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Panel Text")]
        [SerializeField] private TextMeshProUGUI failReasonText;
        [SerializeField] private TextMeshProUGUI completeScoreText;
        [SerializeField] private TextMeshProUGUI completeTitleText;
        [SerializeField] private TextMeshProUGUI menuTitleText;

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
        private const string LevelLabel = "Level 1";

        private void Awake()
        {
            ApplySafeArea();
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

        public void ShowComplete(int correct, int mistakes)
        {
            HideAllPanels();
            SetPanelActive(completePanel, true);

            if (completeTitleText != null)
            {
                completeTitleText.text = CompleteTitle;
            }

            if (completeScoreText != null)
            {
                string mistakeWord = mistakes == 1 ? string.Empty : "s";
                completeScoreText.text =
                    $"You solved {correct} questions with {mistakes} mistake{mistakeWord}.";
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

            if (levelText != null)
            {
                levelText.text = LevelLabel;
            }
        }

        public void SetScore(int correct)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {correct}";
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
