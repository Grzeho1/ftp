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
                    var parts = line.Split('|');      // Zatím znak | protože kodování hesla používá = a dělá to bordel.
                    if (parts.Length == 2)
                    {
                        config[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            // Kontrola jestli je heslo vyplněno, pokud ne tak je potřeba vyplnit heslo.
            if (!config.ContainsKey("sqlPassword") || string.IsNullOrWhiteSpace(config["sqlPassword"]))

            {
                Console.WriteLine("Zadejte heslo do databáze:");
                string password = Console.ReadLine();
                config["sqlPassword"] = EncodeBase64(password);
                SaveConfig(filePath, config);

                Environment.Exit(0);

            }
            else
            {
                config["sqlPassword"] = DecodeBase64(config["sqlPassword"]);
            }

            return config;
        }


        // Zakoduje heslo
        private static string EncodeBase64(string plainText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);


            return Convert.ToBase64String(bytes);
        }

        // Dekoduje heslo
        private static string DecodeBase64(string base64Encoded)
        {
            byte[] bytes = Convert.FromBase64String(base64Encoded);
            return Encoding.UTF8.GetString(bytes);
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
