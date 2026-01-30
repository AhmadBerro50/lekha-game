using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lekha.Animation
{
    /// <summary>
    /// Handles smooth card animations with realistic throwing physics
    /// </summary>
    public class CardAnimator : MonoBehaviour
    {
        public static CardAnimator Instance { get; private set; }

        [Header("Animation Settings")]
        private float dealDuration = 0.2f;
        private float throwDuration = 0.25f;
        private float collectDuration = 0.35f;
        private float flipDuration = 0.15f;

        [Header("Throw Settings")]
        private float maxRotation = 12f; // Max rotation during throw
        private float arcHeight = 40f; // Height of throwing arc

        [Header("Easing")]
        private AnimationCurve throwCurve;
        private AnimationCurve arcCurve;
        private AnimationCurve rotateCurve;

        // Track active animations per card to prevent conflicts
        private Dictionary<RectTransform, Coroutine> activeAnimations = new Dictionary<RectTransform, Coroutine>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create smooth easing curves for realistic motion
            throwCurve = new AnimationCurve(
                new Keyframe(0, 0, 0, 2f),
                new Keyframe(0.7f, 0.95f, 0.5f, 0.5f),
                new Keyframe(1, 1, 0.2f, 0)
            );

            // Arc curve - peaks in the middle
            arcCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.4f, 1f),
                new Keyframe(1, 0)
            );

            // Rotation settles at the end
            rotateCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.3f, 1f),
                new Keyframe(0.7f, 0.3f),
                new Keyframe(1, 0)
            );
        }

        /// <summary>
        /// Stop any active animation on this card
        /// </summary>
        private void StopActiveAnimation(RectTransform card)
        {
            if (card != null && activeAnimations.TryGetValue(card, out Coroutine activeCoroutine))
            {
                if (activeCoroutine != null)
                {
                    StopCoroutine(activeCoroutine);
                }
                activeAnimations.Remove(card);
            }
        }

        /// <summary>
        /// Track an animation for a card
        /// </summary>
        private void TrackAnimation(RectTransform card, Coroutine coroutine)
        {
            if (card != null)
            {
                StopActiveAnimation(card);
                activeAnimations[card] = coroutine;
            }
        }

        /// <summary>
        /// Animate a card moving from one position to another
        /// </summary>
        public void AnimateMove(RectTransform card, Vector2 targetPosition, Action onComplete = null)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                return;
            }
            StopActiveAnimation(card);
            var coroutine = StartCoroutine(MoveCoroutine(card, targetPosition, 0.2f, onComplete));
            TrackAnimation(card, coroutine);
        }

        /// <summary>
        /// Animate a card being dealt to a position
        /// </summary>
        public void AnimateDeal(RectTransform card, Vector2 startPosition, Vector2 targetPosition, float delay, Action onComplete = null)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                return;
            }
            StopActiveAnimation(card);
            var coroutine = StartCoroutine(DealCoroutine(card, startPosition, targetPosition, delay, onComplete));
            TrackAnimation(card, coroutine);
        }

        /// <summary>
        /// Animate a card being thrown to the table - realistic throwing motion
        /// </summary>
        public void AnimatePlay(RectTransform card, Vector2 targetPosition, Action onComplete = null)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                return;
            }
            StopActiveAnimation(card);
            var coroutine = StartCoroutine(ThrowCardCoroutine(card, targetPosition, onComplete));
            TrackAnimation(card, coroutine);
        }

        /// <summary>
        /// Animate collecting trick cards to winner
        /// </summary>
        public void AnimateCollect(RectTransform[] cards, Vector2 targetPosition, Action onComplete = null)
        {
            StartCoroutine(CollectCoroutine(cards, targetPosition, onComplete));
        }

        /// <summary>
        /// Animate card flip
        /// </summary>
        public void AnimateFlip(RectTransform card, Action onFlipMidpoint = null, Action onComplete = null)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                return;
            }
            StopActiveAnimation(card);
            var coroutine = StartCoroutine(FlipCoroutine(card, onFlipMidpoint, onComplete));
            TrackAnimation(card, coroutine);
        }

        /// <summary>
        /// Animate card hover (scale up slightly)
        /// </summary>
        public void AnimateHover(RectTransform card, bool hovering)
        {
            if (card == null) return;
            float targetScale = hovering ? 1.08f : 1f;
            card.localScale = Vector3.one * targetScale;
        }

        /// <summary>
        /// Animate card selection (move up and glow)
        /// </summary>
        public void AnimateSelect(RectTransform card, bool selected, float yOffset)
        {
            if (card == null) return;
            StopActiveAnimation(card);
            var coroutine = StartCoroutine(SelectCoroutine(card, selected, yOffset));
            TrackAnimation(card, coroutine);
        }

        /// <summary>
        /// Punch scale animation for emphasis
        /// </summary>
        public void PunchScale(RectTransform target, float intensity = 0.2f)
        {
            if (target == null) return;
            StartCoroutine(PunchScaleCoroutine(target, intensity));
        }

        private IEnumerator MoveCoroutine(RectTransform card, Vector2 targetPosition, float duration, Action onComplete)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector2 startPosition = card.anchoredPosition;
            float elapsed = 0;

            while (elapsed < duration)
            {
                if (card == null)
                {
                    onComplete?.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                card.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            if (card != null)
            {
                card.anchoredPosition = targetPosition;
                activeAnimations.Remove(card);
            }

            onComplete?.Invoke();
        }

        private IEnumerator DealCoroutine(RectTransform card, Vector2 startPosition, Vector2 targetPosition, float delay, Action onComplete)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Set initial state - invisible and at start
            card.anchoredPosition = startPosition;
            card.localScale = Vector3.one * 0.3f;
            card.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-10f, 10f));

            // Wait for delay
            yield return new WaitForSeconds(delay);

            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0;
            Quaternion startRot = card.localRotation;

            while (elapsed < dealDuration)
            {
                if (card == null)
                {
                    onComplete?.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / dealDuration;
                float smoothT = Mathf.SmoothStep(0, 1, t);
                float scaleT = Mathf.Sin(t * Mathf.PI * 0.5f);

                card.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, smoothT);
                card.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one, scaleT);
                card.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, smoothT);

                yield return null;
            }

            if (card != null)
            {
                card.anchoredPosition = targetPosition;
                card.localScale = Vector3.one;
                card.localRotation = Quaternion.identity;
                activeAnimations.Remove(card);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Realistic card throwing animation with arc and rotation
        /// </summary>
        private IEnumerator ThrowCardCoroutine(RectTransform card, Vector2 targetPosition, Action onComplete)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector2 startPosition = card.anchoredPosition;
            Vector3 startScale = card.localScale;

            // Random rotation direction for variety
            float rotationDir = UnityEngine.Random.Range(-1f, 1f);
            float targetRotation = rotationDir * maxRotation;

            // Calculate arc direction (perpendicular to throw direction)
            Vector2 throwDir = (targetPosition - startPosition).normalized;
            Vector2 arcDir = new Vector2(-throwDir.y, throwDir.x); // Perpendicular

            float elapsed = 0;

            while (elapsed < throwDuration)
            {
                if (card == null)
                {
                    // Card was destroyed mid-animation, still invoke callback
                    onComplete?.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / throwDuration;

                // Position with arc
                float moveT = throwCurve.Evaluate(t);
                float arcT = arcCurve.Evaluate(t);
                Vector2 basePos = Vector2.Lerp(startPosition, targetPosition, moveT);
                Vector2 arcOffset = arcDir * arcT * arcHeight;
                card.anchoredPosition = basePos + arcOffset;

                // Rotation - spins then settles
                float rotT = rotateCurve.Evaluate(t);
                card.localRotation = Quaternion.Euler(0, 0, targetRotation * rotT);

                // Slight scale pulse during throw
                float scaleT = 1f + (0.08f * Mathf.Sin(t * Mathf.PI));
                card.localScale = startScale * scaleT;

                yield return null;
            }

            // Final state - ensure card ends at exact target
            if (card != null)
            {
                card.anchoredPosition = targetPosition;
                card.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-2f, 2f)); // Slight random final rotation
                card.localScale = startScale;
                // Don't remove from tracking - let the next animation or explicit stop handle it
            }

            onComplete?.Invoke();
        }

        private IEnumerator CollectCoroutine(RectTransform[] cards, Vector2 targetPosition, Action onComplete)
        {
            // Brief delay before collecting so all 4 cards are visible
            yield return new WaitForSeconds(0.4f);

            // Store start positions and validate cards - stop any conflicting animations
            List<int> validIndices = new List<int>();
            Vector2[] startPositions = new Vector2[cards.Length];
            Quaternion[] startRotations = new Quaternion[cards.Length];
            Vector3[] startScales = new Vector3[cards.Length];

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                {
                    validIndices.Add(i);
                    startPositions[i] = cards[i].anchoredPosition;
                    startRotations[i] = cards[i].localRotation;
                    startScales[i] = cards[i].localScale;
                    // Stop any active animation on this card to prevent conflicts
                    StopActiveAnimation(cards[i]);
                }
            }

            if (validIndices.Count == 0)
            {
                Debug.LogWarning("[CollectCoroutine] No valid cards to collect!");
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0;

            while (elapsed < collectDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / collectDuration);
                float scaleT = 1 - (t * 0.7f); // Shrink more
                float spinT = t * 120f; // Less spin

                foreach (int i in validIndices)
                {
                    if (cards[i] != null)
                    {
                        cards[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPosition, t);
                        cards[i].localScale = startScales[i] * scaleT;
                        cards[i].localRotation = startRotations[i] * Quaternion.Euler(0, 0, spinT);
                    }
                }

                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator FlipCoroutine(RectTransform card, Action onFlipMidpoint, Action onComplete)
        {
            if (card == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0;
            float halfDuration = flipDuration / 2;
            Vector3 startScale = card.localScale;

            while (elapsed < halfDuration)
            {
                if (card == null)
                {
                    onComplete?.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                card.localScale = new Vector3(startScale.x * (1 - t), startScale.y, startScale.z);
                yield return null;
            }

            onFlipMidpoint?.Invoke();

            elapsed = 0;
            while (elapsed < halfDuration)
            {
                if (card == null)
                {
                    onComplete?.Invoke();
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                card.localScale = new Vector3(startScale.x * t, startScale.y, startScale.z);
                yield return null;
            }

            if (card != null)
            {
                card.localScale = startScale;
                activeAnimations.Remove(card);
            }

            onComplete?.Invoke();
        }

        private IEnumerator SelectCoroutine(RectTransform card, bool selected, float yOffset)
        {
            if (card == null) yield break;

            Vector2 startPos = card.anchoredPosition;
            Vector2 targetPos = startPos;
            targetPos.y += selected ? yOffset : -yOffset;

            float duration = 0.12f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                if (card == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                card.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            if (card != null)
            {
                card.anchoredPosition = targetPos;
                activeAnimations.Remove(card);
            }
        }

        private IEnumerator PunchScaleCoroutine(RectTransform target, float intensity)
        {
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            float duration = 0.25f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                if (target == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float punch = Mathf.Sin(t * Mathf.PI) * intensity;
                target.localScale = originalScale * (1 + punch);
                yield return null;
            }

            if (target != null)
                target.localScale = originalScale;
        }

        private void OnDestroy()
        {
            activeAnimations.Clear();
        }
    }
}
