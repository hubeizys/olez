using System.Threading.Tasks;

namespace YourNamespace.Services
{
    public class CudaInfo
    {
        public bool IsAvailable { get; set; }
        public string Version { get; set; }
        public string DriverVersion { get; set; }
        public string SmiVersion { get; set; }
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
    }
} 