using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Linq;
using PromptMyCircumstance.Models;
using PromptMyCircumstance.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PromptMyCircumstance.Pages
{
    public class AiRequestPayload
    {
        [JsonPropertyName("userPrompt")]
        public string UserPrompt { get; set; } = string.Empty;

        [JsonPropertyName("rawTelemetry")]
        public string RawTelemetry { get; set; } = string.Empty;

        [JsonPropertyName("goldStandard")]
        public string GoldStandard { get; set; } = string.Empty;
    }

    public class AiResponsePayload
    {
        [JsonPropertyName("execution_result")]
        public string ExecutionResult { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }

    public class BatchEvaluationItem
    {
        [JsonPropertyName("challenge_id")]
        public string ChallengeId { get; set; } = string.Empty;

        [JsonPropertyName("failure_analysis")]
        public string FailureAnalysis { get; set; } = string.Empty;

        [JsonPropertyName("actionability_score")]
        public double ActionabilityScore { get; set; }

        [JsonPropertyName("target_alignment_score")]
        public double TargetAlignmentScore { get; set; }
    }

    public class BatchEvaluationResponse
    {
        [JsonPropertyName("results")]
        public List<BatchEvaluationItem> Results { get; set; } = new();
    }

    public partial class Index : ComponentBase
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        public enum GameStage
        {
            Setup,
            Running,
            Prompting,
            Evaluating,
            Finished
        }

        private List<ChallengePayload> Challenges = new();
        private GameStage Stage = GameStage.Setup;
        private int SelectedRunLength = 5;
        private double Distance = 0;
        private double TotalDistance = 1000;
        private bool IsJumping = false;
        private bool IsDashing = false;
        private List<ChallengePayload> ActiveChallenges = new();
        private int ActiveObstacleIndex = 0;
        private string CurrentDirective = "";

        private EvaluationResult? FinalResult;
        private List<string> ActivePhaseLogs = new();
        private bool IsRunning = false;

        private ChallengePayload? ActiveObstacle => (ActiveChallenges != null && ActiveObstacleIndex >= 0 && ActiveObstacleIndex < ActiveChallenges.Count) ? ActiveChallenges[ActiveObstacleIndex] : null;

        protected override void OnInitialized()
        {
            Challenges = Library.GenerateChallenges();
        }

        private void StartRun(int length)
        {
            SelectedRunLength = length;
            var rnd = new Random();
            ActiveChallenges = Challenges.OrderBy(x => rnd.Next()).Take(length).ToList();

            foreach (var c in ActiveChallenges)
            {
                c.EvaluationSchema.EvaluatorInputs.RawPromptText = string.Empty;
                c.EvaluationSchema.EvaluatorInputs.CapturedOutputString = string.Empty;
                c.EvaluationSchema.EvaluatorInputs.AiActionabilityScore = 0;
                c.EvaluationSchema.EvaluatorInputs.AiTargetAlignmentScore = 0;
                c.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = string.Empty;
            }

            Distance = 0;
            ActiveObstacleIndex = 0;
            CurrentDirective = "";
            Stage = GameStage.Running;
            ActivePhaseLogs.Clear();
            FinalResult = null;

            _ = RunGameLoop();
        }

        private async Task RunGameLoop()
        {
            while (Stage == GameStage.Running)
            {
                Distance += 10;

                double targetDistance = TotalDistance;
                if (ActiveObstacleIndex < ActiveChallenges.Count)
                {
                    targetDistance = (ActiveObstacleIndex + 1) * (TotalDistance / (ActiveChallenges.Count + 1));
                }

                if (Distance >= targetDistance)
                {
                    Distance = targetDistance;
                    if (ActiveObstacleIndex < ActiveChallenges.Count)
                    {
                        PauseForObstacle();
                    }
                    else
                    {
                        await FinishRun();
                    }
                    break;
                }

                StateHasChanged();
                await Task.Delay(40);
            }
        }

        private void PauseForObstacle()
        {
            CurrentDirective = "";
            Stage = GameStage.Prompting;
            StateHasChanged();
        }

        private async Task SubmitDirective()
        {
            if (string.IsNullOrWhiteSpace(CurrentDirective)) return;

            if (ActiveObstacle != null)
            {
                ActiveObstacle.EvaluationSchema.EvaluatorInputs.RawPromptText = CurrentDirective;
            }

            var rnd = new Random();
            if (rnd.Next(2) == 0)
            {
                IsJumping = true;
            }
            else
            {
                IsDashing = true;
            }

            StateHasChanged();
            await Task.Delay(800);

            IsJumping = false;
            IsDashing = false;
            ActiveObstacleIndex++;

            Stage = GameStage.Running;
            _ = RunGameLoop();
        }

        private async Task GenerateExecutionResultsInParallel()
        {
            ActivePhaseLogs.Add("[PROCESS] Running AI execution sandbox for all directives in parallel...");
            StateHasChanged();

            var tasks = ActiveChallenges.Select(async c => {
                try
                {
                    var req = new AiRequestPayload
                    {
                        UserPrompt = c.EvaluationSchema.EvaluatorInputs.RawPromptText,
                        RawTelemetry = c.RawTelemetryDump,
                        GoldStandard = c.ReferenceGoldStandardAnswer
                    };
                    var res = await Http.PostAsJsonAsync("/api/generate", req);
                    var aiData = await res.Content.ReadFromJsonAsync<AiResponsePayload>();
                    c.EvaluationSchema.EvaluatorInputs.CapturedOutputString = aiData?.ExecutionResult ?? "";
                }
                catch (Exception ex)
                {
                    c.EvaluationSchema.EvaluatorInputs.CapturedOutputString = $"Execution Error: {ex.Message}";
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }

        private async Task FinishRun()
        {
            Stage = GameStage.Evaluating;
            IsRunning = true;
            ActivePhaseLogs.Clear();
            StateHasChanged();

            try
            {
                await GenerateExecutionResultsInParallel();

                ActivePhaseLogs.Add("[BATCH] Packaging all directives for evaluation...");
                StateHasChanged();
                await Task.Delay(200);

                var batchItems = new List<object>();
                foreach (var c in ActiveChallenges)
                {
                    batchItems.Add(new
                    {
                        id = c.Id,
                        title = c.Title,
                        rawTelemetry = c.RawTelemetryDump,
                        userPrompt = c.EvaluationSchema.EvaluatorInputs.RawPromptText,
                        goldStandard = c.ReferenceGoldStandardAnswer,
                        executionResult = c.EvaluationSchema.EvaluatorInputs.CapturedOutputString
                    });
                }

                ActivePhaseLogs.Add("[BATCH] Dispatching matrix payload to AI Evaluator...");
                StateHasChanged();

                var response = await Http.PostAsJsonAsync("/api/evaluate_batch", new { items = batchItems });
                var batchRes = await response.Content.ReadFromJsonAsync<BatchEvaluationResponse>();

                if (batchRes == null || batchRes.Results == null || batchRes.Results.Count == 0)
                {
                    throw new Exception("Invalid batch evaluation response from server.");
                }

                ActivePhaseLogs.Add("[BATCH] Processing evaluation scores...");
                StateHasChanged();

                foreach (var score in batchRes.Results)
                {
                    var challenge = ActiveChallenges.FirstOrDefault(c => c.Id == score.ChallengeId);
                    if (challenge != null)
                    {
                        challenge.EvaluationSchema.EvaluatorInputs.AiActionabilityScore = score.ActionabilityScore;
                        challenge.EvaluationSchema.EvaluatorInputs.AiTargetAlignmentScore = score.TargetAlignmentScore;
                        challenge.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = score.FailureAnalysis;
                    }
                }

                double totalScoreSum = 0;
                foreach (var challenge in ActiveChallenges)
                {
                    var eval = Engine.Evaluate(challenge.EvaluationSchema);
                    totalScoreSum += eval.TotalScore;
                }

                double averageScore = totalScoreSum / ActiveChallenges.Count;

                FinalResult = new EvaluationResult
                {
                    TotalScore = averageScore
                };

                if (averageScore >= 90.0)
                {
                    FinalResult.OperatorTier = "S-Tier: Master Operator";
                    FinalResult.TierFeedback = "Actionable, direct instruction. Zero translation needed, absolute constraint adherence.";
                }
                else if (averageScore >= 70.0)
                {
                    FinalResult.OperatorTier = "A-Tier: Capable Integrator";
                    FinalResult.TierFeedback = "Correct resolution, but prompt required agent clarification or contained mild semantic drift.";
                }
                else
                {
                    FinalResult.OperatorTier = "B-Tier: Casual Informant";
                    FinalResult.TierFeedback = "High risk of hallucination or non-actionable output. Refine parameters and specify clear outcome bounds.";
                }

                ActivePhaseLogs.Add("[BATCH] Evaluation Complete!");
                Stage = GameStage.Finished;
            }
            catch (Exception ex)
            {
                ActivePhaseLogs.Add($"[BATCH FATAL] {ex.Message}");
                Stage = GameStage.Setup;
            }
            finally
            {
                IsRunning = false;
                StateHasChanged();
            }
        }

        private string GetTierClass(double score)
        {
            if (score >= 90) return "neon-matrix-green";
            if (score >= 70) return "neon-text-blue";
            return "neon-text-red";
        }

        private async Task DownloadCertificate()
        {
            if (FinalResult == null) return;
            await JSRuntime.InvokeVoidAsync("zlaInterop.downloadCertificate", FinalResult.OperatorTier, FinalResult.TotalScore.ToString("F1"), "Prompt My Circumstance Matrix", 5);
        }

        private void ResetToSetup()
        {
            Stage = GameStage.Setup;
            Distance = 0;
            ActiveObstacleIndex = 0;
            ActiveChallenges.Clear();
            FinalResult = null;
        }
    }
}
