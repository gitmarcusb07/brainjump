using System.Collections;
using TMPro;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Physical answer platform. Displays an answer and reports an intentional
    /// player landing to the GameManager.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AnswerPlatform : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private TextMeshPro answerLabel;
        [SerializeField] private Renderer platformRenderer;

        [Header("Colors")]
        [SerializeField] private Color correctColor = new Color(0.9490196f, 0.7215686f, 0.35686275f, 1f); // #F2B85B
        [SerializeField] private Color wrongColor = new Color(0.9137255f, 0.50980395f, 0.4392157f, 1f);   // #E98270
        [SerializeField] private Color idleColor = new Color(0.45882353f, 0.7254902f, 0.83137256f, 1f);   // #75B9D4

        [Header("Bounce")]
        [SerializeField] private float bounceDuration = 0.25f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Material _platformMaterial;
        private Coroutine _bounceCoroutine;
        private bool _answeredThisRound;

        public int AnswerValue { get; private set; }
        public bool IsCorrect { get; private set; }

        private void Awake()
        {
            if (platformRenderer == null)
                platformRenderer = GetComponent<Renderer>();

            if (answerLabel == null)
                answerLabel = GetComponentInChildren<TextMeshPro>(true);

            CacheMaterial();
        }

        private void OnDestroy()
        {
            // The material was created via Renderer.material, so release the instance.
            if (_platformMaterial != null)
                Destroy(_platformMaterial);
        }

        /// <summary>
        /// Assigns the answer displayed by this platform and resets its visual state.
        /// </summary>
        public void SetAnswer(int value, bool isCorrect)
        {
            AnswerValue = value;
            IsCorrect = isCorrect;
            _answeredThisRound = false;

            if (answerLabel != null)
                answerLabel.text = value.ToString();

            ResetVisual();
        }

        public void ShowCorrect()
        {
            ApplyColor(correctColor);
        }

        public void ShowWrong()
        {
            ApplyColor(wrongColor);
        }

        public void ResetVisual()
        {
            StopBounce();
            transform.localScale = Vector3.one;
            ApplyColor(idleColor);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_answeredThisRound)
                return;

            if (collision == null || collision.collider == null || !collision.collider.CompareTag("Player"))
                return;

            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.GameState.Playing)
                return;

            bool hasContact = collision.contactCount > 0;
            bool movingDownOrLevel = collision.relativeVelocity.y <= 0.5f;
            bool contactAboveCenter = hasContact &&
                                      collision.GetContact(0).point.y > transform.position.y;

            if (!movingDownOrLevel && !contactAboveCenter)
                return;

            _answeredThisRound = true;

            StopBounce();
            _bounceCoroutine = StartCoroutine(BounceRoutine());

            manager.ValidateAnswer(this);
        }

        private IEnumerator BounceRoutine()
        {
            Vector3 originalScale = transform.localScale;
            float duration = Mathf.Max(0.01f, bounceDuration);
            float halfDuration = duration * 0.5f;
            Vector3 squashedScale = new Vector3(originalScale.x, originalScale.y * 0.8f, originalScale.z);

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(originalScale, squashedScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.Lerp(squashedScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
            _bounceCoroutine = null;
        }

        private void StopBounce()
        {
            if (_bounceCoroutine == null)
                return;

            StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = null;
        }

        private void CacheMaterial()
        {
            if (_platformMaterial != null || platformRenderer == null)
                return;

            // Renderer.material creates a per-platform material instance.
            _platformMaterial = platformRenderer.material;
        }

        private void ApplyColor(Color color)
        {
            CacheMaterial();
            if (_platformMaterial == null)
                return;

            // Supports both URP/Standard color property names.
            if (_platformMaterial.HasProperty(BaseColorId))
                _platformMaterial.SetColor(BaseColorId, color);

            if (_platformMaterial.HasProperty(ColorId))
                _platformMaterial.SetColor(ColorId, color);
        }
    }
}
