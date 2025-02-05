using System;

namespace ollez.Models
{
    public class DeepseekModel
    {
        public string Size { get; set; }
        public double RequiredVRAM { get; set; }
        public bool IsInstalled { get; set; }

        public static DeepseekModel[] GetDefaultModels()
        {
            return new[]
            {
                new DeepseekModel { Size = "1.5b", RequiredVRAM = 2 },
                new DeepseekModel { Size = "7b", RequiredVRAM = 8 },
                new DeepseekModel { Size = "8b", RequiredVRAM = 8 },
                new DeepseekModel { Size = "14b", RequiredVRAM = 14 },
                new DeepseekModel { Size = "32b", RequiredVRAM = 32 },
                new DeepseekModel { Size = "70b", RequiredVRAM = 70 },
                new DeepseekModel { Size = "671b", RequiredVRAM = 671 }
            };
        }
    }
} 