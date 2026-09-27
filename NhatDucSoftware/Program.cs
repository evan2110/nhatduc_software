using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Services;

namespace NhatDucSoftware
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            DbContext.Configure();
            DatabaseInitializer.Initialize();

            CheckForUpdates();

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

        private static void CheckForUpdates()
        {
            try
            {
                var updateService = new UpdateCheckService();
                var updateInfo = updateService.CheckForUpdateAsync().GetAwaiter().GetResult();
                if (updateInfo is null)
                {
                    return;
                }

                using var form = new UpdateConfirmationForm(updateInfo);
                form.ShowDialog();
            }
            catch
            {
                // Không chặn khởi động ứng dụng khi không kiểm tra được phiên bản.
            }
        }
    }
}