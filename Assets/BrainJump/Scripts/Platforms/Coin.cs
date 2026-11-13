using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// A spinning, bobbing coin pickup. The SceneBuilder creates the visual and the trigger collider.
    /// This script only animates the coin and collects it.
    /// </summary>
    public class Coin : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float spinSpeed = 180f;
        [SerializeField] private float bobAmount = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Transform _visual;
        private float _baseY;

        private void Awake()
        {
            Transform found = transform.Find("Visual");
            _visual = found != null ? found : transform;
            _baseY = transform.localPosition.y;
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

            Collect();
        }

        private void Collect()
        {
            CoinManager.Instance?.CollectCoin(transform.position);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the coin again so it can be collected.
        /// </summary>
        public void ResetCoin()
        {
            gameObject.SetActive(true);
        }
    }
}
