using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Jump trail built from a small pool of shrinking gold sphere "puffs".
    /// Puffs are spawned at a fixed interval while the player is airborne and moving
    /// vertically, then shrink to nothing over their lifetime. No TrailRenderer required.
    /// </summary>
    public class PlayerTrail : MonoBehaviour
    {
        [Header("Trail")]
        [SerializeField] private int poolSize = 12;
        [SerializeField] private float spawnInterval = 0.06f;
        [SerializeField] private float puffLifetime = 0.4f;
        [SerializeField] private float puffSize = 0.14f;
        [SerializeField] private Color puffColor = new Color(0.949f, 0.7216f, 0.3569f, 1f); // warm gold #F2B85B

        /// <summary>One pooled puff: its GameObject and remaining lifetime (seconds).</summary>
        private struct Puff
        {
            public GameObject go;
            public float life;
        }

        private Puff[] _pool;
        private int _nextPuff;
        private float _spawnTimer;
        private PlayerController _player;
        private Material _puffMaterial;

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            if (_player == null)
            {
                Debug.LogWarning("[PlayerTrail] No PlayerController found on this GameObject; trail disabled.");
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                _puffMaterial = new Material(shader);
                _puffMaterial.SetColor("_BaseColor", puffColor);
            }

            _pool = new Puff[Mathf.Max(1, poolSize)];
            for (int i = 0; i < _pool.Length; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (go == null)
                {
                    _pool[i] = new Puff { go = null, life = 0f };
                    continue;
                }

                go.name = "TrailPuff";
                go.transform.SetParent(transform, false);

                // Purely cosmetic — must never touch gameplay physics.
                Collider c = go.GetComponent<Collider>();
                if (c != null)
                {
                    Destroy(c);
                }

                Renderer r = go.GetComponent<Renderer>();
                if (r != null && _puffMaterial != null)
                {
                    r.sharedMaterial = _puffMaterial;
                }

                go.SetActive(false);
                _pool[i] = new Puff { go = go, life = 0f };
            }

            _nextPuff = 0;
            _spawnTimer = 0f;
        }

        private void Update()
        {
            if (_player == null || _pool == null || _pool.Length == 0)
            {
                return;
            }

            bool airborne = !_player.IsGrounded;
            float vy = _player.Velocity.y;

            _spawnTimer -= Time.deltaTime;
            if (airborne && Mathf.Abs(vy) > 0.5f && _spawnTimer <= 0f)
            {
                _spawnTimer = spawnInterval;
                SpawnPuff();
            }

            float lifetime = Mathf.Max(0.0001f, puffLifetime);

            for (int i = 0; i < _pool.Length; i++)
            {
                Puff p = _pool[i];
                if (p.go == null || p.life <= 0f)
                {
                    continue;
                }

                p.life -= Time.deltaTime;

                if (p.life <= 0f)
                {
                    p.life = 0f;
                    p.go.SetActive(false);
                }
                else
                {
                    float k = Mathf.Clamp01(p.life / lifetime);
                    p.go.transform.localScale = Vector3.one * (puffSize * k);
                }

                _pool[i] = p; // struct copy: write back
            }
        }

        private void OnDestroy()
        {
            if (_puffMaterial != null)
            {
                Destroy(_puffMaterial);
                _puffMaterial = null;
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Activates the next pooled puff near the player's torso (round-robin, recycles the oldest).</summary>
        private void SpawnPuff()
        {
            if (_pool == null || _pool.Length == 0)
            {
                return;
            }

            int slot = _nextPuff;
            if (slot < 0 || slot >= _pool.Length)
            {
                slot = 0;
            }
            _nextPuff = (slot + 1) % _pool.Length;

            Puff p = _pool[slot];
            if (p.go == null)
            {
                return;
            }

            p.go.transform.position = transform.position
                + Vector3.up * 0.9f
                + new Vector3(
                    Random.Range(-0.15f, 0.15f),
                    Random.Range(-0.1f, 0.1f),
                    Random.Range(-0.15f, 0.15f));
            p.go.transform.localScale = Vector3.one * puffSize;
            p.life = puffLifetime;
            p.go.SetActive(true);

            _pool[slot] = p; // struct copy: write back into the slot used
        }
    }
}
