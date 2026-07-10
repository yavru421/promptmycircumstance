using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PromptMyCircumstance.Services
{
    #region Balanced Schema DTO Models

    public class BalancedPromptEvaluation
    {
        [JsonPropertyName("evaluator_inputs")]
        public EvaluatorInputs EvaluatorInputs { get; set; } = new();

        [JsonPropertyName("balanced_criteria_weights")]
        public BalancedCriteriaWeights BalancedCriteriaWeights { get; set; } = new();

        [JsonPropertyName("programmatic_validation_rules")]
        public ProgrammaticValidationRules ProgrammaticValidationRules { get; set; } = new();
    }

    public class EvaluatorInputs
    {
        [JsonPropertyName("raw_prompt_text")]
        public string RawPromptText { get; set; } = string.Empty;

        [JsonPropertyName("target_model_id")]
        public string TargetModelId { get; set; } = string.Empty;

        [JsonPropertyName("captured_output_string")]
        public string CapturedOutputString { get; set; } = string.Empty;

        [JsonPropertyName("reference_gold_standard_answer")]
        public string ReferenceGoldStandardAnswer { get; set; } = string.Empty;

        [JsonPropertyName("ai_semantic_score")]
        public double AiSemanticScore { get; set; }

        [JsonPropertyName("ai_failure_analysis")]
        public string AiFailureAnalysis { get; set; } = string.Empty;
    }

    public class BalancedCriteriaWeights
    {
        [JsonPropertyName("logical_boundary_control")]
        public LogicalBoundaryControl LogicalBoundaryControl { get; set; } = new();

        [JsonPropertyName("token_velocity_density")]
        public TokenVelocityDensity TokenVelocityDensity { get; set; } = new();

        [JsonPropertyName("state_context_architecture")]
        public StateContextArchitecture StateContextArchitecture { get; set; } = new();

        [JsonPropertyName("first_shot_output_compliance")]
        public FirstShotOutputCompliance FirstShotOutputCompliance { get; set; } = new();
    }

    public class LogicalBoundaryControl
    {
        [JsonPropertyName("weight_percentage")]
        public double WeightPercentage { get; set; } = 30.0;

        [JsonPropertyName("score_threshold")]
        public int ScoreThreshold { get; set; } = 90;

        [JsonPropertyName("unassigned_variable_rule")]
        public UnassignedVariableRule UnassignedVariableRule { get; set; } = new();
    }

    public class UnassignedVariableRule
    {
        [JsonPropertyName("pattern_to_match")]
        public string PatternToMatch { get; set; } = @"(?i)someone\s+needs\s+to|who\s+is\s+doing";

        [JsonPropertyName("tag_replacement")]
        public string TagReplacement { get; set; } = "[UNASSIGNED]";
    }

    public class TokenVelocityDensity
    {
        [JsonPropertyName("weight_percentage")]
        public double WeightPercentage { get; set; } = 25.0;

        [JsonPropertyName("max_word_limit")]
        public int MaxWordLimit { get; set; } = 50;

        [JsonPropertyName("courtesy_token_tolerance")]
        public int CourtesyTokenTolerance { get; set; } = 5;
    }

    public class StateContextArchitecture
    {
        [JsonPropertyName("weight_percentage")]
        public double WeightPercentage { get; set; } = 20.0;

        [JsonPropertyName("enforce_delimiters")]
        public bool EnforceDelimiters { get; set; } = true;

        [JsonPropertyName("allowed_delimiters")]
        public List<string> AllowedDelimiters { get; set; } = new() { "```", "###", "📂", "📋" };
    }

    public class FirstShotOutputCompliance
    {
        [JsonPropertyName("weight_percentage")]
        public double WeightPercentage { get; set; } = 25.0;

        [JsonPropertyName("enforce_single_turn_success")]
        public bool EnforceSingleTurnSuccess { get; set; } = true;

        [JsonPropertyName("target_format_standard")]
        public string TargetFormatStandard { get; set; } = "markdown_list";
    }

    public class ProgrammaticValidationRules
    {
        [JsonPropertyName("semantic_match_threshold")]
        public double SemanticMatchThreshold { get; set; } = 0.85;

        [JsonPropertyName("output_structural_validation")]
        public OutputStructuralValidation OutputStructuralValidation { get; set; } = new();

        [JsonPropertyName("vibe_shorthand_mappings")]
        public List<VibeShorthandMapping> VibeShorthandMappings { get; set; } = new();
    }

    public class OutputStructuralValidation
    {
        [JsonPropertyName("regex_validations")]
        public List<RegexValidation> RegexValidations { get; set; } = new();

        [JsonPropertyName("strip_patterns")]
        public List<string> StripPatterns { get; set; } = new();
    }

    public class RegexValidation
    {
        [JsonPropertyName("validation_id")]
        public string ValidationId { get; set; } = string.Empty;

        [JsonPropertyName("regex_pattern")]
        public string RegexPattern { get; set; } = string.Empty;

        [JsonPropertyName("required_presence")]
        public bool RequiredPresence { get; set; } = true;
    }

    public class VibeShorthandMapping
    {
        [JsonPropertyName("shorthand_keyword")]
        public string ShorthandKeyword { get; set; } = string.Empty;

        [JsonPropertyName("execution_target_description")]
        public string ExecutionTargetDescription { get; set; } = string.Empty;

        [JsonPropertyName("semantic_space_activation_check")]
        public string SemanticSpaceActivationCheck { get; set; } = string.Empty;
    }

    #endregion

    #region Evaluation Loop Result Models

    public class MetricScore
    {
        public string CriteriaName { get; set; } = string.Empty;
        public double PointsEarned { get; set; }
        public double MaxPoints { get; set; }
        public string Status { get; set; } = "Pending"; // Pass, Warning, Fail
        public List<string> ExecutionLogs { get; set; } = new();
    }

    public class EvaluationResult
    {
        public double TotalScore { get; set; }
        public string OperatorTier { get; set; } = string.Empty;
        public string TierFeedback { get; set; } = string.Empty;
        public List<MetricScore> Metrics { get; set; } = new();
        public List<AnimationProgressStep> AnimationTimeline { get; set; } = new();
    }

    public class AnimationProgressStep
    {
        public string Phase { get; set; } = string.Empty;
        public int CompletionPercentage { get; set; }
        public string LogMessage { get; set; } = string.Empty;
    }

    #endregion

    public class BalancedScoringEngine
    {
        private static readonly string[] CourtesyWords = new[]
        {
            "please", "thank you", "thanks", "kindly", "could you", "would you", "appreciate it"
        };

        private static readonly string[] EmotionalVentingKeywords = new[]
        {
            "sweating bullets", "breathing down my neck", "paperwork again", "garbage", "so tired", "tired of playing catch"
        };

        /// <summary>
        /// Executes the complete reference-guided scoring loop inside browser memory (ZLA).
        /// </summary>
        public EvaluationResult Evaluate(BalancedPromptEvaluation payload)
        {
            var result = new EvaluationResult();
            int currentProgress = 0;

            AddProgressStep(result, "Initializing Crucible Engine", currentProgress += 10, "Setting up stateless memory variables...");

            var boundaryScore = EvaluateLogicalBoundaryControl(payload, result, ref currentProgress);
            result.Metrics.Add(boundaryScore);

            var velocityScore = EvaluateTokenVelocityDensity(payload, result, ref currentProgress);
            result.Metrics.Add(velocityScore);

            var architectureScore = EvaluateStateContextArchitecture(payload, result, ref currentProgress);
            result.Metrics.Add(architectureScore);

            var complianceScore = EvaluateFirstShotOutputCompliance(payload, result, ref currentProgress);
            result.Metrics.Add(complianceScore);

            result.TotalScore = result.Metrics.Sum(m => m.PointsEarned);
            DetermineOperatorTier(result);

            AddProgressStep(result, "Crucible Loop Finished", 100, $"Stateless compilation finished. Score: {result.TotalScore:F1}/100. Assigned {result.OperatorTier}.");

            return result;
        }

        private MetricScore EvaluateLogicalBoundaryControl(BalancedPromptEvaluation payload, EvaluationResult result, ref int progress)
        {
            var metric = new MetricScore
            {
                CriteriaName = "Logical Boundary Control",
                MaxPoints = payload.BalancedCriteriaWeights.LogicalBoundaryControl.WeightPercentage
            };

            AddProgressStep(result, "Evaluating Logical Gating", progress += 15, "Analyzing unassigned variables and liability parsing...");

            double points = 0;
            string rawPrompt = payload.EvaluatorInputs.RawPromptText;
            string capturedOutput = payload.EvaluatorInputs.CapturedOutputString;
            string targetTag = payload.BalancedCriteriaWeights.LogicalBoundaryControl.UnassignedVariableRule.TagReplacement;

            bool hasUnassignedTagInOutput = capturedOutput.Contains(targetTag, StringComparison.OrdinalIgnoreCase);
            if (hasUnassignedTagInOutput)
            {
                points += 15.0;
                metric.ExecutionLogs.Add($"PASS: Found exact validation tag '{targetTag}' inside captured Turn-1 output.");
            }
            else
            {
                metric.ExecutionLogs.Add($"FAIL: Captured Turn-1 output failed to programmatically tag empty variables as '{targetTag}'.");
            }

            var instructionMatch = Regex.Match(rawPrompt, @"(?i)unassigned|tag|missing|without\s+name|no\s+person");
            if (instructionMatch.Success)
            {
                points += 15.0;
                metric.ExecutionLogs.Add("PASS: User prompt successfully dictated boundary control rules for unassigned variables.");
            }
            else
            {
                bool mappedVibe = MapVibeShorthand(rawPrompt, payload, "Logical Boundary Control", metric.ExecutionLogs);
                if (mappedVibe)
                {
                    points += 10.0;
                    metric.ExecutionLogs.Add("WARNING: Prompt relied on semantic pre-trained alignment (Vibe Delegation) instead of absolute logic gates.");
                }
                else
                {
                    metric.ExecutionLogs.Add("FAIL: Raw prompt lacked system instructions to capture missing variables.");
                }
            }

            metric.PointsEarned = points;
            metric.Status = points >= 25.0 ? "Pass" : (points >= 15.0 ? "Warning" : "Fail");
            return metric;
        }

        private MetricScore EvaluateTokenVelocityDensity(BalancedPromptEvaluation payload, EvaluationResult result, ref int progress)
        {
            var metric = new MetricScore
            {
                CriteriaName = "Token Velocity & Density",
                // BUG FIX: was TokenVelocity_Density (underscore), correct C# property name is TokenVelocityDensity
                MaxPoints = payload.BalancedCriteriaWeights.TokenVelocityDensity.WeightPercentage
            };

            AddProgressStep(result, "Scanning Token Footprint", progress += 20, "Calculating word limits and applying courtesy tolerances...");

            string prompt = payload.EvaluatorInputs.RawPromptText;
            int maxLimit = payload.BalancedCriteriaWeights.TokenVelocityDensity.MaxWordLimit;
            int tolerance = payload.BalancedCriteriaWeights.TokenVelocityDensity.CourtesyTokenTolerance;

            string[] rawWords = prompt.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int rawCount = rawWords.Length;
            metric.ExecutionLogs.Add($"Telemetry: Prompt contains {rawCount} raw words.");

            int removedCourtesyCount = 0;
            string cleanedPrompt = prompt;
            foreach (var word in CourtesyWords)
            {
                var matches = Regex.Matches(cleanedPrompt, @"\b" + Regex.Escape(word) + @"\b", RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    removedCourtesyCount += matches.Count * word.Split(' ').Length;
                    cleanedPrompt = Regex.Replace(cleanedPrompt, @"\b" + Regex.Escape(word) + @"\b", "", RegexOptions.IgnoreCase);
                }
            }

            int allowedCourtesyDeduction = Math.Min(removedCourtesyCount, tolerance);
            int adjustedCount = rawCount - allowedCourtesyDeduction;

            metric.ExecutionLogs.Add($"Courtesy Filter: Stripped {removedCourtesyCount} courtesy words. Deducted {allowedCourtesyDeduction} (Tolerance Cap: {tolerance}).");
            metric.ExecutionLogs.Add($"Adjusted Token Velocity: {adjustedCount} words.");

            double points = 0;
            if (adjustedCount <= maxLimit)
            {
                points = 25.0;
                metric.ExecutionLogs.Add($"PASS: Adjusted velocity ({adjustedCount}) is inside target envelope (<= {maxLimit} words).");
            }
            else
            {
                int bloat = adjustedCount - maxLimit;
                points = Math.Max(0.0, 25.0 - (bloat * 1.5));
                metric.ExecutionLogs.Add($"WARNING: Velocity degraded by {bloat} excess tokens. Score: {points:F1}/25.");
            }

            metric.PointsEarned = points;
            metric.Status = points >= 20.0 ? "Pass" : (points >= 10.0 ? "Warning" : "Fail");
            return metric;
        }

        private MetricScore EvaluateStateContextArchitecture(BalancedPromptEvaluation payload, EvaluationResult result, ref int progress)
        {
            var metric = new MetricScore
            {
                CriteriaName = "State & Context Architecture",
                // BUG FIX: was State_Context_Architecture, correct property is StateContextArchitecture
                MaxPoints = payload.BalancedCriteriaWeights.StateContextArchitecture.WeightPercentage
            };

            AddProgressStep(result, "Evaluating Context Separation", progress += 20, "Checking formatting delimiter isolation blocks...");

            bool enforceDelimiters = payload.BalancedCriteriaWeights.StateContextArchitecture.EnforceDelimiters;
            var allowedDelimiters = payload.BalancedCriteriaWeights.StateContextArchitecture.AllowedDelimiters;
            string prompt = payload.EvaluatorInputs.RawPromptText;

            double points = 0;
            if (!enforceDelimiters)
            {
                points = 20.0;
                metric.ExecutionLogs.Add("NOTICE: Delimiter enforcement disabled. Defaulting to Pass.");
            }
            else
            {
                // BUG FIX: was prompt.Contains(word: d) — named param "word:" is invalid; fixed to prompt.Contains(d)
                List<string> foundDelimiters = allowedDelimiters.Where(d => prompt.Contains(d)).ToList();

                if (foundDelimiters.Count > 0)
                {
                    points = 20.0;
                    metric.ExecutionLogs.Add($"PASS: Found delimiter tokens: {string.Join(", ", foundDelimiters)}. Isolation logic enforced.");
                }
                else
                {
                    var bracketMatch = Regex.Match(prompt, @"[""\[{](.*?)[""\]}]");
                    if (bracketMatch.Success)
                    {
                        points = 10.0;
                        metric.ExecutionLogs.Add("WARNING: Direct delimiters missing. Prompt isolated context with bracket parameters.");
                    }
                    else
                    {
                        metric.ExecutionLogs.Add("FAIL: Zero context architecture delimiters. High risk of instruction drowning.");
                    }
                }
            }

            metric.PointsEarned = points;
            metric.Status = points >= 20.0 ? "Pass" : (points >= 10.0 ? "Warning" : "Fail");
            return metric;
        }

        private MetricScore EvaluateFirstShotOutputCompliance(BalancedPromptEvaluation payload, EvaluationResult result, ref int progress)
        {
            var metric = new MetricScore
            {
                CriteriaName = "First-Shot Output Compliance",
                MaxPoints = payload.BalancedCriteriaWeights.FirstShotOutputCompliance.WeightPercentage
            };

            AddProgressStep(result, "Validating Output Compliance", progress += 25, "Evaluating AI Agent evaluation metrics and gold standard scaling...");

            double score = payload.EvaluatorInputs.AiSemanticScore;
            double points = score * metric.MaxPoints;

            metric.ExecutionLogs.Add($"AI Evaluator Scale: {score:F2} / 1.00");
            metric.ExecutionLogs.Add($"Calculated Score: {points:F1} / {metric.MaxPoints}");

            if (!string.IsNullOrWhiteSpace(payload.EvaluatorInputs.AiFailureAnalysis))
            {
                metric.ExecutionLogs.Add($"Judge Feedback: {payload.EvaluatorInputs.AiFailureAnalysis}");
            }

            metric.PointsEarned = points;
            metric.Status = points >= 20.0 ? "Pass" : (points >= 10.0 ? "Warning" : "Fail");
            return metric;
        }

        #region Mathematics Helpers

        private double ComputeJaccardSimilarity(string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
                return 0.0;

            HashSet<string> sourceTokens = TokenizeAndClean(source);
            HashSet<string> targetTokens = TokenizeAndClean(target);

            if (sourceTokens.Count == 0 || targetTokens.Count == 0)
                return 0.0;

            int intersectionCount = sourceTokens.Intersect(targetTokens).Count();
            int unionCount = sourceTokens.Union(targetTokens).Count();

            return (double)intersectionCount / unionCount;
        }

        private HashSet<string> TokenizeAndClean(string text)
        {
            string cleanText = Regex.Replace(text.ToLower(), @"[^\w\s]", "");
            string[] rawWords = cleanText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return new HashSet<string>(rawWords);
        }

        private bool MapVibeShorthand(string prompt, BalancedPromptEvaluation payload, string criteria, List<string> logs)
        {
            foreach (var mapping in payload.ProgrammaticValidationRules.VibeShorthandMappings)
            {
                if (prompt.Contains(mapping.ShorthandKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    logs.Add($"Vibe Activation: Sourced shorthand keyword '{mapping.ShorthandKeyword}' in prompt.");
                    logs.Add($"Orchestrator Directive: Activating semantic space check '{mapping.SemanticSpaceActivationCheck}'.");

                    bool hasSucceeded = Regex.IsMatch(payload.EvaluatorInputs.CapturedOutputString, mapping.SemanticSpaceActivationCheck);
                    if (hasSucceeded)
                    {
                        logs.Add($"Vibe Compliance Success: Checked execution of '{mapping.ExecutionTargetDescription}' in completions.");
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region UI and State Progression Handlers

        private void AddProgressStep(EvaluationResult result, string phase, int completion, string message)
        {
            result.AnimationTimeline.Add(new AnimationProgressStep
            {
                Phase = phase,
                CompletionPercentage = Math.Min(completion, 100),
                LogMessage = message
            });
        }

        private void DetermineOperatorTier(EvaluationResult result)
        {
            if (result.TotalScore >= 90.0)
            {
                result.OperatorTier = "Tier 1: Elite System Orchestrator";
                result.TierFeedback = "Optimal token velocity. Absolute boundary control achieved.";
            }
            else if (result.TotalScore >= 70.0)
            {
                result.OperatorTier = "Tier 2: Technical Integrator";
                result.TierFeedback = "Functional execution, but prone to downstream token bloat. Streamline your intent.";
            }
            else
            {
                result.OperatorTier = "Tier 3: Casual Consumer";
                result.TierFeedback = "High model drift risk. You are talking to the machine instead of programming it.";
            }
        }

        #endregion
    }
}
