using System;
using UnityEngine;

namespace BrainJump
{
    /// <summary>
    /// Generates questions, distributes their options across the three answer
    /// platforms, and tracks the previous question to avoid repeats.
    /// </summary>
    public class QuestionManager : MonoBehaviour
    {
        private const int OptionCount = 3;

        [Header("Scene References")]
        [SerializeField] private AnswerPlatform[] answerPlatforms;
        [SerializeField] private GameProgression gameProgression;
        [SerializeField] private UIManager uiManager;

        private string _previousQuestionText = string.Empty;

        public AnswerPlatform[] AnswerPlatforms => answerPlatforms;

        /// <summary>
        /// Assigns scene references (used by the SceneBuilder). Inspector
        /// assignment works too — this only fills what is still null.
        /// </summary>
        public void Wire(AnswerPlatform[] platforms, GameProgression progression, UIManager ui)
        {
            if (platforms != null) answerPlatforms = platforms;
            if (progression != null) gameProgression = progression;
            if (ui != null) uiManager = ui;
        }

        public QuestionData CurrentQuestion { get; private set; }

        public event System.Action<QuestionData> OnQuestionChanged;

        /// <summary>
        /// Difficulty is sourced from GameProgression when available and
        /// falls back to the opening difficulty otherwise.
        /// </summary>
        public int Difficulty
        {
            get
            {
                if (gameProgression == null)
                {
                    gameProgression = GetComponent<GameProgression>();
                }

                return gameProgression != null
                    ? Mathf.Clamp(gameProgression.Difficulty, 1, 5)
                    : 1;
            }
        }

        private void Awake()
        {
            // Serialized references are preferred. These fallbacks support the
            // common setup where progression and UI live on the same object.
            if (gameProgression == null)
            {
                gameProgression = GetComponent<GameProgression>();
            }

            if (uiManager == null)
            {
                uiManager = GetComponent<UIManager>();
            }
        }

        /// <summary>
        /// Generates and applies a new question only while gameplay is active.
        /// Safe to call from other game states.
        /// </summary>
        public void NewQuestion()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null || manager.State != GameManager.GameState.Playing)
            {
                return;
            }

            QuestionData question = QuestionGenerator.Generate(Difficulty, _previousQuestionText);
            if (question == null || question.options == null)
            {
                return;
            }

            CurrentQuestion = question;
            _previousQuestionText = question.questionText;

            ApplyOptionsToPlatforms(question);

            if (uiManager != null)
            {
                uiManager.SetQuestion(question.questionText);
            }

            OnQuestionChanged?.Invoke(question);
        }

        /// <summary>
        /// Resets every platform to a neutral state, e.g. when returning to menu.
        /// </summary>
        public void ClearPlatforms()
        {
            if (answerPlatforms == null)
            {
                return;
            }

            for (int i = 0; i < answerPlatforms.Length; i++)
            {
                if (answerPlatforms[i] != null)
                {
                    answerPlatforms[i].SetAnswer(0, false);
                }
            }
        }

        private void ApplyOptionsToPlatforms(QuestionData question)
        {
            if (answerPlatforms == null)
            {
                return;
            }

            for (int i = 0; i < OptionCount; i++)
            {
                if (i >= answerPlatforms.Length || answerPlatforms[i] == null)
                {
                    continue;
                }

                int optionValue = i < question.options.Length ? question.options[i] : 0;
                bool isCorrect = i == question.correctIndex;
                answerPlatforms[i].SetAnswer(optionValue, isCorrect);
            }
        }
    }
}
