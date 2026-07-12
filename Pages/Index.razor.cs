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

        private List<ChallengePayload> Challenges = new();
        private int CurrentIndex = 0;
        private string UserPrompt = string.Empty;
        private string ActualAiOutput = string.Empty;

        private EvaluationResult? Result;
        private EvaluationResult? FinalResult;
        private int AnimationPercentage = 0;
        private List<string> ActivePhaseLogs = new();
        private bool IsRunning = false;
        private bool IsTestGraded = false;

        protected override void OnInitialized()
        {
            Challenges = Library.GenerateChallenges();
            ResetState();
        }

        private void SaveCurrentState()
        {
            if (Challenges != null && CurrentIndex >= 0 && CurrentIndex < Challenges.Count)
            {
                var current = Challenges[CurrentIndex];
                current.EvaluationSchema.EvaluatorInputs.RawPromptText = UserPrompt;
                current.EvaluationSchema.EvaluatorInputs.CapturedOutputString = ActualAiOutput;
            }
        }

        private void NextChallenge()
        {
            if (CurrentIndex < Challenges.Count - 1)
            {
                SaveCurrentState();
                CurrentIndex++;
                ResetState();
            }
        }

        private void PrevChallenge()
        {
            if (CurrentIndex > 0)
            {
                SaveCurrentState();
                CurrentIndex--;
                ResetState();
            }
        }

        private void ResetState()
        {
            if (Challenges != null && Challenges.Count > CurrentIndex)
            {
                var current = Challenges[CurrentIndex];
                UserPrompt = current.EvaluationSchema.EvaluatorInputs.RawPromptText;
                ActualAiOutput = current.EvaluationSchema.EvaluatorInputs.CapturedOutputString;
                
                if (IsTestGraded)
                {
                    Result = Engine.Evaluate(current.EvaluationSchema);
                    AnimationPercentage = 100;
                }
                else
                {
                    Result = null;
                    AnimationPercentage = string.IsNullOrEmpty(ActualAiOutput) ? 0 : 100;
                }
            }
            
            ActivePhaseLogs.Clear();
            IsRunning = false;
        }

        private async Task RunLocalEvaluationLoop()
        {
            if (IsRunning) return;
            IsRunning = true;
            ActivePhaseLogs.Clear();
            AnimationPercentage = 0;
            ActualAiOutput = string.Empty;
            Result = null;
            
            var current = Challenges[CurrentIndex];
            
            ActivePhaseLogs.Add("[API] Dispatching instruction payload to Cloudflare AI Worker...");
            StateHasChanged();

            try
            {
                var req = new AiRequestPayload 
                { 
                    UserPrompt = UserPrompt,
                    RawTelemetry = current.RawTelemetryDump,
                    GoldStandard = current.ReferenceGoldStandardAnswer
                };

                var res = await Http.PostAsJsonAsync("/api/generate", req);
                var aiData = await res.Content.ReadFromJsonAsync<AiResponsePayload>();

                if (!string.IsNullOrEmpty(aiData?.Error))
                {
                    ActivePhaseLogs.Add($"[API ERROR] {aiData.Error}");
                    IsRunning = false;
                    return;
                }

                ActualAiOutput = aiData?.ExecutionResult ?? "";
                
                // Save execution results locally
                current.EvaluationSchema.EvaluatorInputs.RawPromptText = UserPrompt;
                current.EvaluationSchema.EvaluatorInputs.CapturedOutputString = ActualAiOutput;

                ActivePhaseLogs.Add("[API] Success. Decoupled execution complete.");
                ActivePhaseLogs.Add("[INFO] Instruction Staged. Output recorded.");
                StateHasChanged();
                
                // Animate progress up to 100% since execution is done
                for (int p = 10; p <= 100; p += 30)
                {
                    AnimationPercentage = p;
                    StateHasChanged();
                    await Task.Delay(100);
                }
                AnimationPercentage = 100;
            }
            catch (Exception ex)
            {
                ActivePhaseLogs.Add($"[API FATAL] {ex.Message}");
                IsRunning = false;
                return;
            }

            IsRunning = false;
        }

        private async Task SubmitBatchEvaluation()
        {
            if (IsRunning) return;
            IsRunning = true;
            ActivePhaseLogs.Clear();
            ActivePhaseLogs.Add("[BATCH] Packaging all staged instructions for evaluation...");
            StateHasChanged();
            
            SaveCurrentState();

            try
            {
                var batchItems = new List<object>();
                foreach (var c in Challenges)
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
                await Task.Delay(200);

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
                    var challenge = Challenges.FirstOrDefault(c => c.Id == score.ChallengeId);
                    if (challenge != null)
                    {
                        challenge.EvaluationSchema.EvaluatorInputs.AiActionabilityScore = score.ActionabilityScore;
                        challenge.EvaluationSchema.EvaluatorInputs.AiTargetAlignmentScore = score.TargetAlignmentScore;
                        challenge.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = score.FailureAnalysis;
                    }
                }

                // Compute Final Overall Score
                double totalScoreSum = 0;
                foreach (var challenge in Challenges)
                {
                    var eval = Engine.Evaluate(challenge.EvaluationSchema);
                    totalScoreSum += eval.TotalScore;
                }
                
                double averageScore = totalScoreSum / Challenges.Count;
                
                // Create a consolidated result
                FinalResult = new EvaluationResult
                {
                    TotalScore = averageScore
                };
                
                // Assign tier to FinalResult
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

                IsTestGraded = true;
                ActivePhaseLogs.Add("[BATCH] Evaluation Complete!");
                ResetState();
            }
            catch (Exception ex)
            {
                ActivePhaseLogs.Add($"[BATCH FATAL] {ex.Message}");
                IsRunning = false;
                return;
            }

            IsRunning = false;
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
    }
}
