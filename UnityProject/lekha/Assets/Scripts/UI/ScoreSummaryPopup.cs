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
    /// Clean, readable score summary popup showing individual player scores
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

        // Clean colors
        private static readonly Color BgDark = new Color(0.08f, 0.10f, 0.14f, 0.98f);
        private static readonly Color HeaderBg = new Color(0.15f, 0.18f, 0.22f, 1f);
        private static readonly Color RowLight = new Color(0.12f, 0.14f, 0.18f, 1f);
        private static readonly Color RowDark = new Color(0.09f, 0.11f, 0.15f, 1f);
        private static readonly Color TextWhite = new Color(1f, 1f, 1f, 1f);
        private static readonly Color TextGray = new Color(0.65f, 0.65f, 0.65f, 1f);
        private static readonly Color YourColor = new Color(0.4f, 0.75f, 1f, 1f);
        private static readonly Color PartnerColor = new Color(0.5f, 0.85f, 0.6f, 1f);
        private static readonly Color OpponentColor = new Color(1f, 0.7f, 0.5f, 1f);
        private static readonly Color DangerRed = new Color(1f, 0.4f, 0.4f, 1f);
        private static readonly Color GoldAccent = new Color(1f, 0.85f, 0.4f, 1f);

        public static ScoreSummaryPopup Create(Transform parent)
        {
            GameObject obj = new GameObject("ScoreSummaryPopup");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

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
            img.color = new Color(0, 0, 0, 0.75f);

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
            popupRect.sizeDelta = new Vector2(700, 480);
            popupRect.anchoredPosition = Vector2.zero;

            Image bgImage = popupPanel.AddComponent<Image>();
            bgImage.color = BgDark;

            Shadow shadow = popupPanel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(0, -8);

            canvasGroup = popupPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            CreateHeader();
            CreateTableHeader();
            CreatePlayerRows();
            CreateFooter();
            CreateCloseButton();
        }

        private void CreateHeader()
        {
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(popupPanel.transform, false);

            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = new Vector2(0, 70);
            headerRect.anchoredPosition = new Vector2(0, -35);

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
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = TextWhite;

            // Round indicator
            GameObject roundObj = new GameObject("Round");
            roundObj.transform.SetParent(headerObj.transform, false);

            RectTransform roundRect = roundObj.AddComponent<RectTransform>();
            roundRect.anchorMin = new Vector2(1, 0.5f);
            roundRect.anchorMax = new Vector2(1, 0.5f);
            roundRect.sizeDelta = new Vector2(120, 40);
            roundRect.anchoredPosition = new Vector2(-80, 0);

            roundNumberText = roundObj.AddComponent<TextMeshProUGUI>();
            roundNumberText.text = "Round 1";
            roundNumberText.fontSize = 20;
            roundNumberText.fontStyle = FontStyles.Normal;
            roundNumberText.alignment = TextAlignmentOptions.Center;
            roundNumberText.color = GoldAccent;
        }

        private void CreateTableHeader()
        {
            GameObject headerRow = new GameObject("TableHeader");
            headerRow.transform.SetParent(popupPanel.transform, false);

            RectTransform rowRect = headerRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.sizeDelta = new Vector2(-60, 50);
            rowRect.anchoredPosition = new Vector2(0, -95);

            Image rowBg = headerRow.AddComponent<Image>();
            rowBg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

            // Column headers
            CreateHeaderCell(headerRow.transform, "PLAYER", 220, 30);
            CreateHeaderCell(headerRow.transform, "THIS ROUND", 180, 280);
            CreateHeaderCell(headerRow.transform, "TOTAL", 150, 500);
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
            cellText.fontSize = 18;
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
            float yOffset = -145 - (rowIndex * 70);

            GameObject rowObj = new GameObject($"Row_{label}");
            rowObj.transform.SetParent(popupPanel.transform, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.sizeDelta = new Vector2(-60, 65);
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
            nameRect.sizeDelta = new Vector2(200, 0);
            nameRect.anchoredPosition = new Vector2(40, 0);

            playerNameTexts[dataIndex] = nameObj.AddComponent<TextMeshProUGUI>();
            playerNameTexts[dataIndex].text = label;
            playerNameTexts[dataIndex].fontSize = 28;
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
            roundRect.sizeDelta = new Vector2(160, 0);
            roundRect.anchoredPosition = new Vector2(280, 0);

            playerRoundTexts[dataIndex] = roundObj.AddComponent<TextMeshProUGUI>();
            playerRoundTexts[dataIndex].text = "+0";
            playerRoundTexts[dataIndex].fontSize = 32;
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
            totalRect.sizeDelta = new Vector2(130, 0);
            totalRect.anchoredPosition = new Vector2(500, 0);

            playerTotalTexts[dataIndex] = totalObj.AddComponent<TextMeshProUGUI>();
            playerTotalTexts[dataIndex].text = "0";
            playerTotalTexts[dataIndex].fontSize = 36;
            playerTotalTexts[dataIndex].fontStyle = FontStyles.Bold;
            playerTotalTexts[dataIndex].alignment = TextAlignmentOptions.Center;
            playerTotalTexts[dataIndex].color = TextWhite;
        }

        private void CreateFooter()
        {
            GameObject footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(popupPanel.transform, false);

            RectTransform footerRect = footerObj.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0, 0);
            footerRect.anchorMax = new Vector2(1, 0);
            footerRect.sizeDelta = new Vector2(0, 50);
            footerRect.anchoredPosition = new Vector2(0, 25);

            TextMeshProUGUI footerText = footerObj.AddComponent<TextMeshProUGUI>();
            footerText.text = "First player to 100+ points loses!";
            footerText.fontSize = 20;
            footerText.alignment = TextAlignmentOptions.Center;
            footerText.color = DangerRed;
            footerText.fontStyle = FontStyles.Italic;
        }

        private void CreateCloseButton()
        {
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(popupPanel.transform, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.sizeDelta = new Vector2(50, 50);
            btnRect.anchoredPosition = new Vector2(-25, -35);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.85f, 0.25f, 0.25f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.85f, 0.25f, 0.25f, 1f);
            colors.highlightedColor = new Color(1f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.65f, 0.15f, 0.15f, 1f);
            btn.colors = colors;

            btn.onClick.AddListener(() => {
                if (!isAnimating)
                {
                    SoundManager.Instance?.PlayButtonClick();
                    HideImmediate();
                }
            });

            // X text
            GameObject xObj = new GameObject("X");
            xObj.transform.SetParent(btnObj.transform, false);

            RectTransform xRect = xObj.AddComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI xText = xObj.AddComponent<TextMeshProUGUI>();
            xText.text = "X";
            xText.fontSize = 28;
            xText.fontStyle = FontStyles.Bold;
            xText.alignment = TextAlignmentOptions.Center;
            xText.color = TextWhite;
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

            foreach (var player in players)
            {
                int index = player.Position switch
                {
                    PlayerPosition.South => 0,
                    PlayerPosition.East => 1,
                    PlayerPosition.North => 2,
                    PlayerPosition.West => 3,
                    _ => 0
                };

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
                    playerRoundTexts[index].color = roundPts > 0 ? GoldAccent : TextGray;
                }
            }
        }
    }
}
