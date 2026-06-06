using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrcSdkLib;

namespace DCSBiosTRC
{
    class DataLoader
    {
        string getDcsbiosPath()
        {
            return "C:/Users/yanni/Saved Games/DCS/Scripts/DCS-BIOS/doc/json";
        }
        public void loadJsons(WebView2 view)
        {
            view.CoreWebView2.ExecuteScriptAsync("window.biosConfigs = {}");
            string[] fileNames = Directory.GetFiles(getDcsbiosPath());
            foreach (string path in fileNames)
            {
                string fileName = Path.GetFileName(path);
                if (!fileName.Contains(".json")) continue;
                string aircraftName = fileName.Replace(".json", "");
                var text = File.ReadAllText(path);
                string script = $"window.dcs.onConfig('{aircraftName}', {text})";
                view.CoreWebView2.ExecuteScriptAsync(script);
            }
        }
    }
}
