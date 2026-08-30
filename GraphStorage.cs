using System;
using System.IO;
using System.Linq;

namespace DCSBiosTRC
{
    class GraphStorage
    {
        public string GetGraphsPath()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SimConnect");
            Directory.CreateDirectory(path);
            return path;
        }

        public string[] GetGraphNames()
        {
            return Directory.GetFiles(GetGraphsPath(), "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name)
                .ToArray();
        }

        public string LoadGraph(string name)
        {
            return File.ReadAllText(GetGraphPath(name));
        }

        private string GetGraphPath(string name)
        {
            return Path.Combine(GetGraphsPath(), SanitizeName(name) + ".json");
        }

        private string SanitizeName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
