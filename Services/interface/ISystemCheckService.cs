using System;
using System.Threading.Tasks;
using ollez.Models;

namespace ollez.Services
{
 

    public interface ISystemCheckService
    {
        Task<CudaInfo> CheckCudaAsync();
        Task<OllamaInfo> CheckOllamaAsync();
        Task<ModelRecommendation> GetModelRecommendationAsync();
    }
} 