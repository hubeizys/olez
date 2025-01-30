using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace YourNamespace.Services
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
                        Arguments = "--query-gpu=driver_version,cuda_version --format=csv,noheader",
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
                    var parts = output.Trim().Split(',');
                    info.IsAvailable = true;
                    info.DriverVersion = parts[0].Trim();
                    info.Version = parts[1].Trim();
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