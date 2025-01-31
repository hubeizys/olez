using System.Threading.Tasks;

namespace ollez.Services
{
    public class GpuInfo
    {
        public string Name { get; set; }          // GPU型号
        public int MemoryTotal { get; set; }      // 总显存(MB)
        public int MemoryFree { get; set; }       // 可用显存(MB)
        public int MemoryUsed { get; set; }       // 已用显存(MB)
        public string Architecture { get; set; }   // GPU架构
    }

    public class CudaInfo
    {
        public bool IsAvailable { get; set; }
        public string Version { get; set; }
        public string DriverVersion { get; set; }
        public string SmiVersion { get; set; }
        public GpuInfo[] Gpus { get; set; }
    }

    public class ModelRequirements
    {
        public string Name { get; set; }          // 模型名称
        public int MinimumVram { get; set; }      // 最小显存要求(MB)
        public string Description { get; set; }    // 模型描述
    }

    public class ModelRecommendation
    {
        public bool CanRunLargeModels { get; set; }        // 是否可以运行大型模型
        public ModelRequirements RecommendedModel { get; set; }  // 推荐的模型
        public string RecommendationReason { get; set; }    // 推荐原因
    }

    public class OllamaInfo
    {
        public bool IsRunning { get; set; }
        public string Version { get; set; }
        public string Endpoint { get; set; }
    }

    public interface ISystemCheckService
    {
        Task<CudaInfo> CheckCudaAsync();
        Task<OllamaInfo> CheckOllamaAsync();
        Task<ModelRecommendation> GetModelRecommendationAsync();
    }
} 