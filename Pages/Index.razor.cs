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
using Microsoft.AspNetCore.Components.Web;
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

        public enum GamePlayMode
        {
            Runner,
            Classic
        }

        private List<ChallengePayload> Challenges = new();
        private GameStage Stage = GameStage.Setup;
        private GamePlayMode SelectedPlayMode = GamePlayMode.Runner;
        
        public class GameCollectible
        {
            public double PositionMeters { get; set; }
            public double YPercent { get; set; }
            public bool Collected { get; set; }
        }

        public class GameHazard
        {
            public double PositionMeters { get; set; }
            public string HazardType { get; set; } = "Rock"; // "Rock", "Branch"
            public bool Hit { get; set; }
        }

        public class SlopeDecoration
        {
            public double PositionMeters { get; set; }
            public string Type { get; set; } = "PineTree"; // "PineTree", "FlagRed", "FlagBlue"
        }

        // Runner (Ski Slalom) Mode variables
        private int SelectedRunLength = 5;
        private double Distance = 0;
        private double TotalDistance = 10000;
        private bool IsJumping = false;
        private bool IsTucking = false;
        private bool IsTripping = false;
        private HashSet<int> TrippedObstacleIndices = new();
        private List<ChallengePayload> ActiveChallenges = new();
        private int ActiveObstacleIndex = 0;
        
        private List<GameCollectible> SlopeCollectibles = new();
        private List<GameHazard> SlopeHazards = new();
        private List<SlopeDecoration> SlopeDecorations = new();
        private int CrystalCount = 0;
        private int ScoreMultiplier = 1;
        private string CurrentDirective = "";

        // Classic Mode variables
        private int CurrentIndex = 0;
        private string UserPrompt = string.Empty;
        private string ActualAiOutput = string.Empty;
        private EvaluationResult? Result;

        private EvaluationResult? FinalResult;
        private List<string> ActivePhaseLogs = new();
        private bool IsRunning = false;

        private ChallengePayload? ActiveObstacle => (ActiveChallenges != null && ActiveObstacleIndex >= 0 && ActiveObstacleIndex < ActiveChallenges.Count) ? ActiveChallenges[ActiveObstacleIndex] : null;

        protected override void OnInitialized()
        {
            Challenges = Library.GenerateChallenges();
        }

        private void StartClassicMode()
        {
            SelectedPlayMode = GamePlayMode.Classic;
            Stage = GameStage.Running;
            CurrentIndex = 0;
            ActivePhaseLogs.Clear();
            FinalResult = null;
            Result = null;
            IsRunning = false;
            
            // Clean up challenges evaluation fields
            foreach (var c in Challenges)
            {
                c.EvaluationSchema.EvaluatorInputs.RawPromptText = string.Empty;
                c.EvaluationSchema.EvaluatorInputs.CapturedOutputString = string.Empty;
                c.EvaluationSchema.EvaluatorInputs.AiActionabilityScore = 0;
                c.EvaluationSchema.EvaluatorInputs.AiTargetAlignmentScore = 0;
                c.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = string.Empty;
            }

            ResetClassicState();
        }

        private void NextChallenge()
        {
            if (CurrentIndex < Challenges.Count - 1)
            {
                SaveCurrentState();
                CurrentIndex++;
                ResetClassicState();
            }
        }

        private void PrevChallenge()
        {
            if (CurrentIndex > 0)
            {
                SaveCurrentState();
                CurrentIndex--;
                ResetClassicState();
            }
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

        private void ResetClassicState()
        {
            if (Challenges != null && Challenges.Count > CurrentIndex)
            {
                var current = Challenges[CurrentIndex];
                UserPrompt = current.EvaluationSchema.EvaluatorInputs.RawPromptText;
                ActualAiOutput = current.EvaluationSchema.EvaluatorInputs.CapturedOutputString;
                
                if (FinalResult != null)
                {
                    Result = Engine.Evaluate(current.EvaluationSchema);
                }
                else
                {
                    Result = null;
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
            ActualAiOutput = string.Empty;
            
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
                current.EvaluationSchema.EvaluatorInputs.RawPromptText = UserPrompt;
                current.EvaluationSchema.EvaluatorInputs.CapturedOutputString = ActualAiOutput;

                ActivePhaseLogs.Add("[API] Success. Decoupled execution complete.");
                ActivePhaseLogs.Add("[INFO] Instruction Staged. Output recorded.");
            }
            catch (Exception ex)
            {
                ActivePhaseLogs.Add($"[API FATAL] {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                StateHasChanged();
            }
        }

        private async Task SubmitClassicBatchEvaluation()
        {
            if (IsRunning) return;
            IsRunning = true;
            ActivePhaseLogs.Clear();
            ActivePhaseLogs.Add("[BATCH] Packaging all staged instructions for evaluation...");
            StateHasChanged();
            
            SaveCurrentState();

            try
            {
                ActivePhaseLogs.Add("[BATCH] Executing unstaged prompts via AI sandbox...");
                StateHasChanged();
                
                var tasks = Challenges.Where(c => !string.IsNullOrEmpty(c.EvaluationSchema.EvaluatorInputs.RawPromptText) 
                                                  && string.IsNullOrEmpty(c.EvaluationSchema.EvaluatorInputs.CapturedOutputString))
                                     .Select(async c => {
                                         try {
                                             var req = new AiRequestPayload { 
                                                 UserPrompt = c.EvaluationSchema.EvaluatorInputs.RawPromptText,
                                                 RawTelemetry = c.RawTelemetryDump,
                                                 GoldStandard = c.ReferenceGoldStandardAnswer
                                             };
                                             var res = await Http.PostAsJsonAsync("/api/generate", req);
                                             var aiData = await res.Content.ReadFromJsonAsync<AiResponsePayload>();
                                             c.EvaluationSchema.EvaluatorInputs.CapturedOutputString = aiData?.ExecutionResult ?? "";
                                         } catch (Exception ex) {
                                             c.EvaluationSchema.EvaluatorInputs.CapturedOutputString = $"Error: {ex.Message}";
                                         }
                                     }).ToList();
                
                await Task.WhenAll(tasks);

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

                double totalScoreSum = 0;
                foreach (var challenge in Challenges)
                {
                    var eval = Engine.Evaluate(challenge.EvaluationSchema);
                    totalScoreSum += eval.TotalScore;
                }

                double averageScore = totalScoreSum / Challenges.Count;

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
                ResetClassicState();
            }
            catch (Exception ex)
            {
                ActivePhaseLogs.Add($"[BATCH WARNING] AI Grader unavailable: {ex.Message}. Using matrix heuristics fallback...");
                StateHasChanged();
                await Task.Delay(1500);

                foreach (var c in Challenges)
                {
                    var rawPrompt = c.EvaluationSchema.EvaluatorInputs.RawPromptText ?? "";
                    bool hasActionability = rawPrompt.Contains("```") || rawPrompt.Contains("run") || rawPrompt.Contains("calculate") || rawPrompt.Contains("write") || rawPrompt.Length > 20;
                    double actionability = hasActionability ? 0.90 : 0.60;
                    double alignment = Math.Min(1.0, 0.4 + (rawPrompt.Length / 100.0));
                    
                    c.EvaluationSchema.EvaluatorInputs.AiActionabilityScore = actionability;
                    c.EvaluationSchema.EvaluatorInputs.AiTargetAlignmentScore = alignment;
                    c.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = "Resolved via client-side matrix heuristic fallback.";
                }

                double totalScoreSum = 0;
                foreach (var challenge in Challenges)
                {
                    var eval = Engine.Evaluate(challenge.EvaluationSchema);
                    totalScoreSum += eval.TotalScore;
                }

                double averageScore = totalScoreSum / Challenges.Count;

                FinalResult = new EvaluationResult
                {
                    TotalScore = averageScore
                };

                if (averageScore >= 90.0)
                {
                    FinalResult.OperatorTier = "S-Tier: Master Operator";
                }
                else if (averageScore >= 70.0)
                {
                    FinalResult.OperatorTier = "A-Tier: Capable Integrator";
                }
                else
                {
                    FinalResult.OperatorTier = "B-Tier: Casual Informant";
                }
                FinalResult.TierFeedback = "Calculated programmatically via matrix fallback heuristics.";
                Stage = GameStage.Finished;
                ResetClassicState();
            }
            finally
            {
                IsRunning = false;
                StateHasChanged();
            }
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

            TrippedObstacleIndices.Clear();
            SlopeCollectibles.Clear();
            SlopeHazards.Clear();
            SlopeDecorations.Clear();
            CrystalCount = 0;
            ScoreMultiplier = 1;
            Distance = 0;
            ActiveObstacleIndex = 0;
            CurrentDirective = "";
            Stage = GameStage.Running;
            ActivePhaseLogs.Clear();
            FinalResult = null;

            TotalDistance = length * 2000;

            // Generate ski slope elements procedurally between obstacles
            for (int i = 0; i < length; i++)
            {
                double sectionStart = i * 2000 + 300;
                double sectionEnd = (i + 1) * 2000 - 300;

                // Spawns 3 collectibles per section
                int numCollectibles = 3;
                for (int c = 0; c < numCollectibles; c++)
                {
                    double pos = sectionStart + (c + 1) * ((sectionEnd - sectionStart) / (numCollectibles + 1));
                    SlopeCollectibles.Add(new GameCollectible 
                    { 
                        PositionMeters = pos, 
                        YPercent = 15 + rnd.Next(0, 20), 
                        Collected = false 
                    });
                }

                // Spawns 1 hazard per section
                SlopeHazards.Add(new GameHazard 
                { 
                    PositionMeters = sectionStart + 800 + rnd.Next(-100, 100), 
                    HazardType = rnd.Next(2) == 0 ? "Rock" : "Branch", 
                    Hit = false 
                });

                // Spawns 5 slalom flags and pine trees decorations per section
                int numDec = 5;
                for (int d = 0; d < numDec; d++)
                {
                    SlopeDecorations.Add(new SlopeDecoration
                    {
                        PositionMeters = sectionStart + d * 350 + rnd.Next(-50, 50),
                        Type = rnd.Next(3) == 0 ? "FlagRed" : (rnd.Next(2) == 0 ? "FlagBlue" : "PineTree")
                    });
                }
            }

            _ = RunGameLoop();
        }

        private async Task TriggerJump()
        {
            if (IsJumping || IsTucking || IsTripping) return;
            IsJumping = true;
            StateHasChanged();
            await Task.Delay(850);
            IsJumping = false;
            StateHasChanged();
        }

        private async Task TriggerSlide()
        {
            if (IsJumping || IsTucking || IsTripping) return;
            IsTucking = true;
            StateHasChanged();
            await Task.Delay(850);
            IsTucking = false;
            StateHasChanged();
        }

        private async Task RunGameLoop()
        {
            while (Stage == GameStage.Running)
            {
                Distance += 12;

                // 1. Check Collectibles collisions
                foreach (var col in SlopeCollectibles)
                {
                    if (!col.Collected && Math.Abs(Distance - col.PositionMeters) < 30)
                    {
                        col.Collected = true;
                        CrystalCount++;
                        ScoreMultiplier = 1 + (CrystalCount / 4);
                        ActivePhaseLogs.Add($"[COLLECT] Crystal acquired! Multiplier: x{ScoreMultiplier}");
                    }
                }

                // 2. Check Hazards collisions
                foreach (var haz in SlopeHazards)
                {
                    if (!haz.Hit && Math.Abs(Distance - haz.PositionMeters) < 30)
                    {
                        haz.Hit = true;
                        bool avoided = false;
                        if (haz.HazardType == "Rock" && IsJumping) avoided = true;
                        if (haz.HazardType == "Branch" && IsTucking) avoided = true;

                        if (!avoided)
                        {
                            IsTripping = true;
                            ScoreMultiplier = Math.Max(1, ScoreMultiplier - 1);
                            ActivePhaseLogs.Add($"[WIPEOUT] Collided with low-lying {haz.HazardType}! Multiplier decreased.");
                            StateHasChanged();
                            await Task.Delay(1500);
                            IsTripping = false;
                        }
                        else
                        {
                            ActivePhaseLogs.Add($"[EVADE] Cleared {haz.HazardType} obstacle smoothly.");
                        }
                    }
                }

                // 3. Check major obstacle checkpoints
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

        private bool IsDirectiveActionable(string directive)
        {
            if (string.IsNullOrWhiteSpace(directive)) return false;
            if (directive.Trim().Length < 15) return false;
            
            var verbs = new[] { "run", "write", "fix", "calculate", "compile", "select", "adjust", "create", "modify", "check", "delete", "use", "add", "remove", "update", "verify", "deploy", "patch", "test" };
            return verbs.Any(v => directive.Contains(v, StringComparison.OrdinalIgnoreCase));
        }

        private async Task SubmitDirective()
        {
            if (string.IsNullOrWhiteSpace(CurrentDirective)) return;

            if (ActiveObstacle != null)
            {
                ActiveObstacle.EvaluationSchema.EvaluatorInputs.RawPromptText = CurrentDirective;
            }

            bool actionable = IsDirectiveActionable(CurrentDirective);

            if (!actionable)
            {
                TrippedObstacleIndices.Add(ActiveObstacleIndex);
                IsTripping = true;
                StateHasChanged();
                await Task.Delay(1500);
                IsTripping = false;
            }
            else
            {
                var rnd = new Random();
                if (rnd.Next(2) == 0)
                {
                    IsJumping = true;
                }
                else
                {
                    IsTucking = true;
                }

                StateHasChanged();
                await Task.Delay(1000);

                IsJumping = false;
                IsTucking = false;
            }

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
                for (int i = 0; i < ActiveChallenges.Count; i++)
                {
                    var challenge = ActiveChallenges[i];
                    var eval = Engine.Evaluate(challenge.EvaluationSchema);
                    double score = eval.TotalScore;
                    if (TrippedObstacleIndices.Contains(i))
                    {
                        score = Math.Min(40.0, score);
                        challenge.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = "TRIPPED: Directive failed actionability. Capped at 40%. " + (challenge.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis ?? "");
                    }
                    totalScoreSum += score;
                }

                double averageScore = totalScoreSum / ActiveChallenges.Count;
                double finalScore = Math.Min(100.0, averageScore + (CrystalCount * 0.4));

                FinalResult = new EvaluationResult
                {
                    TotalScore = finalScore
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
                ActivePhaseLogs.Add($"[BATCH WARNING] AI Grader unavailable: {ex.Message}. Falling back to programmatic matrix heuristics...");
                StateHasChanged();
                await Task.Delay(1500);

                foreach (var challenge in ActiveChallenges)
                {
                    var rawPrompt = challenge.EvaluationSchema.EvaluatorInputs.RawPromptText ?? "";
                    
                    bool hasActionability = rawPrompt.Contains("```") || rawPrompt.Contains("run") || rawPrompt.Contains("calculate") || rawPrompt.Contains("write") || rawPrompt.Length > 20;
                    double actionability = hasActionability ? 0.90 : 0.60;
                    
                    double alignment = Math.Min(1.0, 0.4 + (rawPrompt.Length / 100.0));
                    
                    challenge.EvaluationSchema.EvaluatorInputs.AiActionabilityScore = actionability;
                    challenge.EvaluationSchema.EvaluatorInputs.AiTargetAlignmentScore = alignment;
                    challenge.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = "Resolved via client-side matrix heuristic fallback.";
                }

                double totalScoreSum = 0;
                for (int i = 0; i < ActiveChallenges.Count; i++)
                {
                    var challenge = ActiveChallenges[i];
                    var eval = Engine.Evaluate(challenge.EvaluationSchema);
                    double score = eval.TotalScore;
                    if (TrippedObstacleIndices.Contains(i))
                    {
                        score = Math.Min(40.0, score);
                        challenge.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis = "TRIPPED: Directive failed actionability. Capped at 40%. " + (challenge.EvaluationSchema.EvaluatorInputs.AiFailureAnalysis ?? "");
                    }
                    totalScoreSum += score;
                }

                double averageScore = totalScoreSum / ActiveChallenges.Count;
                double finalScore = Math.Min(100.0, averageScore + (CrystalCount * 0.4));

                FinalResult = new EvaluationResult
                {
                    TotalScore = finalScore
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

                ActivePhaseLogs.Add("[BATCH] Local Fallback Evaluation Complete!");
                Stage = GameStage.Finished;
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

        private string GetObstacleColor(string domain)
        {
            if (string.IsNullOrEmpty(domain)) return "var(--brand-crimson)";
            
            if (domain.Contains("Code", StringComparison.OrdinalIgnoreCase) || 
                domain.Contains("App", StringComparison.OrdinalIgnoreCase) || 
                domain.Contains("System", StringComparison.OrdinalIgnoreCase))
            {
                return "var(--brand-accent)";
            }
            if (domain.Contains("Construction", StringComparison.OrdinalIgnoreCase))
            {
                return "var(--brand-amber)";
            }
            if (domain.Contains("Auto", StringComparison.OrdinalIgnoreCase))
            {
                return "var(--brand-emerald)";
            }
            return "var(--brand-crimson)";
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (Stage != GameStage.Running) return;
            if (e.Key == " " || e.Key == "ArrowUp")
            {
                _ = TriggerJump();
            }
            else if (e.Key == "ArrowDown" || e.Key == "Shift")
            {
                _ = TriggerSlide();
            }
        }
    }
}
