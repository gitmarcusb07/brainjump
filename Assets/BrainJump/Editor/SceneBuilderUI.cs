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
    /// <summary>
    /// SceneBuilder partial: builds the UI canvas, all UI panels and the managers object,
    /// wires their references and persistent button listeners, then saves the main scene.
    /// </summary>
    public static partial class SceneBuilder
    {
        private const string MainScenePath = "Assets/BrainJump/Scenes/MainScene.unity";

        /// <summary>
        /// Builds the canvas, menu / level select / settings / HUD / fail / complete / game over / pause panels,
        /// the managers GameObject, wires everything together and saves the scene into the build settings.
        /// </summary>
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

            Button playButton = MakeButton("PlayButton", menuPanel, "PLAY", warmGold, midnightBlue,
                new Vector2(0f, 0f), new Vector2(520f, 130f), 48);

            Button settingsButton = MakeButton("SettingsButton", menuPanel, "Settings", skyBlue, midnightBlue,
                new Vector2(0f, -180f), new Vector2(420f, 100f), 36);

            Button levelsButton = MakeButton("LevelsButton", menuPanel, "LEVELS", skyBlue, midnightBlue,
                new Vector2(0f, -330f), new Vector2(420f, 100f), 36);

            // ------------------------------------------------------------------
            // Level Select panel
            // ------------------------------------------------------------------
            RectTransform levelSelectPanel = MakePanel("LevelSelectPanel", safeArea, menuBg);

            MakeText("LevelSelectTitle", levelSelectPanel, "Choose a Level", 64, warmGold,
                TextAlignmentOptions.Center, true, new Vector2(0f, 780f), new Vector2(900f, 100f));

            GameObject levelSelectContentGo = new GameObject("Content", typeof(RectTransform));
            RectTransform levelSelectContent = (RectTransform)levelSelectContentGo.transform;
            levelSelectContent.SetParent(levelSelectPanel, false);
            Stretch(levelSelectContent);

            Button levelSelectBackButton = MakeButton("BackButton", levelSelectPanel, "BACK", skyBlue, midnightBlue,
                new Vector2(0f, -830f), new Vector2(420f, 100f), 36);

            // ------------------------------------------------------------------
            // Settings panel
            // ------------------------------------------------------------------
            RectTransform settingsPanel = MakePanel("SettingsPanel", safeArea, new Color(0f, 0f, 0f, 0.6f));
            RectTransform settingsCard = MakeImage("Card", settingsPanel, midnightBlue, Vector2.zero, new Vector2(800f, 1100f));

            MakeText("SettingsTitle", settingsCard, "Settings", 64, warmGold,
                TextAlignmentOptions.Center, true, new Vector2(0f, 420f), new Vector2(700f, 100f));

            Button musicToggleButton = MakeButton("MusicToggleButton", settingsCard, "Music: ON", skyBlue, midnightBlue,
                new Vector2(0f, 280f), new Vector2(460f, 100f), 36);

            Button sfxToggleButton = MakeButton("SfxToggleButton", settingsCard, "SFX: ON", skyBlue, midnightBlue,
                new Vector2(0f, 160f), new Vector2(460f, 100f), 36);

            Button resetProgressButton = MakeButton("ResetProgressButton", settingsCard, "RESET PROGRESS", coral, midnightBlue,
                new Vector2(0f, 40f), new Vector2(460f, 100f), 32);

            MakeText("HowToText", settingsCard,
                "How to play:\nMove with the joystick or WASD.\nJump with JUMP or Space.\nLand on the correct answer.\nCollect coins and power-ups.\n3 hearts — don't waste them!",
                28, offWhite, TextAlignmentOptions.Center, false, new Vector2(0f, -160f), new Vector2(700f, 300f));

            Button settingsCloseButton = MakeButton("CloseButton", settingsCard, "CLOSE", warmGold, midnightBlue,
                new Vector2(0f, -420f), new Vector2(420f, 100f), 36);

            TextMeshProUGUI musicToggleLabel = musicToggleButton != null
                ? musicToggleButton.GetComponentInChildren<TextMeshProUGUI>()
                : null;
            TextMeshProUGUI sfxToggleLabel = sfxToggleButton != null
                ? sfxToggleButton.GetComponentInChildren<TextMeshProUGUI>()
                : null;

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

            TextMeshProUGUI coinsText = MakeText("CoinsText", hudPanel, "Coins: 0", 30, offWhite,
                TextAlignmentOptions.Left, false, new Vector2(-380f, 740f), new Vector2(400f, 50f));

            TextMeshProUGUI heartsText = MakeText("HeartsText", hudPanel, "Lives: 3", 30, offWhite,
                TextAlignmentOptions.Right, false, new Vector2(380f, 750f), new Vector2(300f, 50f));

            TextMeshProUGUI comboText = MakeText("ComboText", hudPanel, "", 40, warmGold,
                TextAlignmentOptions.Center, true, new Vector2(0f, 520f), new Vector2(400f, 60f));

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
            RectTransform completeCard = MakeImage("Card", completePanel, midnightBlue, Vector2.zero, new Vector2(800f, 640f));

            TextMeshProUGUI completeTitleText = MakeText("CompleteTitleText", completeCard, "Level Complete!", 64, warmGold,
                TextAlignmentOptions.Center, true, new Vector2(0f, 160f), new Vector2(760f, 100f));

            TextMeshProUGUI completeScoreText = MakeText("CompleteScoreText", completeCard, "You solved 10 questions!", 36, offWhite,
                TextAlignmentOptions.Center, false, new Vector2(0f, 60f), new Vector2(760f, 60f));

            TextMeshProUGUI completeStarsText = MakeText("CompleteStarsText", completeCard, "Stars: 3/3", 40, warmGold,
                TextAlignmentOptions.Center, true, new Vector2(0f, -20f), new Vector2(700f, 60f));

            Button nextLevelButton = MakeButton("NextLevelButton", completeCard, "NEXT LEVEL", warmGold, midnightBlue,
                new Vector2(0f, -130f), new Vector2(460f, 110f), 40);

            Button continueButton = MakeButton("ContinueButton", completeCard, "MENU", skyBlue, midnightBlue,
                new Vector2(0f, -260f), new Vector2(420f, 90f), 32);

            // ------------------------------------------------------------------
            // Game Over panel
            // ------------------------------------------------------------------
            RectTransform gameOverPanel = MakePanel("GameOverPanel", safeArea, new Color(0f, 0f, 0f, 0.6f));
            RectTransform gameOverCard = MakeImage("Card", gameOverPanel, midnightBlue, Vector2.zero, new Vector2(800f, 560f));

            TextMeshProUGUI gameOverTitleText = MakeText("GameOverTitleText", gameOverCard, "Game Over", 64, coral,
                TextAlignmentOptions.Center, true, new Vector2(0f, 120f), new Vector2(700f, 100f));

            TextMeshProUGUI gameOverScoreText = MakeText("GameOverScoreText", gameOverCard, "Score: 0  •  Best: 0", 36, offWhite,
                TextAlignmentOptions.Center, false, new Vector2(0f, 20f), new Vector2(700f, 60f));

            Button gameOverRetryButton = MakeButton("RetryButton", gameOverCard, "RETRY LEVEL", warmGold, midnightBlue,
                new Vector2(0f, -110f), new Vector2(460f, 110f), 40);

            Button gameOverMenuButton = MakeButton("MenuButton", gameOverCard, "MENU", skyBlue, midnightBlue,
                new Vector2(0f, -240f), new Vector2(420f, 90f), 32);

            // ------------------------------------------------------------------
            // Pause panel
            // ------------------------------------------------------------------
            RectTransform pausePanel = MakePanel("PausePanel", safeArea, new Color(0f, 0f, 0f, 0.6f));
            RectTransform pauseCard = MakeImage("Card", pausePanel, midnightBlue, Vector2.zero, new Vector2(800f, 760f));

            MakeText("PauseTitle", pauseCard, "Paused", 64, offWhite,
                TextAlignmentOptions.Center, true, new Vector2(0f, 220f), new Vector2(700f, 100f));

            Button resumeButton = MakeButton("ResumeButton", pauseCard, "RESUME", warmGold, midnightBlue,
                new Vector2(0f, 80f), new Vector2(420f, 110f), 40);

            Button restartButton = MakeButton("RestartButton", pauseCard, "RESTART", skyBlue, midnightBlue,
                new Vector2(0f, -50f), new Vector2(420f, 100f), 32);

            Button pauseSettingsButton = MakeButton("SettingsButton", pauseCard, "SETTINGS", skyBlue, midnightBlue,
                new Vector2(0f, -180f), new Vector2(420f, 100f), 32);

            Button pauseMenuButton = MakeButton("MenuButton", pauseCard, "MENU", coral, midnightBlue,
                new Vector2(0f, -310f), new Vector2(420f, 90f), 32);

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
            ScoreManager scoreManager = managersGo.AddComponent<ScoreManager>();
            LivesManager livesManager = managersGo.AddComponent<LivesManager>();
            CoinManager coinManager = managersGo.AddComponent<CoinManager>();
            PowerUpManager powerUpManager = managersGo.AddComponent<PowerUpManager>();
            EffectsManager effectsManager = managersGo.AddComponent<EffectsManager>();
            LevelSelectUI levelSelectUI = managersGo.AddComponent<LevelSelectUI>();

            PlayerController playerController = BuiltPlayer != null ? BuiltPlayer.GetComponent<PlayerController>() : null;
            CharacterAnimator characterAnimator = BuiltPlayer != null ? BuiltPlayer.GetComponent<CharacterAnimator>() : null;
            if (BuiltPlayer == null)
            {
                Debug.LogWarning("BrainJump: BuiltPlayer is null, player references will not be wired.");
            }

            if (gameManager != null)
            {
                gameManager.WireReferences(questionManager, gameTimer, uiManager, audioManager, progression,
                    playerController, characterAnimator, BuiltCameraFollow, BuiltSpawn,
                    scoreManager, livesManager, coinManager, powerUpManager, effectsManager, levelSelectUI);
            }

            if (questionManager != null)
            {
                questionManager.Wire(BuiltPlatforms != null ? BuiltPlatforms.ToArray() : null, progression, uiManager);
                questionManager.WirePowerUps(powerUpManager, coinManager,
                    BuiltCoins != null ? BuiltCoins.ToArray() : null, BuiltPowerUpPickup);
            }

            if (gameTimer != null)
            {
                gameTimer.Wire(uiManager);
            }

            if (uiManager != null)
            {
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
            }

            Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BrainJump/Materials/Mat_Particle.mat");
            if (particleMaterial == null)
            {
                Debug.LogWarning("BrainJump: particle material not found at Assets/BrainJump/Materials/Mat_Particle.mat.");
            }
            if (effectsManager != null)
            {
                effectsManager.WireMaterial(particleMaterial);
            }

            if (uiManager != null)
            {
                uiManager.WireExtraPanels(
                    levelSelectPanel.gameObject,
                    settingsPanel.gameObject,
                    gameOverPanel.gameObject,
                    coinsText,
                    heartsText,
                    comboText,
                    completeStarsText,
                    gameOverScoreText,
                    gameOverTitleText,
                    musicToggleLabel,
                    sfxToggleLabel);
            }

            if (levelSelectUI != null)
            {
                levelSelectUI.Wire(levelSelectContent, gameManager);
            }

            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(questionManager);
            EditorUtility.SetDirty(gameTimer);
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(audioManager);
            EditorUtility.SetDirty(progression);
            if (scoreManager != null) EditorUtility.SetDirty(scoreManager);
            if (livesManager != null) EditorUtility.SetDirty(livesManager);
            if (coinManager != null) EditorUtility.SetDirty(coinManager);
            if (powerUpManager != null) EditorUtility.SetDirty(powerUpManager);
            if (effectsManager != null) EditorUtility.SetDirty(effectsManager);
            if (levelSelectUI != null) EditorUtility.SetDirty(levelSelectUI);

            // ------------------------------------------------------------------
            // Persistent button wiring
            // ------------------------------------------------------------------
            if (gameManager != null)
            {
                WireButton(playButton, gameManager.StartGame);
            }
            if (uiManager != null)
            {
                WireButton(settingsButton, uiManager.ShowSettings);
            }
            if (gameManager != null)
            {
                WireButton(levelsButton, gameManager.OpenLevelSelect);
                WireButton(pauseButton, gameManager.PauseGame);
                WireButton(resumeButton, gameManager.ResumeGame);
                WireButton(restartButton, gameManager.RestartLevel);
                WireButton(pauseMenuButton, gameManager.QuitToMenu);
                WireButton(retryButton, gameManager.RestartQuestion);
                WireButton(failMenuButton, gameManager.QuitToMenu);
                WireButton(continueButton, gameManager.QuitToMenu);
                WireButton(nextLevelButton, gameManager.NextLevel);
                WireButton(gameOverRetryButton, gameManager.RestartLevel);
                WireButton(gameOverMenuButton, gameManager.QuitToMenu);
            }
            if (uiManager != null)
            {
                WireButton(musicToggleButton, uiManager.ToggleMusic);
                WireButton(sfxToggleButton, uiManager.ToggleSfx);
                WireButton(resetProgressButton, uiManager.ResetProgress);
                WireButton(settingsCloseButton, uiManager.HideSettings);
                WireButton(pauseSettingsButton, uiManager.ShowSettings);
                WireButton(levelSelectBackButton, uiManager.ShowMenu);
            }

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
            levelSelectPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
            gameOverPanel.gameObject.SetActive(false);

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

        /// <summary>Stretches a RectTransform to fill its parent.</summary>
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>Creates a full-stretch panel with a background Image.</summary>
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

        /// <summary>Creates a centred Image with the given position and size.</summary>
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

        /// <summary>Creates a centred TextMeshProUGUI label.</summary>
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

        /// <summary>Creates a centred Button with a bold TMP label child.</summary>
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

        /// <summary>Adds a persistent (serialized) onClick listener to the button.</summary>
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
