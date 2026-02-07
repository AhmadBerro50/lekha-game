using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Lekha.Audio;

namespace Lekha.Effects
{
    /// <summary>
    /// Dramatic visual effect for special cards (Queen of Spades, 10 of Diamonds)
    /// Creates screen shake, crack overlay, and impact effect - MAXIMUM DRAMA VERSION
    /// </summary>
    public class SpecialCardEffect : MonoBehaviour
    {
        public static SpecialCardEffect Instance { get; private set; }

        private Canvas effectCanvas;
        private RectTransform canvasRect;
        private bool isAnimating = false;

        private void Awake()
        {
            Debug.Log("[SpecialCardEffect] Awake called");
            if (Instance != null && Instance != this)
            {
                Debug.Log("[SpecialCardEffect] Another instance exists, destroying this one");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[SpecialCardEffect] Instance set successfully");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Initialize(Canvas parentCanvas)
        {
            effectCanvas = parentCanvas;
            canvasRect = parentCanvas.GetComponent<RectTransform>();
            Debug.Log($"[SpecialCardEffect] Initialized with canvas: {parentCanvas.name}");
        }

        /// <summary>
        /// Play the Queen of Spades effect (dangerous card - red/purple theme) - DEVASTATING
        /// </summary>
        public void PlayQueenOfSpadesEffect()
        {
            Debug.Log($"[SpecialCardEffect] PlayQueenOfSpadesEffect called! isAnimating={isAnimating}, effectCanvas={effectCanvas != null}");
            if (isAnimating) return;
            StartCoroutine(PlayImpactEffect(
                new Color(0.6f, 0.1f, 0.8f, 1f),  // Purple
                new Color(0.9f, 0.2f, 0.3f, 1f),  // Red
                "+13",  // Show the actual points!
                "QUEEN OF SPADES!",
                true  // More intense
            ));
        }

        /// <summary>
        /// Play the 10 of Diamonds effect (special card - yellow/gold theme)
        /// </summary>
        public void PlayTenOfDiamondsEffect()
        {
            Debug.Log($"[SpecialCardEffect] PlayTenOfDiamondsEffect called! isAnimating={isAnimating}, effectCanvas={effectCanvas != null}");
            if (isAnimating) return;
            StartCoroutine(PlayImpactEffect(
                new Color(1f, 0.85f, 0.2f, 1f),   // Yellow
                new Color(1f, 0.6f, 0.1f, 1f),   // Orange
                "×0",  // Multiply by zero
                "10 OF DIAMONDS!",
                false  // Less intense but still impactful
            ));
        }

        private IEnumerator PlayImpactEffect(Color primaryColor, Color secondaryColor, string pointsLabel, string cardName, bool intense)
        {
            Debug.Log($"[SpecialCardEffect] PlayImpactEffect starting! label={pointsLabel}, intense={intense}, effectCanvas={effectCanvas != null}");

            if (effectCanvas == null)
            {
                Debug.LogError("[SpecialCardEffect] effectCanvas is NULL! Cannot play effect.");
                yield break;
            }

            isAnimating = true;

            // Play breaking sound
            Debug.Log("[SpecialCardEffect] Playing impact sound...");
            SoundManager.Instance?.PlaySpecialCardImpact(intense);

            // Create effect container
            GameObject effectObj = new GameObject("ImpactEffect");
            effectObj.transform.SetParent(effectCanvas.transform, false);

            RectTransform effectRect = effectObj.AddComponent<RectTransform>();
            effectRect.anchorMin = Vector2.zero;
            effectRect.anchorMax = Vector2.one;
            effectRect.sizeDelta = Vector2.zero;

            // Ensure it's on top
            Canvas effectLayer = effectObj.AddComponent<Canvas>();
            effectLayer.overrideSorting = true;
            effectLayer.sortingOrder = 500;
            effectObj.AddComponent<GraphicRaycaster>();

            // Create dark vignette overlay (screen darkening)
            GameObject vignetteObj = new GameObject("Vignette");
            vignetteObj.transform.SetParent(effectObj.transform, false);
            RectTransform vignetteRect = vignetteObj.AddComponent<RectTransform>();
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.sizeDelta = Vector2.zero;

            Image vignette = vignetteObj.AddComponent<Image>();
            vignette.sprite = CreateVignetteSprite(256);
            vignette.color = new Color(0, 0, 0, 0);
            vignette.raycastTarget = false;

            // Create flash overlay
            GameObject flashObj = new GameObject("Flash");
            flashObj.transform.SetParent(effectObj.transform, false);
            RectTransform flashRect = flashObj.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.sizeDelta = Vector2.zero;

            Image flash = flashObj.AddComponent<Image>();
            flash.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0f);
            flash.raycastTarget = false;

            // Create MORE crack lines for drama
            GameObject cracksObj = CreateCrackLines(effectObj.transform, primaryColor, secondaryColor, intense ? 16 : 10);

            // Create center impact circle - BIGGER
            GameObject impactObj = new GameObject("ImpactCircle");
            impactObj.transform.SetParent(effectObj.transform, false);
            RectTransform impactRect = impactObj.AddComponent<RectTransform>();
            impactRect.anchorMin = new Vector2(0.5f, 0.5f);
            impactRect.anchorMax = new Vector2(0.5f, 0.5f);
            impactRect.sizeDelta = new Vector2(250, 250);  // Bigger!

            Image impactImage = impactObj.AddComponent<Image>();
            impactImage.sprite = CreateGlowSprite(128);
            impactImage.color = primaryColor;
            impactImage.raycastTarget = false;

            // Create secondary shockwave ring
            GameObject ringObj = new GameObject("ShockwaveRing");
            ringObj.transform.SetParent(effectObj.transform, false);
            RectTransform ringRect = ringObj.AddComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.sizeDelta = new Vector2(100, 100);

            Image ringImage = ringObj.AddComponent<Image>();
            ringImage.sprite = CreateRingSprite(128);
            ringImage.color = secondaryColor;
            ringImage.raycastTarget = false;

            // Create card name text (top)
            GameObject nameObj = new GameObject("CardName");
            nameObj.transform.SetParent(effectObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(600, 80);
            nameRect.anchoredPosition = new Vector2(0, 120);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = cardName;
            nameText.fontSize = intense ? 48 : 42;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.raycastTarget = false;
            nameText.outlineWidth = 0.25f;
            nameText.outlineColor = new Color(0, 0, 0, 0.9f);

            // Create points label text (center) - HUGE
            GameObject labelObj = new GameObject("PointsLabel");
            labelObj.transform.SetParent(effectObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(300, 150);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = pointsLabel;
            labelText.fontSize = intense ? 120 : 96;  // HUGE text
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
            labelText.outlineWidth = 0.3f;
            labelText.outlineColor = new Color(0, 0, 0, 0.9f);

            // Create "POINTS!" subtitle
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(effectObj.transform, false);
            RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtitleRect.sizeDelta = new Vector2(300, 50);
            subtitleRect.anchoredPosition = new Vector2(0, -80);

            TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleText.text = intense ? "POINTS!" : "POINTS SAVED!";
            subtitleText.fontSize = 32;
            subtitleText.fontStyle = FontStyles.Bold;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = secondaryColor;
            subtitleText.raycastTarget = false;
            subtitleText.outlineWidth = 0.2f;
            subtitleText.outlineColor = new Color(0, 0, 0, 0.8f);

            // Animation parameters - LONGER and MORE DRAMATIC
            float shakeIntensity = intense ? 40f : 25f;
            float totalDuration = intense ? 2.0f : 1.5f;

            // Store original canvas position for shake
            Vector3 originalPos = effectCanvas.transform.localPosition;

            // Initialize states
            impactRect.localScale = Vector3.zero;
            labelRect.localScale = Vector3.zero;
            nameRect.localScale = Vector3.zero;
            subtitleRect.localScale = Vector3.zero;
            ringRect.localScale = Vector3.zero;

            // ========== PHASE 1: INITIAL IMPACT (0.2s) ==========
            float phase1Time = 0.2f;
            float elapsed = 0;

            while (elapsed < phase1Time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / phase1Time;

                // Dark vignette fades in
                vignette.color = new Color(0, 0, 0, Mathf.Lerp(0, intense ? 0.6f : 0.4f, t));

                // Bright flash
                flash.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, Mathf.Lerp(0, 0.7f, t));

                // Impact circle EXPLODES in
                float impactScale = Mathf.Lerp(0, 2f, EaseOutBack(t));
                impactRect.localScale = Vector3.one * impactScale;

                // Points label SLAMS in
                labelRect.localScale = Vector3.one * Mathf.Lerp(0, 1.5f, EaseOutBack(t));

                // Card name slides in
                nameRect.localScale = Vector3.one * Mathf.Lerp(0, 1.2f, EaseOutBack(Mathf.Clamp01(t * 1.5f)));

                // Heavy screen shake
                Vector3 shake = new Vector3(
                    Random.Range(-1f, 1f) * shakeIntensity,
                    Random.Range(-1f, 1f) * shakeIntensity,
                    0
                );
                effectCanvas.transform.localPosition = originalPos + shake;

                yield return null;
            }

            // ========== PHASE 2: SHOCKWAVE (0.3s) ==========
            float phase2Time = 0.3f;
            elapsed = 0;

            while (elapsed < phase2Time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / phase2Time;

                // Flash fades slightly
                flash.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, Mathf.Lerp(0.7f, 0.3f, t));

                // Shockwave ring expands
                float ringScale = Mathf.Lerp(0.5f, 4f, t);
                ringRect.localScale = Vector3.one * ringScale;
                ringImage.color = new Color(secondaryColor.r, secondaryColor.g, secondaryColor.b, Mathf.Lerp(0.8f, 0f, t));

                // Impact pulses
                float pulse = 1.8f + Mathf.Sin(t * Mathf.PI * 4) * 0.3f;
                impactRect.localScale = Vector3.one * pulse;

                // Subtitle appears
                subtitleRect.localScale = Vector3.one * Mathf.Lerp(0, 1.1f, EaseOutBack(t));

                // Decreasing shake
                float shakeAmount = shakeIntensity * (1 - t * 0.5f);
                Vector3 shake = new Vector3(
                    Random.Range(-1f, 1f) * shakeAmount,
                    Random.Range(-1f, 1f) * shakeAmount,
                    0
                );
                effectCanvas.transform.localPosition = originalPos + shake;

                yield return null;
            }

            // ========== PHASE 3: DRAMATIC HOLD (0.8s for intense, 0.5s for normal) ==========
            float phase3Time = intense ? 0.8f : 0.5f;
            elapsed = 0;

            while (elapsed < phase3Time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / phase3Time;

                // Slow pulsing
                float pulse = 1.5f + Mathf.Sin(elapsed * 8f) * 0.15f * (1 - t);
                impactRect.localScale = Vector3.one * pulse;

                // Text pulsing for emphasis
                float textPulse = 1.3f + Mathf.Sin(elapsed * 6f) * 0.1f * (1 - t);
                labelRect.localScale = Vector3.one * textPulse;

                // Gentle residual shake
                float shakeAmount = shakeIntensity * 0.3f * (1 - t);
                Vector3 shake = new Vector3(
                    Random.Range(-1f, 1f) * shakeAmount,
                    Random.Range(-1f, 1f) * shakeAmount,
                    0
                );
                effectCanvas.transform.localPosition = originalPos + shake;

                // Crack lines pulse
                foreach (Transform child in cracksObj.transform)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        float a = 0.8f + Mathf.Sin(elapsed * 10f + child.GetSiblingIndex()) * 0.2f;
                        Color c = img.color;
                        img.color = new Color(c.r, c.g, c.b, a * (1 - t * 0.3f));
                    }
                }

                yield return null;
            }

            // Reset position
            effectCanvas.transform.localPosition = originalPos;

            // ========== PHASE 4: FADE OUT (0.5s) ==========
            float phase4Time = 0.5f;
            elapsed = 0;
            Color vignetteColor = vignette.color;
            Color flashColor = flash.color;
            Color impactColor = impactImage.color;
            Color textColor = labelText.color;
            Color nameColor = nameText.color;
            Color subtitleColor = subtitleText.color;

            while (elapsed < phase4Time)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / phase4Time;

                vignette.color = new Color(0, 0, 0, vignetteColor.a * (1 - t));
                flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * (1 - t));
                impactImage.color = new Color(impactColor.r, impactColor.g, impactColor.b, impactColor.a * (1 - t));
                labelText.color = new Color(textColor.r, textColor.g, textColor.b, textColor.a * (1 - t));
                nameText.color = new Color(nameColor.r, nameColor.g, nameColor.b, nameColor.a * (1 - t));
                subtitleText.color = new Color(subtitleColor.r, subtitleColor.g, subtitleColor.b, subtitleColor.a * (1 - t));

                // Scale down elegantly
                impactRect.localScale = Vector3.one * Mathf.Lerp(1.5f, 0.5f, t);
                labelRect.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.8f, t);
                nameRect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.9f, t);

                // Fade crack lines
                foreach (Transform child in cracksObj.transform)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        Color c = img.color;
                        img.color = new Color(c.r, c.g, c.b, c.a * (1 - t * 2));
                    }
                }

                yield return null;
            }

            // Cleanup
            Destroy(effectObj);
            isAnimating = false;
        }

        private GameObject CreateCrackLines(Transform parent, Color primary, Color secondary, int numCracks)
        {
            GameObject cracksObj = new GameObject("Cracks");
            cracksObj.transform.SetParent(parent, false);

            RectTransform cracksRect = cracksObj.AddComponent<RectTransform>();
            cracksRect.anchorMin = new Vector2(0.5f, 0.5f);
            cracksRect.anchorMax = new Vector2(0.5f, 0.5f);
            cracksRect.sizeDelta = Vector2.zero;

            // Create radiating crack lines
            for (int i = 0; i < numCracks; i++)
            {
                float angle = (360f / numCracks) * i + Random.Range(-10f, 10f);
                float length = Random.Range(400f, 700f);  // Longer cracks
                float width = Random.Range(4f, 12f);  // Thicker cracks

                GameObject crackLine = new GameObject($"Crack_{i}");
                crackLine.transform.SetParent(cracksObj.transform, false);

                RectTransform lineRect = crackLine.AddComponent<RectTransform>();
                lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                lineRect.pivot = new Vector2(0, 0.5f);
                lineRect.sizeDelta = new Vector2(length, width);
                lineRect.localRotation = Quaternion.Euler(0, 0, angle);

                Image lineImage = crackLine.AddComponent<Image>();
                lineImage.color = i % 2 == 0 ? primary : secondary;
                lineImage.raycastTarget = false;

                // Add glow effect
                Shadow glow = crackLine.AddComponent<Shadow>();
                glow.effectColor = new Color(lineImage.color.r, lineImage.color.g, lineImage.color.b, 0.7f);
                glow.effectDistance = new Vector2(0, 0);
            }

            return cracksObj;
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
                    float alpha = Mathf.Pow(Mathf.Max(0, 1 - dist), 2f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRingSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float ringWidth = 0.15f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float alpha = 0;
                    if (dist > 0.7f && dist < 0.7f + ringWidth)
                    {
                        float ringDist = Mathf.Abs(dist - (0.7f + ringWidth / 2f)) / (ringWidth / 2f);
                        alpha = 1 - ringDist;
                    }
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateVignetteSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    // Vignette - dark edges, clear center
                    float alpha = Mathf.Pow(dist, 1.5f);
                    tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }
    }
}
