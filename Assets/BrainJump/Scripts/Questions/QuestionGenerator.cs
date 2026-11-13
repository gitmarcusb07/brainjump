using System.Collections.Generic;
using UnityEngine;

namespace BrainJump
{
    public enum QuestionCategory
    {
        Addition,
        Subtraction,
        Multiplication,
        Division
    }

    [System.Serializable]
    public class QuestionData
    {
        public string questionText;
        public int correctAnswer;
        public int[] options;
        public int correctIndex;
        public QuestionCategory category;

        /// <summary>
        /// True when the question asks for a missing operand (for example "12 - ? = 7")
        /// rather than the result. Normal questions leave this false.
        /// </summary>
        public bool isMissingNumber;
    }

    public static class QuestionGenerator
    {
        private const int MinDifficulty = 1;
        private const int MaxDifficulty = 5;
        private const int AttemptsToAvoidRepeat = 10;

        /// <summary>
        /// Generates a question for the supplied difficulty. If the resulting question text
        /// matches the previous question, another question is generated for up to 10 attempts.
        /// </summary>
        public static QuestionData Generate(int difficulty, string previousQuestionText)
        {
            difficulty = Mathf.Clamp(difficulty, MinDifficulty, MaxDifficulty);

            QuestionData generated = null;

            for (int attempt = 0; attempt < AttemptsToAvoidRepeat; attempt++)
            {
                generated = GenerateOne(difficulty);

                if (string.IsNullOrEmpty(previousQuestionText) ||
                    generated.questionText != previousQuestionText)
                {
                    return generated;
                }
            }

            // After the retry limit, accept the final generated question.
            return generated;
        }

        private static QuestionData GenerateOne(int difficulty)
        {
            QuestionCategory category = (QuestionCategory)Random.Range(0, 4);

            switch (category)
            {
                case QuestionCategory.Addition:
                    return CreateAdditionQuestion(difficulty);

                case QuestionCategory.Subtraction:
                    return CreateSubtractionQuestion(difficulty);

                case QuestionCategory.Multiplication:
                    return CreateMultiplicationQuestion(difficulty);

                case QuestionCategory.Division:
                    return CreateDivisionQuestion(difficulty);

                default:
                    return CreateAdditionQuestion(difficulty);
            }
        }

        private static QuestionData CreateAdditionQuestion(int difficulty)
        {
            int maxOperand = 6 + difficulty * 3;
            int a = Random.Range(2, maxOperand + 1);
            int b = Random.Range(2, maxOperand + 1);

            return CreateQuestion(
                $"{a} + {b} = ?",
                a + b,
                QuestionCategory.Addition);
        }

        private static QuestionData CreateSubtractionQuestion(int difficulty)
        {
            int maxA = 10 + difficulty * 4;
            int a = Random.Range(5, maxA + 1);

            // Upper bound is exclusive, so b is selected from [2, a - 1].
            int b = Random.Range(2, a);

            return CreateQuestion(
                $"{a} - {b} = ?",
                a - b,
                QuestionCategory.Subtraction);
        }

        private static QuestionData CreateMultiplicationQuestion(int difficulty)
        {
            int maxA = 4 + difficulty;
            int a = Random.Range(2, maxA + 1);
            int b = Random.Range(2, 10);

            return CreateQuestion(
                $"{a} × {b} = ?",
                a * b,
                QuestionCategory.Multiplication);
        }

        private static QuestionData CreateDivisionQuestion(int difficulty)
        {
            int divisor = Random.Range(2, 10);
            int quotient = Random.Range(2, 5 + difficulty + 1);
            int dividend = divisor * quotient;

            // dividend ÷ divisor = quotient, so the displayed question's
            // correct answer is the quotient.
            return CreateQuestion(
                $"{dividend} ÷ {divisor} = ?",
                quotient,
                QuestionCategory.Division);
        }

        private static QuestionData CreateQuestion(
            string questionText,
            int correctAnswer,
            QuestionCategory category)
        {
            List<int> wrongAnswers = CreateWrongAnswers(correctAnswer);

            List<int> options = new List<int>(3)
            {
                correctAnswer,
                wrongAnswers[0],
                wrongAnswers[1]
            };

            Shuffle(options);

            return new QuestionData
            {
                questionText = questionText,
                correctAnswer = correctAnswer,
                options = options.ToArray(),
                correctIndex = options.IndexOf(correctAnswer),
                category = category
            };
        }

        private static List<int> CreateWrongAnswers(int correctAnswer)
        {
            HashSet<int> usedValues = new HashSet<int> { correctAnswer };
            List<int> wrongAnswers = new List<int>(2);
            List<int> candidates = new List<int>(16);

            // Plausible near-miss answers.
            candidates.Add(correctAnswer - 1);
            candidates.Add(correctAnswer - 2);
            candidates.Add(correctAnswer + 1);
            candidates.Add(correctAnswer + 2);
            candidates.Add(correctAnswer - 10);
            candidates.Add(correctAnswer + 10);
            candidates.Add(correctAnswer + Mathf.RoundToInt(correctAnswer / 2f));

            if (correctAnswer * 2 <= 200)
            {
                candidates.Add(correctAnswer * 2);
            }

            // Add several random values in a plausible range around the correct answer.
            int randomMin = Mathf.Max(0, correctAnswer - 5);
            int randomMax = correctAnswer + 10;

            for (int i = 0; i < 12; i++)
            {
                candidates.Add(Random.Range(randomMin, randomMax + 1));
            }

            Shuffle(candidates);

            for (int i = 0; i < candidates.Count && wrongAnswers.Count < 2; i++)
            {
                int candidate = candidates[i];

                if (candidate >= 0 && usedValues.Add(candidate))
                {
                    wrongAnswers.Add(candidate);
                }
            }

            // Guarantee two valid wrong answers even if the candidate pool was unsuitable.
            int offset = 1;

            while (wrongAnswers.Count < 2)
            {
                int lowerCandidate = correctAnswer - offset;

                if (lowerCandidate >= 0 && usedValues.Add(lowerCandidate))
                {
                    wrongAnswers.Add(lowerCandidate);
                }

                if (wrongAnswers.Count < 2)
                {
                    int upperCandidate = correctAnswer + offset;

                    if (usedValues.Add(upperCandidate))
                    {
                        wrongAnswers.Add(upperCandidate);
                    }
                }

                offset++;
            }

            return wrongAnswers;
        }

        private static void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                T temp = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = temp;
            }
        }

        private static QuestionData CreateMissingNumberQuestion(
            string questionText,
            int correctAnswer,
            QuestionCategory category)
        {
            QuestionData q = CreateQuestion(questionText, correctAnswer, category);
            q.isMissingNumber = true;
            return q;
        }

        /// <summary>
        /// Generates a question appropriate for the given world (0..2) and level (0..9).
        /// World 0 is addition, world 1 is subtraction and world 2 is multiplication/division;
        /// worlds 1 and 2 introduce missing-number questions from level 4 onwards.
        /// If the resulting question text matches the previous question, another question is
        /// generated for up to 10 attempts, after which the last generated question is returned.
        /// </summary>
        /// <param name="world">World index, clamped to 0..2.</param>
        /// <param name="level">Level index within the world, clamped to 0..9.</param>
        /// <param name="previousQuestionText">Text of the previously shown question, or null/empty.</param>
        /// <returns>The generated question.</returns>
        public static QuestionData GenerateForLevel(int world, int level, string previousQuestionText)
        {
            world = Mathf.Clamp(world, 0, 2);
            level = Mathf.Clamp(level, 0, 9);

            QuestionData generated = null;

            for (int attempt = 0; attempt < AttemptsToAvoidRepeat; attempt++)
            {
                generated = GenerateOneForLevel(world, level);

                if (string.IsNullOrEmpty(previousQuestionText) ||
                    generated.questionText != previousQuestionText)
                {
                    return generated;
                }
            }

            // After the retry limit, accept the final generated question.
            return generated;
        }

        private static QuestionData GenerateOneForLevel(int world, int level)
        {
            switch (world)
            {
                case 1:
                {
                    // Subtraction Hills.
                    if (level >= 4 && Random.value < 0.5f)
                    {
                        int ma = Random.Range(8, 12 + level * 3 + 1);
                        int mb = Random.Range(2, ma);
                        int result = ma - mb;

                        return CreateMissingNumberQuestion(
                            $"{ma} - ? = {result}",
                            mb,
                            QuestionCategory.Subtraction);
                    }

                    int a = Random.Range(5, 10 + level * 3 + 1);

                    // Upper bound is exclusive, so b is selected from [2, a - 1].
                    int b = Random.Range(2, a);

                    return CreateQuestion(
                        $"{a} - {b} = ?",
                        a - b,
                        QuestionCategory.Subtraction);
                }

                case 2:
                {
                    // Multiplication Peaks.
                    if (level >= 4 && Random.value < 0.5f)
                    {
                        if (Random.value < 0.5f)
                        {
                            int ma = Random.Range(2, 10);
                            int mb = Random.Range(2, 10);

                            return CreateMissingNumberQuestion(
                                $"{ma} × ? = {ma * mb}",
                                mb,
                                QuestionCategory.Multiplication);
                        }

                        int mDivisor = Random.Range(2, 10);
                        int mQuotient = Random.Range(2, 3 + level + 1);
                        int mDividend = mDivisor * mQuotient;

                        return CreateMissingNumberQuestion(
                            $"{mDividend} ÷ ? = {mQuotient}",
                            mDivisor,
                            QuestionCategory.Division);
                    }

                    if (Random.value < 0.5f)
                    {
                        int a = Random.Range(2, 3 + level / 2 + 1);
                        int b = Random.Range(2, 10);

                        return CreateQuestion(
                            $"{a} × {b} = ?",
                            a * b,
                            QuestionCategory.Multiplication);
                    }

                    int divisor = Random.Range(2, 10);
                    int quotient = Random.Range(2, 3 + level + 1);
                    int dividend = divisor * quotient;

                    return CreateQuestion(
                        $"{dividend} ÷ {divisor} = ?",
                        quotient,
                        QuestionCategory.Division);
                }

                default:
                {
                    // Addition Meadows (world 0).
                    int a = Random.Range(2, 5 + level * 2 + 1);
                    int b = Random.Range(2, 5 + level * 2 + 1);

                    return CreateQuestion(
                        $"{a} + {b} = ?",
                        a + b,
                        QuestionCategory.Addition);
                }
            }
        }
    }
}
