using System;
using Serilog.Events;
using Serilog;

namespace ollez.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LoggerAttribute : Attribute
    {
        public string LogFileName { get; }
        public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;
        public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;
        public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        public LoggerAttribute(string logFileName)
        {
            LogFileName = logFileName;
        }
    }
} 