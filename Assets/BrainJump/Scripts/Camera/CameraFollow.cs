using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Follows the player from a fixed elevated offset and looks slightly ahead of them.
    /// Designed for portrait play so the answer platforms ahead remain visible.
    /// Does not orbit or allow runtime offset changes.
    /// Supports transient camera shake and field-of-view kicks for game feel.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Framing")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 8.5f, -8f);
        [SerializeField] private float lookAhead = 2f;
        [SerializeField] private float lookAtHeight = 1.2f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float smoothSpeed = 5f;

        // Camera shake state.
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimeLeft;

        // FOV kick state.
        private Camera _camera;
        private float _baseFov = 60f;
        private float _fovOffset;

        /// <summary>
        /// The transform to follow. Backed by the serialized field so SceneBuilder can assign it.
        /// </summary>
        public Transform Target
        {
            get { return target; }
            set { target = value; }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera != null)
            {
                _baseFov = _camera.fieldOfView;
            }
            else
            {
                _baseFov = 60f;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            float blend = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);

            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= Time.deltaTime;
                float k = _shakeDuration > 0f ? Mathf.Clamp01(_shakeTimeLeft / _shakeDuration) : 0f;
                Vector3 shakeOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * _shakeIntensity * k;
                transform.position += shakeOffset;
            }

            if (_camera != null && _fovOffset > 0.01f)
            {
                _fovOffset = Mathf.Lerp(_fovOffset, 0f, Time.deltaTime * 6f);
                _camera.fieldOfView = _baseFov + _fovOffset;
            }
            else if (_camera != null && _fovOffset != 0f)
            {
                _fovOffset = 0f;
                _camera.fieldOfView = _baseFov;
            }

            transform.LookAt(GetLookTarget());
        }

        /// <summary>
        /// Instantly snaps the camera to its framing position. Use after spawning or respawning
        /// to avoid sweeping the camera across the level.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + offset;
            transform.LookAt(GetLookTarget());

            _shakeTimeLeft = 0f;
            _shakeIntensity = 0f;

            _fovOffset = 0f;
            if (_camera != null)
            {
                _camera.fieldOfView = _baseFov;
            }
        }

        /// <summary>
        /// Starts or extends a camera shake. The strongest intensity and longest duration
        /// requested are kept, and the shake timer restarts. Non-positive values are ignored.
        /// </summary>
        /// <param name="intensity">Maximum positional offset in world units. Must be greater than zero.</param>
        /// <param name="duration">Shake duration in seconds. Must be greater than zero.</param>
        public void AddShake(float intensity, float duration)
        {
            if (!(intensity > 0f) || !(duration > 0f))
            {
                return;
            }

            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _shakeTimeLeft = _shakeDuration;
        }

        /// <summary>
        /// Applies a temporary field-of-view increase that eases back to the base FOV.
        /// Non-positive values are ignored.
        /// </summary>
        /// <param name="amount">Extra field of view in degrees. Must be greater than zero.</param>
        public void FovKick(float amount)
        {
            if (!(amount > 0f))
            {
                return;
            }

            _fovOffset = amount;
        }

        private Vector3 GetLookTarget()
        {
            return target.position + Vector3.up * lookAtHeight + target.forward * lookAhead;
        }
    }
}
