using System;
using System.Collections.Concurrent;
using System.Reflection;
using ollez.Attributes;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ollez.Services
{
    public static class LoggerInjector
    {
        private static readonly ConcurrentDictionary<string, ILogger> _loggerCache = new ConcurrentDictionary<string, ILogger>();

        public static void InjectLoggers(object instance)
        {
            var type = instance.GetType();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<LoggerAttribute>();
                if (attribute != null)
                {
                    var logger = GetOrCreateLogger(attribute);
                    field.SetValue(instance, logger);
                }
            }
        }

        private static ILogger GetOrCreateLogger(LoggerAttribute attribute)
        {
            return _loggerCache.GetOrAdd(attribute.LogFileName, _ => CreateLogger(attribute));
        }

        private static ILogger CreateLogger(LoggerAttribute attribute)
        {
            var config = new LoggerConfiguration()
                .MinimumLevel.Is(attribute.MinimumLevel)
                .WriteTo.File(
                    $"logs/{attribute.LogFileName}.log",
                    rollingInterval: attribute.RollingInterval,
                    outputTemplate: attribute.OutputTemplate
                );

            return config.CreateLogger();
        }
    }
}