using System;
using System.IO;
using TPU_Assembly.Class;

namespace Lighting_Params
{
    public class FileRW
    {
        protected string ReadValue(string _model, string section, string key, string defaultValue)
        {
            string filePath = string.IsNullOrEmpty(_model) ? "LightingParam.ini" : _model;

            if (!File.Exists(filePath))
            {
                return defaultValue;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                bool inSection = false;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        string currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                        inSection = currentSection.Equals(section, StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (inSection && trimmedLine.Contains('='))
                    {
                        int equalsIndex = trimmedLine.IndexOf('=');
                        string currentKey = trimmedLine.Substring(0, equalsIndex).Trim();

                        if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            return trimmedLine.Substring(equalsIndex + 1).Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MSystem.InsertAndSaveLogs($"[FileRW Error] Lỗi đọc file INI: {ex.Message}", Color.Red);
            }

            return defaultValue;
        }
    }
}