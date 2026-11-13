using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrainJump
{
    /// <summary>
    /// Bottom-left virtual joystick for touch movement. Works with mouse pointer events in the editor.
    /// Exposes a dead-zoned, magnitude-normalized input vector for PlayerController to poll.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;

        [Header("Settings")]
        [SerializeField, Min(0f)] private float deadZone = 0.15f;
        [SerializeField, Min(0f)] private float knobRadius = 80f;

        /// <summary>
        /// Current joystick input. Zero when untouched. Magnitude is clamped to 0..1,
        /// with the configured dead zone removed and the remaining range rescaled.
        /// </summary>
        public Vector2 Input { get; private set; }

        private bool hasActivePointer;
        private int activePointerId;

        private void Awake()
        {
            if (background == null)
            {
                background = transform as RectTransform;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // A second finger must not take over an active joystick drag.
            if (hasActivePointer)
            {
                return;
            }

            hasActivePointer = true;
            activePointerId = eventData.pointerId;
            UpdateKnob(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!hasActivePointer || eventData.pointerId != activePointerId)
            {
                return;
            }

            UpdateKnob(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!hasActivePointer || eventData.pointerId != activePointerId)
            {
                return;
            }

            ResetJoystick();
        }

        private void OnDisable()
        {
            // Prevent stuck input if the joystick is disabled mid-drag.
            ResetJoystick();
        }

        private void OnValidate()
        {
            deadZone = Mathf.Clamp01(deadZone);
            knobRadius = Mathf.Max(0f, knobRadius);
        }

        private void UpdateKnob(PointerEventData eventData)
        {
            if (background == null || knob == null || eventData == null)
            {
                return;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
            {
                return;
            }

            float radius = Mathf.Max(0f, knobRadius);
            if (radius <= 0f)
            {
                knob.anchoredPosition = Vector2.zero;
                Input = Vector2.zero;
                return;
            }

            Vector2 clampedPoint = Vector2.ClampMagnitude(localPoint, radius);
            knob.anchoredPosition = clampedPoint;

            // localPoint.y is positive when dragged upward, which maps directly to forward movement.
            Vector2 rawInput = clampedPoint / radius;
            Input = ApplyDeadZone(rawInput);
        }

        private Vector2 ApplyDeadZone(Vector2 input)
        {
            float magnitude = input.magnitude;
            float clampedDeadZone = Mathf.Clamp01(deadZone);

            if (magnitude <= clampedDeadZone)
            {
                return Vector2.zero;
            }

            float range = 1f - clampedDeadZone;
            if (range <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            float rescaledMagnitude = Mathf.Clamp01((magnitude - clampedDeadZone) / range);
            return input / magnitude * rescaledMagnitude;
        }

        private void ResetJoystick()
        {
            hasActivePointer = false;
            Input = Vector2.zero;

            if (knob != null)
            {
                knob.anchoredPosition = Vector2.zero;
            }
        }
    }
}
