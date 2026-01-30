using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Core;
using Lekha.GameLogic;
using Lekha.Audio;
using System.Collections;
using System.Collections.Generic;

namespace Lekha.UI
{
    /// <summary>
    /// Enhanced game over screen with luxurious animations and statistics
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        public static GameOverScreen Instance { get; private set; }

        private Canvas screenCanvas;
        private GameObject screenPanel;
        private CanvasGroup canvasGroup;

        // UI Elements
        private TextMeshProUGUI resultTitle;
        private TextMeshProUGUI resultSubtitle;
        private Image resultIcon;
        private GameObject statsContainer;
        private List<GameObject> confettiParticles = new List<GameObject>();
        private Coroutine confettiCoroutine;

        // Colors
        private static readonly Color WinGold = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color WinGoldDark = new Color(0.85f, 0.68f, 0.25f, 1f);
        private static readonly Color LoseRed = new Color(0.8f, 0.25f, 0.2f, 1f);
        private static readonly Color LoseRedDark = new Color(0.6f, 0.15f, 0.12f, 1f);
        private static readonly Color DeepGreen = new Color(0.04f, 0.12f, 0.08f, 1f);
        private static readonly Color CreamWhite = new Color(0.98f, 0.96f, 0.90f, 1f);

        public System.Action OnPlayAgain;
        public System.Action OnMainMenu;

        // Losing player info
        private string losingPlayerName;
        private int losingPlayerScore;
        private bool isOnlineGame;

        // Barteyyeh UI
        private TextMeshProUGUI barteyyehText;
        private TextMeshProUGUI playAgainButtonText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Show(Team winningTeam, int yourScore, int opponentScore, int roundsPlayed)
        {
            Show(winningTeam, yourScore, opponentScore, roundsPlayed, null, 0, false);
        }

        public void Show(Team winningTeam, int yourScore, int opponentScore, int roundsPlayed, string loserName, int loserScore, bool online)
        {
            // Determine if the local player won based on their actual team
            bool playerWon;
            if (GameManager.Instance != null)
            {
                var localPlayer = GameManager.Instance.GetHumanPlayer();
                playerWon = localPlayer != null && localPlayer.Team == winningTeam;
            }
            else
            {
                playerWon = winningTeam == Team.NorthSouth;
            }
            losingPlayerName = loserName;
            losingPlayerScore = loserScore;
            isOnlineGame = online;

            if (screenCanvas == null)
            {
                CreateUI();
            }

            // Update content based on result
            UpdateContent(playerWon, yourScore, opponentScore, roundsPlayed);

            screenCanvas.gameObject.SetActive(true);
            StartCoroutine(AnimateEntrance(playerWon));
        }

        public void Hide()
        {
            // Stop confetti coroutine before starting exit animation
            if (confettiCoroutine != null)
            {
                StopCoroutine(confettiCoroutine);
                confettiCoroutine = null;
            }
            StartCoroutine(AnimateExit());
        }

        private void CreateUI()
        {
            // Canvas
            GameObject canvasObj = new GameObject("GameOverCanvas");
            canvasObj.transform.SetParent(transform);
            screenCanvas = canvasObj.AddComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            screenCanvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            // Dark overlay
            GameObject overlayObj = new GameObject("Overlay");
            overlayObj.transform.SetParent(canvasObj.transform, false);

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.92f);

            // Main panel
            screenPanel = new GameObject("GameOverPanel");
            screenPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = screenPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700, 650);

            Image panelBg = screenPanel.AddComponent<Image>();
            panelBg.color = new Color(DeepGreen.r, DeepGreen.g, DeepGreen.b, 0.98f);

            // Create UI elements
            CreateResultSection(screenPanel.transform);
            CreateBarteyyehSection(screenPanel.transform);
            CreateStatsSection(screenPanel.transform);
            CreateButtons(screenPanel.transform);
        }

        private void CreateResultSection(Transform parent)
        {
            // Result icon container (trophy or X)
            GameObject iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(parent, false);

            RectTransform iconContainerRect = iconContainer.AddComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0.5f, 1f);
            iconContainerRect.anchorMax = new Vector2(0.5f, 1f);
            iconContainerRect.anchoredPosition = new Vector2(0, -100);
            iconContainerRect.sizeDelta = new Vector2(120, 120);

            // Glow behind icon
            GameObject glowObj = new GameObject("IconGlow");
            glowObj.transform.SetParent(iconContainer.transform, false);

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(180, 180);

            Image glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = CreateGlowSprite(128);
            glowImg.color = new Color(WinGold.r, WinGold.g, WinGold.b, 0.5f);
            glowImg.raycastTarget = false;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(iconContainer.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(100, 100);

            resultIcon = iconObj.AddComponent<Image>();
            resultIcon.sprite = CreateTrophySprite();
            resultIcon.color = WinGold;

            // Result title
            GameObject titleObj = new GameObject("ResultTitle");
            titleObj.transform.SetParent(parent, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -200);
            titleRect.sizeDelta = new Vector2(600, 80);

            resultTitle = titleObj.AddComponent<TextMeshProUGUI>();
            resultTitle.text = "VICTORY!";
            resultTitle.fontSize = 72;
            resultTitle.fontStyle = FontStyles.Bold;
            resultTitle.alignment = TextAlignmentOptions.Center;
            resultTitle.enableVertexGradient = true;
            resultTitle.colorGradient = new VertexGradient(WinGold, WinGold, WinGoldDark, WinGoldDark);

            // Subtitle
            GameObject subObj = new GameObject("ResultSubtitle");
            subObj.transform.SetParent(parent, false);

            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0, -265);
            subRect.sizeDelta = new Vector2(500, 40);

            resultSubtitle = subObj.AddComponent<TextMeshProUGUI>();
            resultSubtitle.text = "You and your partner won the game!";
            resultSubtitle.fontSize = 24;
            resultSubtitle.alignment = TextAlignmentOptions.Center;
            resultSubtitle.color = CreamWhite;
            resultSubtitle.fontStyle = FontStyles.Italic;
        }

        private void CreateStatsSection(Transform parent)
        {
            statsContainer = new GameObject("StatsContainer");
            statsContainer.transform.SetParent(parent, false);

            RectTransform containerRect = statsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.anchoredPosition = new Vector2(0, -380);
            containerRect.sizeDelta = new Vector2(550, 150);

            Image containerBg = statsContainer.AddComponent<Image>();
            containerBg.color = new Color(0, 0, 0, 0.3f);

            // Stats will be populated dynamically
        }

        private void CreateStatItem(Transform parent, string label, string value, Vector2 position, Color valueColor)
        {
            GameObject itemObj = new GameObject($"Stat_{label}");
            itemObj.transform.SetParent(parent, false);

            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0.5f, 0.5f);
            itemRect.anchorMax = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = position;
            itemRect.sizeDelta = new Vector2(150, 80);

            // Value
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(itemObj.transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 1f);
            valueRect.anchorMax = new Vector2(0.5f, 1f);
            valueRect.anchoredPosition = new Vector2(0, -15);
            valueRect.sizeDelta = new Vector2(150, 50);

            TextMeshProUGUI valueTmp = valueObj.AddComponent<TextMeshProUGUI>();
            valueTmp.text = value;
            valueTmp.fontSize = 42;
            valueTmp.fontStyle = FontStyles.Bold;
            valueTmp.alignment = TextAlignmentOptions.Center;
            valueTmp.color = valueColor;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(itemObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0, 15);
            labelRect.sizeDelta = new Vector2(150, 30);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 16;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = new Color(CreamWhite.r, CreamWhite.g, CreamWhite.b, 0.7f);
        }

        private void CreateBarteyyehSection(Transform parent)
        {
            GameObject barteyyehObj = new GameObject("BarteyyehScore");
            barteyyehObj.transform.SetParent(parent, false);

            RectTransform rect = barteyyehObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -310);
            rect.sizeDelta = new Vector2(400, 40);

            barteyyehText = barteyyehObj.AddComponent<TextMeshProUGUI>();
            barteyyehText.text = "";
            barteyyehText.fontSize = 22;
            barteyyehText.fontStyle = FontStyles.Bold;
            barteyyehText.alignment = TextAlignmentOptions.Center;
            barteyyehText.color = WinGold;
        }

        private void CreateButtons(Transform parent)
        {
            // Play Again / Next Game button
            GameObject playBtn = CreateButton(parent, "NEXT GAME", new Vector2(0, -520), true, () => {
                SoundManager.Instance?.PlayButtonClick();
                Hide();
                OnPlayAgain?.Invoke();
            });
            playAgainButtonText = playBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Main Menu button
            CreateButton(parent, "MAIN MENU", new Vector2(0, -590), false, () => {
                SoundManager.Instance?.PlayButtonClick();
                Hide();
                OnMainMenu?.Invoke();
            });
        }

        private GameObject CreateButton(Transform parent, string text, Vector2 position, bool isPrimary, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = isPrimary ? new Vector2(280, 60) : new Vector2(220, 50);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = CreateRoundedRectSprite(128, 48, 12);
            img.type = Image.Type.Sliced;
            img.color = isPrimary ? WinGold : new Color(WinGold.r, WinGold.g, WinGold.b, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            if (isPrimary)
            {
                colors.normalColor = WinGold;
                colors.highlightedColor = new Color(WinGold.r * 1.1f, WinGold.g * 1.1f, WinGold.b * 1.1f, 1f);
                colors.pressedColor = WinGoldDark;
            }
            else
            {
                colors.normalColor = new Color(1, 1, 1, 0.2f);
                colors.highlightedColor = new Color(1, 1, 1, 0.35f);
                colors.pressedColor = new Color(1, 1, 1, 0.15f);
            }
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            if (!isPrimary)
            {
                Outline outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(WinGold.r, WinGold.g, WinGold.b, 0.5f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // Shadow
            Shadow shadow = btnObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(2, -2);

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = isPrimary ? 26 : 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isPrimary ? new Color(0.15f, 0.08f, 0.04f, 1f) : CreamWhite;

            return btnObj;
        }

        private void UpdateContent(bool playerWon, int yourScore, int opponentScore, int roundsPlayed)
        {
            // Build subtitle with losing player info
            string loserInfo = "";
            if (!string.IsNullOrEmpty(losingPlayerName))
            {
                loserInfo = $"{losingPlayerName} reached {losingPlayerScore} points!";
            }

            if (playerWon)
            {
                resultTitle.text = "VICTORY!";
                resultTitle.colorGradient = new VertexGradient(WinGold, WinGold, WinGoldDark, WinGoldDark);
                resultSubtitle.text = !string.IsNullOrEmpty(loserInfo)
                    ? loserInfo
                    : "You and your partner won the game!";
                resultIcon.sprite = CreateTrophySprite();
                resultIcon.color = WinGold;

                Transform glow = resultIcon.transform.parent.Find("IconGlow");
                if (glow != null)
                {
                    glow.GetComponent<Image>().color = new Color(WinGold.r, WinGold.g, WinGold.b, 0.5f);
                }
            }
            else
            {
                resultTitle.text = "DEFEAT";
                resultTitle.colorGradient = new VertexGradient(LoseRed, LoseRed, LoseRedDark, LoseRedDark);
                resultSubtitle.text = !string.IsNullOrEmpty(loserInfo)
                    ? loserInfo
                    : "Better luck next time!";
                resultIcon.sprite = CreateDefeatSprite();
                resultIcon.color = LoseRed;

                Transform glow = resultIcon.transform.parent.Find("IconGlow");
                if (glow != null)
                {
                    glow.GetComponent<Image>().color = new Color(LoseRed.r, LoseRed.g, LoseRed.b, 0.4f);
                }
            }

            // Clear old stats
            foreach (Transform child in statsContainer.transform)
            {
                Destroy(child.gameObject);
            }

            // Show all 4 player scores
            if (GameManager.Instance != null)
            {
                var players = GameManager.Instance.Players;
                float spacing = 130f;
                float startX = -(spacing * 1.5f);
                for (int i = 0; i < 4; i++)
                {
                    string name = players[i].PlayerName;
                    if (name.Length > 6) name = name.Substring(0, 5) + "..";
                    int score = players[i].TotalPoints;
                    Color color = score >= 101 ? LoseRed : (playerWon && players[i].Team == Team.NorthSouth ? WinGold : CreamWhite);
                    CreateStatItem(statsContainer.transform, name, score.ToString(), new Vector2(startX + i * spacing, 0), color);
                }
            }
            else
            {
                // Fallback to team scores
                Color scoreColor = playerWon ? WinGold : LoseRed;
                CreateStatItem(statsContainer.transform, "YOUR SCORE", yourScore.ToString(), new Vector2(-120, 0), scoreColor);
                CreateStatItem(statsContainer.transform, "OPPONENT", opponentScore.ToString(), new Vector2(120, 0), CreamWhite);
            }

            // Update Barteyyeh display
            UpdateBarteyyehDisplay(playerWon);
        }

        private void UpdateBarteyyehDisplay(bool playerWon)
        {
            var bm = BarteyyehManager.Instance;
            if (bm == null || barteyyehText == null)
            {
                if (barteyyehText != null) barteyyehText.text = "";
                return;
            }

            // Get local player's team for relative display
            Team localTeam = Team.NorthSouth;
            if (GameManager.Instance != null)
            {
                var localPlayer = GameManager.Instance.GetHumanPlayer();
                if (localPlayer != null) localTeam = localPlayer.Team;
            }

            string seriesScore = bm.GetSeriesScoreForTeam(localTeam);
            barteyyehText.text = $"BARTEYYEH  {seriesScore}";
            barteyyehText.color = WinGold;

            if (bm.IsBarteyyehComplete)
            {
                bool localTeamWonBarteyyeh = bm.BarteyyehWinner == localTeam;

                // Override title for Barteyyeh completion
                if (localTeamWonBarteyyeh)
                {
                    resultTitle.text = "BARTEYYEH!";
                    resultTitle.colorGradient = new VertexGradient(WinGold, WinGold, WinGoldDark, WinGoldDark);
                    resultSubtitle.text = $"You won the Barteyyeh {seriesScore}!";
                }
                else
                {
                    resultTitle.text = "BARTEYYEH LOST";
                    resultTitle.colorGradient = new VertexGradient(LoseRed, LoseRed, LoseRedDark, LoseRedDark);
                    resultSubtitle.text = $"You lost the Barteyyeh {seriesScore}";
                }

                // Button: NEW BARTEYYEH
                if (playAgainButtonText != null)
                    playAgainButtonText.text = "NEW BARTEYYEH";
            }
            else
            {
                // Series still in progress
                barteyyehText.text = $"BARTEYYEH  Game {bm.GamesPlayed}/3  ({seriesScore})";

                if (playAgainButtonText != null)
                    playAgainButtonText.text = "NEXT GAME";
            }
        }

        private IEnumerator AnimateEntrance(bool playerWon)
        {
            // Fade in
            float duration = 0.4f;
            float elapsed = 0;

            RectTransform panelRect = screenPanel.GetComponent<RectTransform>();
            Vector2 targetPos = panelRect.anchoredPosition;
            panelRect.anchoredPosition = targetPos + new Vector2(0, -50);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = 1 - Mathf.Pow(1 - t, 3);

                canvasGroup.alpha = easeT;
                panelRect.anchoredPosition = Vector2.Lerp(targetPos + new Vector2(0, -50), targetPos, easeT);
                yield return null;
            }

            canvasGroup.alpha = 1;
            panelRect.anchoredPosition = targetPos;

            // Start confetti for win
            if (playerWon)
            {
                confettiCoroutine = StartCoroutine(SpawnConfetti());
            }
        }

        private IEnumerator AnimateExit()
        {
            // Destroy confetti particles (don't use StopAllCoroutines - it kills this coroutine too!)
            foreach (var particle in confettiParticles)
            {
                if (particle != null) Destroy(particle);
            }
            confettiParticles.Clear();

            float duration = 0.3f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = 1 - t;
                yield return null;
            }

            screenCanvas.gameObject.SetActive(false);
            canvasGroup.alpha = 0;
        }

        private IEnumerator SpawnConfetti()
        {
            while (true)
            {
                for (int i = 0; i < 3; i++)
                {
                    CreateConfettiParticle();
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void CreateConfettiParticle()
        {
            GameObject particle = new GameObject("Confetti");
            particle.transform.SetParent(screenCanvas.transform, false);
            confettiParticles.Add(particle);

            RectTransform rect = particle.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(Random.Range(-500f, 500f), 50);
            rect.sizeDelta = new Vector2(Random.Range(8f, 16f), Random.Range(8f, 16f));
            rect.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            Image img = particle.AddComponent<Image>();
            Color[] colors = { WinGold, new Color(1f, 0.5f, 0.2f), new Color(0.3f, 0.8f, 0.5f), CreamWhite };
            img.color = colors[Random.Range(0, colors.Length)];

            StartCoroutine(AnimateConfetti(rect, img));
        }

        private IEnumerator AnimateConfetti(RectTransform rect, Image img)
        {
            float duration = Random.Range(2f, 4f);
            float elapsed = 0;
            float rotSpeed = Random.Range(-180f, 180f);
            float wobbleSpeed = Random.Range(2f, 5f);
            float wobbleAmount = Random.Range(30f, 80f);
            Vector2 startPos = rect.anchoredPosition;

            while (elapsed < duration && rect != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float y = Mathf.Lerp(startPos.y, -700f, t);
                float x = startPos.x + Mathf.Sin(elapsed * wobbleSpeed) * wobbleAmount;
                rect.anchoredPosition = new Vector2(x, y);
                rect.Rotate(0, 0, rotSpeed * Time.deltaTime);

                // Fade out at end
                if (t > 0.7f)
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 1 - ((t - 0.7f) / 0.3f));
                }

                yield return null;
            }

            if (rect != null)
            {
                confettiParticles.Remove(rect.gameObject);
                Destroy(rect.gameObject);
            }
        }

        private Sprite CreateGlowSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float alpha = Mathf.Pow(Mathf.Max(0, 1 - dist), 2);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateTrophySprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // Clear
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Draw simple trophy shape
            // Cup body
            for (int y = 25; y < 55; y++)
            {
                int width = 30 - (int)((y - 25) * 0.5f);
                int startX = 32 - width / 2;
                for (int x = startX; x < startX + width; x++)
                {
                    if (x >= 0 && x < size)
                        tex.SetPixel(x, y, Color.white);
                }
            }

            // Handles
            for (int y = 35; y < 50; y++)
            {
                tex.SetPixel(12, y, Color.white);
                tex.SetPixel(13, y, Color.white);
                tex.SetPixel(50, y, Color.white);
                tex.SetPixel(51, y, Color.white);
            }
            for (int x = 12; x < 20; x++) { tex.SetPixel(x, 35, Color.white); tex.SetPixel(x, 50, Color.white); }
            for (int x = 44; x < 52; x++) { tex.SetPixel(x, 35, Color.white); tex.SetPixel(x, 50, Color.white); }

            // Stem
            for (int y = 10; y < 25; y++)
            {
                for (int x = 28; x < 36; x++)
                    tex.SetPixel(x, y, Color.white);
            }

            // Base
            for (int y = 5; y < 12; y++)
            {
                for (int x = 20; x < 44; x++)
                    tex.SetPixel(x, y, Color.white);
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateDefeatSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // Clear
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Draw X
            int thickness = 6;
            for (int i = 10; i < 54; i++)
            {
                for (int t = -thickness / 2; t <= thickness / 2; t++)
                {
                    // Diagonal 1
                    int x1 = i;
                    int y1 = i + t;
                    if (x1 >= 0 && x1 < size && y1 >= 0 && y1 < size)
                        tex.SetPixel(x1, y1, Color.white);

                    // Diagonal 2
                    int x2 = i;
                    int y2 = (size - 1 - i) + t;
                    if (x2 >= 0 && x2 < size && y2 >= 0 && y2 < size)
                        tex.SetPixel(x2, y2, Color.white);
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRoundedRectSprite(int width, int height, int radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, width, height, radius);
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;

            if (x < radius && y < radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius;
            if (x >= width - radius && y < radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) <= radius;
            if (x < radius && y >= height - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) <= radius;
            if (x >= width - radius && y >= height - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) <= radius;

            return true;
        }

        private void OnDestroy()
        {
            foreach (var particle in confettiParticles)
            {
                if (particle != null) Destroy(particle);
            }
            confettiParticles.Clear();
        }
    }
}
