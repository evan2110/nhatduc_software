using NhatDucSoftware.Core.Data;
using NhatDucSoftware.Core.Models;
using NhatDucSoftware.Core.Services;
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

            var authService = new AuthService();
            var rememberedLoginService = new RememberedLoginService();
            var allowAutoLogin = true;

            while (true)
            {
                AuthenticatedUser? user = null;
                if (allowAutoLogin)
                {
                    user = TryAutoLogin(authService, rememberedLoginService);
                    allowAutoLogin = false;
                }

                if (user is null)
                {
                    using var loginForm = new LoginForm();
                    if (loginForm.ShowDialog() != DialogResult.OK || loginForm.AuthenticatedUser is null)
                    {
                        break;
                    }

                    user = loginForm.AuthenticatedUser;
                }

                using var mainForm = new Form1(user);
                Application.Run(mainForm);

                if (!mainForm.RequestLogout)
                {
                    break;
                }
            }
        }

        private static AuthenticatedUser? TryAutoLogin(AuthService authService, RememberedLoginService rememberedLoginService)
        {
            var (rememberMe, username, password) = rememberedLoginService.Load();
            if (!rememberMe)
            {
                return null;
            }

            var user = authService.Login(username, password);
            if (user is null)
            {
                rememberedLoginService.Clear();
            }

            return user;
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