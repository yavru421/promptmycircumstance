using System;
using System.Collections.Generic;
using PromptMyCircumstance.Models;

namespace PromptMyCircumstance.Services
{
    public class ChallengeLibrary
    {
        public List<ChallengePayload> GenerateChallenges()
        {
            var challenges = new List<ChallengePayload>();

            // Domain 1: Project Manager Overhaul
            for (int i = 1; i <= 6; i++)
            {
                challenges.Add(new ChallengePayload
                {
                    Id = $"PM-{i}",
                    Title = $"Project Manager Overhaul - Vol {i}",
                    DomainTag = "Project Manager Overhaul",
                    DifficultyStars = (i % 3) + 1,
                    RawTelemetryDump = "The new UI is garbage. I'm sweating bullets trying to get the dumpster layout cleared. Lead times are awful. Someone needs to call the yard.",
                    ReferenceGoldStandardAnswer = "- [UNASSIGNED]: Clear dumpster layout\n- [UNASSIGNED]: Call the yard",
                    EvaluationSchema = new BalancedPromptEvaluation
                    {
                        EvaluatorInputs = new EvaluatorInputs
                        {
                            ReferenceGoldStandardAnswer = "- [UNASSIGNED]: Clear dumpster layout\n- [UNASSIGNED]: Call the yard"
                        },
                        BalancedCriteriaWeights = new BalancedCriteriaWeights
                        {
                            FirstShotOutputCompliance = new FirstShotOutputCompliance { TargetFormatStandard = "markdown_list" },
                            LogicalBoundaryControl = new LogicalBoundaryControl { UnassignedVariableRule = new UnassignedVariableRule { TagReplacement = "[UNASSIGNED]" } }
                        }
                    }
                });
            }

            // Domain 2: Data Pipeline Sanitization
            for (int i = 1; i <= 6; i++)
            {
                challenges.Add(new ChallengePayload
                {
                    Id = $"DATA-{i}",
                    Title = $"Telemetry Noise Filter - Vol {i}",
                    DomainTag = "Data Pipeline Sanitization",
                    DifficultyStars = (i % 3) + 1,
                    RawTelemetryDump = "System crashed again. I am so tired of playing catch up. Error 404 on module X. It's breathing down my neck.",
                    ReferenceGoldStandardAnswer = "- Error 404 on module X",
                    EvaluationSchema = new BalancedPromptEvaluation
                    {
                        EvaluatorInputs = new EvaluatorInputs
                        {
                            ReferenceGoldStandardAnswer = "- Error 404 on module X"
                        },
                        BalancedCriteriaWeights = new BalancedCriteriaWeights
                        {
                            FirstShotOutputCompliance = new FirstShotOutputCompliance { TargetFormatStandard = "markdown_list" }
                        }
                    }
                });
            }

            // Domain 3: Meeting Transcript Extraction
            for (int i = 1; i <= 6; i++)
            {
                challenges.Add(new ChallengePayload
                {
                    Id = $"MTG-{i}",
                    Title = $"Action Item Extraction - Vol {i}",
                    DomainTag = "Meeting Transcript Extraction",
                    DifficultyStars = (i % 3) + 1,
                    RawTelemetryDump = "Dave said the marketing budget is paperwork again. We need to finalize the Q3 numbers. I'm drowning in spreadsheets.",
                    ReferenceGoldStandardAnswer = "- Finalize Q3 numbers",
                    EvaluationSchema = new BalancedPromptEvaluation
                    {
                        EvaluatorInputs = new EvaluatorInputs
                        {
                            ReferenceGoldStandardAnswer = "- Finalize Q3 numbers"
                        },
                        BalancedCriteriaWeights = new BalancedCriteriaWeights
                        {
                            FirstShotOutputCompliance = new FirstShotOutputCompliance { TargetFormatStandard = "markdown_list" }
                        }
                    }
                });
            }

            // Domain 4: Sales Email Compression
            for (int i = 1; i <= 6; i++)
            {
                challenges.Add(new ChallengePayload
                {
                    Id = $"SALES-{i}",
                    Title = $"Sales Copy Compression - Vol {i}",
                    DomainTag = "Sales Email Compression",
                    DifficultyStars = (i % 3) + 1,
                    RawTelemetryDump = "Hey folks, wanted to circle back and touch base on the synergies. The product is revolutionary. Let's schedule a call.",
                    ReferenceGoldStandardAnswer = "Requesting a call to discuss the product.",
                    EvaluationSchema = new BalancedPromptEvaluation
                    {
                        EvaluatorInputs = new EvaluatorInputs
                        {
                            ReferenceGoldStandardAnswer = "Requesting a call to discuss the product."
                        },
                        BalancedCriteriaWeights = new BalancedCriteriaWeights
                        {
                            FirstShotOutputCompliance = new FirstShotOutputCompliance { TargetFormatStandard = "plain_text" }
                        }
                    }
                });
            }

            // Domain 5: Incident Report Triage
            for (int i = 1; i <= 6; i++)
            {
                challenges.Add(new ChallengePayload
                {
                    Id = $"INC-{i}",
                    Title = $"Incident Report Triage - Vol {i}",
                    DomainTag = "Incident Report Triage",
                    DifficultyStars = (i % 3) + 1,
                    RawTelemetryDump = "Server went down at 3AM. Total garbage infrastructure. Who is doing the reboot? Sweating bullets here.",
                    ReferenceGoldStandardAnswer = "- Incident: Server down at 3AM\n- Action: Reboot server ([UNASSIGNED])",
                    EvaluationSchema = new BalancedPromptEvaluation
                    {
                        EvaluatorInputs = new EvaluatorInputs
                        {
                            ReferenceGoldStandardAnswer = "- Incident: Server down at 3AM\n- Action: Reboot server ([UNASSIGNED])"
                        },
                        BalancedCriteriaWeights = new BalancedCriteriaWeights
                        {
                            FirstShotOutputCompliance = new FirstShotOutputCompliance { TargetFormatStandard = "markdown_list" },
                            LogicalBoundaryControl = new LogicalBoundaryControl { UnassignedVariableRule = new UnassignedVariableRule { TagReplacement = "[UNASSIGNED]" } }
                        }
                    }
                });
            }

            return challenges;
        }
    }
}
