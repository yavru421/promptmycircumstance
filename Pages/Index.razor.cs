using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

        [JsonPropertyName("actionability_score")]
        public double ActionabilityScore { get; set; }

        [JsonPropertyName("constraint_adherence_score")]
        public double ConstraintAdherenceScore { get; set; }

        [JsonPropertyName("target_alignment_score")]
        public double TargetAlignmentScore { get; set; }

        [JsonPropertyName("failure_analysis")]
        public string FailureAnalysis { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }

    public partial class Index : ComponentBase
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;
        private List<ChallengePayload> Challenges = new();
        private int CurrentIndex = 0;
        private string UserPrompt = string.Empty;
        private string ActualAiOutput = string.Empty;

        private EvaluationResult Result;
        private int AnimationPercentage = 0;
        private List<string> ActivePhaseLogs = new();
        private bool IsRunning = false;

        protected override void OnInitialized()
        {
            Challenges = Library.GenerateChallenges();
            ResetState();
        }

        private void NextChallenge()
        {
            if (CurrentIndex < Challenges.Count - 1)
            {
                CurrentIndex++;
                ResetState();
            }
        }

        private void PrevChallenge()
        {
            if (CurrentIndex > 0)
            {
                CurrentIndex--;
                ResetState();
            }
        }

        private void ResetState()
        {
            UserPrompt = string.Empty;
            ActualAiOutput = string.Empty;
            Result = null;
            AnimationPercentage = 0;
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
                ActivePhaseLogs.Add("[API] Success. Decoupled execution complete.");
                StateHasChanged();
                await Task.Delay(500);

                var payload = current.EvaluationSchema;
                payload.EvaluatorInputs.RawPromptText = UserPrompt;
                payload.EvaluatorInputs.CapturedOutputString = ActualAiOutput;
                payload.EvaluatorInputs.AiActionabilityScore = aiData?.ActionabilityScore ?? 0.0;
                payload.EvaluatorInputs.AiConstraintAdherenceScore = aiData?.ConstraintAdherenceScore ?? 0.0;
                payload.EvaluatorInputs.AiTargetAlignmentScore = aiData?.TargetAlignmentScore ?? 0.0;
                payload.EvaluatorInputs.AiFailureAnalysis = aiData?.FailureAnalysis ?? "";

                Result = Engine.Evaluate(payload);
            }
            catch (Exception ex)
            {
                ActivePhaseLogs.Add($"[API FATAL] {ex.Message}");
                IsRunning = false;
                return;
            }

            foreach (var step in Result.AnimationTimeline)
            {
                AnimationPercentage = step.CompletionPercentage;
                ActivePhaseLogs.Add($"[{step.Phase}] {step.LogMessage}");
                StateHasChanged();
                await Task.Delay(400); 
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
            if (Result == null) return;
            var current = Challenges[CurrentIndex];
            await JSRuntime.InvokeVoidAsync("zlaInterop.downloadCertificate", Result.OperatorTier, Result.TotalScore.ToString("F1"), current.Title, current.DifficultyStars);
        }
    }
}
