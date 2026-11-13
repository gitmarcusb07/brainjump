using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Fully procedural cartoon animation for the capsule character assembled from primitives.
    /// Handles idle breathing, running bob + limb swing, blinking, squash &amp; stretch on
    /// jump/land, the celebration hop and the "explode into debris" fail effect.
    /// The root transform is never scaled: it owns the collider.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [Header("Idle Breathing")]
        [SerializeField] private float breatheSpeed = 2f;
        [SerializeField] private float breatheAmount = 0.03f;

        [Header("Running")]
        [SerializeField] private float runBobAmount = 0.08f;
        [SerializeField] private float runBobSpeed = 10f;

        [Header("Squash & Stretch")]
        [SerializeField] private float squashDuration = 0.18f;

        [Header("Blinking")]
        [SerializeField] private float blinkIntervalMin = 2.5f;
        [SerializeField] private float blinkIntervalMax = 4.5f;
        [SerializeField] private float blinkCloseDuration = 0.12f;
        [SerializeField] private float blinkClosedScaleY = 0.1f;

        [Header("Explosion")]
        [SerializeField] private int debrisCount = 24;
        [SerializeField] private float debrisLifetime = 0.7f;

        // ------------------------------------------------------------------ state

        private bool _running;
        private bool _exploded;
        private Coroutine _currentAnim;

        /// <summary>True while a one-shot animation owns the visual transform.</summary>
        private bool _animating;

        // Body parts created by the SceneBuilder. Any of them may be missing.
        private Transform _visual;        // "Visual" container -> "Body" -> root fallback
        private bool _canAnimateVisual;   // false when we fell back to the root transform
        private Transform _body;
        private Transform _head;
        private Transform _eyeL;
        private Transform _eyeR;
        private Transform _armL;
        private Transform _armR;
        private Transform _legL;
        private Transform _legR;

        // Stored rest poses.
        private Vector3 _visualBaseLocalPosition;
        private Vector3 _visualBaseLocalScale;
        private Quaternion _visualBaseLocalRotation;

        private Vector3 _bodyBaseScale;
        private Quaternion _headBaseRotation;

        private Vector3 _eyeLBaseScale;
        private Vector3 _eyeRBaseScale;

        private Quaternion _armLBaseRotation;
        private Quaternion _armRBaseRotation;
        private Quaternion _legLBaseRotation;
        private Quaternion _legRBaseRotation;

        // Blink timing (done inline in Update — cheaper than a coroutine on mobile).
        private float _blinkTimer;
        private float _blinkTimeLeft;
        private bool _blinking;

        private ParticleSystem _particles;                       // optional, preferred if present
        private readonly List<GameObject> _debris = new List<GameObject>();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            // The "visual" is the thing we scale / hide / rotate: the Visual container if
            // present, otherwise the Body, otherwise the root (position effects only).
            Transform visualChild = transform.Find("Visual");
            if (visualChild == null)
            {
                visualChild = transform.Find("Body");
            }

            _canAnimateVisual = visualChild != null;
            _visual = _canAnimateVisual ? visualChild : transform;

            // Body parts live UNDER the Visual container (see SceneBuilder), so
            // search there first and fall back to direct children of the root.
            _body = FindPart("Body");
            _head = FindPart("Head");
            _eyeL = FindPart("EyeL");
            _eyeR = FindPart("EyeR");
            _armL = FindPart("ArmL");
            _armR = FindPart("ArmR");
            _legL = FindPart("LegL");
            _legR = FindPart("LegR");

            _visualBaseLocalPosition = _visual.localPosition;
            _visualBaseLocalScale = _visual.localScale;
            _visualBaseLocalRotation = _visual.localRotation;

            if (_body != null) _bodyBaseScale = _body.localScale;
            if (_head != null) _headBaseRotation = _head.localRotation;
            if (_eyeL != null) _eyeLBaseScale = _eyeL.localScale;
            if (_eyeR != null) _eyeRBaseScale = _eyeR.localScale;
            if (_armL != null) _armLBaseRotation = _armL.localRotation;
            if (_armR != null) _armRBaseRotation = _armR.localRotation;
            if (_legL != null) _legLBaseRotation = _legL.localRotation;
            if (_legR != null) _legRBaseRotation = _legR.localRotation;

            _particles = GetComponentInChildren<ParticleSystem>();

            _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        /// <summary>Finds a named body part under the Visual container, falling back to the root's direct children.</summary>
        private Transform FindPart(string name)
        {
            Transform found = _visual != null ? _visual.Find(name) : null;
            if (found == null && _visual != transform)
            {
                found = transform.Find(name);
            }
            return found;
        }

        // ------------------------------------------------------------------ per-frame animation

        private void Update()
        {
            if (_exploded)
            {
                return; // hidden until GameManager calls ResetCharacter()
            }

            UpdateBlink();
            UpdateLimbs();

            if (_animating)
            {
                return; // a one-shot coroutine owns the visual transform right now
            }

            UpdateBreathing();
            UpdateRunBob();
        }

        /// <summary>Vertical breathing on the body (inverse squeeze on X/Z for volume preservation).</summary>
        private void UpdateBreathing()
        {
            if (_body == null)
            {
                return;
            }

            float breath = 1f + Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;

            _body.localScale = new Vector3(
                _bodyBaseScale.x / breath,
                _bodyBaseScale.y * breath,
                _bodyBaseScale.z / breath);
        }

        /// <summary>Vertical bob of the visual while running.</summary>
        private void UpdateRunBob()
        {
            if (!_canAnimateVisual)
            {
                return; // never move/scale the root: it owns the collider
            }

            Vector3 position = _visualBaseLocalPosition;

            if (_running)
            {
                position.y += Mathf.Abs(Mathf.Sin(Time.time * runBobSpeed)) * runBobAmount;
            }

            _visual.localPosition = position;
        }

        /// <summary>Cheap limb swing while running plus a lazy head sway at all times.</summary>
        private void UpdateLimbs()
        {
            float swing = _running ? Mathf.Sin(Time.time * runBobSpeed) * 35f : 0f;

            if (_armL != null) _armL.localRotation = _armLBaseRotation * Quaternion.Euler(swing, 0f, 0f);
            if (_armR != null) _armR.localRotation = _armRBaseRotation * Quaternion.Euler(-swing, 0f, 0f);
            if (_legL != null) _legL.localRotation = _legLBaseRotation * Quaternion.Euler(-swing, 0f, 0f);
            if (_legR != null) _legR.localRotation = _legRBaseRotation * Quaternion.Euler(swing, 0f, 0f);

            if (_head != null)
            {
                float sway = Mathf.Sin(Time.time * breatheSpeed * 1.5f) * (breatheAmount * 60f);
                _head.localRotation = _headBaseRotation * Quaternion.Euler(sway, 0f, 0f);
            }
        }

        /// <summary>Timer-driven blink: squashes both eyes on Y for a fraction of a second.</summary>
        private void UpdateBlink()
        {
            if (_eyeL == null && _eyeR == null)
            {
                return;
            }

            if (_blinking)
            {
                _blinkTimeLeft -= Time.deltaTime;

                if (_blinkTimeLeft <= 0f)
                {
                    SetEyesOpen(true);
                    _blinking = false;
                    _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
                }

                return;
            }

            _blinkTimer -= Time.deltaTime;

            if (_blinkTimer <= 0f)
            {
                SetEyesOpen(false);
                _blinking = true;
                _blinkTimeLeft = Mathf.Max(0.02f, blinkCloseDuration);
            }
        }

        private void SetEyesOpen(bool open)
        {
            float factor = open ? 1f : Mathf.Clamp01(blinkClosedScaleY);

            if (_eyeL != null)
            {
                Vector3 scale = _eyeLBaseScale;
                scale.y *= factor;
                _eyeL.localScale = scale;
            }

            if (_eyeR != null)
            {
                Vector3 scale = _eyeRBaseScale;
                scale.y *= factor;
                _eyeR.localScale = scale;
            }
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Enables / disables the running bob and limb swing.</summary>
        public void SetRunning(bool running)
        {
            _running = running;
        }

        /// <summary>Quick vertical stretch that eases back to rest.</summary>
        public void OnJump()
        {
            if (_exploded || !_canAnimateVisual)
            {
                return;
            }

            PrepareForOneShot();
            _currentAnim = StartCoroutine(StretchRoutine());
        }

        /// <summary>Squash on landing, then recover over <see cref="squashDuration"/>.</summary>
        public void OnLand()
        {
            if (_exploded || !_canAnimateVisual)
            {
                return;
            }

            PrepareForOneShot();
            _currentAnim = StartCoroutine(SquashRoutine());
        }

        /// <summary>Happy hop with a full spin and a scale pulse. Ends exactly at the rest pose.</summary>
        public void OnCelebrate()
        {
            if (_exploded)
            {
                return;
            }

            PrepareForOneShot();
            _currentAnim = StartCoroutine(CelebrateRoutine());
        }

        /// <summary>Hides the character and bursts it into primitive debris. Stays hidden until reset.</summary>
        public void OnExplode()
        {
            if (_exploded)
            {
                return;
            }

            _exploded = true;
            _running = false;
            StopCurrentAnim();

            if (_particles != null)
            {
                _particles.Clear();
                _particles.Play();
            }

            if (_canAnimateVisual)
            {
                _visual.gameObject.SetActive(false);
            }

            // Spawns immediately (the routine runs up to its first yield synchronously).
            StartCoroutine(DebrisRoutine());
        }

        /// <summary>Restores the character after a fail: position, rotation, scale, eyes and debris.</summary>
        public void ResetCharacter()
        {
            StopAllCoroutines();

            _currentAnim = null;
            _animating = false;
            _exploded = false;
            _running = false;

            // Remove anything still flying around.
            for (int i = _debris.Count - 1; i >= 0; i--)
            {
                if (_debris[i] != null)
                {
                    DestroyDebris(_debris[i]);
                }
            }
            _debris.Clear();

            if (_visual != null)
            {
                _visual.gameObject.SetActive(true);
                _visual.localPosition = _visualBaseLocalPosition;

                if (_canAnimateVisual)
                {
                    _visual.localScale = _visualBaseLocalScale;
                    _visual.localRotation = _visualBaseLocalRotation;
                }
            }

            if (_body != null) _body.localScale = _bodyBaseScale;
            if (_head != null) _head.localRotation = _headBaseRotation;
            if (_armL != null) _armL.localRotation = _armLBaseRotation;
            if (_armR != null) _armR.localRotation = _armRBaseRotation;
            if (_legL != null) _legL.localRotation = _legLBaseRotation;
            if (_legR != null) _legR.localRotation = _legRBaseRotation;

            SetEyesOpen(true);

            _blinking = false;
            _blinkTimeLeft = 0f;
            _blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        // ------------------------------------------------------------------ one-shot coroutines

        /// <summary>Stops the running one-shot and puts the visual back exactly where it belongs.</summary>
        private void PrepareForOneShot()
        {
            StopCurrentAnim();
            RestoreVisualTransform();
        }

        private void StopCurrentAnim()
        {
            if (_currentAnim != null)
            {
                StopCoroutine(_currentAnim);
                _currentAnim = null;
            }

            _animating = false;
        }

        private void RestoreVisualTransform()
        {
            if (!_canAnimateVisual)
            {
                return;
            }

            _visual.localPosition = _visualBaseLocalPosition;
            _visual.localScale = _visualBaseLocalScale;
            _visual.localRotation = _visualBaseLocalRotation;
        }

        private IEnumerator StretchRoutine()
        {
            _animating = true;

            const float stretchTime = 0.25f;
            Vector3 stretched = Vector3.Scale(_visualBaseLocalScale, new Vector3(0.9f, 1.15f, 0.9f));

            _visual.localScale = stretched;

            float elapsed = 0f;
            while (elapsed < stretchTime)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / stretchTime));
                _visual.localScale = Vector3.Lerp(stretched, _visualBaseLocalScale, k);
                yield return null;
            }

            _visual.localScale = _visualBaseLocalScale;

            _animating = false;
            _currentAnim = null;
        }

        private IEnumerator SquashRoutine()
        {
            _animating = true;

            float duration = Mathf.Max(0.01f, squashDuration);
            Vector3 squashed = Vector3.Scale(_visualBaseLocalScale, new Vector3(1.15f, 0.8f, 1.15f));

            _visual.localScale = squashed;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _visual.localScale = Vector3.Lerp(squashed, _visualBaseLocalScale, k);
                yield return null;
            }

            _visual.localScale = _visualBaseLocalScale;

            _animating = false;
            _currentAnim = null;
        }

        private IEnumerator CelebrateRoutine()
        {
            _animating = true;

            const float duration = 0.6f;
            const float hopHeight = 0.5f;
            const float pulseScale = 0.1f;

            bool canScale = _canAnimateVisual; // never scale / rotate the root (collider!)

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                float arc = Mathf.Sin(k * Mathf.PI); // 0 -> 1 -> 0

                _visual.localPosition = _visualBaseLocalPosition + Vector3.up * (arc * hopHeight);

                if (canScale)
                {
                    _visual.localRotation = _visualBaseLocalRotation * Quaternion.Euler(0f, 360f * k, 0f);
                    _visual.localScale = _visualBaseLocalScale * (1f + arc * pulseScale);
                }

                yield return null;
            }

            // Land back on the exact rest pose.
            _visual.localPosition = _visualBaseLocalPosition;
            if (canScale)
            {
                _visual.localRotation = _visualBaseLocalRotation;
                _visual.localScale = _visualBaseLocalScale;
            }

            _animating = false;
            _currentAnim = null;
        }

        // ------------------------------------------------------------------ explosion debris

        /// <summary>Spawns the debris burst, simulates it (gravity + shrink) and cleans up.</summary>
        private IEnumerator DebrisRoutine()
        {
            int count = Mathf.Max(1, debrisCount);
            float life = Mathf.Max(0.05f, debrisLifetime);

            Vector3 origin = _canAnimateVisual
                ? _visual.position
                : transform.position + Vector3.up * 0.5f;

            Color gold = new Color(1f, 0.78f, 0.25f);
            Color coral = new Color(1f, 0.45f, 0.35f);

            GameObject[] pieces = new GameObject[count];
            Vector3[] velocities = new Vector3[count];
            float[] sizes = new float[count];

            for (int i = 0; i < count; i++)
            {
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                piece.name = "Debris";
                piece.transform.position = origin + Random.insideUnitSphere * 0.2f;

                float size = Random.Range(0.08f, 0.18f);
                sizes[i] = size;
                piece.transform.localScale = Vector3.one * size;

                // Debris is purely cosmetic — strip the collider so it cannot touch gameplay.
                Collider pieceCollider = piece.GetComponent<Collider>();
                if (pieceCollider != null)
                {
                    pieceCollider.enabled = false;
                    Destroy(pieceCollider);
                }

                Renderer pieceRenderer = piece.GetComponent<Renderer>();
                if (pieceRenderer != null)
                {
                    Material material = pieceRenderer.material; // per-instance copy
                    if (material != null)
                    {
                        Color tint = Color.Lerp(gold, coral, Random.value);
                        if (material.HasProperty(BaseColorId))
                        {
                            material.SetColor(BaseColorId, tint);
                        }
                        else if (material.HasProperty(ColorId))
                        {
                            material.SetColor(ColorId, tint);
                        }
                    }
                }

                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) + 0.5f; // bias upward
                velocities[i] = direction.normalized * Random.Range(2.5f, 5f);

                pieces[i] = piece;
                _debris.Add(piece);
            }

            float elapsed = 0f;
            while (elapsed < life)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float k = Mathf.Clamp01(elapsed / life);

                for (int i = 0; i < count; i++)
                {
                    GameObject piece = pieces[i];
                    if (piece == null)
                    {
                        continue;
                    }

                    velocities[i] += Physics.gravity * dt;
                    piece.transform.position += velocities[i] * dt;
                    piece.transform.localScale = Vector3.one * (sizes[i] * (1f - k));
                    piece.transform.Rotate(180f * dt, 240f * dt, 120f * dt, Space.Self);
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (pieces[i] != null)
                {
                    _debris.Remove(pieces[i]);
                    DestroyDebris(pieces[i]);
                }
            }
        }

        /// <summary>Destroys a debris object and the material instance created for it.</summary>
        private static void DestroyDebris(GameObject piece)
        {
            if (piece == null)
            {
                return;
            }

            Renderer pieceRenderer = piece.GetComponent<Renderer>();
            if (pieceRenderer != null)
            {
                Material material = pieceRenderer.material;
                if (material != null)
                {
                    Destroy(material);
                }
            }

            Destroy(piece);
        }
    }
}
