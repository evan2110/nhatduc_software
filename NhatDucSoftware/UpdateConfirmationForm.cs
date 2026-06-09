using NhatDucSoftware.Services;

namespace NhatDucSoftware;

public sealed class UpdateConfirmationForm : Form
{
    private readonly UpdateInfo _updateInfo;
    private readonly UpdateCheckService _updateService = new();
    private readonly Label _lblStatus;
    private readonly ProgressBar _progressBar;
    private readonly Button _btnUpdate;
    private readonly Button _btnSkip;

    public UpdateConfirmationForm(UpdateInfo updateInfo)
    {
        _updateInfo = updateInfo;

        Text = "Cập nhật phần mềm";
        Width = 560;
        Height = 480;
        MinimumSize = new Size(560, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        UiBackgroundHelper.ApplyBackground(this);

        var lblTitle = new Label
        {
            AutoSize = false,
            Left = 20,
            Top = 20,
            Width = 500,
            Height = 24,
            Text = "Đã có phiên bản mới",
            Font = new Font(Font.FontFamily, 11F, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var lblCurrent = new Label
        {
            AutoSize = true,
            Left = 20,
            Top = 55,
            Text = $"Phiên bản hiện tại: {_updateInfo.CurrentVersion}"
        };

        var lblLatest = new Label
        {
            AutoSize = true,
            Left = 20,
            Top = 80,
            Text = $"Phiên bản mới: {_updateInfo.LatestVersion} — {_updateInfo.ReleaseTitle}"
        };

        var lblNotes = new Label
        {
            AutoSize = true,
            Left = 20,
            Top = 115,
            Text = "Nội dung cập nhật:"
        };

        var txtNotes = new TextBox
        {
            Left = 20,
            Top = 140,
            Width = 500,
            Height = 200,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = string.IsNullOrWhiteSpace(_updateInfo.ReleaseNotes)
                ? "Không có ghi chú phát hành."
                : _updateInfo.ReleaseNotes,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        _progressBar = new ProgressBar
        {
            Left = 20,
            Top = 350,
            Width = 500,
            Height = 18,
            Visible = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        _lblStatus = new Label
        {
            AutoSize = true,
            Left = 20,
            Top = 375,
            Text = string.Empty,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        _btnUpdate = new Button
        {
            Left = 320,
            Top = 400,
            Width = 95,
            Height = 32,
            Text = "Cập nhật",
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        _btnUpdate.Click += BtnUpdate_Click;

        _btnSkip = new Button
        {
            Left = 425,
            Top = 400,
            Width = 95,
            Height = 32,
            Text = "Bỏ qua",
            DialogResult = DialogResult.Cancel,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };

        var lblCopyright = new Label
        {
            AutoSize = true,
            Text = "Make by Nhật Đức",
            Left = 10,
            Top = ClientSize.Height - 22,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        CancelButton = _btnSkip;
        AcceptButton = _btnUpdate;

        Controls.Add(lblTitle);
        Controls.Add(lblCurrent);
        Controls.Add(lblLatest);
        Controls.Add(lblNotes);
        Controls.Add(txtNotes);
        Controls.Add(_progressBar);
        Controls.Add(_lblStatus);
        Controls.Add(_btnUpdate);
        Controls.Add(_btnSkip);
        Controls.Add(lblCopyright);
    }

    private async void BtnUpdate_Click(object? sender, EventArgs e)
    {
        _btnUpdate.Enabled = false;
        _btnSkip.Enabled = false;
        _progressBar.Visible = true;
        _progressBar.Value = 0;
        _lblStatus.Text = "Đang tải bản cập nhật...";

        try
        {
            var progress = new Progress<int>(percent =>
            {
                _progressBar.Value = Math.Clamp(percent, 0, 100);
                _lblStatus.Text = percent < 100
                    ? "Đang tải bản cập nhật..."
                    : "Đang giải nén và cài đặt bản cập nhật...";
            });

            var downloadedPath = await _updateService.DownloadUpdateAsync(_updateInfo, progress);
            await Task.Run(() => new UpdateApplyService().ApplyUpdateAndRestart(downloadedPath));

            Application.Exit();
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể tải bản cập nhật: {ex.Message}",
                "Lỗi cập nhật",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _btnUpdate.Enabled = true;
            _btnSkip.Enabled = true;
            _progressBar.Visible = false;
            _lblStatus.Text = string.Empty;
        }
    }
}
