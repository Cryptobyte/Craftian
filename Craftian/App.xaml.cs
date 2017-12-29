using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Craftian
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            Console.WriteLine(@"Starting ..");

            int argC = 0;
            foreach (string arg in e.Args)
            {
                Console.WriteLine(@"	Arg: {0} {1}", argC, arg);
                argC++;
            }
        }
    }
}
