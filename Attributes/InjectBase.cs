using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ollez.Attributes
{
    public class InjectBase
    {
        public InjectBase()
        {   
            Services.LoggerInjector.InjectLoggers(this);
        }
    }
}
