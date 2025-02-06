/*
 * 文件名：InstallationStep.cs
 * 创建者：yunsong
 * 创建时间：2024/03/21
 * 描述：安装步骤模型
 */

namespace ollez.Models
{
    public class InstallationStep
    {
        public string Title { get; }
        public string Description { get; }
        public string Link { get; }
        public bool IsCompleted { get; set; }

        public InstallationStep(string title, string description, string link, bool isCompleted = false)
        {
            Title = title;
            Description = description;
            Link = link;
            IsCompleted = isCompleted;
        }
    }
} 