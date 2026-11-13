using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Counts down a round timer using scaled time so Time.timeScale = 0 pauses it.
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        [SerializeField] private float defaultDuration = 10f;
        [SerializeField] private UIManager uiManager;

        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool Running { get; private set; }

        public event System.Action OnTimeout;
        public event System.Action<float> OnTick;

        private void Awake()
        {
            Duration = defaultDuration;
            Remaining = defaultDuration;
        }

        /// <summary>
        /// Assigns the UI reference (used by the SceneBuilder). The field can
        /// also be assigned in the inspector.
        /// </summary>
        public void Wire(UIManager ui)
        {
            if (ui != null) uiManager = ui;
        }

        /// <summary>
        /// Starts a countdown. Non-positive durations fall back to defaultDuration.
        /// </summary>
        public void StartTimer(float duration)
        {
            Duration = duration > 0f ? duration : defaultDuration;
            Remaining = Duration;
            Running = true;

            UpdateUI();
        }

        public void StartTimer()
        {
            StartTimer(defaultDuration);
        }

        public void StopTimer()
        {
            Running = false;
        }

        public void ResetTimer()
        {
            Remaining = Duration;
            Running = false;
        }

        private void Update()
        {
            if (!Running)
                return;

            Remaining -= Time.deltaTime;

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                Running = false;

                OnTick?.Invoke(Remaining);
                UpdateUI();
                OnTimeout?.Invoke();
                return;
            }

            OnTick?.Invoke(Remaining);
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (uiManager != null)
                uiManager.SetTimer(Remaining, Duration);
        }
    }
}
