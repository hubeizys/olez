using System;

namespace ollez.Models
{
    public class OllamaConfig
    {
        public int Id { get; set; }
        public string InstallPath { get; set; } = string.Empty;
        public string ModelsPath { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
} 