using System;
using System.Threading.Tasks;

namespace ollez.Services
{
    public class GpuInfo
    {
        public string Name { get; set; } = string.Empty;          // GPU型号
        public int MemoryTotal { get; set; }      // 总显存(MB)
        public int MemoryFree { get; set; }       // 可用显存(MB)
        public int MemoryUsed { get; set; }       // 已用显存(MB)
        public string Architecture { get; set; } = string.Empty;   // GPU架构
    }

    public class CudaInfo
    {
        public bool IsAvailable { get; set; }
        public string Version { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string SmiVersion { get; set; } = string.Empty;
        public GpuInfo[] Gpus { get; set; } = Array.Empty<GpuInfo>();
    }

    public class ModelRequirements
    {
        public string Name { get; set; } = string.Empty;          // 模型名称
        public int MinimumVram { get; set; }      // 最小显存要求(MB)
        public string Description { get; set; } = string.Empty;    // 模型描述
    }

    public class ModelRecommendation
    {
        public bool CanRunLargeModels { get; set; }        // 是否可以运行大型模型
        public ModelRequirements RecommendedModel { get; set; } = new();  // 推荐的模型
        public string RecommendationReason { get; set; } = string.Empty;    // 推荐原因
    }

    public class OllamaModelInfo
    {
        public string Name { get; set; } = string.Empty;           // 模型名称
        public string Digest { get; set; } = string.Empty;         // 模型摘要
        public long Size { get; set; }             // 模型大小
        public string ModifiedAt { get; set; } = string.Empty;     // 最后修改时间
        public bool IsRunning { get; set; }        // 是否正在运行
        public bool HasError { get; set; }         // 是否有错误
        public string Error { get; set; } = string.Empty;          // 错误信息
        public string Status { get; set; } = string.Empty;         // 运行状态
    }

    public class OllamaInfo
    {
        public bool IsRunning { get; set; }        // 服务是否运行
        public string Version { get; set; } = string.Empty;        // 版本号
        public string BuildType { get; set; } = string.Empty;      // 构建类型
        public string CommitHash { get; set; } = string.Empty;     // 提交哈希
        public string Endpoint { get; set; } = string.Empty;       // API端点
        public bool HasError { get; set; }         // 是否有错误
        public string Error { get; set; } = string.Empty;          // 错误信息
        public OllamaModelInfo[] InstalledModels { get; set; } = Array.Empty<OllamaModelInfo>();  // 已安装的模型
    }

    public interface ISystemCheckService
    {
        Task<CudaInfo> CheckCudaAsync();
        Task<OllamaInfo> CheckOllamaAsync();
        Task<ModelRecommendation> GetModelRecommendationAsync();
    }
} 