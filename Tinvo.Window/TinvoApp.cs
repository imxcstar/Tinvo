using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormedge;

namespace Tinvo
{
    public class TinvoApp : AppStartup
    {
        protected override AppCreationAction? OnApplicationStartup(StartupSettings options)
        {
            return options.UseMainWindow(new MainWindow());
        }
    }
}
