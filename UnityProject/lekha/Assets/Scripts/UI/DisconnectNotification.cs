using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Core;

namespace Lekha.UI
{
    /// <summary>
    /// Centered banner notification for player disconnect/reconnect/bot replacement.
    /// Shows countdown timer for disconnections, auto-dismiss for reconnections.
    /// </summary>
    public class DisconnectNotification : MonoBehaviour
    {
        public static DisconnectNotification Instance { get; private set; }

        private RectTransform rootPanel;
        private Image backgroundImage;
        private TextMeshProUGUI messageText;
        private CanvasGroup canvasGroup;

        // Tracking active notifications
        private class NotificationEntry
        {
            public PlayerPosition Position;
            public string PlayerName;
            public float TimeRemaining;
            public NotificationType Type;
            public float AutoDismissTime;
        }

        private enum NotificationType
        {
            Disconnected,
            Reconnected,
            BotReplaced
        }

        private Dictionary<PlayerPosition, NotificationEntry> activeNotifications = new Dictionary<PlayerPosition, NotificationEntry>();
        private Coroutine updateCoroutine;

        // Colors
        private static readonly Color BgColor = new Color(0.08f, 0.10f, 0.15f, 0.88f);
        private static readonly Color DisconnectColor = new Color(1f, 0.65f, 0.2f, 1f);   // Orange
        private static readonly Color ReconnectColor = new Color(0.3f, 0.85f, 0.5f, 1f);   // Green
        private static readonly Color BotColor = new Color(0.5f, 0.7f, 1f, 1f);            // Light blue

        public static DisconnectNotification Create(Transform parent)
        {
            GameObject obj = new GameObject("DisconnectNotification");
            obj.transform.SetParent(parent, false);

            DisconnectNotification notif = obj.AddComponent<DisconnectNotification>();
            return notif;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildUI();
            rootPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void BuildUI()
        {
            // Root panel - centered horizontally, upper-center of screen
            GameObject panelObj = new GameObject("NotifPanel");
            panelObj.transform.SetParent(transform, false);

            rootPanel = panelObj.AddComponent<RectTransform>();
            rootPanel.anchorMin = new Vector2(0.5f, 0.5f);
            rootPanel.anchorMax = new Vector2(0.5f, 0.5f);
            rootPanel.pivot = new Vector2(0.5f, 0.5f);
            rootPanel.anchoredPosition = new Vector2(0, 300); // Above center, below top panels
            rootPanel.sizeDelta = new Vector2(650, 70);

            canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            // Background
            backgroundImage = panelObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                backgroundImage.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = new Color(1f, 1f, 1f, 0.92f);
            }
            else
            {
                backgroundImage.color = BgColor;
            }

            // Border outline
            Outline outline = panelObj.AddComponent<Outline>();
            outline.effectColor = new Color(DisconnectColor.r, DisconnectColor.g, DisconnectColor.b, 0.5f);
            outline.effectDistance = new Vector2(2, -2);

            // Message text
            GameObject textObj = new GameObject("Message");
            textObj.transform.SetParent(panelObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 8);
            textRect.offsetMax = new Vector2(-20, -8);

            messageText = textObj.AddComponent<TextMeshProUGUI>();
            messageText.text = "";
            messageText.fontSize = 22;
            messageText.fontStyle = FontStyles.Bold;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = DisconnectColor;
            messageText.raycastTarget = false;
        }

        public void ShowDisconnected(PlayerPosition pos, string playerName, float timeoutSeconds)
        {
            activeNotifications[pos] = new NotificationEntry
            {
                Position = pos,
                PlayerName = playerName,
                TimeRemaining = timeoutSeconds,
                Type = NotificationType.Disconnected,
                AutoDismissTime = -1 // No auto-dismiss, countdown drives it
            };

            RefreshDisplay();
            rootPanel.gameObject.SetActive(true);

            if (updateCoroutine == null)
                updateCoroutine = StartCoroutine(CountdownCoroutine());
        }

        public void ShowReconnected(PlayerPosition pos, string playerName)
        {
            // Remove disconnect entry and show reconnect message
            activeNotifications[pos] = new NotificationEntry
            {
                Position = pos,
                PlayerName = playerName,
                TimeRemaining = 0,
                Type = NotificationType.Reconnected,
                AutoDismissTime = 3f
            };

            RefreshDisplay();
            rootPanel.gameObject.SetActive(true);
        }

        public void ShowBotReplaced(PlayerPosition pos, string playerName)
        {
            activeNotifications[pos] = new NotificationEntry
            {
                Position = pos,
                PlayerName = playerName,
                TimeRemaining = 0,
                Type = NotificationType.BotReplaced,
                AutoDismissTime = 5f
            };

            RefreshDisplay();
            rootPanel.gameObject.SetActive(true);
        }

        private IEnumerator CountdownCoroutine()
        {
            while (activeNotifications.Count > 0)
            {
                List<PlayerPosition> toRemove = new List<PlayerPosition>();

                foreach (var kvp in activeNotifications)
                {
                    var entry = kvp.Value;

                    if (entry.Type == NotificationType.Disconnected)
                    {
                        entry.TimeRemaining -= Time.deltaTime;
                        if (entry.TimeRemaining <= 0)
                            toRemove.Add(kvp.Key);
                    }
                    else if (entry.AutoDismissTime > 0)
                    {
                        entry.AutoDismissTime -= Time.deltaTime;
                        if (entry.AutoDismissTime <= 0)
                            toRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in toRemove)
                    activeNotifications.Remove(key);

                RefreshDisplay();

                if (activeNotifications.Count == 0)
                {
                    rootPanel.gameObject.SetActive(false);
                    updateCoroutine = null;
                    yield break;
                }

                yield return null;
            }

            rootPanel.gameObject.SetActive(false);
            updateCoroutine = null;
        }

        private void RefreshDisplay()
        {
            if (activeNotifications.Count == 0)
            {
                rootPanel.gameObject.SetActive(false);
                return;
            }

            // Build combined message from all active notifications
            // Priority: Disconnected > BotReplaced > Reconnected
            NotificationEntry primary = null;
            foreach (var kvp in activeNotifications)
            {
                if (primary == null ||
                    kvp.Value.Type == NotificationType.Disconnected ||
                    (kvp.Value.Type == NotificationType.BotReplaced && primary.Type != NotificationType.Disconnected))
                {
                    primary = kvp.Value;
                }
            }

            if (primary == null) return;

            switch (primary.Type)
            {
                case NotificationType.Disconnected:
                    int seconds = Mathf.CeilToInt(primary.TimeRemaining);
                    messageText.text = $"{primary.PlayerName} disconnected. Reconnecting... ({seconds}s)";
                    messageText.color = DisconnectColor;
                    SetOutlineColor(DisconnectColor);
                    break;

                case NotificationType.Reconnected:
                    messageText.text = $"{primary.PlayerName} reconnected!";
                    messageText.color = ReconnectColor;
                    SetOutlineColor(ReconnectColor);
                    break;

                case NotificationType.BotReplaced:
                    messageText.text = $"{primary.PlayerName} replaced by bot";
                    messageText.color = BotColor;
                    SetOutlineColor(BotColor);
                    break;
            }

            // If multiple disconnections, show count
            int disconnectCount = 0;
            foreach (var kvp in activeNotifications)
            {
                if (kvp.Value.Type == NotificationType.Disconnected)
                    disconnectCount++;
            }
            if (disconnectCount > 1)
            {
                messageText.text += $" (+{disconnectCount - 1} more)";
            }
        }

        private void SetOutlineColor(Color color)
        {
            var outline = rootPanel.GetComponent<Outline>();
            if (outline != null)
                outline.effectColor = new Color(color.r, color.g, color.b, 0.5f);
        }
    }
}
