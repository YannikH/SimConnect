using System;
using System.IO;
using System.Linq;

namespace DCSBiosTRC
{
    public class GraphStorage
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

        // The one place a graph JSON gets written to disk anywhere in the app - used both for
        // user-initiated Save As (an arbitrary user-chosen path) and for GraphRunner's own
        // persisted "active graph" marker (a fixed path outside Documents/SimConnect).
        public void WriteGraphFile(string path, string json)
        {
            File.WriteAllText(path, json);
        }

        public string GetGraphPath(string name)
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
