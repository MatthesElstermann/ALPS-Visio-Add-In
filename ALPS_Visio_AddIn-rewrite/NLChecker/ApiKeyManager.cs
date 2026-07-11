using System;
using System.IO;
using System.Windows.Forms;

namespace ALPS_Visio_AddIn_rewrite.NLChecker
{
    /// <summary>
    /// Stores and retrieves the LLM API key. The original add-in used VSTO user settings; here the key
    /// is kept in a small file under %APPDATA%\ALPS_Visio_AddIn to avoid the (fragile) settings/config
    /// plumbing and stay self-contained.
    /// </summary>
    public static class ApiKeyManager
    {
        private static string KeyFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ALPS_Visio_AddIn", "llm_api_key.txt");

        public static string GetApiKey()
        {
            try
            {
                return File.Exists(KeyFilePath) ? File.ReadAllText(KeyFilePath).Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>Shows the dialog and persists a non-empty key. Returns true if the key was updated.</summary>
        public static bool UpdateApiKey()
        {
            using (var dialog = new ApiKeyDialog(GetApiKey()))
            {
                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.ApiKey))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(KeyFilePath));
                    File.WriteAllText(KeyFilePath, dialog.ApiKey.Trim());
                    return true;
                }
            }
            return false;
        }

        public static void ClearApiKey()
        {
            try
            {
                if (File.Exists(KeyFilePath)) File.Delete(KeyFilePath);
            }
            catch
            {
                // ignore
            }
        }
    }
}
