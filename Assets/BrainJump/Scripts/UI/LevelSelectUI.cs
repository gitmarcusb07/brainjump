using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrainJump
{
    /// <summary>
    /// Builds the level select grid (3 worlds x 10 levels) entirely in code.
    /// Every element is placed with explicit anchored positions; no layout components are used.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private GameManager gameManager;

        // Palette (shared with the rest of the game).
        private static readonly Color MidnightBlue = new Color(0.0902f, 0.1686f, 0.2588f, 1f); // #172B42
        private static readonly Color SkyBlue = new Color(0.4588f, 0.7255f, 0.8314f, 1f);      // #75B9D4
        private static readonly Color LeafGreen = new Color(0.2980f, 0.5882f, 0.4392f, 1f);    // #4C9670
        private static readonly Color Gold = new Color(0.9490f, 0.7216f, 0.3569f, 1f);         // #F2B85B
        private static readonly Color Coral = new Color(0.9137f, 0.5098f, 0.4392f, 1f);        // #E98270
        private static readonly Color OffWhite = new Color(0.9686f, 0.9569f, 0.9255f, 1f);     // #F7F4EC
        private static readonly Color SlateRock = new Color(0.2f, 0.2784f, 0.3686f, 1f);       // #33475E

        // Layout constants (reference resolution 1080x1920).
        private const float SectionTopStartY = 620f;
        private const float SectionSpacingY = 560f;
        private const float HeaderWidth = 900f;
        private const float HeaderHeight = 70f;
        private const int HeaderFontSize = 44;
        private const int Columns = 5;
        private const float ButtonWidth = 190f;
        private const float ButtonHeight = 150f;
        private const float ColumnStartX = -400f;
        private const float ColumnSpacingX = 200f;
        private const float FirstRowOffsetY = 150f;
        private const float RowSpacingY = 170f;
        private const int NumberFontSize = 56;
        private const int SubFontSize = 28;

        /// <summary>
        /// Wires the content root and game manager references. Null arguments are ignored
        /// so existing serialized references are not overwritten with null.
        /// </summary>
        /// <param name="content">The RectTransform that will hold the generated grid.</param>
        /// <param name="gm">The GameManager used to start levels.</param>
        public void Wire(RectTransform content, GameManager gm)
        {
            if (content != null)
            {
                contentRoot = content;
            }
            else
            {
                Debug.LogWarning("[LevelSelectUI] Wire called with null content root.");
            }

            if (gm != null)
            {
                gameManager = gm;
            }
            else
            {
                Debug.LogWarning("[LevelSelectUI] Wire called with null GameManager.");
            }
        }

        /// <summary>
        /// Destroys and rebuilds the whole level grid. Call every time the level select
        /// screen is shown so that star counts and lock states are up to date.
        /// </summary>
        public void Build()
        {
            if (contentRoot == null)
            {
                Debug.LogWarning("[LevelSelectUI] No content root wired.");
                return;
            }

            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = contentRoot.GetChild(i);
                if (child != null && child.gameObject != null)
                {
                    Destroy(child.gameObject);
                }
            }

            for (int world = 0; world < SaveManager.Worlds; world++)
            {
                float sectionTopY = SectionTopStartY - world * SectionSpacingY;

                string worldName = SaveManager.WorldName(world);
                string worldSymbol = SaveManager.WorldSymbol(world);
                if (worldName == null) worldName = "World " + (world + 1);
                if (worldSymbol == null) worldSymbol = string.Empty;

                MakeLabel(
                    "WorldHeader_" + world,
                    contentRoot,
                    worldName + "  (" + worldSymbol + ")",
                    HeaderFontSize,
                    Gold,
                    new Vector2(0f, sectionTopY),
                    new Vector2(HeaderWidth, HeaderHeight));

                for (int level = 0; level < SaveManager.LevelsPerWorld; level++)
                {
                    int levelIndex = world * SaveManager.LevelsPerWorld + level;
                    int col = level % Columns;
                    int row = level / Columns;
                    Vector2 pos = new Vector2(
                        ColumnStartX + col * ColumnSpacingX,
                        sectionTopY - FirstRowOffsetY - row * RowSpacingY);
                    MakeLevelButton(world, level, levelIndex, pos);
                }
            }
        }

        /// <summary>
        /// Refreshes the grid by rebuilding it.
        /// </summary>
        public void Refresh()
        {
            Build();
        }

        private void MakeLevelButton(int world, int level, int levelIndex, Vector2 anchoredPos)
        {
            if (contentRoot == null)
            {
                return;
            }

            bool unlocked = SaveManager.IsLevelUnlocked(levelIndex);
            int stars = SaveManager.GetStars(levelIndex);
            if (stars < 0) stars = 0;
            if (stars > 3) stars = 3;

            GameObject go = new GameObject(
                "LevelButton_W" + (world + 1) + "_L" + (level + 1),
                typeof(RectTransform), typeof(Image), typeof(Button));
            if (go == null)
            {
                return;
            }

            RectTransform rt = go.transform as RectTransform;
            if (rt == null)
            {
                Debug.LogWarning("[LevelSelectUI] Level button has no RectTransform.");
                return;
            }

            rt.SetParent(contentRoot, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            rt.anchoredPosition = anchoredPos;

            Image image = go.GetComponent<Image>();
            Color buttonColor = unlocked ? WorldColor(world) : SlateRock;
            if (image != null)
            {
                image.color = buttonColor;
            }

            Button button = go.GetComponent<Button>();
            if (button != null)
            {
                if (image != null)
                {
                    button.targetGraphic = image;
                }
                button.interactable = unlocked;
            }

            MakeLabel("Number", rt, (level + 1).ToString(), NumberFontSize, MidnightBlue,
                Vector2.zero, new Vector2(180f, 90f));

            MakeLabel("Sub", rt, unlocked ? (stars + "/3") : "LOCKED", SubFontSize,
                unlocked ? OffWhite : Coral, new Vector2(0f, -48f), new Vector2(180f, 40f));

            if (unlocked && button != null && button.onClick != null)
            {
                int w = world;
                int l = level;
                button.onClick.AddListener(() =>
                {
                    if (gameManager != null)
                    {
                        gameManager.StartLevel(w, l);
                    }
                    else
                    {
                        Debug.LogWarning("[LevelSelectUI] No GameManager wired; cannot start level.");
                    }
                });
            }
        }

        private Color WorldColor(int world)
        {
            switch (world)
            {
                case 0: return LeafGreen;
                case 1: return SkyBlue;
                case 2: return Coral;
                default: return Gold;
            }
        }

        private TextMeshProUGUI MakeLabel(string name, Transform parent, string text, int fontSize,
            Color color, Vector2 anchoredPos, Vector2 size)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject go = new GameObject(name ?? "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            if (go == null)
            {
                return null;
            }

            RectTransform rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.SetParent(parent, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = size;
                rt.anchoredPosition = anchoredPos;
            }
            else
            {
                go.transform.SetParent(parent, false);
            }

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                return null;
            }

            tmp.text = text ?? string.Empty;
            tmp.enableAutoSizing = false;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
