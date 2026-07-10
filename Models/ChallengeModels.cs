namespace PromptMyCircumstance.Models
{
    public class ChallengePayload
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DomainTag { get; set; } = string.Empty;
        public int DifficultyStars { get; set; }
        public string RawTelemetryDump { get; set; } = string.Empty;
        public string ReferenceGoldStandardAnswer { get; set; } = string.Empty;
        public Services.BalancedPromptEvaluation EvaluationSchema { get; set; } = new();
    }
}
