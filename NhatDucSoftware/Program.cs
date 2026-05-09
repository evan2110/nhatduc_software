using AutoUpdaterDotNET;
using NhatDucSoftware.Data;
using NhatDucSoftware.Services;

namespace NhatDucSoftware
{
    internal static class Program
    {
        private const string AppCastUrl = "https://raw.githubusercontent.com/evan2110/nhatduc_software/master/appcast.xml";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            DatabaseInitializer.Initialize();

            ConfigureAutoUpdater();

            while (true)
            {
                using var loginForm = new LoginForm();
                if (loginForm.ShowDialog() != DialogResult.OK || loginForm.AuthenticatedUser is null)
                {
                    break;
                }

                using var mainForm = new Form1(loginForm.AuthenticatedUser);
                Application.Run(mainForm);

                if (!mainForm.RequestLogout)
                {
                    break;
                }
            }
        }

        private static void ConfigureAutoUpdater()
        {
            AutoUpdater.AppTitle = "NhatDucSoftware";
            AutoUpdater.ShowSkipButton = true;
            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.Start(AppCastUrl);
        }
    }
}