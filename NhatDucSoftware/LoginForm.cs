using System.Drawing;
using System.Reflection;
using NhatDucSoftware.Models;
using NhatDucSoftware.Services;

namespace NhatDucSoftware;

public class LoginForm : Form
{
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;
    private readonly Button _btnLogin;
    private readonly AuthService _authService = new();

    public AuthenticatedUser? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        Text = "Đăng nhập";
        Width = 500;
        Height = 360;
        MinimumSize = new Size(500, 360);
        StartPosition = FormStartPosition.CenterScreen;
        UiBackgroundHelper.ApplyBackground(this);

        var lblUser = new Label { Left = 20, Top = 25, Width = 90, Text = "Tài khoản", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        var lblPass = new Label { Left = 20, Top = 65, Width = 90, Text = "Mật khẩu", Anchor = AnchorStyles.Top | AnchorStyles.Left };

        _txtUsername = new TextBox { Left = 120, Top = 20, Width = 200, Text = "admin", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _txtPassword = new TextBox { Left = 120, Top = 60, Width = 200, UseSystemPasswordChar = true, Text = "123456", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        _btnLogin = new Button { Left = 120, Top = 105, Width = 120, Text = "Đăng nhập", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _btnLogin.Click += BtnLogin_Click;

        var lblCopyright = new Label
        {
            AutoSize = true,
            Text = "Make by Nhật Đức",
            Left = 10,
            Top = ClientSize.Height - 22,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
        };

        var lblVersion = new Label
        {
            AutoSize = true,
            Text = $"Phiên bản {GetDisplayVersion()}",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 8.25f),
        };

        Controls.Add(lblUser);
        Controls.Add(lblPass);
        Controls.Add(_txtUsername);
        Controls.Add(_txtPassword);
        Controls.Add(_btnLogin);
        Controls.Add(lblCopyright);
        Controls.Add(lblVersion);

        lblVersion.Location = new Point(ClientSize.Width - lblVersion.Width - 12, ClientSize.Height - lblVersion.Height - 10);
    }

    private static string GetDisplayVersion()
    {
        string path = Path.Combine(AppPaths.InstallDirectory, "version.txt");
        if (File.Exists(path))
        {
            string v = File.ReadAllText(path).Trim();
            if (!string.IsNullOrEmpty(v))
                return v;
        }

        Version? asm = Assembly.GetExecutingAssembly().GetName().Version;
        return asm is null ? "1.0.0" : $"{asm.Major}.{asm.Minor}.{asm.Build}";
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        var user = _authService.Login(_txtUsername.Text.Trim(), _txtPassword.Text.Trim());
        if (user is null)
        {
            MessageBox.Show("Sai tài khoản hoặc mật khẩu.");
            return;
        }

        AuthenticatedUser = user;
        DialogResult = DialogResult.OK;
        Close();
    }
}
