using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ftp;

namespace ftp
{
    public static class ConfigLoader
    {

        public static Dictionary<string, string> LoadConfig(string filePath)
        {
            var config = new Dictionary<string, string>();

            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    // Ignorujeme prázdné řádky a komentáře
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    // Rozdělení na klíč a hodnotu
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        config[parts[0].Trim()] = parts[1].Trim();
                      //  Console.WriteLine($"Načteno: Klíč: {parts[0].Trim()} = {parts[1].Trim()}"); // Logování 
                    }
                    else
                    {
                        Console.WriteLine($"Chybný formát řádku: {line}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Soubor konfigurace nebyl nalezen: " + filePath);
            }

            // Výpis načtených klíčů a hodnot
           

            return config;
        }



        private static void SaveConfig(string filePath, Dictionary<string, string> config)
        {
            // Načtu soubor a jeho řádky
            var lines = new List<string>(File.Exists(filePath) ? File.ReadAllLines(filePath) : Array.Empty<string>());
            bool passwordUpdated = false;

            // Procházení řádky a hledám sqlPassword
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("sqlPassword"))
                {
                    // Změním heslo pokud existuje klíč
                    lines[i] = $"sqlPassword|{config["sqlPassword"]}";
                    passwordUpdated = true;
                    break;
                }
            }

            // Pokud ne přidám řádek
            if (!passwordUpdated)
            {
                lines.Add($"sqlPassword|{config["sqlPassword"]}");
            }

            
            File.WriteAllLines(filePath, lines);
        }
    }
}
