using System;
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
                    Log.Information("CUDA 可用，完整输出:\n{Output}", output);

                    var lines = output.Split('\n');
                    if (lines.Length > 0)
                    {
                        // 获取包含版本信息的行
                        var versionLine = lines.FirstOrDefault(l => l.Contains("NVIDIA-SMI") && l.Contains("Driver Version") && l.Contains("CUDA Version"));
                        if (versionLine != null)
                        {
                            Log.Information("找到版本信息行: {Line}", versionLine);

                            // 解析 NVIDIA-SMI 版本
                            var smiMatch = System.Text.RegularExpressions.Regex.Match(versionLine, @"NVIDIA-SMI\s+([\d.]+)");
                            if (smiMatch.Success)
                            {
                                info.SmiVersion = smiMatch.Groups[1].Value;
                                Log.Information("NVIDIA-SMI 版本: {Version}", info.SmiVersion);
                            }
                            else
                            {
                                Log.Warning("无法解析 NVIDIA-SMI 版本");
                            }

                            // 解析驱动版本
                            var driverMatch = System.Text.RegularExpressions.Regex.Match(versionLine, @"Driver Version:\s*([\d.]+)");
                            if (driverMatch.Success)
                            {
                                info.DriverVersion = driverMatch.Groups[1].Value;
                                Log.Information("驱动版本: {Version}", info.DriverVersion);
                            }
                            else
                            {
                                Log.Warning("无法解析驱动版本");
                            }

                            // 解析CUDA版本
                            var cudaMatch = System.Text.RegularExpressions.Regex.Match(versionLine, @"CUDA Version:\s*([\d.]+)");
                            if (cudaMatch.Success)
                            {
                                info.Version = cudaMatch.Groups[1].Value;
                                Log.Information("CUDA版本: {Version}", info.Version);
                            }
                            else
                            {
                                Log.Warning("无法解析CUDA版本");
                            }
                        }
                        else
                        {
                            Log.Warning("未找到包含版本信息的行");
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