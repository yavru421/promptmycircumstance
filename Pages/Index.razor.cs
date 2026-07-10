using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PromptMyCircumstance.Models;
using PromptMyCircumstance.Services;
using Microsoft.AspNetCore.Components;

namespace PromptMyCircumstance.Pages
{
    public partial class Index : ComponentBase
    {
        private List<ChallengePayload> Challenges = new();
        private int CurrentIndex = 0;
        private string UserPrompt = string.Empty;
        private string SimulatedOutput = string.Empty;

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
            SimulatedOutput = string.Empty;
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
            
            var current = Challenges[CurrentIndex];
            
            var payload = current.EvaluationSchema;
            payload.EvaluatorInputs.RawPromptText = UserPrompt;
            payload.EvaluatorInputs.CapturedOutputString = SimulatedOutput;
            // The reference is already set in the seeded schema

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
