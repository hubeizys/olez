using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using System.Text.Json;
using ollez.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace ollez.Services
{
    public class SystemCheckService : ISystemCheckService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _ollamaEndpoint;
        private bool _disposed = false;

        public SystemCheckService()
        {
            _httpClient = new HttpClient();
            _ollamaEndpoint = "http://localhost:11434";
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        ~SystemCheckService()
        {
            Dispose(false);
        }

        public async Task<CudaInfo> CheckCudaAsync()
        {
            var info = new CudaInfo();
            try
            {
                Log.Information("开始检查CUDA状态");

                string output;
                string error;
                using (var process = new Process
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
                })
                {
                    process.Start();
                    output = await process.StandardOutput.ReadToEndAsync();
                    error = await process.StandardError.ReadToEndAsync();
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
                    }
                }

                // 获取版本信息，使用新的Process实例
                string versionOutput;
                using (var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                })
                {
                    process.Start();
                    versionOutput = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                }

                // 解析版本输出
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

                // 检查 CUDNN
                if (info.IsAvailable)
                {
                    try
                    {
                        // 从系统环境变量 PATH 中查找 CUDNN
                        var pathValues = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();
                        var cudnnPath = pathValues
                            .Where(path => path.Contains("NVIDIA") && path.Contains("CUDNN"))
                            .OrderByDescending(path => path) // 获取最新版本
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(cudnnPath))
                        {
                            // 获取最新版本的目录
                            var versionDirs = Directory.GetDirectories(cudnnPath)
                                .OrderByDescending(d => d).FirstOrDefault();
                            if (versionDirs != null)
                            {
                                info.HasCudnn = true;
                                info.CudnnVersion = Path.GetFileName(versionDirs);
                                Log.Information("找到 CUDNN: {Path}", versionDirs);
                            }


                        }

                        // if (!string.IsNullOrEmpty(cudnnPath))
                        // {
                        //     // 检查是否存在 cudnn64_*.dll
                        //     var cudnnFiles = Directory.GetFiles(cudnnPath, "cudnn64_*.dll");
                        //     if (cudnnFiles.Length > 0)
                        //     {
                        //         info.HasCudnn = true;
                        //         // 获取版本信息
                        //         var fileInfo = FileVersionInfo.GetVersionInfo(cudnnFiles[0]);
                        //         info.CudnnVersion = fileInfo.FileVersion ?? "未知";
                        //         Log.Information("找到 CUDNN: {Path}", cudnnPath);
                        //     }
                        // }
                        // else
                        // {
                        //     // 尝试在默认安装路径查找
                        //     var nvidiaProgramFiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA");
                        //     if (Directory.Exists(nvidiaProgramFiles))
                        //     {
                        //         var cudnnDir = Directory.GetDirectories(nvidiaProgramFiles, "CUDNN")
                        //             .FirstOrDefault();

                        //         if (cudnnDir != null)
                        //         {
                        //             // 获取最新版本的目录
                        //             var versionDirs = Directory.GetDirectories(cudnnDir)
                        //                 .Where(d => Path.GetFileName(d).StartsWith("v"))
                        //                 .OrderByDescending(d => d);

                        //             foreach (var versionDir in versionDirs)
                        //             {
                        //                 var binPath = Path.Combine(versionDir, "bin");
                        //                 if (Directory.Exists(binPath))
                        //                 {
                        //                     var cudnnFiles = Directory.GetFiles(binPath, "cudnn64_*.dll", SearchOption.AllDirectories);
                        //                     if (cudnnFiles.Length > 0)
                        //                     {
                        //                         info.HasCudnn = true;
                        //                         var fileInfo = FileVersionInfo.GetVersionInfo(cudnnFiles[0]);
                        //                         info.CudnnVersion = fileInfo.FileVersion ?? "未知";
                        //                         Log.Information("找到 CUDNN: {Path}", binPath);
                        //                         break;
                        //                     }
                        //                 }
                        //             }
                        //         }
                        //     }
                        // }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "检查 CUDNN 状态时发生错误");
                    }
                }

                if (!info.IsAvailable)
                {
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

        public async Task<OllamaInfo> CheckOllamaAsync()
        {
            var ollamaInfo = new OllamaInfo
            {
                Endpoint = _ollamaEndpoint,
                IsRunning = false,
                InstalledModels = new ObservableCollection<OllamaModel>()
            };

            try
            {
                var response = await _httpClient.GetAsync($"{_ollamaEndpoint}/api/version");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var versionInfo = JsonSerializer.Deserialize<JsonElement>(content);

                    ollamaInfo.IsRunning = true;
                    ollamaInfo.Version = versionInfo.GetProperty("version").GetString() ?? string.Empty;

                    // 获取已安装的模型
                    var modelsResponse = await _httpClient.GetAsync($"{_ollamaEndpoint}/api/tags");
                    if (modelsResponse.IsSuccessStatusCode)
                    {
                        var modelsContent = await modelsResponse.Content.ReadAsStringAsync();
                        var modelsInfo = JsonSerializer.Deserialize<JsonElement>(modelsContent);
                        var modelsArray = modelsInfo.GetProperty("models");
                        var models = modelsArray.EnumerateArray()
                            .Select(m => new OllamaModel
                            {
                                Name = m.GetProperty("name").GetString() ?? string.Empty,
                                Size = m.GetProperty("size").GetInt64() / (1024.0 * 1024.0 * 1024.0), // Convert bytes to GB
                                Status = "已安装",
                                IsRunning = false
                            });

                        ollamaInfo.InstalledModels = new ObservableCollection<OllamaModel>(models);
                    }
                }
            }
            catch (Exception ex)
            {
                ollamaInfo.Error = ex.Message;
                ollamaInfo.HasError = true;
            }

            return ollamaInfo;
        }

        public async Task<bool> StartOllamaAsync()
        {
            try
            {
                // 检查 Ollama 是否已经在运行
                var ollamaInfo = await CheckOllamaAsync();
                if (ollamaInfo.IsRunning)
                {
                    return true;
                }

                // 检查是否已有 ollama 进程在运行
                var existingProcesses = Process.GetProcessesByName("ollama");
                foreach (var proc in existingProcesses)
                {
                    try
                    {
                        if (!proc.HasExited)
                        {
                            proc.Kill();
                            await proc.WaitForExitAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "清理已存在的 Ollama 进程时出错");
                    }
                }

                // 启动 Ollama 进程
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = "serve",
                    UseShellExecute = false,
                    Verb = "runas", // 请求管理员权限
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                var process = Process.Start(startInfo);
                if (process == null)
                {
                    Log.Error("无法启动 Ollama 进程");
                    return false;
                }

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Log.Information("Ollama 进程输出: {Output}", e.Data);
                    }
                };

                process.BeginOutputReadLine();

                // 等待一段时间让服务启动
                await Task.Delay(2000);

                // 检查服务是否成功启动
                ollamaInfo = await CheckOllamaAsync();
                return ollamaInfo.IsRunning;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "启动 Ollama 时发生错误");
                return false;
            }
        }

        public async Task<bool> DeleteModelAsync(string modelName)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new { model = modelName }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"{_ollamaEndpoint}/api/delete"),
                    Content = content
                };

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "删除模型时发生错误");
                return false;
            }
        }

        private long ParseSize(string sizeStr)
        {
            try
            {
                sizeStr = sizeStr.Trim();
                if (string.IsNullOrEmpty(sizeStr))
                    return 0;

                // 移除可能的空格
                sizeStr = sizeStr.Replace(" ", "");

                if (sizeStr.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(sizeStr.Replace("GB", ""), out double gbSize))
                        return (long)(gbSize * 1024 * 1024 * 1024);
                }
                else if (sizeStr.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(sizeStr.Replace("MB", ""), out double mbSize))
                        return (long)(mbSize * 1024 * 1024);
                }
                else if (sizeStr.EndsWith("KB", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(sizeStr.Replace("KB", ""), out double kbSize))
                        return (long)(kbSize * 1024);
                }
                else if (double.TryParse(sizeStr, out double bytes))
                {
                    return (long)bytes;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
