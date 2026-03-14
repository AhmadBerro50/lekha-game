using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Core;
using Lekha.GameLogic;
using Lekha.Audio;
using System.Collections.Generic;

namespace Lekha.UI
{
    /// <summary>
    /// Score summary popup with round history log
    /// </summary>
    public class ScoreSummaryPopup : MonoBehaviour
    {
        public static ScoreSummaryPopup Instance { get; private set; }

        private GameObject dimOverlay;
        private GameObject popupPanel;
        private CanvasGroup canvasGroup;
        private bool isVisible = false;
        private bool isAnimating = false;
        private float animationTime = 0f;
        private RectTransform popupRect;

        // Player score displays
        private TextMeshProUGUI[] playerNameTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI[] playerTotalTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI[] playerRoundTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI roundNumberText;

        // History section
        private Transform historyContent;
        private List<RoundHistoryEntry> roundHistory = new List<RoundHistoryEntry>();

        // Round history data
        private struct RoundHistoryEntry
        {
            public int RoundNumber;
            public int[] PlayerScores; // S, E, N, W
        }

        // Modern 2026 glassmorphism colors
        private static readonly Color BgDark = new Color(0.08f, 0.10f, 0.16f, 0.95f);
        private static readonly Color GlassPanel = new Color(0.12f, 0.14f, 0.22f, 0.92f);
        private static readonly Color HeaderBg = new Color(0.16f, 0.12f, 0.28f, 0.95f);
        private static readonly Color RowLight = new Color(0.14f, 0.16f, 0.24f, 0.85f);
        private static readonly Color RowDark = new Color(0.10f, 0.12f, 0.20f, 0.85f);
        private static readonly Color HistoryBg = new Color(0.06f, 0.08f, 0.14f, 0.90f);
        private static readonly Color TextWhite = new Color(1f, 1f, 1f, 1f);
        private static readonly Color TextGray = new Color(0.70f, 0.72f, 0.78f, 1f);
        private static readonly Color YourColor = new Color(0.40f, 0.85f, 1f, 1f);
        private static readonly Color PartnerColor = new Color(0.45f, 0.95f, 0.70f, 1f);
        private static readonly Color OpponentColor = new Color(0.85f, 0.45f, 0.95f, 1f);
        private static readonly Color DangerRed = new Color(0.95f, 0.35f, 0.45f, 1f);
        private static readonly Color AccentCyan = new Color(0.40f, 0.75f, 1f, 1f);
        private static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.15f);

        public static ScoreSummaryPopup Create(Transform parent)
        {
            GameObject obj = new GameObject("ScoreSummaryPopup");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add Canvas with high sorting order to ensure popup appears above everything
            Canvas popupCanvas = obj.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 200;

            obj.AddComponent<GraphicRaycaster>();

            ScoreSummaryPopup popup = obj.AddComponent<ScoreSummaryPopup>();
            popup.BuildUI();

            Instance = popup;
            return popup;
        }

        private void BuildUI()
        {
            CreateDimOverlay();
            CreatePopupPanel();

            popupPanel.SetActive(false);
            dimOverlay.SetActive(false);
        }

        private void CreateDimOverlay()
        {
            dimOverlay = new GameObject("DimOverlay");
            dimOverlay.transform.SetParent(transform, false);

            RectTransform rect = dimOverlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = dimOverlay.AddComponent<Image>();
            img.color = new Color(0.04f, 0.05f, 0.10f, 0.85f);

            Button btn = dimOverlay.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnDimOverlayClicked);
        }

        private void OnDimOverlayClicked()
        {
            if (!isAnimating)
            {
                SoundManager.Instance?.PlayButtonClick();
                HideImmediate();
            }
        }

        private void CreatePopupPanel()
        {
            popupPanel = new GameObject("Panel");
            popupPanel.transform.SetParent(transform, false);

            popupRect = popupPanel.AddComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(750, 700); // Taller for history
            popupRect.anchoredPosition = Vector2.zero;

            Image bgImage = popupPanel.AddComponent<Image>();
            bgImage.sprite = CreateGlassPanelSprite(128, 24);
            bgImage.type = Image.Type.Sliced;
            bgImage.color = GlassPanel;

            Shadow shadow = popupPanel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -6);

            // Add outer glow
            Outline outline = popupPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.40f, 0.75f, 1f, 0.15f);
            outline.effectDistance = new Vector2(2, -2);

            canvasGroup = popupPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            CreateHeader();
            CreateTableHeader();
            CreatePlayerRows();
            CreateHistorySection();
            CreateCloseButton();
        }

        private void CreateHeader()
        {
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(popupPanel.transform, false);

            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = new Vector2(0, 60);
            headerRect.anchoredPosition = new Vector2(0, -30);

            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = HeaderBg;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "SCOREBOARD";
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = TextWhite;

            // Round indicator
            GameObject roundObj = new GameObject("Round");
            roundObj.transform.SetParent(headerObj.transform, false);

            RectTransform roundRect = roundObj.AddComponent<RectTransform>();
            roundRect.anchorMin = new Vector2(1, 0.5f);
            roundRect.anchorMax = new Vector2(1, 0.5f);
            roundRect.sizeDelta = new Vector2(100, 40);
            roundRect.anchoredPosition = new Vector2(-70, 0);

            roundNumberText = roundObj.AddComponent<TextMeshProUGUI>();
            roundNumberText.text = "Round 1";
            roundNumberText.fontSize = 18;
            roundNumberText.fontStyle = FontStyles.Bold;
            roundNumberText.alignment = TextAlignmentOptions.Center;
            roundNumberText.color = AccentCyan;
        }

        private void CreateTableHeader()
        {
            GameObject headerRow = new GameObject("TableHeader");
            headerRow.transform.SetParent(popupPanel.transform, false);

            RectTransform rowRect = headerRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.sizeDelta = new Vector2(-40, 40);
            rowRect.anchoredPosition = new Vector2(0, -80);

            Image rowBg = headerRow.AddComponent<Image>();
            rowBg.color = new Color(0.18f, 0.14f, 0.30f, 0.90f);

            // Column headers
            CreateHeaderCell(headerRow.transform, "PLAYER", 180, 20);
            CreateHeaderCell(headerRow.transform, "THIS ROUND", 140, 240);
            CreateHeaderCell(headerRow.transform, "TOTAL", 120, 420);
        }

        private void CreateHeaderCell(Transform parent, string text, float width, float xPos)
        {
            GameObject cellObj = new GameObject($"Header_{text}");
            cellObj.transform.SetParent(parent, false);

            RectTransform cellRect = cellObj.AddComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0, 0);
            cellRect.anchorMax = new Vector2(0, 1);
            cellRect.pivot = new Vector2(0, 0.5f);
            cellRect.sizeDelta = new Vector2(width, 0);
            cellRect.anchoredPosition = new Vector2(xPos, 0);

            TextMeshProUGUI cellText = cellObj.AddComponent<TextMeshProUGUI>();
            cellText.text = text;
            cellText.fontSize = 16;
            cellText.fontStyle = FontStyles.Bold;
            cellText.alignment = TextAlignmentOptions.Center;
            cellText.color = TextGray;
        }

        private void CreatePlayerRows()
        {
            string[] playerLabels = { "YOU", "PARTNER", "EAST", "WEST" };
            Color[] playerColors = { YourColor, PartnerColor, OpponentColor, OpponentColor };
            int[] playerIndices = { 0, 2, 1, 3 }; // South, North, East, West

            for (int i = 0; i < 4; i++)
            {
                CreatePlayerRow(i, playerLabels[i], playerColors[i], playerIndices[i]);
            }
        }

        private void CreatePlayerRow(int rowIndex, string label, Color labelColor, int dataIndex)
        {
            float yOffset = -120 - (rowIndex * 55);

            GameObject rowObj = new GameObject($"Row_{label}");
            rowObj.transform.SetParent(popupPanel.transform, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.sizeDelta = new Vector2(-40, 50);
            rowRect.anchoredPosition = new Vector2(0, yOffset);

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = rowIndex % 2 == 0 ? RowLight : RowDark;

            // Player name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(rowObj.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(0, 1);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.sizeDelta = new Vector2(160, 0);
            nameRect.anchoredPosition = new Vector2(30, 0);

            playerNameTexts[dataIndex] = nameObj.AddComponent<TextMeshProUGUI>();
            playerNameTexts[dataIndex].text = label;
            playerNameTexts[dataIndex].fontSize = 24;
            playerNameTexts[dataIndex].fontStyle = FontStyles.Bold;
            playerNameTexts[dataIndex].alignment = TextAlignmentOptions.Left;
            playerNameTexts[dataIndex].color = labelColor;

            // Round score
            GameObject roundObj = new GameObject("RoundScore");
            roundObj.transform.SetParent(rowObj.transform, false);

            RectTransform roundRect = roundObj.AddComponent<RectTransform>();
            roundRect.anchorMin = new Vector2(0, 0);
            roundRect.anchorMax = new Vector2(0, 1);
            roundRect.pivot = new Vector2(0, 0.5f);
            roundRect.sizeDelta = new Vector2(120, 0);
            roundRect.anchoredPosition = new Vector2(240, 0);

            playerRoundTexts[dataIndex] = roundObj.AddComponent<TextMeshProUGUI>();
            playerRoundTexts[dataIndex].text = "+0";
            playerRoundTexts[dataIndex].fontSize = 26;
            playerRoundTexts[dataIndex].fontStyle = FontStyles.Bold;
            playerRoundTexts[dataIndex].alignment = TextAlignmentOptions.Center;
            playerRoundTexts[dataIndex].color = TextWhite;

            // Total score
            GameObject totalObj = new GameObject("TotalScore");
            totalObj.transform.SetParent(rowObj.transform, false);

            RectTransform totalRect = totalObj.AddComponent<RectTransform>();
            totalRect.anchorMin = new Vector2(0, 0);
            totalRect.anchorMax = new Vector2(0, 1);
            totalRect.pivot = new Vector2(0, 0.5f);
            totalRect.sizeDelta = new Vector2(100, 0);
            totalRect.anchoredPosition = new Vector2(420, 0);

            playerTotalTexts[dataIndex] = totalObj.AddComponent<TextMeshProUGUI>();
            playerTotalTexts[dataIndex].text = "0";
            playerTotalTexts[dataIndex].fontSize = 30;
            playerTotalTexts[dataIndex].fontStyle = FontStyles.Bold;
            playerTotalTexts[dataIndex].alignment = TextAlignmentOptions.Center;
            playerTotalTexts[dataIndex].color = TextWhite;
        }

        private void CreateHistorySection()
        {
            // History section title
            GameObject historyTitleObj = new GameObject("HistoryTitle");
            historyTitleObj.transform.SetParent(popupPanel.transform, false);

            RectTransform titleRect = historyTitleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(-40, 35);
            titleRect.anchoredPosition = new Vector2(0, -355);

            Image titleBg = historyTitleObj.AddComponent<Image>();
            titleBg.color = HeaderBg;

            GameObject titleTextObj = new GameObject("Text");
            titleTextObj.transform.SetParent(historyTitleObj.transform, false);

            RectTransform textRect = titleTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "ROUND HISTORY";
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = AccentCyan;

            // History scroll view
            GameObject scrollObj = new GameObject("HistoryScroll");
            scrollObj.transform.SetParent(popupPanel.transform, false);

            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 1);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.sizeDelta = new Vector2(-40, 250);
            scrollRect.anchoredPosition = new Vector2(0, -520);

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = HistoryBg;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);

            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            Image viewportImg = viewportObj.AddComponent<Image>();
            viewportImg.color = Color.white;

            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 2;
            layout.padding = new RectOffset(10, 10, 5, 5);

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            historyContent = contentObj.transform;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            // Add "No history yet" placeholder
            CreateHistoryPlaceholder();
        }

        private void CreateHistoryPlaceholder()
        {
            if (historyContent == null) return;

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(historyContent, false);

            RectTransform rect = placeholderObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            LayoutElement layout = placeholderObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;
            layout.flexibleWidth = 1;

            TextMeshProUGUI text = placeholderObj.AddComponent<TextMeshProUGUI>();
            text.text = "Complete a round to see history";
            text.fontSize = 16;
            text.fontStyle = FontStyles.Italic;
            text.alignment = TextAlignmentOptions.Center;
            text.color = TextGray;
        }

        private void CreateCloseButton()
        {
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(popupPanel.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.sizeDelta = new Vector2(44, 44);
            btnRect.anchoredPosition = new Vector2(-22, -30);

            // Create circular button background with modern styling
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.sprite = CreateSoftCircleSprite(128);
            btnBg.color = new Color(0.85f, 0.30f, 0.50f, 1f);
            btnBg.type = Image.Type.Simple;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            btn.onClick.AddListener(() => {
                if (!isAnimating)
                {
                    SoundManager.Instance?.PlayButtonClick();
                    HideImmediate();
                }
            });

            // X text - use simple ASCII X for better compatibility
            GameObject xObj = new GameObject("X");
            xObj.transform.SetParent(btnObj.transform, false);

            RectTransform xRect = xObj.AddComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI xText = xObj.AddComponent<TextMeshProUGUI>();
            xText.text = "X";
            xText.fontSize = 26;
            xText.fontStyle = FontStyles.Bold;
            xText.alignment = TextAlignmentOptions.Center;
            xText.color = TextWhite;
        }

        /// <summary>
        /// Creates a soft-edged circle sprite with anti-aliasing
        /// </summary>
        private Sprite CreateSoftCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2;
            float edgeSoftness = 2.5f; // Anti-aliasing edge

            Color circleColor = Color.white; // Use white and apply color via Image.color

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    if (dist <= radius - edgeSoftness)
                    {
                        // Fully inside
                        tex.SetPixel(x, y, circleColor);
                    }
                    else if (dist <= radius + edgeSoftness)
                    {
                        // Edge - anti-alias
                        float alpha = 1f - ((dist - (radius - edgeSoftness)) / (edgeSoftness * 2f));
                        alpha = Mathf.Clamp01(alpha);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        // Outside
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>
        /// Creates a glass panel sprite with rounded corners and subtle border
        /// </summary>
        private Sprite CreateGlassPanelSprite(int size, int cornerRadius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float borderWidth = 1.5f;
            Color fillColor = new Color(1f, 1f, 1f, 1f);
            Color borderColor = new Color(1f, 1f, 1f, 0.25f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, size, size, cornerRadius);

                    if (dist < -borderWidth)
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                    else if (dist < 0)
                    {
                        float t = (dist + borderWidth) / borderWidth;
                        tex.SetPixel(x, y, Color.Lerp(fillColor, borderColor, t));
                    }
                    else if (dist < 1.5f)
                    {
                        float alpha = 1f - (dist / 1.5f);
                        tex.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();

            int border = cornerRadius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private float DistanceToRoundedRect(float x, float y, float width, float height, float radius)
        {
            float halfW = width / 2f;
            float halfH = height / 2f;
            float px = x - halfW;
            float py = y - halfH;

            float qx = Mathf.Abs(px) - halfW + radius;
            float qy = Mathf.Abs(py) - halfH + radius;

            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0);

            return outside + inside - radius;
        }

        /// <summary>
        /// Record a completed round's scores to history
        /// Call this at the end of each round
        /// </summary>
        public void RecordRoundHistory(int roundNumber, int southScore, int eastScore, int northScore, int westScore)
        {
            var entry = new RoundHistoryEntry
            {
                RoundNumber = roundNumber,
                PlayerScores = new int[] { southScore, eastScore, northScore, westScore }
            };
            roundHistory.Add(entry);

            // Update history display
            RefreshHistoryDisplay();
        }

        /// <summary>
        /// Clear all history (call when starting a new game)
        /// </summary>
        public void ClearHistory()
        {
            roundHistory.Clear();
            RefreshHistoryDisplay();
        }

        private void RefreshHistoryDisplay()
        {
            if (historyContent == null) return;

            // Clear existing entries
            foreach (Transform child in historyContent)
            {
                Destroy(child.gameObject);
            }

            if (roundHistory.Count == 0)
            {
                CreateHistoryPlaceholder();
                return;
            }

            // Create header row
            CreateHistoryRow(-1, "RND", "YOU", "PTR", "EAST", "WEST", true);

            // Create rows for each round (newest first)
            for (int i = roundHistory.Count - 1; i >= 0; i--)
            {
                var entry = roundHistory[i];
                CreateHistoryRow(
                    entry.RoundNumber,
                    entry.RoundNumber.ToString(),
                    $"+{entry.PlayerScores[0]}",
                    $"+{entry.PlayerScores[2]}",
                    $"+{entry.PlayerScores[1]}",
                    $"+{entry.PlayerScores[3]}",
                    false
                );
            }
        }

        private void CreateHistoryRow(int roundNum, string col0, string col1, string col2, string col3, string col4, bool isHeader)
        {
            GameObject rowObj = new GameObject($"HistoryRow_{roundNum}");
            rowObj.transform.SetParent(historyContent, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, isHeader ? 28 : 26);

            LayoutElement layout = rowObj.AddComponent<LayoutElement>();
            layout.preferredHeight = isHeader ? 28 : 26;
            layout.flexibleWidth = 1;

            Image rowBg = rowObj.AddComponent<Image>();
            rowBg.color = isHeader ? new Color(0.16f, 0.14f, 0.26f, 0.95f) :
                          (roundNum % 2 == 0 ? new Color(0.12f, 0.13f, 0.20f, 0.85f) : new Color(0.09f, 0.10f, 0.17f, 0.85f));

            HorizontalLayoutGroup hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlHeight = true;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.spacing = 5;
            hLayout.padding = new RectOffset(5, 5, 0, 0);

            float fontSize = isHeader ? 13 : 14;
            Color textColor = isHeader ? TextGray : TextWhite;

            CreateHistoryCell(rowObj.transform, col0, fontSize, textColor, 0.12f);
            CreateHistoryCell(rowObj.transform, col1, fontSize, isHeader ? textColor : YourColor, 0.22f);
            CreateHistoryCell(rowObj.transform, col2, fontSize, isHeader ? textColor : PartnerColor, 0.22f);
            CreateHistoryCell(rowObj.transform, col3, fontSize, isHeader ? textColor : OpponentColor, 0.22f);
            CreateHistoryCell(rowObj.transform, col4, fontSize, isHeader ? textColor : OpponentColor, 0.22f);
        }

        private void CreateHistoryCell(Transform parent, string text, float fontSize, Color color, float flexWidth)
        {
            GameObject cellObj = new GameObject("Cell");
            cellObj.transform.SetParent(parent, false);

            LayoutElement layout = cellObj.AddComponent<LayoutElement>();
            layout.flexibleWidth = flexWidth;

            TextMeshProUGUI tmp = cellObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
        }

        public void Show()
        {
            if (isVisible || isAnimating) return;

            UpdateScores();
            dimOverlay.SetActive(true);
            popupPanel.SetActive(true);
            isVisible = true;
            isAnimating = true;
            animationTime = 0f;
            popupRect.localScale = Vector3.one * 0.9f;
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (!isVisible || isAnimating) return;

            isAnimating = true;
            animationTime = 0f;
        }

        public void HideImmediate()
        {
            isAnimating = false;
            isVisible = false;
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            popupPanel.SetActive(false);
            dimOverlay.SetActive(false);
        }

        public void Toggle()
        {
            if (isAnimating) return;

            if (isVisible)
                HideImmediate();
            else
                Show();
        }

        private void Update()
        {
            if (!isAnimating) return;

            animationTime += Time.deltaTime * 8f;

            if (isVisible)
            {
                float t = Mathf.Min(1f, animationTime);
                canvasGroup.alpha = t;
                popupRect.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, t);

                if (animationTime >= 1f)
                {
                    isAnimating = false;
                    canvasGroup.alpha = 1;
                    popupRect.localScale = Vector3.one;
                }
            }
            else
            {
                float t = Mathf.Min(1f, animationTime);
                canvasGroup.alpha = 1f - t;
                popupRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, t);

                if (animationTime >= 1f)
                {
                    isAnimating = false;
                    canvasGroup.alpha = 0;
                    canvasGroup.blocksRaycasts = false;
                    popupPanel.SetActive(false);
                    dimOverlay.SetActive(false);
                    isVisible = false;
                }
            }
        }

        private void UpdateScores()
        {
            if (GameManager.Instance == null) return;

            int roundNum = GameManager.Instance.RoundNumber;
            if (roundNumberText != null)
            {
                roundNumberText.text = $"Round {roundNum}";
            }

            Player[] players = GameManager.Instance.Players;

            // Default labels and colors by visual position
            string[] defaultLabels = { "YOU", "EAST", "PARTNER", "WEST" };
            Color[] defaultColors = { YourColor, OpponentColor, PartnerColor, OpponentColor };

            foreach (var player in players)
            {
                // Use visual position so local player always maps to "YOU" (South/index 0)
                PlayerPosition visualPos = GameManager.Instance.GetVisualPosition(player.Position);
                int index = visualPos switch
                {
                    PlayerPosition.South => 0,
                    PlayerPosition.East => 1,
                    PlayerPosition.North => 2,
                    PlayerPosition.West => 3,
                    _ => 0
                };

                // Update player name - show actual name if available, fallback to label
                if (playerNameTexts[index] != null)
                {
                    string displayName = !string.IsNullOrEmpty(player.PlayerName) ? player.PlayerName : defaultLabels[index];
                    playerNameTexts[index].text = displayName;
                    playerNameTexts[index].color = defaultColors[index];
                }

                if (playerTotalTexts[index] != null)
                {
                    int total = player.TotalPoints;
                    playerTotalTexts[index].text = total.ToString();
                    playerTotalTexts[index].color = total >= 80 ? DangerRed : TextWhite;
                }

                if (playerRoundTexts[index] != null)
                {
                    int roundPts = player.RoundPoints;
                    playerRoundTexts[index].text = roundPts > 0 ? $"+{roundPts}" : "0";
                    playerRoundTexts[index].color = roundPts > 0 ? AccentCyan : TextGray;
                }
            }
        }
    }
}
