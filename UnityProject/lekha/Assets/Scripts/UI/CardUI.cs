using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Lekha.Core;
using Lekha.Animation;
using Lekha.Audio;
using System.Collections;

namespace Lekha.UI
{
    /// <summary>
    /// Visual representation of a single card with enhanced visual feedback
    /// Includes hover effects, glow, scale animations, and sound
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Components")]
        private Image cardImage;
        private RectTransform rectTransform;
        private Shadow cardShadow;
        private Image glowImage;
        private Outline cardOutline;

        [Header("Card Data")]
        private Card card;
        public Card Card => card;

        [Header("State")]
        private bool isSelected = false;
        private bool isPlayable = true;
        private bool isFaceUp = true;
        private bool isHovering = false;
        private bool isPassedCard = false;
        private GameObject passBadge;
        private Vector2 originalPosition;
        private float originalScale = 1f;
        private float hoverOffset = 30f;
        private float selectedOffset = 50f;
        private float hoverScale = 1.08f;
        private Coroutine scaleCoroutine;
        private Coroutine glowCoroutine;

        // Visual settings - no glow for playable cards (cleaner look)
        private static readonly Color PlayableGlow = Color.clear;
        private static readonly Color SelectedGlow = new Color(0.4f, 1f, 0.5f, 0.8f);
        private static readonly Color HoverGlow = new Color(1f, 1f, 1f, 0.5f);

        // Events
        public System.Action<CardUI> OnCardClicked;
        public System.Action<CardUI> OnCardHovered;

        private void Awake()
        {
            cardImage = GetComponent<Image>();
            if (cardImage == null)
            {
                cardImage = gameObject.AddComponent<Image>();
            }

            rectTransform = GetComponent<RectTransform>();
            originalScale = rectTransform.localScale.x;

            // Add shadow for depth
            cardShadow = gameObject.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0, 0, 0, 0.5f);
            cardShadow.effectDistance = new Vector2(4, -4);

            // Add outline for selection/hover feedback
            cardOutline = gameObject.AddComponent<Outline>();
            cardOutline.effectColor = Color.clear;
            cardOutline.effectDistance = new Vector2(2, -2);

            // Create glow effect (behind card)
            CreateGlowEffect();
        }

        private void CreateGlowEffect()
        {
            GameObject glowObj = new GameObject("CardGlow");
            glowObj.transform.SetParent(transform, false);
            glowObj.transform.SetAsFirstSibling(); // Behind the card

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = rectTransform.sizeDelta + new Vector2(30, 30);

            glowImage = glowObj.AddComponent<Image>();
            glowImage.sprite = CreateGlowSprite(64);
            glowImage.color = Color.clear;
            glowImage.raycastTarget = false;
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
                    float alpha = Mathf.Pow(Mathf.Max(0, 1 - dist), 1.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Initialize the card with data
        /// </summary>
        public void SetCard(Card card, bool faceUp = true)
        {
            this.card = card;
            this.isFaceUp = faceUp;
            this.isPlayable = true;
            this.isPassedCard = false;
            UpdateSprite();
            HidePassBadge();

            if (cardImage != null)
            {
                cardImage.color = Color.white;
            }
        }

        /// <summary>
        /// Mark this card as passed (received from another player). Shows orange "P" on top-left.
        /// </summary>
        public void ShowPassBadge()
        {
            isPassedCard = true;
            if (passBadge != null) { passBadge.SetActive(true); return; }

            passBadge = new GameObject("PassBadge");
            passBadge.transform.SetParent(transform, false);

            RectTransform pRect = passBadge.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 1f);
            pRect.anchorMax = new Vector2(0f, 1f);
            pRect.pivot = new Vector2(0f, 1f);
            pRect.anchoredPosition = new Vector2(2, -2);
            pRect.sizeDelta = new Vector2(24, 24);

            Image pBg = passBadge.AddComponent<Image>();
            pBg.color = new Color(1f, 0.55f, 0.10f, 1f); // Orange
            pBg.raycastTarget = false;

            GameObject pTextObj = new GameObject("PText");
            pTextObj.transform.SetParent(passBadge.transform, false);
            RectTransform pTextRect = pTextObj.AddComponent<RectTransform>();
            pTextRect.anchorMin = Vector2.zero;
            pTextRect.anchorMax = Vector2.one;
            pTextRect.sizeDelta = Vector2.zero;
            var pText = pTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            pText.text = "P";
            pText.fontSize = 14;
            pText.fontStyle = TMPro.FontStyles.Bold;
            pText.color = Color.white;
            pText.alignment = TMPro.TextAlignmentOptions.Center;
            pText.raycastTarget = false;
        }

        public void HidePassBadge()
        {
            isPassedCard = false;
            if (passBadge != null) passBadge.SetActive(false);
        }

        public bool IsPassedCard => isPassedCard;

        /// <summary>
        /// Update the card sprite based on current state
        /// </summary>
        private void UpdateSprite()
        {
            // Use original JPG card sprites for best quality
            if (CardSpriteManager.Instance != null)
            {
                if (isFaceUp && card != null)
                {
                    cardImage.sprite = CardSpriteManager.Instance.GetCardSprite(card);
                }
                else
                {
                    cardImage.sprite = CardSpriteManager.Instance.GetCardBack();
                }
            }
        }

        /// <summary>
        /// Set whether this card can be played
        /// </summary>
        public void SetPlayable(bool playable)
        {
            isPlayable = playable;

            if (playable)
            {
                cardImage.color = Color.white;
                cardShadow.effectDistance = new Vector2(4, -4);

                // No glow for playable cards - cleaner look
                if (glowImage != null)
                {
                    SetGlow(Color.clear, 0.3f);
                }
                if (cardOutline != null)
                {
                    cardOutline.effectColor = Color.clear;
                }
            }
            else
            {
                // Dimmed when not playable
                cardImage.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                cardShadow.effectDistance = new Vector2(2, -2);

                // Remove glow
                if (glowImage != null)
                {
                    SetGlow(Color.clear, 0.2f);
                }
                if (cardOutline != null)
                {
                    cardOutline.effectColor = Color.clear;
                }
            }
        }

        private void SetGlow(Color targetColor, float duration)
        {
            if (glowCoroutine != null)
                StopCoroutine(glowCoroutine);
            glowCoroutine = StartCoroutine(AnimateGlow(targetColor, duration));
        }

        private IEnumerator AnimateGlow(Color targetColor, float duration)
        {
            if (glowImage == null) yield break;

            Color startColor = glowImage.color;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                glowImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            glowImage.color = targetColor;
        }

        private void SetScale(float targetScale, float duration)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(AnimateScale(targetScale, duration));
        }

        private IEnumerator AnimateScale(float targetScale, float duration)
        {
            float startScale = rectTransform.localScale.x;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = 1 - Mathf.Pow(1 - t, 2); // Ease out
                float scale = Mathf.Lerp(startScale, targetScale, easeT);
                rectTransform.localScale = Vector3.one * scale;
                yield return null;
            }

            rectTransform.localScale = Vector3.one * targetScale;
        }

        /// <summary>
        /// Set whether this card is selected
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            // Move card up when selected
            Vector2 targetPos = originalPosition;
            if (selected)
            {
                targetPos.y += selectedOffset;
                cardShadow.effectDistance = new Vector2(6, -6);

                // Play sound
                SoundManager.Instance?.PlayCardSelect();
            }
            else
            {
                cardShadow.effectDistance = new Vector2(4, -4);
            }

            // Animate to position
            if (CardAnimator.Instance != null)
            {
                CardAnimator.Instance.AnimateMove(rectTransform, targetPos);
            }
            else
            {
                rectTransform.anchoredPosition = targetPos;
            }
        }

        /// <summary>
        /// Flip the card face up or face down
        /// </summary>
        public void SetFaceUp(bool faceUp)
        {
            if (CardAnimator.Instance != null && isFaceUp != faceUp)
            {
                CardAnimator.Instance.AnimateFlip(rectTransform, () => {
                    isFaceUp = faceUp;
                    UpdateSprite();
                });
            }
            else
            {
                isFaceUp = faceUp;
                UpdateSprite();
            }
        }

        /// <summary>
        /// Store the original position for hover/select animations
        /// </summary>
        public void SetOriginalPosition(Vector2 position)
        {
            originalPosition = position;
            if (!isHovering && !isSelected)
            {
                rectTransform.anchoredPosition = position;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isPlayable)
            {
                // Punch animation on click
                if (CardAnimator.Instance != null)
                {
                    CardAnimator.Instance.PunchScale(rectTransform, 0.1f);
                }

                OnCardClicked?.Invoke(this);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;

            if (isPlayable && !isSelected)
            {
                Vector2 pos = originalPosition;
                pos.y += hoverOffset;

                // Animate hover with scale
                if (CardAnimator.Instance != null)
                {
                    CardAnimator.Instance.AnimateHover(rectTransform, true);
                    CardAnimator.Instance.AnimateMove(rectTransform, pos);
                }
                else
                {
                    rectTransform.anchoredPosition = pos;
                }

                // Enhanced visual feedback
                SetScale(hoverScale, 0.15f);
                SetGlow(HoverGlow, 0.15f);
                cardShadow.effectDistance = new Vector2(8, -8);
                cardShadow.effectColor = new Color(0, 0, 0, 0.6f);

                if (cardOutline != null)
                {
                    cardOutline.effectColor = new Color(1f, 1f, 1f, 0.6f);
                }

                // Play subtle hover sound
                SoundManager.Instance?.PlayCardHover();
            }

            OnCardHovered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;

            if (!isSelected)
            {
                // Animate back to original
                if (CardAnimator.Instance != null)
                {
                    CardAnimator.Instance.AnimateHover(rectTransform, false);
                    CardAnimator.Instance.AnimateMove(rectTransform, originalPosition);
                }
                else
                {
                    rectTransform.anchoredPosition = originalPosition;
                }

                // Reset visual feedback
                SetScale(originalScale, 0.15f);
                cardShadow.effectDistance = new Vector2(4, -4);
                cardShadow.effectColor = new Color(0, 0, 0, 0.5f);

                // Clear glow on exit - no persistent glow for cleaner look
                SetGlow(Color.clear, 0.2f);
                if (cardOutline != null)
                {
                    cardOutline.effectColor = Color.clear;
                }
            }
        }

        /// <summary>
        /// Clear all visual effects (used when card is played)
        /// </summary>
        public void ClearEffects()
        {
            if (glowImage != null)
                glowImage.color = Color.clear;
            if (cardOutline != null)
                cardOutline.effectColor = Color.clear;
            rectTransform.localScale = Vector3.one * originalScale;
        }
    }
}
