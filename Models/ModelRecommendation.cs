namespace ollez.Models
{
    public class ModelRequirements
    {
        public string Name { get; set; } = string.Empty;
        public int MinimumVram { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ModelRecommendation
    {
        public bool CanRunLargeModels { get; set; }
        public ModelRequirements RecommendedModel { get; set; } = new();
        public string RecommendationReason { get; set; } = string.Empty;
    }
}