// 自定义监听器类
using System;
using System.Diagnostics;

public class DebugTraceListener : TraceListener
{
    private static readonly object LockObj = new object();
    private const string BindingErrorPrefix = "[WPF-BINDING-ERROR]";
    
    public override void Write(string message)
    {
        lock (LockObj)
        {
            try
            {
                // 过滤掉一些不必要的绑定错误
                if (!IsBindingError(message))
                {
                    Console.Write($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}");
                    System.Diagnostics.Debug.Write($"[TRACE] {message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] {ex.Message}");
            }
        }
    }

    public override void WriteLine(string message)
    {
        lock (LockObj)
        {
            try
            {
                var formattedMsg = FormatMessage(message);
                if (!string.IsNullOrEmpty(formattedMsg))
                {
                    Console.WriteLine(formattedMsg);
                    System.Diagnostics.Debug.WriteLine(formattedMsg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] {ex.Message}");
            }
        }
    }

    private bool IsBindingError(string message)
    {
        return message.Contains("Cannot retrieve value using the binding") &&
               message.Contains("using default instead");
    }

    private string FormatMessage(string message)
    {
        // 过滤掉一些常见的无用绑定错误
        if (IsBindingError(message)) return null;
        
        return $"[DEBUG] {DateTime.Now:HH:mm:ss.fff} {message}";
    }
}
