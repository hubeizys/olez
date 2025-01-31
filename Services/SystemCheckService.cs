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
                Name = "DeepSeek-R1-Distill-Qwen-1.5B",
                MinimumVram = 2000,  // 2GB
                Description = "运行命令：ollama run deepseek-r1:1.5b"
            },
            new ModelRequirements
            {
                Name = "DeepSeek-R1-Distill-Qwen-7B",
                MinimumVram = 4000,  // 4GB
                Description = "运行命令：ollama run deepseek-r1:7b"
            },
            new ModelRequirements
            {
                Name = "DeepSeek-R1-Distill-Llama-8B",
                MinimumVram = 4000,  // 4GB
                Description = "运行命令：ollama run deepseek-r1:8b"
            },
            new ModelRequirements
            {
                Name = "DeepSeek-R1-Distill-Qwen-14B",
                MinimumVram = 6000,  // 6GB
                Description = "运行命令：ollama run deepseek-r1:14b"
            },
            new ModelRequirements
            {
                Name = "DeepSeek-R1-Distill-Qwen-32B",
                MinimumVram = 8000,  // 8GB
                Description = "运行命令：ollama run deepseek-r1:32b"
            },
            new ModelRequirements
            {
                Name = "DeepSeek-R1-Distill-Llama-70B",
                MinimumVram = 8000,  // 8GB
                Description = "运行命令：ollama run deepseek-r1:70b"
            },
            new ModelRequirements
            {
                Name = "DeepSeek-R1",
                MinimumVram = 8000,  // 8GB
                Description = "运行命令：ollama run deepseek-r1:671b"
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

            // 根据显存大小推荐模型，平衡性能和体验
            if (maxAvailableVram >= 16000) // 16GB及以上，如4090
            {
                recommendation.CanRunLargeModels = true;
                recommendation.RecommendedModel = _deepseekModels[5]; // 推荐70B
                recommendation.RecommendationReason = $"检测到强大的GPU ({gpuName})，显存充沛 ({maxAvailableVram}MB)。\n" +
                    "推荐命令：ollama run deepseek-r1:70b（超流畅运行）\n" +
                    "• 同样流畅：DeepSeek-R1-Distill-Qwen-32B\n" +
                    "• 更大规模：DeepSeek-R1 (671B)";
            }
            else if (maxAvailableVram >= 12000) // 12GB，如3080Ti
            {
                recommendation.CanRunLargeModels = true;
                recommendation.RecommendedModel = _deepseekModels[3]; // 推荐14B
                recommendation.RecommendationReason = $"检测到高性能GPU ({gpuName})，显存{maxAvailableVram}MB。\n" +
                    "推荐命令：ollama run deepseek-r1:14b（流畅运行）\n" +
                    "• 极致流畅：DeepSeek-R1-Distill-Llama-8B\n" +
                    "• 可尝试：DeepSeek-R1-Distill-Qwen-32B（运行较慢）";
            }
            else if (maxAvailableVram >= 6000) // 6GB
            {
                recommendation.CanRunLargeModels = false;
                recommendation.RecommendedModel = _deepseekModels[2]; // 推荐8B
                recommendation.RecommendationReason = $"检测到性能不俗的GPU ({gpuName})，显存{maxAvailableVram}MB。\n" +
                    "推荐命令：ollama run deepseek-r1:8b（流畅运行）\n" +
                    "• 极致流畅：DeepSeek-R1-Distill-Qwen-7B\n" +
                    "• 可尝试：DeepSeek-R1-Distill-Qwen-14B（需要更多耐心）";
            }
            else if (maxAvailableVram >= 4000) // 4GB
            {
                recommendation.CanRunLargeModels = false;
                recommendation.RecommendedModel = _deepseekModels[1]; // 推荐7B
                recommendation.RecommendationReason = $"检测到配置适中的GPU ({gpuName})，显存{maxAvailableVram}MB。\n" +
                    "推荐命令：ollama run deepseek-r1:7b（正常运行）\n" +
                    "• 超流畅：DeepSeek-R1-Distill-Qwen-1.5B\n" +
                    "• 可尝试：DeepSeek-R1-Distill-Llama-8B（运行稍慢）";
            }
            else
            {
                recommendation.CanRunLargeModels = false;
                recommendation.RecommendedModel = _deepseekModels[0]; // 1.5B
                recommendation.RecommendationReason = $"检测到基础配置GPU ({gpuName})，显存{maxAvailableVram}MB。\n" +
                    "推荐命令：ollama run deepseek-r1:1.5b（流畅运行）\n" +
                    "• 可尝试：DeepSeek-R1-Distill-Qwen-7B（需要更多耐心）";
            }

            return recommendation;
        }

        private async Task<bool> CheckOllamaServiceAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "list",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error) && error.Contains("connect: connection refused"))
                {
                    Log.Warning("Ollama服务未运行，请使用命令：ollama serve");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "检查Ollama服务状态时发生错误");
                return false;
            }
        }

        private async Task<(string Output, string Error)> ExecuteOllamaCommandAsync(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = command,
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

                return (output, error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"执行Ollama命令时发生错误: {command}");
                return (string.Empty, ex.Message);
            }
        }

        public async Task<OllamaInfo> CheckOllamaAsync()
        {
            var info = new OllamaInfo
            {
                Endpoint = "http://localhost:11434"
            };

            // 首先检查ollama服务是否运行
            info.IsRunning = await CheckOllamaServiceAsync();
            if (!info.IsRunning)
            {
                Log.Warning("Ollama服务未运行。请执行命令：ollama serve");
                info.HasError = true;
                info.Error = "Ollama服务未运行，请执行命令：ollama serve";
                return info;
            }

            try
            {
                // 获取版本信息
                var (versionOutput, versionError) = await ExecuteOllamaCommandAsync("version");
                if (string.IsNullOrEmpty(versionError))
                {
                    var versionLines = versionOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (versionLines.Length > 0)
                    {
                        info.Version = versionLines[0].Trim();
                        info.BuildType = "正式版";  // 可以根据版本号格式判断是否为正式版
                    }
                }
                
                // 获取已安装的模型列表
                var (listOutput, listError) = await ExecuteOllamaCommandAsync("list");
                if (string.IsNullOrEmpty(listError))
                {
                    var modelsList = new List<OllamaModelInfo>();
                    var lines = listOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines.Skip(1)) // 跳过标题行
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var modelInfo = new OllamaModelInfo
                            {
                                Name = parts[0],
                                Size = parts[1].EndsWith("GB") ?
                                    long.Parse(parts[1].Replace("GB", "")) * 1024 * 1024 * 1024 :
                                    long.Parse(parts[1].Replace("MB", "")) * 1024 * 1024,
                                ModifiedAt = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : string.Empty,
                                IsRunning = false,
                                Status = "已安装"
                            };
                            modelsList.Add(modelInfo);
                        }
                    }

                    // 获取正在运行的模型状态
                    var (psOutput, psError) = await ExecuteOllamaCommandAsync("ps");
                    if (string.IsNullOrEmpty(psError) && !string.IsNullOrEmpty(psOutput))
                    {
                        var psLines = psOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in psLines.Skip(1)) // 跳过标题行
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 1)
                            {
                                var modelName = parts[0];
                                var model = modelsList.FirstOrDefault(m => m.Name == modelName);
                                if (model != null)
                                {
                                    model.IsRunning = true;
                                    model.Status = "运行中";
                                    if (parts.Length > 1)
                                    {
                                        model.Status = $"运行中 - {string.Join(" ", parts.Skip(1))}";
                                    }
                                }
                            }
                        }
                    }

                    info.InstalledModels = modelsList.OrderBy(m => m.Name).ToArray();
                }
                else
                {
                    Log.Warning("获取Ollama模型列表失败: {Error}", listError);
                    info.HasError = true;
                    info.Error = $"获取模型列表失败: {listError}";
                }
            }
            catch (Exception ex)
            {
                info.HasError = true;
                info.Error = ex.Message;
                Log.Error(ex, "检查Ollama状态时发生错误");
            }

            Log.Information("Ollama检查结果: {@Info}", info);
            return info;
        }
    }
} 