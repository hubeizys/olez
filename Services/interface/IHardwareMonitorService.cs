using ollez.Models;
using System.Threading.Tasks;

namespace ollez.Services
{
    public interface IHardwareMonitorService
    {
        Task<HardwareInfo> GetHardwareInfoAsync();
        void StartMonitoring();
        void StopMonitoring();
    }
}
