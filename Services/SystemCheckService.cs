using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    info.IsAvailable = true;

                    // 解析第一行获取版本信息
                    var lines = output.Split('\n');
                    if (lines.Length > 0)
                    {
                        var firstLine = lines[0].Trim();
                        
                        // 解析 NVIDIA-SMI 版本
                        var smiVersionMatch = System.Text.RegularExpressions.Regex.Match(firstLine, @"NVIDIA-SMI\s+(\d+\.\d+)");
                        if (smiVersionMatch.Success)
                        {
                            info.SmiVersion = smiVersionMatch.Groups[1].Value;
                        }

                        // 解析驱动版本
                        var driverVersionMatch = System.Text.RegularExpressions.Regex.Match(firstLine, @"Driver Version:\s+(\d+\.\d+)");
                        if (driverVersionMatch.Success)
                        {
                            info.DriverVersion = driverVersionMatch.Groups[1].Value;
                        }

                        // 解析CUDA版本
                        var cudaVersionMatch = System.Text.RegularExpressions.Regex.Match(firstLine, @"CUDA Version:\s+(\d+\.\d+)");
                        if (cudaVersionMatch.Success)
                        {
                            info.Version = cudaVersionMatch.Groups[1].Value;
                        }
                    }
                }
            }
            catch
            {
                info.IsAvailable = false;
            }
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