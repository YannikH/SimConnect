using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DCSBiosTRC
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

#if DEBUG
            Process.Start(new ProcessStartInfo
            {
                FileName = "py",
                Arguments = System.IO.Path.Combine(Application.StartupPath, @"..\..\serve_web.py"),
                UseShellExecute = true,
                CreateNoWindow = false,
            });
#endif
            var director = new Director();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(director));
        }
    }
}
