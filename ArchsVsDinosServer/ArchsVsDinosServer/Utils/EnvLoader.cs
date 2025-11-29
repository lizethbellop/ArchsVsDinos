using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public static class EnvLoader
    {

        public static Dictionary<string, string> LoadEnv(string fileName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            var values = new Dictionary<string, string>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Environment file not found: {filePath}");
            }

            foreach (string line in File.ReadAllLines(filePath))
            {
                if (line.Contains("="))
                {
                    string[] parts = line.Split('=');
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    values[key] = value;
                }
            }

            return values;
        }

    }
}
