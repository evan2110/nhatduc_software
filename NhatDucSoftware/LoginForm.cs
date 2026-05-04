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
        Width = 360;
        Height = 220;
        StartPosition = FormStartPosition.CenterScreen;

        var lblUser = new Label { Left = 20, Top = 25, Width = 90, Text = "Tài khoản" };
        var lblPass = new Label { Left = 20, Top = 65, Width = 90, Text = "Mật khẩu" };

        _txtUsername = new TextBox { Left = 120, Top = 20, Width = 200, Text = "admin" };
        _txtPassword = new TextBox { Left = 120, Top = 60, Width = 200, UseSystemPasswordChar = true, Text = "123456" };

        _btnLogin = new Button { Left = 120, Top = 105, Width = 120, Text = "Đăng nhập" };
        _btnLogin.Click += BtnLogin_Click;

        Controls.Add(lblUser);
        Controls.Add(lblPass);
        Controls.Add(_txtUsername);
        Controls.Add(_txtPassword);
        Controls.Add(_btnLogin);
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
