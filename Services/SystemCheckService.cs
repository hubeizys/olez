using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

namespace ollez.Services
{
    public class SystemCheckService : ISystemCheckService
    {
        private readonly HttpClient _httpClient;

        public SystemCheckService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<CudaInfo> CheckCudaAsync()
        {
            var info = new CudaInfo();
            try
            {
                Log.Information("开始检查CUDA状态");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        Arguments = "--query-gpu=name,memory.total,memory.free,memory.used,compute_cap --format=csv,noheader,nounits",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                Log.Information("nvidia-smi 退出代码: {ExitCode}", process.ExitCode);

                if (!string.IsNullOrEmpty(error))
                {
                    Log.Warning("nvidia-smi 错误输出: {Error}", error);
                }

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    info.IsAvailable = true;
                    Log.Information("CUDA 可用，GPU信息输出:\n{Output}", output);

                    // 获取GPU信息
                    var gpuList = new List<GpuInfo>();
                    var gpuLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in gpuLines)
                    {
                        var parts = line.Split(',').Select(p => p.Trim()).ToArray();
                        if (parts.Length >= 5)
                        {
                            var gpuInfo = new GpuInfo
                            {
                                Name = parts[0],
                                MemoryTotal = int.Parse(parts[1]),
                                MemoryFree = int.Parse(parts[2]),
                                MemoryUsed = int.Parse(parts[3]),
                                Architecture = $"Compute {parts[4]}"
                            };
                            gpuList.Add(gpuInfo);
                        }
                    }
                    info.Gpus = gpuList.ToArray();

                    // 获取版本信息
                    process.StartInfo.Arguments = "";
                    process.Start();
                    string versionOutput = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    var lines = versionOutput.Split('\n');
                    if (lines.Length > 0)
                    {
                        var versionLine = lines.FirstOrDefault(l => l.Contains("NVIDIA-SMI") && l.Contains("Driver Version") && l.Contains("CUDA Version"));
                        if (versionLine != null)
                        {
                            var smiMatch = System.Text.RegularExpressions.Regex.Match(versionLine, @"NVIDIA-SMI\s+([\d.]+)");
                            if (smiMatch.Success)
                            {
                                info.SmiVersion = smiMatch.Groups[1].Value;
                            }

                            var driverMatch = System.Text.RegularExpressions.Regex.Match(versionLine, @"Driver Version:\s*([\d.]+)");
                            if (driverMatch.Success)
                            {
                                info.DriverVersion = driverMatch.Groups[1].Value;
                            }

                            var cudaMatch = System.Text.RegularExpressions.Regex.Match(versionLine, @"CUDA Version:\s*([\d.]+)");
                            if (cudaMatch.Success)
                            {
                                info.Version = cudaMatch.Groups[1].Value;
                            }
                        }
                    }
                }
                else
                {
                    info.IsAvailable = false;
                    Log.Warning("CUDA 不可用");
                }
            }
            catch (Exception ex)
            {
                info.IsAvailable = false;
                Log.Error(ex, "检查CUDA状态时发生错误");
            }

            Log.Information("CUDA检查结果: {@Info}", info);
            return info;
        }

        private readonly ModelRequirements[] _deepseekModels = new[]
        {
            new ModelRequirements
            {
                Name = "deepseek-coder-6.7b-base",
                MinimumVram = 8000,  // 8GB
                Description = "适合基础编程任务，资源占用较少"
            },
            new ModelRequirements
            {
                Name = "deepseek-coder-33b-base",
                MinimumVram = 16000, // 16GB
                Description = "大型模型，适合复杂编程任务，代码质量更高"
            }
        };

        public async Task<ModelRecommendation> GetModelRecommendationAsync()
        {
            var cudaInfo = await CheckCudaAsync();
            var recommendation = new ModelRecommendation();

            if (!cudaInfo.IsAvailable || cudaInfo.Gpus == null || cudaInfo.Gpus.Length == 0)
            {
                recommendation.CanRunLargeModels = false;
                recommendation.RecommendedModel = _deepseekModels[0]; // 默认推荐最小模型
                recommendation.RecommendationReason = "未检测到可用的NVIDIA GPU，建议使用最基础的模型。";
                return recommendation;
            }

            // 获取最大可用显存
            int maxAvailableVram = cudaInfo.Gpus.Max(g => g.MemoryTotal);
            string gpuName = cudaInfo.Gpus[0].Name;

            if (maxAvailableVram >= 24000) // 24GB
            {
                recommendation.CanRunLargeModels = true;
                recommendation.RecommendedModel = _deepseekModels[1];
                recommendation.RecommendationReason = $"检测到高性能GPU ({gpuName})，显存充足 ({maxAvailableVram}MB)，可以运行33B大模型以获得最佳效果。";
            }
            else if (maxAvailableVram >= 8000) // 8GB
            {
                recommendation.CanRunLargeModels = false;
                recommendation.RecommendedModel = _deepseekModels[0];
                recommendation.RecommendationReason = $"检测到GPU ({gpuName})，显存{maxAvailableVram}MB，建议使用6.7B基础模型以获得流畅体验。";
            }
            else
            {
                recommendation.CanRunLargeModels = false;
                recommendation.RecommendedModel = _deepseekModels[0];
                recommendation.RecommendationReason = $"检测到GPU ({gpuName})显存较小 ({maxAvailableVram}MB)，建议使用6.7B基础模型，但可能会遇到性能瓶颈。";
            }

            return recommendation;
        }

        public async Task<OllamaInfo> CheckOllamaAsync()
        {
            var info = new OllamaInfo
            {
                Endpoint = "http://localhost:11434"
            };

            try
            {
                var response = await _httpClient.GetAsync($"{info.Endpoint}/api/version");
                if (response.IsSuccessStatusCode)
                {
                    var version = await response.Content.ReadAsStringAsync();
                    info.IsRunning = true;
                    info.Version = version;
                }
            }
            catch
            {
                info.IsRunning = false;
            }
            return info;
        }
    }
} 