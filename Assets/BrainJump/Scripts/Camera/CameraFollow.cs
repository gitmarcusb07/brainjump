using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Follows the player from a fixed elevated offset and looks slightly ahead of them.
    /// Designed for portrait play so the answer platforms ahead remain visible.
    /// Does not orbit or allow runtime offset changes.
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

        /// <summary>
        /// The transform to follow. Backed by the serialized field so SceneBuilder can assign it.
        /// </summary>
        public Transform Target
        {
            get { return target; }
            set { target = value; }
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
        }

        private Vector3 GetLookTarget()
        {
            return target.position + Vector3.up * lookAtHeight + target.forward * lookAhead;
        }
    }
}
