using NhatDucSoftware.Models;
using NhatDucSoftware.Services;

namespace NhatDucSoftware;

public class LoginForm : Form
{
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;
    private readonly CheckBox _chkRemember;
    private readonly Button _btnLogin;
    private readonly AuthService _authService = new();
    private readonly RememberedLoginService _rememberedLoginService = new();

    public AuthenticatedUser? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        Text = "Đăng nhập";
        Width = 500;
        Height = 390;
        MinimumSize = new Size(500, 390);
        StartPosition = FormStartPosition.CenterScreen;
        UiBackgroundHelper.ApplyBackground(this);

        var lblUser = new Label { Left = 20, Top = 25, Width = 90, Text = "Tài khoản", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        var lblPass = new Label { Left = 20, Top = 65, Width = 90, Text = "Mật khẩu", Anchor = AnchorStyles.Top | AnchorStyles.Left };

        _txtUsername = new TextBox { Left = 120, Top = 20, Width = 200, Text = "", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _txtPassword = new TextBox { Left = 120, Top = 60, Width = 200, UseSystemPasswordChar = true, Text = "", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

        _chkRemember = new CheckBox
        {
            Left = 120,
            Top = 95,
            Width = 200,
            Text = "Lưu thông tin đăng nhập",
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        _btnLogin = new Button { Left = 120, Top = 130, Width = 120, Text = "Đăng nhập", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _btnLogin.Click += BtnLogin_Click;

        var lblCopyright = new Label
        {
            AutoSize = true,
            Text = "Make by Nhật Đức",
            Left = 10,
            Top = ClientSize.Height - 22,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        Controls.Add(lblUser);
        Controls.Add(lblPass);
        Controls.Add(_txtUsername);
        Controls.Add(_txtPassword);
        Controls.Add(_chkRemember);
        Controls.Add(_btnLogin);
        Controls.Add(lblCopyright);

        LoadRememberedLogin();
    }

    private void LoadRememberedLogin()
    {
        var (rememberMe, username, password) = _rememberedLoginService.Load();
        if (!rememberMe)
        {
            return;
        }

        _txtUsername.Text = username;
        _txtPassword.Text = password;
        _chkRemember.Checked = true;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text.Trim();

        var user = _authService.Login(username, password);
        if (user is null)
        {
            MessageBox.Show("Sai tài khoản hoặc mật khẩu.");
            return;
        }

        if (_chkRemember.Checked)
        {
            _rememberedLoginService.Save(username, password);
        }
        else
        {
            _rememberedLoginService.Clear();
        }

        AuthenticatedUser = user;
        DialogResult = DialogResult.OK;
        Close();
    }
}
