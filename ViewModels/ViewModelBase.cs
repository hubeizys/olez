using ollez.Attributes;
using Prism.Mvvm;
using System;

namespace ollez.ViewModels
{
    public abstract class ViewModelBase : BindableBase
    {


        protected ViewModelBase()
        {
            // 在构造函数中自动注入日志
            Services.LoggerInjector.InjectLoggers(this);
        }
    }
}

