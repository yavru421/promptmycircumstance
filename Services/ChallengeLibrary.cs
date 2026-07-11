using System;
using System.Collections.Generic;
using PromptMyCircumstance.Models;
using PromptMyCircumstance.Services;

namespace PromptMyCircumstance.Services
{
    public class ChallengeLibrary
    {
        public List<ChallengePayload> GenerateChallenges()
        {
            return new List<ChallengePayload>
            {
                new ChallengePayload
                {
                    Id = "PMC-Code-Debugging-1",
                    Title = "Refactoring Legacy Python Modules",
                    DomainTag = "Code-Debugging",
                    DifficultyStars = 2,
                    RawTelemetryDump = "My old scripts are in a messy folder and I want them organized in the new workspace.",
                    ReferenceGoldStandardAnswer = "Generated the file path structures and verified the extraction of the Python utility classes into the new directory.",
                    EvaluationSchema = CreateDefaultSchema("Generated the file path structures and verified the extraction of the Python utility classes into the new directory.")
                },
                new ChallengePayload
                {
                    Id = "PMC-App-Dev-2",
                    Title = "21-Yard Concrete Volume Calculator",
                    DomainTag = "App-Dev",
                    DifficultyStars = 3,
                    RawTelemetryDump = "I need a way to calculate concrete volume on my phone while at the site.",
                    ReferenceGoldStandardAnswer = "Provided the complete C# code and deployment steps for a PWA volume calculator.",
                    EvaluationSchema = CreateDefaultSchema("Provided the complete C# code and deployment steps for a PWA volume calculator.")
                },
                new ChallengePayload
                {
                    Id = "PMC-System-Ops-3",
                    Title = "Acer Predator Helios Thermal Baselines",
                    DomainTag = "System-Ops",
                    DifficultyStars = 1,
                    RawTelemetryDump = "My laptop is getting really hot while playing games and compiling.",
                    ReferenceGoldStandardAnswer = "Detailed the thermal thresholds and recommended NVIDIA Optimus settings for the specific hardware.",
                    EvaluationSchema = CreateDefaultSchema("Detailed the thermal thresholds and recommended NVIDIA Optimus settings for the specific hardware.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Construction-Ops-4",
                    Title = "Modular Packout Staging System",
                    DomainTag = "Construction-Ops",
                    DifficultyStars = 2,
                    RawTelemetryDump = "I'm wasting time hauling my Packout boxes between the company truck and my truck.",
                    ReferenceGoldStandardAnswer = "Proposed a modular loading strategy utilizing the wheelbarrow for rapid transfer between the specific truck beds.",
                    EvaluationSchema = CreateDefaultSchema("Proposed a modular loading strategy utilizing the wheelbarrow for rapid transfer between the specific truck beds.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Interpersonal-5",
                    Title = "Framing Crew Staging Conflict",
                    DomainTag = "Interpersonal",
                    DifficultyStars = 3,
                    RawTelemetryDump = "My project manager is messing up the site logistics and it's causing delays.",
                    ReferenceGoldStandardAnswer = "Drafted a direct, professional message addressing the material staging issues to the project manager.",
                    EvaluationSchema = CreateDefaultSchema("Drafted a direct, professional message addressing the material staging issues to the project manager.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Auto-Repair-6",
                    Title = "F-150 Dual-Piston Brake Compression",
                    DomainTag = "Auto-Repair",
                    DifficultyStars = 2,
                    RawTelemetryDump = "I can't get the brake part to fit over the new pads on my truck.",
                    ReferenceGoldStandardAnswer = "Explained the manual process for compressing the caliper pistons and seating the replacement clips.",
                    EvaluationSchema = CreateDefaultSchema("Explained the manual process for compressing the caliper pistons and seating the replacement clips.")
                },
                new ChallengePayload
                {
                    Id = "PMC-App-Dev-7",
                    Title = "Safari Geolocation API Sync",
                    DomainTag = "App-Dev",
                    DifficultyStars = 4,
                    RawTelemetryDump = "I just started building a tracking app a few hours ago and the browser isn't pulling my location.",
                    ReferenceGoldStandardAnswer = "Debugged the JavaScript geolocation API call and corrected the permissions flow for iOS Safari.",
                    EvaluationSchema = CreateDefaultSchema("Debugged the JavaScript geolocation API call and corrected the permissions flow for iOS Safari.")
                },
                new ChallengePayload
                {
                    Id = "PMC-System-Ops-8",
                    Title = "Automated Config App-Purger",
                    DomainTag = "System-Ops",
                    DifficultyStars = 2,
                    RawTelemetryDump = "I have too many temp files clogging up my local system.",
                    ReferenceGoldStandardAnswer = "Created an unmanaged script hook and macro definition to automate local file cleanup.",
                    EvaluationSchema = CreateDefaultSchema("Created an unmanaged script hook and macro definition to automate local file cleanup.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Code-Debugging-9",
                    Title = "WinForms Memory Pinning Leak",
                    DomainTag = "Code-Debugging",
                    DifficultyStars = 4,
                    RawTelemetryDump = "My tracking application freezes and crashes after running for a few minutes.",
                    ReferenceGoldStandardAnswer = "Identified the memory leak in the unmanaged C# wrappers and provided a safe disposal pattern.",
                    EvaluationSchema = CreateDefaultSchema("Identified the memory leak in the unmanaged C# wrappers and provided a safe disposal pattern.")
                },
                new ChallengePayload
                {
                    Id = "PMC-App-Dev-10",
                    Title = "3x3 Inverse Homography Tracking",
                    DomainTag = "App-Dev",
                    DifficultyStars = 5,
                    RawTelemetryDump = "I need my camera app to map a flat surface from an angle.",
                    ReferenceGoldStandardAnswer = "Generated the mathematical matrix transformation code for the coordinate tracking loop.",
                    EvaluationSchema = CreateDefaultSchema("Generated the mathematical matrix transformation code for the coordinate tracking loop.")
                },
                new ChallengePayload
                {
                    Id = "PMC-System-Ops-11",
                    Title = "Multi-Agent Parallel File Locking",
                    DomainTag = "System-Ops",
                    DifficultyStars = 3,
                    RawTelemetryDump = "My local AI scripts keep overwriting each other when they run at the same time.",
                    ReferenceGoldStandardAnswer = "Provided a concurrent file-locking mechanism for the multi-agent orchestration framework.",
                    EvaluationSchema = CreateDefaultSchema("Provided a concurrent file-locking mechanism for the multi-agent orchestration framework.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Construction-Math-12",
                    Title = "Framing Length Material Takeoff",
                    DomainTag = "Construction-Math",
                    DifficultyStars = 2,
                    RawTelemetryDump = "My board count doesn't match the standard estimate.",
                    ReferenceGoldStandardAnswer = "Recalculated the material takeoff specifically accounting for continuous 16-foot framing lengths instead of 12-footers.",
                    EvaluationSchema = CreateDefaultSchema("Recalculated the material takeoff specifically accounting for continuous 16-foot framing lengths instead of 12-footers.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Construction-Structural-13",
                    Title = "Flush-Cut Railing Post Framing",
                    DomainTag = "Construction-Structural",
                    DifficultyStars = 3,
                    RawTelemetryDump = "The railing I'm building has a flat top, not raised posts.",
                    ReferenceGoldStandardAnswer = "Corrected the structural analysis to match the flush-cut horizontal framing layer.",
                    EvaluationSchema = CreateDefaultSchema("Corrected the structural analysis to match the flush-cut horizontal framing layer.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Media-Editing-14",
                    Title = "FFmpeg Job Site Time-lapse Pipeline",
                    DomainTag = "Media-Editing",
                    DifficultyStars = 2,
                    RawTelemetryDump = "My construction photos take up gigabytes and won't combine into a video.",
                    ReferenceGoldStandardAnswer = "Wrote a command-line script to batch resize images and encode them into an optimized video file.",
                    EvaluationSchema = CreateDefaultSchema("Wrote a command-line script to batch resize images and encode them into an optimized video file.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Maintenance-15",
                    Title = "Armstrong Drop Ceiling Light Extenders",
                    DomainTag = "Maintenance",
                    DifficultyStars = 3,
                    RawTelemetryDump = "The acoustic ceiling tiles don't sit right around the old light fixtures.",
                    ReferenceGoldStandardAnswer = "Outlined the manual installation procedure for securely fitting extenders in a commercial drop-ceiling.",
                    EvaluationSchema = CreateDefaultSchema("Outlined the manual installation procedure for securely fitting extenders in a commercial drop-ceiling.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Auto-Repair-16",
                    Title = "F-150 Lower Ball Joint Steering Knuckle Press",
                    DomainTag = "Auto-Repair",
                    DifficultyStars = 3,
                    RawTelemetryDump = "The old suspension joint on my truck is completely stuck.",
                    ReferenceGoldStandardAnswer = "Detailed the mechanical breakdown steps and the proper use of a ball joint press kit.",
                    EvaluationSchema = CreateDefaultSchema("Detailed the mechanical breakdown steps and the proper use of a ball joint press kit.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Weather-Analysis-17",
                    Title = "Precipitation Bands Radar Trajectory",
                    DomainTag = "Weather-Analysis",
                    DifficultyStars = 2,
                    RawTelemetryDump = "It looks like it's going to rain on the site but the app says clear.",
                    ReferenceGoldStandardAnswer = "Re-analyzed the provided radar image and confirmed the immediate trajectory of the heavy precipitation.",
                    EvaluationSchema = CreateDefaultSchema("Re-analyzed the provided radar image and confirmed the immediate trajectory of the heavy precipitation.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Construction-Material-18",
                    Title = "CertainTeed Batch Code Verification",
                    DomainTag = "Construction-Material",
                    DifficultyStars = 2,
                    RawTelemetryDump = "There's a stamp on this roofing material and I don't know if it's the right one.",
                    ReferenceGoldStandardAnswer = "Confirmed the marking was a factory batch identifier, overriding the previous incorrect regional code assumption.",
                    EvaluationSchema = CreateDefaultSchema("Confirmed the marking was a factory batch identifier, overriding the previous incorrect regional code assumption.")
                },
                new ChallengePayload
                {
                    Id = "PMC-3D-Modeling-19",
                    Title = "OpenSCAD Support Post Validation",
                    DomainTag = "3D-Modeling",
                    DifficultyStars = 3,
                    RawTelemetryDump = "The digital drawing is missing the vertical supports.",
                    ReferenceGoldStandardAnswer = "Acknowledged the absence of the supports in the provided OpenSCAD model and adjusted the structural advice.",
                    EvaluationSchema = CreateDefaultSchema("Acknowledged the absence of the supports in the provided OpenSCAD model and adjusted the structural advice.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Construction-Structural-20",
                    Title = "Garage Multi-Ply Beam Span",
                    DomainTag = "Construction-Structural",
                    DifficultyStars = 4,
                    RawTelemetryDump = "I need to span a wide opening for a detached garage.",
                    ReferenceGoldStandardAnswer = "Provided the load-bearing requirements and assembly instructions utilizing GRK structural screws.",
                    EvaluationSchema = CreateDefaultSchema("Provided the load-bearing requirements and assembly instructions utilizing GRK structural screws.")
                },
                new ChallengePayload
                {
                    Id = "PMC-System-Ops-21",
                    Title = "PowerToys Command Script Mapping",
                    DomainTag = "System-Ops",
                    DifficultyStars = 2,
                    RawTelemetryDump = "I can't launch my custom tools from my quick menu.",
                    ReferenceGoldStandardAnswer = "Guided the user through configuring custom run commands and environment variables within PowerToys.",
                    EvaluationSchema = CreateDefaultSchema("Guided the user through configuring custom run commands and environment variables within PowerToys.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Interpersonal-22",
                    Title = "Siding Progress Field Update",
                    DomainTag = "Interpersonal",
                    DifficultyStars = 1,
                    RawTelemetryDump = "I'm hanging really long vinyl panels alone and it's taking too long.",
                    ReferenceGoldStandardAnswer = "Drafted a concise field update for the business owner detailing the manual installation constraints.",
                    EvaluationSchema = CreateDefaultSchema("Drafted a concise field update for the business owner detailing the manual installation constraints.")
                },
                new ChallengePayload
                {
                    Id = "PMC-System-Ops-23",
                    Title = "remember_recent Session Macro",
                    DomainTag = "System-Ops",
                    DifficultyStars = 3,
                    RawTelemetryDump = "The AI keeps forgetting what we just talked about in the terminal.",
                    ReferenceGoldStandardAnswer = "Wrote a localized Markdown-based prompt injection module to persist conversational context locally.",
                    EvaluationSchema = CreateDefaultSchema("Wrote a localized Markdown-based prompt injection module to persist conversational context locally.")
                },
                new ChallengePayload
                {
                    Id = "PMC-App-Dev-24",
                    Title = "Stateless URL Routing Blueprint",
                    DomainTag = "App-Dev",
                    DifficultyStars = 4,
                    RawTelemetryDump = "I need to share my ETA without setting up a whole server.",
                    ReferenceGoldStandardAnswer = "Designed a stateless architecture utilizing URL parameters and open API endpoints for location sharing.",
                    EvaluationSchema = CreateDefaultSchema("Designed a stateless architecture utilizing URL parameters and open API endpoints for location sharing.")
                },
                new ChallengePayload
                {
                    Id = "PMC-Music-Ops-25",
                    Title = "Odd Signature Drum Sight-Reading",
                    DomainTag = "Music-Ops",
                    DifficultyStars = 3,
                    RawTelemetryDump = "I have a complicated drum chart to sight-read tonight.",
                    ReferenceGoldStandardAnswer = "Provided a breakdown of counting methods and sticking patterns for complex time signatures.",
                    EvaluationSchema = CreateDefaultSchema("Provided a breakdown of counting methods and sticking patterns for complex time signatures.")
                }
            };
        }

        private BalancedPromptEvaluation CreateDefaultSchema(string goldStandard)
        {
            return new BalancedPromptEvaluation
            {
                EvaluatorInputs = new EvaluatorInputs
                {
                    ReferenceGoldStandardAnswer = goldStandard
                },
                BalancedCriteriaWeights = new BalancedCriteriaWeights
                {
                    ActionabilityWeight = 40.0,
                    ConstraintAdherenceWeight = 30.0,
                    TargetAlignmentWeight = 30.0
                }
            };
        }
    }
}
