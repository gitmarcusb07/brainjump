using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BrainJump
{
    /// <summary>
    /// Large bottom-right jump button. Supports touch and mouse input and invokes
    /// onJump once per accepted press. Includes visual press feedback and a brief cooldown.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Events")]
        public UnityEvent onJump = new UnityEvent();

        [Header("References")]
        [SerializeField] private Image buttonImage;

        [Header("Visuals")]
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField, Range(0f, 1f)] private float pressedDarkenAmount = 0.15f;

        [Header("Input")]
        [SerializeField, Min(0f)] private float pressCooldown = 0.15f;

        private Vector3 originalScale;
        private Color originalColor;
        private bool hasOriginalColor;
        private bool isPressed;
        private int activePointerId;
        private float lastPressTime = float.NegativeInfinity;

        private void Awake()
        {
            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }

            originalScale = transform.localScale;

            if (buttonImage != null)
            {
                originalColor = buttonImage.color;
                hasOriginalColor = true;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Ignore additional fingers while this button is already held.
            if (isPressed)
            {
                return;
            }

            // Prevent accidental double jumps from a single tap registering twice.
            if (Time.unscaledTime - lastPressTime < pressCooldown)
            {
                return;
            }

            isPressed = true;
            activePointerId = eventData.pointerId;
            lastPressTime = Time.unscaledTime;

            ApplyPressedVisual();
            onJump?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPressed || eventData.pointerId != activePointerId)
            {
                return;
            }

            isPressed = false;
            RestoreVisual();
        }

        private void OnDisable()
        {
            // Avoid leaving the button in its pressed visual state if disabled mid-press.
            if (isPressed)
            {
                isPressed = false;
                RestoreVisual();
            }
        }

        private void OnValidate()
        {
            pressedScale = Mathf.Max(0f, pressedScale);
            pressedDarkenAmount = Mathf.Clamp01(pressedDarkenAmount);
            pressCooldown = Mathf.Max(0f, pressCooldown);
        }

        private void ApplyPressedVisual()
        {
            transform.localScale = originalScale * pressedScale;

            if (buttonImage != null && hasOriginalColor)
            {
                float darkenFactor = 1f - Mathf.Clamp01(pressedDarkenAmount);
                buttonImage.color = new Color(
                    originalColor.r * darkenFactor,
                    originalColor.g * darkenFactor,
                    originalColor.b * darkenFactor,
                    originalColor.a);
            }
        }

        private void RestoreVisual()
        {
            transform.localScale = originalScale;

            if (buttonImage != null && hasOriginalColor)
            {
                buttonImage.color = originalColor;
            }
        }
    }
}
