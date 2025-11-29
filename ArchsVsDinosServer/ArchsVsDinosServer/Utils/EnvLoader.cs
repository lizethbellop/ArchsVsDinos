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
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(basePath, fileName);

            if (File.Exists(filePath))
                return ParseEnvFile(filePath);

            string utilsPath = Path.Combine(basePath, @"..\..\..\ArchsVsDinosServer\Utils", fileName);
            utilsPath = Path.GetFullPath(utilsPath);

            if (File.Exists(utilsPath))
                return ParseEnvFile(utilsPath);

            throw new FileNotFoundException($"Environment file not found in: \n{filePath}\n{utilsPath}");
        }

        private static Dictionary<string, string> ParseEnvFile(string path)
        {
            var values = new Dictionary<string, string>();

            foreach (string line in File.ReadAllLines(path))
            {
                if (line.Contains("="))
                {
                    string[] parts = line.Split('=');
                    values[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return values;
        }


    }
}
