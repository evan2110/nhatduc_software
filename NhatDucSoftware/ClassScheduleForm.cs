using NhatDucSoftware.Core.Models;
using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware;

public class ClassScheduleForm : Form
{
    private readonly ClassScheduleService _scheduleService = new();
    private readonly int _classId;
    private readonly string _className;
    private readonly DataGridView _dgvSchedule;
    private readonly DateTimePicker _dtpWeek;
    private readonly Button _btnLoad;
    private readonly Button _btnSave;

    public ClassScheduleForm(int classId, string className)
    {
        _classId = classId;
        _className = className;

        Text = $"Lịch học - {className}";
        Width = 750;
        Height = 420;
        MinimumSize = new Size(750, 420);
        StartPosition = FormStartPosition.CenterParent;
        UiBackgroundHelper.ApplyBackground(this);

        var lblWeek = new Label { Left = 10, Top = 15, Width = 60, Text = "Tuần:", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _dtpWeek = new DateTimePicker { Left = 75, Top = 12, Width = 150, Format = DateTimePickerFormat.Short, Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _dtpWeek.Value = ClassWeeklySchedule.GetMondayOfWeek(DateTime.Today);

        _btnLoad = new Button { Left = 235, Top = 11, Width = 80, Height = 25, Text = "Xem", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _btnLoad.Click += (_, _) => LoadSchedule();

        _btnSave = new Button { Left = 325, Top = 11, Width = 120, Height = 25, Text = "Lưu lịch tuần này", Anchor = AnchorStyles.Top | AnchorStyles.Left };
        _btnSave.Click += BtnSave_Click;

        _dgvSchedule = new DataGridView
        {
            Left = 10,
            Top = 45,
            Width = 710,
            Height = 320,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        var lblCopyright = new Label
        {
            AutoSize = true,
            Text = "Make by Nhật Đức",
            Left = 10,
            Top = ClientSize.Height - 22,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };

        Controls.AddRange(new Control[] { lblWeek, _dtpWeek, _btnLoad, _btnSave, _dgvSchedule, lblCopyright });

        Load += (_, _) => LoadSchedule();
    }

    private void LoadSchedule()
    {
        var monday = ClassWeeklySchedule.GetMondayOfWeek(_dtpWeek.Value);
        var schedule = _scheduleService.GetScheduleForWeek(_classId, monday);

        // Build grid: rows = days (Mon-Sun), columns = 5 shifts
        _dgvSchedule.Columns.Clear();
        _dgvSchedule.Rows.Clear();

        _dgvSchedule.Columns.Add("Day", "Ngày");
        _dgvSchedule.Columns[0].Width = 100;
        _dgvSchedule.Columns[0].ReadOnly = true;

        for (int s = 1; s <= 5; s++)
        {
            var col = new DataGridViewCheckBoxColumn
            {
                Name = $"Shift{s}",
                HeaderText = TeacherTimesheet.GetShiftDescription(s),
                Width = 115
            };
            _dgvSchedule.Columns.Add(col);
        }

        for (int d = 0; d < 7; d++)
        {
            var rowIdx = _dgvSchedule.Rows.Add();
            var row = _dgvSchedule.Rows[rowIdx];
            row.Cells[0].Value = ClassWeeklySchedule.GetDayName(d);
            for (int s = 1; s <= 5; s++)
            {
                bool hasShift = schedule.Any(x => x.DayOfWeek == d && x.ShiftNumber == s);
                row.Cells[s].Value = hasShift;
            }
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var monday = ClassWeeklySchedule.GetMondayOfWeek(_dtpWeek.Value);
        var entries = new List<(int DayOfWeek, int ShiftNumber)>();

        for (int d = 0; d < 7; d++)
        {
            for (int s = 1; s <= 5; s++)
            {
                var val = _dgvSchedule.Rows[d].Cells[s].Value;
                if (val is true)
                {
                    entries.Add((d, s));
                }
            }
        }

        _scheduleService.SaveScheduleForWeek(_classId, monday, entries);
        MessageBox.Show("Đã lưu lịch học!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
