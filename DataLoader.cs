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
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return $"{userProfile}/Saved Games/DCS/Scripts/DCS-BIOS/doc/json";
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
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/SimConnect";
        }

        public void loadConfigJsons(WebView2 view)
        {

        }

        public void loadBiosJsons(CoreWebView2 view)
        {
            view.ExecuteScriptAsync("window.biosConfigs = {}");
            string[] folderFileNames = Directory.GetFiles(getDcsbiosPath());
            var configFileNames = folderFileNames.Where(f => f.Contains(".json"));
            foreach (string path in configFileNames)
            {
                string fileName = Path.GetFileName(path);
                string aircraftName = fileName.Replace(".json", "");
                var text = File.ReadAllText(path);
                string script = $"window.dcs.onBiosConfig('{aircraftName}', {text})";
                view.ExecuteScriptAsync(script);
            }
        }
    }
}
