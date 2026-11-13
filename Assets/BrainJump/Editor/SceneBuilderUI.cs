using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrainJump.Editor
{
    public static partial class SceneBuilder
    {
        private const string MainScenePath = "Assets/BrainJump/Scenes/MainScene.unity";

        internal static void BuildUIAndManagers()
        {
            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning("BrainJump: import TMP Essentials once via Window > TextMeshPro > Import TMP Essential Resources, otherwise UI text may not render.");
            }

            Color midnightBlue = ColorFromHex("#172B42");
            Color skyBlue = ColorFromHex("#75B9D4");
            Color warmGold = ColorFromHex("#F2B85B");
            Color coral = ColorFromHex("#E98270");
            Color offWhite = ColorFromHex("#F7F4EC");

            // ------------------------------------------------------------------
            // Canvas
            // ------------------------------------------------------------------
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject safeAreaGo = new GameObject("SafeArea", typeof(RectTransform));
            RectTransform safeArea = (RectTransform)safeAreaGo.transform;
            safeArea.SetParent(canvasGo.transform, false);
            Stretch(safeArea);

            // ------------------------------------------------------------------
            // EventSystem (only if the scene has none)
            // ------------------------------------------------------------------
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            // ------------------------------------------------------------------
            // Menu panel
            // ------------------------------------------------------------------
            Color menuBg = midnightBlue;
            menuBg.a = 0.96f;
            RectTransform menuPanel = MakePanel("MenuPanel", safeArea, menuBg);

            TextMeshProUGUI menuTitleText = MakeText("Title", menuPanel, "BrainJump", 110, warmGold,
                TextAlignmentOptions.Center, true, new Vector2(0f, 420f), new Vector2(900f, 160f));

            MakeText("Subtitle", menuPanel, "Jump to the right answer!", 40, offWhite,
                TextAlignmentOptions.Center, false, new Vector2(0f, 300f), new Vector2(900f, 80f));

            Button playButton = MakeButton("PlayButton", menuPanel, "▶ PLAY", warmGold, midnightBlue,
                new Vector2(0f, 0f), new Vector2(520f, 130f), 48);

            Button settingsButton = MakeButton("SettingsButton", menuPanel, "Settings", skyBlue, midnightBlue,
                new Vector2(0f, -180f), new Vector2(420f, 100f), 36);

            // ------------------------------------------------------------------
            // HUD panel (transparent)
            // ------------------------------------------------------------------
            RectTransform hudPanel = MakePanel("HUD", safeArea, new Color(0f, 0f, 0f, 0f));
            Image hudImage = hudPanel.GetComponent<Image>();
            if (hudImage != null)
            {
                hudImage.raycastTarget = false;
            }

            TextMeshProUGUI levelText = MakeText("LevelText", hudPanel, "Level 1", 36, offWhite,
                TextAlignmentOptions.Left, true, new Vector2(-380f, 880f), new Vector2(300f, 60f));

            TextMeshProUGUI progressText = MakeText("ProgressText", hudPanel, "Question 1 / 10", 36, offWhite,
                TextAlignmentOptions.Center, false, new Vector2(0f, 880f), new Vector2(400f, 60f));

            TextMeshProUGUI timerText = MakeText("TimerText", hudPanel, "10s", 44, offWhite,
                TextAlignmentOptions.Right, true, new Vector2(380f, 880f), new Vector2(200f, 60f));

            TextMeshProUGUI scoreText = MakeText("ScoreText", hudPanel, "Score: 0", 30, offWhite,
                TextAlignmentOptions.Left, false, new Vector2(-380f, 810f), new Vector2(400f, 50f));

            // Timer bar background + fill
            RectTransform timerBarBg = MakeImage("TimerBarBackground", hudPanel, new Color(0f, 0f, 0f, 0.35f),
                new Vector2(330f, 830f), new Vector2(260f, 18f));

            RectTransform timerFillRect = MakeImage("TimerFill", timerBarBg, warmGold,
                Vector2.zero, new Vector2(260f, 18f));
            timerFillRect.anchorMin = Vector2.zero;
            timerFillRect.anchorMax = Vector2.one;
            timerFillRect.offsetMin = Vector2.zero;
            timerFillRect.offsetMax = Vector2.zero;

            Image timerFillImage = timerFillRect.GetComponent<Image>();
            timerFillImage.type = Image.Type.Filled;
            timerFillImage.fillMethod = Image.FillMethod.Horizontal;
            timerFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            timerFillImage.fillAmount = 1f;

            TextMeshProUGUI questionText = MakeText("QuestionText", hudPanel, "3 + 4 = ?", 84, offWhite,
                TextAlignmentOptions.Center, true, new Vector2(0f, 620f), new Vector2(1000f, 140f));

            Color pauseBg = skyBlue;
            pauseBg.a = 0.85f;
            Button pauseButton = MakeButton("PauseButton", hudPanel, "II", pauseBg, midnightBlue,
                new Vector2(460f, 880f), new Vector2(90f, 90f), 36);

            // Joystick
            GameObject joystickGo = new GameObject("Joystick", typeof(RectTransform), typeof(Image));
            RectTransform joystickRect = (RectTransform)joystickGo.transform;
            joystickRect.SetParent(hudPanel, false);
            joystickRect.anchorMin = new Vector2(0.5f, 0.5f);
            joystickRect.anchorMax = new Vector2(0.5f, 0.5f);
            joystickRect.pivot = new Vector2(0.5f, 0.5f);
            joystickRect.sizeDelta = new Vector2(280f, 280f);
            joystickRect.anchoredPosition = new Vector2(-360f, -680f);

            Image joystickImage = joystickGo.GetComponent<Image>();
            joystickImage.color = new Color(1f, 1f, 1f, 0.25f);
            joystickImage.raycastTarget = true;

            RectTransform knobRect = MakeImage("Knob", joystickRect, warmGold,
                Vector2.zero, new Vector2(120f, 120f));
            Image knobImage = knobRect.GetComponent<Image>();
            if (knobImage != null)
            {
                knobImage.raycastTarget = false;
            }

            MobileJoystick mobileJoystick = joystickGo.AddComponent<MobileJoystick>();
            SerializedObject joystickSerialized = new SerializedObject(mobileJoystick);
            SerializedProperty backgroundProp = joystickSerialized.FindProperty("background");
            if (backgroundProp != null)
            {
                backgroundProp.objectReferenceValue = joystickRect;
            }
            SerializedProperty knobProp = joystickSerialized.FindProperty("knob");
            if (knobProp != null)
            {
                knobProp.objectReferenceValue = knobRect;
            }
            joystickSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mobileJoystick);

            // Jump button
            Button jumpButton = MakeButton("JumpButton", hudPanel, "JUMP", coral, offWhite,
                new Vector2(360f, -680f), new Vector2(240f, 240f), 44);
            JumpButton jumpButtonComponent = jumpButton.gameObject.AddComponent<JumpButton>();
            EditorUtility.SetDirty(jumpButtonComponent);

            // ------------------------------------------------------------------
            // Fail panel
            // ------------------------------------------------------------------
            RectTransform failPanel = MakePanel("FailPanel", safeArea, new Color(0f, 0f, 0f, 0.6f));
            RectTransform failCard = MakeImage("Card", failPanel, midnightBlue, Vector2.zero, new Vector2(800f, 560f));

            TextMeshProUGUI failReasonText = MakeText("FailReasonText", failCard, "Wrong answer!", 64, coral,
                TextAlignmentOptions.Center, true, new Vector2(0f, 120f), new Vector2(700f, 100f));

            MakeText("FailSubtext", failCard, "Try again!", 36, offWhite,
                TextAlignmentOptions.Center, false, new Vector2(0f, 20f), new Vector2(700f, 60f));

            Button retryButton = MakeButton("RetryButton", failCard, "RETRY", warmGold, midnightBlue,
                new Vector2(0f, -110f), new Vector2(420f, 110f), 40);

            Button failMenuButton = MakeButton("MenuButton", failCard, "MENU", skyBlue, midnightBlue,
                new Vector2(0f, -240f), new Vector2(420f, 90f), 32);

            // ------------------------------------------------------------------
            // Complete panel
            // ------------------------------------------------------------------
            RectTransform completePanel = MakePanel("CompletePanel", safeArea, new Color(0f, 0f, 0f, 0.6f));
            RectTransform completeCard = MakeImage("Card", completePanel, midnightBlue, Vector2.zero, new Vector2(800f, 560f));

            TextMeshProUGUI completeTitleText = MakeText("CompleteTitleText", completeCard, "Level Complete!", 64, warmGold,
                TextAlignmentOptions.Center, true, new Vector2(0f, 120f), new Vector2(760f, 100f));

            TextMeshProUGUI completeScoreText = MakeText("CompleteScoreText", completeCard, "You solved 10 questions!", 36, offWhite,
                TextAlignmentOptions.Center, false, new Vector2(0f, 20f), new Vector2(760f, 60f));

            Button continueButton = MakeButton("ContinueButton", completeCard, "CONTINUE", warmGold, midnightBlue,
                new Vector2(0f, -110f), new Vector2(460f, 110f), 40);

            // ------------------------------------------------------------------
            // Pause panel
            // ------------------------------------------------------------------
            RectTransform pausePanel = MakePanel("PausePanel", safeArea, new Color(0f, 0f, 0f, 0.6f));
            RectTransform pauseCard = MakeImage("Card", pausePanel, midnightBlue, Vector2.zero, new Vector2(800f, 640f));

            MakeText("PauseTitle", pauseCard, "Paused", 64, offWhite,
                TextAlignmentOptions.Center, true, new Vector2(0f, 160f), new Vector2(700f, 100f));

            Button resumeButton = MakeButton("ResumeButton", pauseCard, "RESUME", warmGold, midnightBlue,
                new Vector2(0f, 20f), new Vector2(420f, 110f), 40);

            Button restartButton = MakeButton("RestartButton", pauseCard, "RESTART", skyBlue, midnightBlue,
                new Vector2(0f, -110f), new Vector2(420f, 100f), 32);

            Button pauseMenuButton = MakeButton("MenuButton", pauseCard, "MENU", coral, midnightBlue,
                new Vector2(0f, -230f), new Vector2(420f, 90f), 32);

            // ------------------------------------------------------------------
            // Managers
            // ------------------------------------------------------------------
            GameObject managersGo = new GameObject("Managers");
            GameManager gameManager = managersGo.AddComponent<GameManager>();
            QuestionManager questionManager = managersGo.AddComponent<QuestionManager>();
            GameTimer gameTimer = managersGo.AddComponent<GameTimer>();
            UIManager uiManager = managersGo.AddComponent<UIManager>();
            AudioManager audioManager = managersGo.AddComponent<AudioManager>();
            GameProgression progression = managersGo.AddComponent<GameProgression>();

            PlayerController playerController = BuiltPlayer.GetComponent<PlayerController>();
            CharacterAnimator characterAnimator = BuiltPlayer.GetComponent<CharacterAnimator>();

            gameManager.WireReferences(questionManager, gameTimer, uiManager, audioManager, progression,
                playerController, characterAnimator, BuiltCameraFollow, BuiltSpawn);

            questionManager.Wire(BuiltPlatforms.ToArray(), progression, uiManager);
            gameTimer.Wire(uiManager);

            uiManager.WireReferences(
                menuPanel.gameObject,
                hudPanel.gameObject,
                failPanel.gameObject,
                completePanel.gameObject,
                pausePanel.gameObject,
                questionText,
                timerText,
                levelText,
                progressText,
                scoreText,
                failReasonText,
                completeScoreText,
                completeTitleText,
                menuTitleText,
                timerFillImage);

            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(questionManager);
            EditorUtility.SetDirty(gameTimer);
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(audioManager);
            EditorUtility.SetDirty(progression);

            // ------------------------------------------------------------------
            // Persistent button wiring
            // ------------------------------------------------------------------
            WireButton(playButton, gameManager.StartGame);
            WireButton(settingsButton, gameManager.PlayClickSound);
            WireButton(pauseButton, gameManager.PauseGame);
            WireButton(resumeButton, gameManager.ResumeGame);
            WireButton(restartButton, gameManager.RestartLevel);
            WireButton(pauseMenuButton, gameManager.QuitToMenu);
            WireButton(retryButton, gameManager.RestartQuestion);
            WireButton(failMenuButton, gameManager.QuitToMenu);
            WireButton(continueButton, gameManager.QuitToMenu);

            if (jumpButtonComponent != null && jumpButtonComponent.onJump != null && playerController != null)
            {
                UnityEventTools.AddPersistentListener(jumpButtonComponent.onJump, playerController.Jump);
                EditorUtility.SetDirty(jumpButtonComponent);
            }
            else
            {
                Debug.LogWarning("BrainJump: could not wire JumpButton.onJump to PlayerController.Jump.");
            }

            // ------------------------------------------------------------------
            // Initial visibility (the menu state gets saved with the scene)
            // ------------------------------------------------------------------
            menuPanel.gameObject.SetActive(true);
            hudPanel.gameObject.SetActive(false);
            failPanel.gameObject.SetActive(false);
            completePanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);

            // ------------------------------------------------------------------
            // Build settings + save
            // ------------------------------------------------------------------
            string sceneDirectory = Path.GetDirectoryName(MainScenePath);
            if (!string.IsNullOrEmpty(sceneDirectory) && !Directory.Exists(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
                AssetDatabase.Refresh();
            }

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int existingIndex = buildScenes.FindIndex(s => s.path == MainScenePath);
            if (existingIndex < 0)
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(MainScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
            else if (!buildScenes[existingIndex].enabled)
            {
                buildScenes[existingIndex] = new EditorBuildSettingsScene(MainScenePath, true);
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainScenePath))
            {
                Debug.LogError("BrainJump: failed to save scene to " + MainScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("BrainJump: prototype scene built. Open MainScene and press Play.");
        }

        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static RectTransform MakePanel(string name, Transform parent, Color bg)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);

            Image image = go.GetComponent<Image>();
            image.color = bg;
            return rt;
        }

        private static RectTransform MakeImage(string name, Transform parent, Color color, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image image = go.GetComponent<Image>();
            image.color = color;
            return rt;
        }

        private static TextMeshProUGUI MakeText(string name, Transform parent, string text, int fontSize, Color color,
            TextAlignmentOptions align, bool bold, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.enableAutoSizing = false;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button MakeButton(string name, Transform parent, string label, Color bgColor, Color textColor,
            Vector2 anchoredPos, Vector2 size, int fontSize = 36)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image image = go.GetComponent<Image>();
            image.color = bgColor;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.SetParent(rt, false);
            Stretch(labelRt);

            TextMeshProUGUI labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.enableAutoSizing = false;
            labelTmp.fontSize = fontSize;
            labelTmp.color = textColor;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.raycastTarget = false;

            return button;
        }

        private static void WireButton(Button button, UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }
    }
}
