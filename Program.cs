using System;
using System.Windows.Forms;

// ReSharper disable HeuristicUnreachableCode
#pragma warning disable CS0162

namespace CMI
{
    internal static class Program
    {
        private const bool isUsingTestMode = true;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                if (isUsingTestMode)
                {
                    CMI.modSoundFolderPath = "D:\\SteamLibrary\\steamapps\\common\\ELDEN RING\\Game\\modengine2\\mod\\sound";
                    CMI.soundJsonFilePath = $"{CMI.appRootPath}\\sound.json";
                }
                else
                {
                    CMI.modSoundFolderPath = $"{args[0]}\\sound";
                    CMI.soundJsonFilePath = $"{CMI.modSoundFolderPath}\\sound.json";
                }
            }
            catch
            {
                Environment.Exit(0);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CMI());
        }
    }
}