using Microsoft.Web.WebView2.Core;
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
        public DataLoader()
        {
            Directory.CreateDirectory(getConfigsPath());
        }
        string getDcsbiosPath()
        {
            return "C:/Users/yanni/Saved Games/DCS/Scripts/DCS-BIOS/doc/json";
        }

        public string getFileContent(string path)
        {
            return File.ReadAllText(path);
        }

        public string[] getConfigFilePaths()
        {
            return Directory.GetFiles(getConfigsPath());
        }

        public string getConfigsPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "DCS_SimConnect";
        }

        public void loadConfigJsons(WebView2 view)
        {

        }

        public void loadBiosJsons(CoreWebView2 view)
        {
            view.ExecuteScriptAsync("window.biosConfigs = {}");
            string[] fileNames = Directory.GetFiles(getDcsbiosPath());
            foreach (string path in fileNames)
            {
                string fileName = Path.GetFileName(path);
                if (!fileName.Contains(".json")) continue;
                string aircraftName = fileName.Replace(".json", "");
                var text = File.ReadAllText(path);
                string script = $"window.dcs.onBiosConfig('{aircraftName}', {text})";
                view.ExecuteScriptAsync(script);
            }
        }
    }
}
