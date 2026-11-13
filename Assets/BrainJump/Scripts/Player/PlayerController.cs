using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Physics-based player movement for BrainJump.
    /// Handles keyboard and joystick input, jumping, ground/landing detection,
    /// fall detection, respawning, and freeze control from the GameManager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpVelocity = 8.5f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 0.15f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Fall Detection")]
        [SerializeField] private float fallY = -10f;

        [Header("References")]
        [SerializeField] private MobileJoystick joystick;
        [SerializeField] private Transform cameraTransform;

        private Rigidbody _rigidbody;
        private CapsuleCollider _capsuleCollider;
        private CharacterAnimator _characterAnimator;

        private Vector2 _joystickInput;
        private Vector2 _currentInput;
        private bool _wasGrounded;
        private bool _fallReported;
        private bool _frozen;

        public bool IsGrounded { get; private set; }

        public Vector3 Velocity => _rigidbody != null ? _rigidbody.velocity : Vector3.zero;

        private bool IsGamePlaying
        {
            get
            {
                GameManager gameManager = GameManager.Instance;
                return gameManager != null && gameManager.State == GameManager.GameState.Playing;
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
            _characterAnimator = GetComponent<CharacterAnimator>();

            if (_rigidbody != null)
            {
                _rigidbody.useGravity = true;
                _rigidbody.freezeRotation = true;
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (joystick == null)
            {
                joystick = FindFirstObjectByType<MobileJoystick>();
            }
        }

        private void Update()
        {
            Vector2 keyboardInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            Vector2 joystickInput = GetJoystickInput();
            Vector2 effectiveInput = keyboardInput.magnitude > 0.1f ? keyboardInput : joystickInput;

            _currentInput = _frozen ? Vector2.zero : Vector2.ClampMagnitude(effectiveInput, 1f);

            UpdateGrounded();

            if (!_frozen && Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            if (!_fallReported && transform.position.y < fallY)
            {
                _fallReported = true;
                GameManager.Instance?.OnPlayerFell();
            }
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null)
            {
                return;
            }

            if (_frozen)
            {
                _rigidbody.velocity = Vector3.zero;
                _characterAnimator?.SetRunning(false);
                return;
            }

            if (!IsGamePlaying)
            {
                _characterAnimator?.SetRunning(false);
                return;
            }

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            right.y = 0f;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }
            right.Normalize();

            Vector3 desiredVelocity = (forward * _currentInput.y + right * _currentInput.x) * moveSpeed;
            Vector3 currentVelocity = _rigidbody.velocity;
            _rigidbody.velocity = new Vector3(desiredVelocity.x, currentVelocity.y, desiredVelocity.z);

            float inputMagnitude = _currentInput.magnitude;
            if (inputMagnitude > 0.1f)
            {
                Vector3 moveDirection = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
                if (moveDirection.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                    _rigidbody.rotation = Quaternion.Slerp(
                        _rigidbody.rotation,
                        targetRotation,
                        rotationSpeed * Time.fixedDeltaTime);
                }
            }

            _characterAnimator?.SetRunning(inputMagnitude > 0.1f && IsGrounded);
        }

        /// <summary>
        /// Stores joystick input for use by the keyboard/joystick input selection in Update.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _joystickInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Requests a jump. Ignored unless the game is playing and the player is grounded.
        /// </summary>
        public void Jump()
        {
            if (_frozen || !IsGrounded || _rigidbody == null)
            {
                return;
            }

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.State != GameManager.GameState.Playing)
            {
                return;
            }

            Vector3 velocity = _rigidbody.velocity;
            velocity.y = jumpVelocity;
            _rigidbody.velocity = velocity;

            IsGrounded = false;
            _wasGrounded = false;

            AudioManager.Instance?.PlayJump();
            _characterAnimator?.OnJump();
        }

        /// <summary>
        /// Raycasts below the capsule to update grounded state and detect landings.
        /// </summary>
        private void UpdateGrounded()
        {
            bool wasGrounded = _wasGrounded;

            float capsuleHeight = _capsuleCollider != null ? _capsuleCollider.height : 2f;
            float halfHeight = capsuleHeight * 0.5f;
            // Raycast from the capsule centre (not the feet): with a feet-based
            // origin the check stays true for ~0.9 m after take-off, which
            // would allow a mid-air double jump. From the centre, the player
            // is only "grounded" within groundCheckDistance of the surface.
            float checkDistance = halfHeight + groundCheckDistance;
            Vector3 rayOrigin = transform.position + Vector3.up * (halfHeight + 0.1f);

            IsGrounded = Physics.Raycast(
                rayOrigin,
                Vector3.down,
                checkDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore);

            float verticalVelocity = _rigidbody != null ? _rigidbody.velocity.y : 0f;

            if (IsGrounded && !wasGrounded && verticalVelocity < -1f)
            {
                AudioManager.Instance?.PlayLand();
                _characterAnimator?.OnLand();
                GameManager.Instance?.OnPlayerLanded();
            }

            _wasGrounded = IsGrounded;
        }

        /// <summary>
        /// Uses the externally supplied joystick value when present, otherwise polls
        /// the assigned joystick so the player remains usable without an input bridge.
        /// </summary>
        private Vector2 GetJoystickInput()
        {
            if (_joystickInput.sqrMagnitude > 0.0001f)
            {
                return _joystickInput;
            }

            return joystick != null ? joystick.Input : Vector2.zero;
        }

        /// <summary>
        /// Teleports the player back to the supplied spawn point and clears transient state.
        /// </summary>
        public void ResetToSpawn(Vector3 spawnPosition)
        {
            transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            _fallReported = false;
            _wasGrounded = false;
            IsGrounded = false;
            _currentInput = Vector2.zero;
            _joystickInput = Vector2.zero;

            _characterAnimator?.ResetCharacter();
        }

        /// <summary>
        /// Stops all movement while frozen. Used by the GameManager on failure or completion.
        /// </summary>
        public void Freeze(bool frozen)
        {
            _frozen = frozen;

            if (frozen)
            {
                _currentInput = Vector2.zero;

                if (_rigidbody != null)
                {
                    _rigidbody.velocity = Vector3.zero;
                }

                _characterAnimator?.SetRunning(false);
            }
        }
    }
}
