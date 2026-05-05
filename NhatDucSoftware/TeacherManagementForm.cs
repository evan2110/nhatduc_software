using NhatDucSoftware.Models;
using NhatDucSoftware.Services;

namespace NhatDucSoftware;

public class TeacherManagementForm : Form
{
    private readonly TeacherService _teacherService = new();

    private readonly DataGridView _dgvTeachers = new();
    private readonly TextBox _txtName = new();
    private readonly TextBox _txtPhone = new();
    private readonly TextBox _txtEmail = new();
    private readonly ComboBox _cmbStatus = new();

    private List<Teacher> _teachers = new();

    public TeacherManagementForm()
    {
        Text = "Quản lý giáo viên";
        Width = 980;
        Height = 520;
        MinimumSize = new Size(980, 520);
        StartPosition = FormStartPosition.CenterParent;
        UiBackgroundHelper.ApplyBackground(this);

        _dgvTeachers.Location = new Point(10, 10);
        _dgvTeachers.Size = new Size(600, 450);
        _dgvTeachers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _dgvTeachers.SelectionChanged += DgvTeachers_SelectionChanged;

        var lblName = new Label { Left = 630, Top = 20, Width = 150, Text = "Họ và tên", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtName.SetBounds(630, 40, 300, 23);
        _txtName.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var lblPhone = new Label { Left = 630, Top = 75, Width = 150, Text = "Số điện thoại", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtPhone.SetBounds(630, 95, 300, 23);
        _txtPhone.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var lblEmail = new Label { Left = 630, Top = 130, Width = 150, Text = "Email", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtEmail.SetBounds(630, 150, 300, 23);
        _txtEmail.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var lblStatus = new Label { Left = 630, Top = 185, Width = 150, Text = "Trạng thái", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbStatus.SetBounds(630, 205, 300, 23);
        _cmbStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _cmbStatus.Items.AddRange(new object[] { "Active", "Inactive" });
        _cmbStatus.Text = "Active";

        var btnAdd = new Button { Left = 630, Top = 245, Width = 95, Height = 30, Text = "Thêm", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        btnAdd.Click += BtnAdd_Click;

        var btnUpdate = new Button { Left = 733, Top = 245, Width = 95, Height = 30, Text = "Sửa", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        btnUpdate.Click += BtnUpdate_Click;

        var btnDelete = new Button { Left = 835, Top = 245, Width = 95, Height = 30, Text = "Xóa", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        btnDelete.Click += BtnDelete_Click;

        var lblCopyright = new Label
        {
            AutoSize = true,
            Text = "Make by Nhật Đức",
            Left = 10,
            Top = ClientSize.Height - 22,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        Controls.AddRange(new Control[]
        {
            _dgvTeachers,
            lblName, _txtName,
            lblPhone, _txtPhone,
            lblEmail, _txtEmail,
            lblStatus, _cmbStatus,
            btnAdd, btnUpdate, btnDelete,
            lblCopyright
        });

        Load += TeacherManagementForm_Load;
    }

    private void TeacherManagementForm_Load(object? sender, EventArgs e)
    {
        LoadTeachers();
    }

    private void LoadTeachers()
    {
        _teachers = _teacherService.GetAll();
        _dgvTeachers.DataSource = null;
        _dgvTeachers.DataSource = _teachers;

        if (_dgvTeachers.Columns[nameof(Teacher.Id)] is not null) _dgvTeachers.Columns[nameof(Teacher.Id)].HeaderText = "Mã GV";
        if (_dgvTeachers.Columns[nameof(Teacher.FullName)] is not null) _dgvTeachers.Columns[nameof(Teacher.FullName)].HeaderText = "Họ và tên";
        if (_dgvTeachers.Columns[nameof(Teacher.Phone)] is not null) _dgvTeachers.Columns[nameof(Teacher.Phone)].HeaderText = "Số điện thoại";
        if (_dgvTeachers.Columns[nameof(Teacher.Email)] is not null) _dgvTeachers.Columns[nameof(Teacher.Email)].HeaderText = "Email";
        if (_dgvTeachers.Columns[nameof(Teacher.Status)] is not null) _dgvTeachers.Columns[nameof(Teacher.Status)].HeaderText = "Trạng thái";
    }

    private void DgvTeachers_SelectionChanged(object? sender, EventArgs e)
    {
        if (_dgvTeachers.CurrentRow?.DataBoundItem is not Teacher t)
        {
            return;
        }

        _txtName.Text = t.FullName;
        _txtPhone.Text = t.Phone;
        _txtEmail.Text = t.Email;
        _cmbStatus.Text = t.Status;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        _teacherService.Add(new Teacher
        {
            FullName = _txtName.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Email = _txtEmail.Text.Trim(),
            Status = _cmbStatus.Text
        });
        LoadTeachers();
    }

    private void BtnUpdate_Click(object? sender, EventArgs e)
    {
        if (_dgvTeachers.CurrentRow?.DataBoundItem is not Teacher t)
        {
            return;
        }

        t.FullName = _txtName.Text.Trim();
        t.Phone = _txtPhone.Text.Trim();
        t.Email = _txtEmail.Text.Trim();
        t.Status = _cmbStatus.Text;

        _teacherService.Update(t);
        LoadTeachers();
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_dgvTeachers.CurrentRow?.DataBoundItem is not Teacher t)
        {
            return;
        }

        try
        {
            _teacherService.Delete(t.Id);
            LoadTeachers();
        }
        catch
        {
            MessageBox.Show("Không thể xóa giáo viên đang được sử dụng trong hệ thống.");
        }
    }
}
