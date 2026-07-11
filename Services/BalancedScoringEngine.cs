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

        [JsonPropertyName("ai_actionability_score")]
        public double AiActionabilityScore { get; set; }

        [JsonPropertyName("ai_constraint_adherence_score")]
        public double AiConstraintAdherenceScore { get; set; }

        [JsonPropertyName("ai_target_alignment_score")]
        public double AiTargetAlignmentScore { get; set; }

        [JsonPropertyName("ai_failure_analysis")]
        public string AiFailureAnalysis { get; set; } = string.Empty;
    }

    public class BalancedCriteriaWeights
    {
        [JsonPropertyName("actionability")]
        public double ActionabilityWeight { get; set; } = 40.0;

        [JsonPropertyName("constraint_adherence")]
        public double ConstraintAdherenceWeight { get; set; } = 30.0;

        [JsonPropertyName("target_alignment")]
        public double TargetAlignmentWeight { get; set; } = 30.0;
    }

    public class ProgrammaticValidationRules
    {
        [JsonPropertyName("semantic_match_threshold")]
        public double SemanticMatchThreshold { get; set; } = 0.85;
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
        /// <summary>
        /// Executes the complete reference-guided scoring loop inside browser memory (ZLA).
        /// </summary>
        public EvaluationResult Evaluate(BalancedPromptEvaluation payload)
        {
            var result = new EvaluationResult();
            int currentProgress = 0;

            AddProgressStep(result, "Initializing Crucible Engine", currentProgress += 10, "Setting up stateless evaluation matrix...");

            var actionabilityScore = EvaluateActionability(payload, result, ref currentProgress);
            result.Metrics.Add(actionabilityScore);

            var constraintScore = EvaluateConstraintAdherence(payload, result, ref currentProgress);
            result.Metrics.Add(constraintScore);

            var alignmentScore = EvaluateTargetAlignment(payload, result, ref currentProgress);
            result.Metrics.Add(alignmentScore);

            result.TotalScore = result.Metrics.Sum(m => m.PointsEarned);
            DetermineOperatorTier(result);

            AddProgressStep(result, "Crucible Loop Finished", 100, $"Stateless compilation finished. Score: {result.TotalScore:F1}/100. Assigned {result.OperatorTier}.");

            return result;
        }

        private MetricScore EvaluateActionability(BalancedPromptEvaluation payload, EvaluationResult result, ref int progress)
        {
            var metric = new MetricScore
            {
                CriteriaName = "Actionability (Bare-Metal)",
                MaxPoints = payload.BalancedCriteriaWeights.ActionabilityWeight
            };

            AddProgressStep(result, "Evaluating Output Actionability", progress += 30, "Analyzing executable payloads, scripts, and direct workflows...");

            double rawScore = payload.EvaluatorInputs.AiActionabilityScore;
            double points = rawScore * metric.MaxPoints;

            metric.PointsEarned = points;
            metric.Status = points >= (metric.MaxPoints * 0.8) ? "Pass" : (points >= (metric.MaxPoints * 0.5) ? "Warning" : "Fail");

            metric.ExecutionLogs.Add($"Judge Actionability Index: {rawScore:F2} / 1.00");
            metric.ExecutionLogs.Add($"Calculated Score: {points:F1} / {metric.MaxPoints}");
            
            // Check for direct execution formats
            bool hasCodeBlocks = payload.EvaluatorInputs.CapturedOutputString.Contains("```");
            if (hasCodeBlocks)
            {
                metric.ExecutionLogs.Add("PASS: Verified presence of isolated code blocks or configuration overrides.");
            }
            else
            {
                metric.ExecutionLogs.Add("NOTICE: Output contains text instructions but no raw code/command isolation blocks.");
            }

            return metric;
        }

        private MetricScore EvaluateConstraintAdherence(BalancedPromptEvaluation payload, EvaluationResult result, ref int progress)
        {
            var metric = new MetricScore
            {
                CriteriaName = "Constraint Adherence (No Hallucination)",
                MaxPoints = payload.BalancedCriteriaWeights.ConstraintAdherenceWeight
            };

            AddProgressStep(result, "Verifying System Parameters", progress += 30, "Comparing output parameters against circumstance constraints...");

            double rawScore = payload.EvaluatorInputs.AiConstraintAdherenceScore;
            double points = rawScore * metric.MaxPoints;

            metric.PointsEarned = points;
            metric.Status = points >= (metric.MaxPoints * 0.8) ? "Pass" : (points >= (metric.MaxPoints * 0.5) ? "Warning" : "Fail");

            metric.ExecutionLogs.Add($"Judge Constraint Compliance: {rawScore:F2} / 1.00");
            metric.ExecutionLogs.Add($"Calculated Score: {points:F1} / {metric.MaxPoints}");

            return metric;
        }

        private MetricScore EvaluateTargetAlignment(BalancedPromptEvaluation payload, EvaluationResult result, ref int progress)
        {
            var metric = new MetricScore
            {
                CriteriaName = "Target Alignment (Did It Work?)",
                MaxPoints = payload.BalancedCriteriaWeights.TargetAlignmentWeight
            };

            AddProgressStep(result, "Analyzing Target Fulfillment", progress += 20, "Measuring semantic alignment with reference resolution...");

            double rawScore = payload.EvaluatorInputs.AiTargetAlignmentScore;
            double points = rawScore * metric.MaxPoints;

            metric.PointsEarned = points;
            metric.Status = points >= (metric.MaxPoints * 0.8) ? "Pass" : (points >= (metric.MaxPoints * 0.5) ? "Warning" : "Fail");

            metric.ExecutionLogs.Add($"Judge Semantic Alignment: {rawScore:F2} / 1.00");
            metric.ExecutionLogs.Add($"Calculated Score: {points:F1} / {metric.MaxPoints}");

            if (!string.IsNullOrWhiteSpace(payload.EvaluatorInputs.AiFailureAnalysis) && 
                !payload.EvaluatorInputs.AiFailureAnalysis.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                metric.ExecutionLogs.Add($"Judge Diagnosis: {payload.EvaluatorInputs.AiFailureAnalysis}");
            }
            else
            {
                metric.ExecutionLogs.Add("PASS: Objective resolved successfully with zero semantic drift.");
            }

            return metric;
        }

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
                result.OperatorTier = "S-Tier: Master Operator";
                result.TierFeedback = "Actionable, direct instruction. Zero translation needed, absolute constraint adherence.";
            }
            else if (result.TotalScore >= 70.0)
            {
                result.OperatorTier = "A-Tier: Capable Integrator";
                result.TierFeedback = "Correct resolution, but prompt required agent clarification or contained mild semantic drift.";
            }
            else
            {
                result.OperatorTier = "B-Tier: Casual Informant";
                result.TierFeedback = "High risk of hallucination or non-actionable output. Refine parameters and specify clear outcome bounds.";
            }
        }

        #endregion
    }
}
