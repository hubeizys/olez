// 自定义监听器类
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
public class DebugTraceListener : TraceListener
{
    public override void Write(string message) { }

    public override void WriteLine(string message)
    {
        var stack = new StackTrace(true);
        var frame = stack.GetFrame(5); // 定位到实际触发位置
        var method = frame.GetMethod();
        
        var log = $"[WPF-BINDING-ERROR][{DateTime.Now:HH:mm:ss.fff}]" +
                  $"\nMessage: {message}" +
                  $"\nSource: {frame.GetFileName()}:{frame.GetFileLineNumber()}" +
                  $"\nControl Type: {method.DeclaringType?.FullName}" +
                  $"\nVisual Tree Path:\n{GetVisualTreePath()}";
        
        System.Diagnostics.Debug.WriteLine(log);

    }

    private string GetVisualTreePath()
    {
        var sb = new StringBuilder();
        var current = Keyboard.FocusedElement as DependencyObject;
        while (current != null)
        {
            sb.Insert(0, $"{current.GetType().Name} -> ");
            current = VisualTreeHelper.GetParent(current);
        }
        return sb.ToString();
    }
}
