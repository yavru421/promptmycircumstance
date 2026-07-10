using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PromptMyCircumstance.Models;
using PromptMyCircumstance.Services;
using Microsoft.AspNetCore.Components;

namespace PromptMyCircumstance.Pages
{
    public class AiRequestPayload
    {
        [JsonPropertyName("systemPrompt")]
        public string SystemPrompt { get; set; } = string.Empty;

        [JsonPropertyName("userPrompt")]
        public string UserPrompt { get; set; } = string.Empty;
    }

    public class AiResponsePayload
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }

    public partial class Index : ComponentBase
    {
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
            
            ActivePhaseLogs.Add("[API] Dispatching payload to Cloudflare AI Worker (Llama-3)...");
            StateHasChanged();

            try
            {
                var req = new AiRequestPayload 
                { 
                    SystemPrompt = $"You are processing raw data. Strict instructions: {UserPrompt}",
                    UserPrompt = current.RawTelemetryDump 
                };

                var res = await Http.PostAsJsonAsync("/api/generate", req);
                var aiData = await res.Content.ReadFromJsonAsync<AiResponsePayload>();

                if (!string.IsNullOrEmpty(aiData?.Error))
                {
                    ActivePhaseLogs.Add($"[API ERROR] {aiData.Error}");
                    IsRunning = false;
                    return;
                }

                ActualAiOutput = aiData?.Result ?? "";
                ActivePhaseLogs.Add("[API] Success. Captured completion from model.");
                StateHasChanged();
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                ActivePhaseLogs.Add($"[API FATAL] {ex.Message}");
                IsRunning = false;
                return;
            }
            
            var payload = current.EvaluationSchema;
            payload.EvaluatorInputs.RawPromptText = UserPrompt;
            payload.EvaluatorInputs.CapturedOutputString = ActualAiOutput;

            Result = Engine.Evaluate(payload);

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
    }
}
