using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// A spinning, bobbing power-up pickup. The SceneBuilder creates the visual and the trigger collider.
    /// This script colours the visual by type, animates it and grants the power-up when touched.
    /// </summary>
    public class PowerUpPickup : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly Color ExtraTimeColor = new Color(0.4588f, 0.7255f, 0.8314f);
        private static readonly Color DoubleJumpColor = new Color(0.9490f, 0.7216f, 0.3569f);
        private static readonly Color ShieldColor = new Color(0.2980f, 0.5882f, 0.4392f);

        [Header("Animation")]
        [SerializeField] private float spinSpeed = 120f;
        [SerializeField] private float bobAmount = 0.2f;
        [SerializeField] private float bobSpeed = 2f;

        private Transform _visual;
        private Renderer _visualRenderer;
        private Material _material;
        private float _baseY;
        private bool _cached;

        /// <summary>
        /// The type of power-up this pickup grants.
        /// </summary>
        public PowerUpType Type { get; private set; }

        private void Awake()
        {
            CacheReferences();
            _baseY = transform.localPosition.y;
        }

        private void CacheReferences()
        {
            if (_cached)
            {
                return;
            }

            Transform found = transform.Find("Visual");
            _visual = found != null ? found : transform;
            _visualRenderer = _visual.GetComponent<Renderer>();
            _material = _visualRenderer != null ? _visualRenderer.material : null;
            _cached = true;
        }

        /// <summary>
        /// Sets the power-up type and tints the visual to match it.
        /// </summary>
        /// <param name="type">The power-up type this pickup should grant.</param>
        public void Setup(PowerUpType type)
        {
            Type = type;
            CacheReferences();

            switch (type)
            {
                case PowerUpType.ExtraTime:
                    ApplyColor(ExtraTimeColor);
                    break;
                case PowerUpType.DoubleJump:
                    ApplyColor(DoubleJumpColor);
                    break;
                case PowerUpType.Shield:
                    ApplyColor(ShieldColor);
                    break;
            }
        }

        private void ApplyColor(Color color)
        {
            if (_material == null)
            {
                return;
            }

            if (_material.HasProperty(BaseColorId))
            {
                _material.SetColor(BaseColorId, color);
            }

            if (_material.HasProperty(ColorId))
            {
                _material.SetColor(ColorId, color);
            }
        }

        private void Update()
        {
            if (_visual != null)
            {
                _visual.localRotation = Quaternion.Euler(0f, spinSpeed * Time.deltaTime, 0f) * _visual.localRotation;
            }

            Vector3 position = transform.localPosition;
            position.y = _baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.localPosition = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !other.CompareTag("Player"))
            {
                return;
            }

            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.GameState.Playing)
            {
                return;
            }

            PowerUpManager.Instance?.GrantPowerUp(Type);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the pickup again so it can be collected.
        /// </summary>
        public void ResetPickup()
        {
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }
    }
}
