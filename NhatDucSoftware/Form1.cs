using System.Drawing.Drawing2D;
using System.Globalization;
using NhatDucSoftware.Models;
using NhatDucSoftware.Services;

namespace NhatDucSoftware
{
    public partial class Form1 : Form
    {
        private static readonly NumberFormatInfo MoneyNumberFormat = new()
        {
            NumberDecimalDigits = 0,
            NumberGroupSeparator = " "
        };

        private readonly AuthenticatedUser _currentUser;
        private readonly StudentService _studentService = new();
        private readonly CourseService _courseService = new();
        private readonly ClassService _classService = new();
        private readonly TeacherService _teacherService = new();
        private readonly PaymentService _paymentService = new();
        private readonly AttendanceService _attendanceService = new();
        private readonly EvaluationService _evaluationService = new();
        private readonly ReportService _reportService = new();
        private readonly TeacherTimesheetService _timesheetService = new();
        private readonly ClassScheduleService _classScheduleService = new();
        private readonly ExcelExportService _excelExportService = new();

        private List<Student> _students = new();
        private List<Course> _courses = new();
        private List<ClassInfo> _classes = new();
        private List<Teacher> _teachers = new();
        private List<RevenueByYearStat> _revenueByYear = new();
        private List<RevenueByMonthStat> _revenueByMonth = new();
        private bool _isBindingPaymentStudents;

        public bool RequestLogout { get; private set; }

        public Form1(AuthenticatedUser user)
        {
            _currentUser = user;
            InitializeComponent();
            InitializeMoneyFormatting();
            Text = $"Nhat Duc Software - {_currentUser.Role}: {_currentUser.Username}";
            UiBackgroundHelper.ApplyBackground(this);
            AddCopyrightLabel();
        }

        private static string FormatCurrency(decimal amount)
        {
            return $"{amount.ToString("N0", MoneyNumberFormat)}đ";
        }

        private static string FormatMoneyInput(decimal amount)
        {
            return amount.ToString("N0", MoneyNumberFormat);
        }

        private static bool TryParseMoney(string? text, out decimal amount)
        {
            var normalized = (text ?? string.Empty)
                .Replace("đ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty)
                .Trim();

            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        private void InitializeMoneyFormatting()
        {
            WireMoneyInput(txtCourseFee);
            WireMoneyInput(txtPaymentAmount);

            dgvCourses.CellFormatting += (_, e) => FormatMoneyCell(dgvCourses, e, nameof(Course.TuitionFee));
            dgvRevenueByYear.CellFormatting += (_, e) => FormatMoneyCell(dgvRevenueByYear, e, nameof(RevenueByYearStat.TotalRevenue), nameof(RevenueByMonthStat.TotalRevenue));
            dgvAttendanceDetail.CellFormatting += (_, e) => FormatMoneyCell(dgvAttendanceDetail, e, "SoTien");
        }

        private static void WireMoneyInput(TextBox textBox)
        {
            textBox.Leave += (_, _) =>
            {
                if (TryParseMoney(textBox.Text, out var amount))
                {
                    textBox.Text = FormatMoneyInput(amount);
                }
            };
        }

        private static void FormatMoneyCell(DataGridView grid, DataGridViewCellFormattingEventArgs e, params string[] propertyNames)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var column = grid.Columns[e.ColumnIndex];
            if (!propertyNames.Contains(column.DataPropertyName) && !propertyNames.Contains(column.Name))
            {
                return;
            }

            if (e.Value is decimal decimalValue)
            {
                e.Value = FormatCurrency(decimalValue);
                e.FormattingApplied = true;
                return;
            }

            if (e.Value is not null && TryParseMoney(e.Value.ToString(), out var parsedValue))
            {
                e.Value = FormatCurrency(parsedValue);
                e.FormattingApplied = true;
            }
        }

        private void AddCopyrightLabel()
        {
            var lblCopyright = new Label
            {
                AutoSize = true,
                Text = "Make by Nhật Đức",
                Location = new Point(10, ClientSize.Height - 22),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };
            Controls.Add(lblCopyright);
            lblCopyright.BringToFront();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (_currentUser.Role == "Teacher")
            {
                tabAdmin.Parent = null;
                dtpTeacherWeek.ValueChanged += (_, _) => LoadTeacherWeeklySchedule();
                btnLoadTeacherSchedule.Visible = false;

                btnSaveAttendance.Enabled = false;
                dgvAttendance.CurrentCellDirtyStateChanged += (_, _) =>
                {
                    if (dgvAttendance.IsCurrentCellDirty)
                    {
                        dgvAttendance.CommitEdit(DataGridViewDataErrorContexts.Commit);
                    }
                };
                dgvAttendance.CellEndEdit += dgvAttendance_CellEndEdit;
                dgvAttendance.CellValueChanged += (_, _) => UpdateTeacherAttendanceSaveButtonState();
            }
            else
            {
                tabTeacher.Parent = null;
                InitializeAdminMakeupFeatures();
                InitializeReportFeatures();
            }

            cmbStudentPayment.SelectedIndexChanged += cmbStudentPayment_SelectedIndexChanged;

            LoadCoursesToCombos();
            LoadStudents();
            LoadClasses();
            LoadReports();
            LoadTeachers();
            LoadTeacherManagement();
            InitPayrollCombos();

            if (_currentUser.Role == "Teacher")
            {
                InitTimesheetCombos();
                LoadTimesheet();
                LoadTeacherWeeklySchedule();
            }
        }

        private static bool IsValidAttendanceStatus(string? status)
        {
            var normalized = (status ?? string.Empty).Trim().ToUpperInvariant();
            return normalized is "C" or "V";
        }

        private void UpdateTeacherAttendanceSaveButtonState()
        {
            var rows = dgvAttendance.DataSource as List<AttendanceRow>;
            btnSaveAttendance.Enabled = rows is { Count: > 0 } && rows.All(x => IsValidAttendanceStatus(x.Status));
        }

        private void dgvAttendance_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (dgvAttendance.Columns[e.ColumnIndex].DataPropertyName == nameof(AttendanceRow.Status)
                && dgvAttendance.Rows[e.RowIndex].DataBoundItem is AttendanceRow row)
            {
                row.Status = (row.Status ?? string.Empty).Trim().ToUpperInvariant();
                dgvAttendance.Refresh();
            }

            UpdateTeacherAttendanceSaveButtonState();
        }

        private void InitializeAdminMakeupFeatures()
        {
            if (tabAdminPayroll.Controls.ContainsKey("btnAdminMakeupTimesheet") || tabAdminPayroll.Controls.ContainsKey("btnAdminMakeupAttendance"))
            {
                return;
            }

            var btnAdminMakeupTimesheet = new Button
            {
                Name = "btnAdminMakeupTimesheet",
                Text = "Chấm công bù GV",
                Location = new Point(320, 11),
                Size = new Size(150, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnAdminMakeupTimesheet.Click += btnAdminMakeupTimesheet_Click;

            var btnAdminMakeupAttendance = new Button
            {
                Name = "btnAdminMakeupAttendance",
                Text = "Điểm danh bù HS",
                Location = new Point(480, 11),
                Size = new Size(170, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnAdminMakeupAttendance.Click += btnAdminMakeupAttendance_Click;

            tabAdminPayroll.Controls.Add(btnAdminMakeupTimesheet);
            tabAdminPayroll.Controls.Add(btnAdminMakeupAttendance);
        }

        private void InitializeReportFeatures()
        {
            if (tabAdminReports.Controls.ContainsKey("cmbReportYear"))
            {
                return;
            }

            var lblYearSelect = new Label
            {
                Name = "lblYearSelect",
                Text = "Chọn năm:",
                Location = new Point(20, 10),
                Size = new Size(70, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            var cmbReportYear = new ComboBox
            {
                Name = "cmbReportYear",
                Location = new Point(95, 8),
                Size = new Size(100, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbReportYear.SelectedIndexChanged += CmbReportYear_SelectedIndexChanged;

            var btnExportByMonth = new Button
            {
                Name = "btnExportByMonth",
                Text = "Xuất Excel (tháng)",
                Location = new Point(210, 7),
                Size = new Size(140, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnExportByMonth.Click += BtnExportByMonth_Click;

            var btnExportByYear = new Button
            {
                Name = "btnExportByYear",
                Text = "Xuất Excel (năm)",
                Location = new Point(360, 7),
                Size = new Size(130, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnExportByYear.Click += BtnExportByYear_Click;

            tabAdminReports.Controls.Add(lblYearSelect);
            tabAdminReports.Controls.Add(cmbReportYear);
            tabAdminReports.Controls.Add(btnExportByMonth);
            tabAdminReports.Controls.Add(btnExportByYear);
        }

        private void BindReportYears()
        {
            if (tabAdminReports.Controls["cmbReportYear"] is not ComboBox cmbYear)
            {
                return;
            }

            var selectedYear = cmbYear.SelectedItem is int year ? year : DateTime.Now.Year;
            List<int> years;

            if (_revenueByYear.Count == 0)
            {
                years = new List<int> { DateTime.Now.Year };
            }
            else
            {
                var minYear = _revenueByYear.Min(x => x.Year);
                var maxYear = Math.Max(_revenueByYear.Max(x => x.Year), DateTime.Now.Year);
                years = Enumerable.Range(minYear, maxYear - minYear + 1)
                    .OrderByDescending(x => x)
                    .ToList();
            }

            cmbYear.SelectedIndexChanged -= CmbReportYear_SelectedIndexChanged;
            cmbYear.Items.Clear();
            foreach (var item in years)
            {
                cmbYear.Items.Add(item);
            }

            var targetYear = years.Contains(selectedYear) ? selectedYear : years.First();
            cmbYear.SelectedItem = targetYear;
            cmbYear.SelectedIndexChanged += CmbReportYear_SelectedIndexChanged;
        }

        private void CmbReportYear_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadMonthlyRevenueData();
        }

        private void LoadMonthlyRevenueData()
        {
            if (tabAdminReports.Controls["cmbReportYear"] is not ComboBox cmbYear || cmbYear.SelectedItem is not int year)
            {
                return;
            }

            _revenueByMonth = _reportService.GetRevenueByMonth(year);

            dgvRevenueByYear.DataSource = null;
            dgvRevenueByYear.DataSource = _revenueByMonth;
            ApplyRevenueByMonthHeaders(dgvRevenueByYear);

            lblRevenueChartTitle.Text = $"Biểu đồ doanh thu theo tháng năm {year}";
            pnlRevenueChart.Invalidate();
        }

        private void ApplyRevenueByMonthHeaders(DataGridView dgv)
        {
            SetGridHeaders(dgv, new Dictionary<string, string>
            {
                [nameof(RevenueByMonthStat.MonthName)] = "Tháng",
                [nameof(RevenueByMonthStat.TotalRevenue)] = "Doanh thu"
            });

            // Hide the Month column as we use MonthName
            if (dgv.Columns.Contains(nameof(RevenueByMonthStat.Month)))
            {
                dgv.Columns[nameof(RevenueByMonthStat.Month)].Visible = false;
            }

            if (dgv.Columns[nameof(RevenueByMonthStat.TotalRevenue)] is DataGridViewColumn revenueCol)
            {
                revenueCol.DefaultCellStyle.Format = "N0";
            }
        }

        private void PnlMonthlyChart_Paint(object? sender, PaintEventArgs e)
        {
            var panel = (Panel)sender!;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            var chartArea = panel.ClientRectangle;
            if (chartArea.Width < 120 || chartArea.Height < 120 || _revenueByMonth.Count == 0)
            {
                using var emptyBrush = new SolidBrush(Color.Gray);
                using var emptyFont = new Font("Segoe UI", 10f);
                var text = "Chưa có dữ liệu doanh thu";
                var size = g.MeasureString(text, emptyFont);
                g.DrawString(text, emptyFont, emptyBrush,
                    (chartArea.Width - size.Width) / 2,
                    (chartArea.Height - size.Height) / 2);
                return;
            }

            var data = _revenueByMonth.OrderBy(x => x.Month).ToList();

            const int leftPad = 60;
            const int rightPad = 20;
            const int topPad = 20;
            const int bottomPad = 50;

            var plot = new Rectangle(
                leftPad,
                topPad,
                chartArea.Width - leftPad - rightPad,
                chartArea.Height - topPad - bottomPad);

            using var axisPen = new Pen(Color.DimGray, 1.2f);
            g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
            g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);

            var maxRevenue = data.Max(x => x.TotalRevenue);
            if (maxRevenue <= 0)
            {
                maxRevenue = 1;
            }

            using var gridPen = new Pen(Color.Gainsboro, 1f);
            using var labelBrush = new SolidBrush(Color.DimGray);
            using var axisFont = new Font("Segoe UI", 8.5f);

            const int gridLines = 4;
            for (int i = 0; i <= gridLines; i++)
            {
                var ratio = i / (float)gridLines;
                var y = plot.Bottom - ratio * plot.Height;
                g.DrawLine(gridPen, plot.Left, y, plot.Right, y);

                var value = maxRevenue * (decimal)ratio;
                var yLabel = FormatCurrency(value);
                var ySize = g.MeasureString(yLabel, axisFont);
                g.DrawString(yLabel, axisFont, labelBrush, plot.Left - ySize.Width - 6, y - ySize.Height / 2);
            }

            var slotWidth = plot.Width / (float)data.Count;
            var barWidth = Math.Max(16f, slotWidth * 0.55f);

            using var barBrush = new SolidBrush(Color.FromArgb(66, 133, 244));
            using var valueFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var valueBrush = new SolidBrush(Color.FromArgb(40, 40, 40));

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var barHeight = (float)((double)(item.TotalRevenue / maxRevenue) * plot.Height);
                var x = plot.Left + i * slotWidth + (slotWidth - barWidth) / 2;
                var y = plot.Bottom - barHeight;

                g.FillRectangle(barBrush, x, y, barWidth, barHeight);

                var monthText = $"T{item.Month}";
                var monthSize = g.MeasureString(monthText, axisFont);
                g.DrawString(monthText, axisFont, labelBrush, x + (barWidth - monthSize.Width) / 2, plot.Bottom + 6);

                var valueText = FormatCurrency(item.TotalRevenue);
                var valueSize = g.MeasureString(valueText, valueFont);
                var valueX = x + (barWidth - valueSize.Width) / 2;
                var valueY = y - valueSize.Height - 3;

                if (valueY > plot.Top - valueSize.Height)
                {
                    g.DrawString(valueText, valueFont, valueBrush, valueX, valueY);
                }
            }
        }

        private void BtnExportByMonth_Click(object? sender, EventArgs e)
        {
            if (tabAdminReports.Controls["cmbReportYear"] is not ComboBox cmbYear || cmbYear.SelectedItem is not int year)
            {
                MessageBox.Show("Vui lòng chọn năm.");
                return;
            }

            using var dialog = new SaveFileDialog
            {
                FileName = $"BaoCaoDoanhthuThang_{year}.xlsx",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Lưu báo cáo doanh thu theo tháng"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                _excelExportService.ExportRevenueByMonthToExcel(year, _revenueByMonth, dialog.FileName);
                MessageBox.Show($"Đã xuất báo cáo thành công:\n{dialog.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportByYear_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                FileName = "BaoCaoDoanhthuNam.xlsx",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Title = "Lưu báo cáo doanh thu theo năm"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                _excelExportService.ExportRevenueByYearToExcel(_revenueByYear, dialog.FileName);
                MessageBox.Show($"Đã xuất báo cáo thành công:\n{dialog.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất báo cáo: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAdminMakeupTimesheet_Click(object? sender, EventArgs e)
        {
            if (dgvPayroll.CurrentRow is null || !dgvPayroll.Columns.Contains("TeacherId"))
            {
                MessageBox.Show("Vui lòng chọn giáo viên ở bảng ngày công.");
                return;
            }

            var teacherId = Convert.ToInt32(dgvPayroll.CurrentRow.Cells["TeacherId"].Value);
            var teacherName = dgvPayroll.CurrentRow.Cells["Giáo viên"].Value?.ToString() ?? "Giáo viên";

            using var form = new Form
            {
                Text = $"Chấm công bù - {teacherName}",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(420, 340),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblDate = new Label { Text = "Ngày", Location = new Point(20, 20), Size = new Size(120, 20) };
            var dtpDate = new DateTimePicker { Location = new Point(20, 42), Size = new Size(360, 23), Format = DateTimePickerFormat.Short };

            var lblShift = new Label { Text = "Ca", Location = new Point(20, 75), Size = new Size(120, 20) };
            var cmbShift = new ComboBox { Location = new Point(20, 97), Size = new Size(360, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            for (int s = 1; s <= 5; s++) cmbShift.Items.Add(s);
            cmbShift.SelectedIndex = 0;

            var chkPresent = new CheckBox { Text = "Có mặt", Location = new Point(20, 130), Size = new Size(120, 23), Checked = true };

            var lblNote = new Label { Text = "Ghi chú", Location = new Point(20, 156), Size = new Size(120, 20) };
            var txtNote = new TextBox { Location = new Point(20, 178), Size = new Size(360, 23) };

            var btnSave = new Button { Text = "Lưu chấm công bù", Location = new Point(20, 208), Size = new Size(360, 30) };
            btnSave.Click += (_, _) =>
            {
                if (cmbShift.SelectedItem is not int shift)
                {
                    MessageBox.Show("Vui lòng chọn ca.");
                    return;
                }

                _timesheetService.SaveTimesheet(teacherId, dtpDate.Value.Date, shift, chkPresent.Checked, string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim());
                MessageBox.Show("Đã lưu châm công bù.");
                form.DialogResult = DialogResult.OK;
                form.Close();
            };

            form.Controls.Add(lblDate);
            form.Controls.Add(dtpDate);
            form.Controls.Add(lblShift);
            form.Controls.Add(cmbShift);
            form.Controls.Add(chkPresent);
            form.Controls.Add(lblNote);
            form.Controls.Add(txtNote);
            form.Controls.Add(btnSave);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadPayroll();
            }
        }

        private void btnAdminMakeupAttendance_Click(object? sender, EventArgs e)
        {
            using var form = new Form
            {
                Text = "Điểm danh bù học sinh (Admin)",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(900, 560)
            };

            var lblClass = new Label { Text = "Lớp", Location = new Point(10, 10), Size = new Size(120, 20) };
            var cmbClass = new ComboBox { Location = new Point(10, 32), Size = new Size(300, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbClass.DataSource = _classes.Select(c => new ClassInfo { Id = c.Id, ClassName = c.ClassName }).ToList();
            cmbClass.DisplayMember = nameof(ClassInfo.ClassName);
            cmbClass.ValueMember = nameof(ClassInfo.Id);

            var lblDate = new Label { Text = "Ngày học", Location = new Point(320, 10), Size = new Size(120, 20) };
            var dtpDate = new DateTimePicker { Location = new Point(320, 32), Size = new Size(180, 23), Format = DateTimePickerFormat.Short };

            var btnSave = new Button { Text = "Lưu điểm danh bù", Location = new Point(510, 31), Size = new Size(370, 25), Enabled = false };

            var dgv = new DataGridView
            {
                Location = new Point(10, 65),
                Size = new Size(870, 445),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };

            static bool IsValidAttendanceStatus(string? status)
            {
                var normalized = (status ?? string.Empty).Trim().ToUpperInvariant();
                return normalized is "C" or "V";
            }

            void UpdateSaveButtonState()
            {
                var rows = dgv.DataSource as List<AttendanceRow>;
                btnSave.Enabled = rows is { Count: > 0 } && rows.All(x => IsValidAttendanceStatus(x.Status));
            }

            void LoadStudentsForMakeup()
            {
                if (cmbClass.SelectedValue is not int classId)
                {
                    dgv.DataSource = null;
                    btnSave.Enabled = false;
                    return;
                }

                var savedRecords = _attendanceService.GetAttendanceByClassAndDate(classId, dtpDate.Value.Date);
                var students = _attendanceService.GetStudentsByClass(classId)
                    .Select(x => new AttendanceRow
                    {
                        StudentId = x.StudentId,
                        StudentName = x.FullName,
                        Status = savedRecords.TryGetValue(x.StudentId, out var status) ? status : string.Empty
                    })
                    .ToList();

                dgv.DataSource = null;
                dgv.DataSource = students;

                if (dgv.Columns.Contains(nameof(AttendanceRow.StudentId))) dgv.Columns[nameof(AttendanceRow.StudentId)].HeaderText = "Mã học viên";
                if (dgv.Columns.Contains(nameof(AttendanceRow.StudentName))) dgv.Columns[nameof(AttendanceRow.StudentName)].HeaderText = "Tên học viên";
                if (dgv.Columns.Contains(nameof(AttendanceRow.Status)))
                {
                    dgv.Columns[nameof(AttendanceRow.Status)].HeaderText = "Điểm danh (C/V)";
                    dgv.Columns[nameof(AttendanceRow.Status)].ToolTipText = "Chỉ nhập C hoặc V";
                }

                UpdateSaveButtonState();
            }

            cmbClass.SelectedIndexChanged += (_, _) => LoadStudentsForMakeup();
            dtpDate.ValueChanged += (_, _) => LoadStudentsForMakeup();

            dgv.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (dgv.IsCurrentCellDirty)
                {
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            dgv.CellValueChanged += (_, e2) =>
            {
                if (e2.RowIndex < 0)
                {
                    return;
                }

                if (dgv.Columns[e2.ColumnIndex].DataPropertyName == nameof(AttendanceRow.Status)
                    && dgv.Rows[e2.RowIndex].DataBoundItem is AttendanceRow row)
                {
                    row.Status = (row.Status ?? string.Empty).Trim().ToUpperInvariant();
                }

                UpdateSaveButtonState();
            };

            btnSave.Click += (_, _) =>
            {
                if (cmbClass.SelectedValue is not int classId)
                {
                    MessageBox.Show("Vui lòng chọn lớp.");
                    return;
                }

                var teacherId = _classService.GetTeacherIdByClass(classId);
                if (teacherId is null)
                {
                    MessageBox.Show("Lớp chưa có giáo viên phụ trách, không thể lưu điểm danh bù.");
                    return;
                }

                dgv.EndEdit();

                var rows = dgv.DataSource as List<AttendanceRow>;
                if (rows is null || rows.Count == 0)
                {
                    MessageBox.Show("Chưa có dữ liệu điểm danh.");
                    return;
                }

                var invalidRow = rows.FirstOrDefault(x => !IsValidAttendanceStatus(x.Status));
                if (invalidRow is not null)
                {
                    MessageBox.Show($"Học viên '{invalidRow.StudentName}' phải nhập điểm danh là C hoặc V.");
                    UpdateSaveButtonState();
                    return;
                }

                var records = rows.ToDictionary(x => x.StudentId, x => x.Status.Trim().ToUpperInvariant());
                _attendanceService.SaveAttendance(classId, teacherId.Value, dtpDate.Value.Date, records);
                MessageBox.Show("Đã lưu điểm danh bù.");
                UpdateSaveButtonState();
            };

            form.Controls.Add(lblClass);
            form.Controls.Add(cmbClass);
            form.Controls.Add(lblDate);
            form.Controls.Add(dtpDate);
            form.Controls.Add(btnSave);
            form.Controls.Add(dgv);

            form.Shown += (_, _) => LoadStudentsForMakeup();
            form.ShowDialog(this);
        }

        private void SetGridHeaders(DataGridView grid, Dictionary<string, string> headers)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (headers.TryGetValue(column.DataPropertyName, out var text) || headers.TryGetValue(column.Name, out text))
                {
                    column.HeaderText = text;
                }
            }
        }

        private void ApplyStudentHeaders()
        {
            SetGridHeaders(dgvStudents, new Dictionary<string, string>
            {
                [nameof(Student.Id)] = "Mã học viên",
                [nameof(Student.FullName)] = "Họ và tên",
                [nameof(Student.Phone)] = "Số điện thoại",
                [nameof(Student.Email)] = "Email",
                [nameof(Student.BirthYear)] = "Năm sinh",
                [nameof(Student.Address)] = "Địa chỉ",
                [nameof(Student.Status)] = "Trạng thái"
            });
        }

        private void ApplyCourseHeaders()
        {
            SetGridHeaders(dgvCourses, new Dictionary<string, string>
            {
                [nameof(Course.Id)] = "Mã khóa",
                [nameof(Course.Name)] = "Tên khóa học",
                [nameof(Course.TuitionFee)] = "Học phí",
                [nameof(Course.Status)] = "Trạng thái"
            });
        }

        private void ApplyClassHeaders(DataGridView grid)
        {
            SetGridHeaders(grid, new Dictionary<string, string>
            {
                [nameof(ClassInfo.Id)] = "Mã lớp",
                [nameof(ClassInfo.ClassName)] = "Tên lớp",
                [nameof(ClassInfo.CourseCode)] = "Khóa học",
                [nameof(ClassInfo.TeacherName)] = "Giáo viên",
                [nameof(ClassInfo.CurrentSize)] = "Sĩ số",
                [nameof(ClassInfo.Status)] = "Trạng thái"
            });
        }

        private void ApplyAttendanceHeaders()
        {
            SetGridHeaders(dgvAttendance, new Dictionary<string, string>
            {
                [nameof(AttendanceRow.StudentId)] = "Mã học viên",
                [nameof(AttendanceRow.StudentName)] = "Tên học viên",
                [nameof(AttendanceRow.Status)] = "Điểm danh"
            });
        }

        private void ApplyTeacherHeaders()
        {
            SetGridHeaders(dgvTeachers, new Dictionary<string, string>
            {
                [nameof(Teacher.Id)] = "Mã giáo viên",
                [nameof(Teacher.FullName)] = "Họ và tên",
                [nameof(Teacher.Phone)] = "Số điện thoại",
                [nameof(Teacher.Email)] = "Email",
                [nameof(Teacher.Status)] = "Trạng thái"
            });
        }

        private void ApplyRevenueByYearHeaders()
        {
            SetGridHeaders(dgvRevenueByYear, new Dictionary<string, string>
            {
                [nameof(RevenueByYearStat.Year)] = "Năm",
                [nameof(RevenueByYearStat.TotalRevenue)] = "Doanh thu"
            });

            if (dgvRevenueByYear.Columns[nameof(RevenueByYearStat.TotalRevenue)] is DataGridViewColumn revenueCol)
            {
                revenueCol.DefaultCellStyle.Format = "N0";
            }
        }

        private void LoadTeachers()
        {
            var teachers = _classService.GetTeachers();
            cmbTeacherClass.DataSource = teachers;
            cmbTeacherClass.DisplayMember = nameof(Teacher.FullName);
            cmbTeacherClass.ValueMember = nameof(Teacher.Id);
        }

        private void LoadTeacherManagement()
        {
            _teachers = _teacherService.GetAll();
            dgvTeachers.DataSource = null;
            dgvTeachers.DataSource = _teachers;
            ApplyTeacherHeaders();
        }

        private void LoadStudents()
        {
            _students = _studentService.GetAll();
            dgvStudents.DataSource = null;
            dgvStudents.DataSource = _students;
            ApplyStudentHeaders();

            cmbStudentPayment.DataSource = null;
            cmbStudentPayment.DataSource = _students.ToList();
            cmbStudentPayment.DisplayMember = nameof(Student.FullName);
            cmbStudentPayment.ValueMember = nameof(Student.Id);

            cmbStudentEvaluate.DataSource = null;
            cmbStudentEvaluate.DataSource = _students.ToList();
            cmbStudentEvaluate.DisplayMember = nameof(Student.FullName);
            cmbStudentEvaluate.ValueMember = nameof(Student.Id);

            cmbStudentClass.DataSource = null;
            cmbStudentClass.DataSource = _students.ToList();
            cmbStudentClass.DisplayMember = nameof(Student.FullName);
            cmbStudentClass.ValueMember = nameof(Student.Id);
        }

        private void LoadCoursesToCombos()
        {
            _courses = _courseService.GetAll();
            dgvCourses.DataSource = null;
            dgvCourses.DataSource = _courses;
            ApplyCourseHeaders();

            cmbCourseClass.DataSource = null;
            cmbCourseClass.DataSource = _courses.ToList();
            cmbCourseClass.DisplayMember = nameof(Course.Name);
            cmbCourseClass.ValueMember = nameof(Course.Id);
        }

        private void LoadClasses()
        {
            LoadClassesAndRestore(-1);
        }

        private void LoadClassesAndRestore(int classId)
        {
            _classes = _classService.GetAll();
            dgvClasses.DataSource = null;
            dgvClasses.DataSource = _classes;
            ApplyClassHeaders(dgvClasses);

            cmbClassAttendance.DataSource = null;
            cmbClassAttendance.DataSource = _classes.ToList();
            cmbClassAttendance.DisplayMember = nameof(ClassInfo.ClassName);
            cmbClassAttendance.ValueMember = nameof(ClassInfo.Id);

            LoadPaymentClassFilter();

            cmbClassEvaluate.DataSource = null;
            cmbClassEvaluate.DataSource = _classes.ToList();
            cmbClassEvaluate.DisplayMember = nameof(ClassInfo.ClassName);
            cmbClassEvaluate.ValueMember = nameof(ClassInfo.Id);

            cmbClassAddStudent.DataSource = null;
            cmbClassAddStudent.DataSource = _classes.ToList();
            cmbClassAddStudent.DisplayMember = nameof(ClassInfo.ClassName);
            cmbClassAddStudent.ValueMember = nameof(ClassInfo.Id);

            var teacherClasses = _currentUser.Role == "Teacher" && _currentUser.TeacherId.HasValue
                ? _classService.GetClassesByTeacher(_currentUser.TeacherId.Value)
                : _classes;
            dgvTeacherClasses.DataSource = null;
            dgvTeacherClasses.DataSource = teacherClasses.ToList();
            ApplyClassHeaders(dgvTeacherClasses);

            if (classId <= 0) return;

            // Restore selection to the class that was modified
            foreach (DataGridViewRow row in dgvClasses.Rows)
            {
                if (row.DataBoundItem is ClassInfo ci && ci.Id == classId)
                {
                    dgvClasses.ClearSelection();
                    row.Selected = true;
                    dgvClasses.CurrentCell = row.Cells[0];
                    break;
                }
            }

            cmbClassAddStudent.SelectedValue = classId;
            LoadClassStudents();
        }

        private void LoadReports()
        {
            var summary = _reportService.GetSummary();
            lblTotalStudents.Text = $"Tổng học viên: {summary.TotalStudents}";
            lblTotalRevenue.Text = $"Doanh thu: {FormatCurrency(summary.TotalRevenue)}";
            lblActiveClasses.Text = $"Lớp hoạt động: {summary.ActiveClasses}";

            _revenueByYear = _reportService.GetRevenueByYear();

            if (_currentUser.Role != "Teacher")
            {
                InitializeReportFeatures();
                BindReportYears();
                LoadMonthlyRevenueData();
            }
        }

        private void pnlRevenueChart_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            var chartArea = pnlRevenueChart.ClientRectangle;
            if (chartArea.Width < 120 || chartArea.Height < 120)
            {
                return;
            }

            if (_revenueByMonth.Count == 0)
            {
                using var emptyBrush = new SolidBrush(Color.Gray);
                using var emptyFont = new Font("Segoe UI", 10f);
                var text = "Chưa có dữ liệu doanh thu";
                var size = g.MeasureString(text, emptyFont);
                g.DrawString(text, emptyFont, emptyBrush,
                    (chartArea.Width - size.Width) / 2,
                    (chartArea.Height - size.Height) / 2);
                return;
            }

            var data = _revenueByMonth.OrderBy(x => x.Month).ToList();

            const int leftPad = 60;
            const int rightPad = 20;
            const int topPad = 20;
            const int bottomPad = 50;

            var plot = new Rectangle(
                leftPad,
                topPad,
                chartArea.Width - leftPad - rightPad,
                chartArea.Height - topPad - bottomPad);

            using var axisPen = new Pen(Color.DimGray, 1.2f);
            g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
            g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);

            var maxRevenue = data.Max(x => x.TotalRevenue);
            if (maxRevenue <= 0)
            {
                maxRevenue = 1;
            }

            using var gridPen = new Pen(Color.Gainsboro, 1f);
            using var labelBrush = new SolidBrush(Color.DimGray);
            using var axisFont = new Font("Segoe UI", 8.5f);

            const int gridLines = 4;
            for (int i = 0; i <= gridLines; i++)
            {
                var ratio = i / (float)gridLines;
                var y = plot.Bottom - ratio * plot.Height;
                g.DrawLine(gridPen, plot.Left, y, plot.Right, y);

                var value = maxRevenue * (decimal)ratio;
                var yLabel = FormatCurrency(value);
                var ySize = g.MeasureString(yLabel, axisFont);
                g.DrawString(yLabel, axisFont, labelBrush, plot.Left - ySize.Width - 6, y - ySize.Height / 2);
            }

            var slotWidth = plot.Width / (float)data.Count;
            var barWidth = Math.Max(16f, slotWidth * 0.55f);

            using var barBrush = new SolidBrush(Color.FromArgb(66, 133, 244));
            using var valueFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var valueBrush = new SolidBrush(Color.FromArgb(40, 40, 40));

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                var barHeight = (float)((double)(item.TotalRevenue / maxRevenue) * plot.Height);
                var x = plot.Left + i * slotWidth + (slotWidth - barWidth) / 2;
                var y = plot.Bottom - barHeight;

                g.FillRectangle(barBrush, x, y, barWidth, barHeight);

                var monthText = $"T{item.Month}";
                var monthSize = g.MeasureString(monthText, axisFont);
                g.DrawString(monthText, axisFont, labelBrush, x + (barWidth - monthSize.Width) / 2, plot.Bottom + 6);

                var valueText = FormatCurrency(item.TotalRevenue);
                var valueSize = g.MeasureString(valueText, valueFont);
                var valueX = x + (barWidth - valueSize.Width) / 2;
                var valueY = y - valueSize.Height - 3;

                if (valueY > plot.Top - valueSize.Height)
                {
                    g.DrawString(valueText, valueFont, valueBrush, valueX, valueY);
                }
            }
        }

        private void btnAddStudent_Click(object sender, EventArgs e)
        {
            try
            {
                _studentService.Add(new Student
                {
                    FullName = txtStudentName.Text.Trim(),
                    Phone = txtStudentPhone.Text.Trim(),
                    Email = txtStudentEmail.Text.Trim(),
                    BirthYear = int.TryParse(txtStudentBirthYear.Text.Trim(), out var birthYear) ? birthYear : null,
                    Address = txtStudentAddress.Text.Trim(),
                    Status = cmbStudentStatus.Text
                });
                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnImportStudents_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Chọn file danh sách học viên"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var imported = 0;
            var skipped = 0;
            var errors = new List<string>();

            foreach (var item in File.ReadLines(dialog.FileName).Select((line, index) => new { line, index }))
            {
                var raw = item.line.Trim();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    skipped++;
                    continue;
                }

                var parts = raw.Split('-', 5);
                var fullName = parts.Length > 0 ? parts[0].Trim() : string.Empty;
                var phone = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                var email = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                var birthYearText = parts.Length > 3 ? parts[3].Trim() : string.Empty;
                var address = parts.Length > 4 ? parts[4].Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    skipped++;
                    continue;
                }

                int? birthYear = null;
                if (!string.IsNullOrWhiteSpace(birthYearText))
                {
                    if (!int.TryParse(birthYearText, out var yearValue))
                    {
                        skipped++;
                        errors.Add($"Dòng {item.index + 1}: Năm sinh không hợp lệ '{birthYearText}'.");
                        continue;
                    }
                    birthYear = yearValue;
                }

                try
                {
                    _studentService.Add(new Student
                    {
                        FullName = fullName,
                        Phone = phone,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        BirthYear = birthYear,
                        Address = string.IsNullOrWhiteSpace(address) ? null : address,
                        Status = "Active"
                    });
                    imported++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    errors.Add($"Dòng {item.index + 1}: {ex.Message}");
                }
            }

            LoadStudents();

            var message = $"Đã nhập {imported} học viên. Bỏ qua {skipped} dòng.";
            if (errors.Count > 0)
            {
                var preview = string.Join("\n", errors.Take(5));
                message += $"\n\nChi tiết lỗi:\n{preview}";
                if (errors.Count > 5)
                {
                    message += "\n...";
                }
            }

            MessageBox.Show(message, "Nhập hàng loạt học viên", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnUpdateStudent_Click(object sender, EventArgs e)
        {
            if (dgvStudents.CurrentRow?.DataBoundItem is not Student s)
            {
                return;
            }

            try
            {
                s.FullName = txtStudentName.Text.Trim();
                s.Phone = txtStudentPhone.Text.Trim();
                s.Email = txtStudentEmail.Text.Trim();
                s.BirthYear = int.TryParse(txtStudentBirthYear.Text.Trim(), out var birthYear) ? birthYear : null;
                s.Address = txtStudentAddress.Text.Trim();
                s.Status = cmbStudentStatus.Text;
                _studentService.Update(s);
                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnDeleteStudent_Click(object sender, EventArgs e)
        {
            var selected = dgvStudents.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.DataBoundItem is Student)
                .Select(r => (Student)r.DataBoundItem!)
                .ToList();

            if (selected.Count == 0) return;

            var confirm = MessageBox.Show(
                selected.Count == 1
                    ? $"Xóa học viên '{selected[0].FullName}'?"
                    : $"Xóa {selected.Count} học viên đã chọn?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            foreach (var s in selected)
            {
                _studentService.Delete(s.Id);
            }
            LoadStudents();
        }

        private void dgvStudents_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvStudents.CurrentRow?.DataBoundItem is not Student s)
            {
                return;
            }

            txtStudentName.Text = s.FullName;
            txtStudentPhone.Text = s.Phone;
            txtStudentEmail.Text = s.Email;
            txtStudentBirthYear.Text = s.BirthYear?.ToString() ?? string.Empty;
            txtStudentAddress.Text = s.Address ?? string.Empty;
            cmbStudentStatus.Text = s.Status;
        }

        private void btnViewEvaluations_Click(object sender, EventArgs e)
        {
            if (dgvStudents.CurrentRow?.DataBoundItem is not Student s)
            {
                return;
            }

            var evaluations = _evaluationService.GetByStudent(s.Id);

            var form = new Form
            {
                Text = $"Điểm / Nhận xét - {s.FullName}",
                Size = new Size(700, 450),
                StartPosition = FormStartPosition.CenterParent
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DataSource = evaluations
            };

            form.Controls.Add(dgv);
            form.Shown += (_, _) =>
            {
                if (dgv.Columns.Count > 0)
                {
                    dgv.Columns["Lop"].HeaderText = "Lớp";
                    dgv.Columns["GiaoVien"].HeaderText = "Giáo viên";
                    dgv.Columns["Diem"].HeaderText = "Điểm";
                    dgv.Columns["NhanXet"].HeaderText = "Nhận xét";
                    dgv.Columns["Ngay"].HeaderText = "Ngày";
                }
            };

            form.ShowDialog(this);
        }

        private void btnAddCourse_Click(object sender, EventArgs e)
        {
            try
            {
                _courseService.Add(new Course
                {
                    Name = txtCourseName.Text.Trim(),
                    TuitionFee = TryParseMoney(txtCourseFee.Text, out var fee) ? fee : 0,
                    Status = "Active"
                });
                LoadCoursesToCombos();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi trùng khóa học", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnUpdateCourse_Click(object sender, EventArgs e)
        {
            if (dgvCourses.CurrentRow?.DataBoundItem is not Course c) return;

            c.Name = txtCourseName.Text.Trim();
            c.TuitionFee = TryParseMoney(txtCourseFee.Text, out var fee) ? fee : 0;

            try
            {
                _courseService.Update(c);
                LoadCoursesToCombos();
                MessageBox.Show("Đã cập nhật khóa học.");
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Lỗi trùng khóa học", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void dgvCourses_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvCourses.CurrentRow?.DataBoundItem is not Course c) return;
            txtCourseName.Text = c.Name;
            txtCourseFee.Text = FormatMoneyInput(c.TuitionFee);
        }

        private void btnCreateClass_Click(object sender, EventArgs e)
        {
            int? teacherId = cmbTeacherClass.SelectedValue is int selectedTeacherId ? selectedTeacherId : null;

            _classService.AddClass(
                txtClassName.Text.Trim(),
                cmbCourseClass.SelectedValue is int courseId ? courseId : 0,
                teacherId,
                "Active");

            LoadClasses();
        }

        private void btnAddStudentToClass_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbClassAddStudent.SelectedValue is not int classId || cmbStudentClass.SelectedValue is not int studentId)
                {
                    return;
                }

                _classService.AddStudentToClass(classId, studentId);
                LoadClassesAndRestore(classId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnUpdateClass_Click(object sender, EventArgs e)
        {
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c) return;
            int? teacherId = cmbTeacherClass.SelectedValue is int tid ? tid : null;
            _classService.UpdateClass(c.Id, txtClassName.Text.Trim(),
                cmbCourseClass.SelectedValue is int cid ? cid : 0, teacherId);
            LoadClasses();
            MessageBox.Show("Đã cập nhật lớp học.");
        }

        private void btnDeleteClass_Click(object sender, EventArgs e)
        {
            var selected = dgvClasses.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.DataBoundItem is ClassInfo)
                .Select(r => (ClassInfo)r.DataBoundItem!)
                .ToList();

            if (selected.Count == 0) return;

            var confirm = MessageBox.Show(
                selected.Count == 1
                    ? $"Xóa lớp '{selected[0].ClassName}'?"
                    : $"Xóa {selected.Count} lớp đã chọn?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            foreach (var c in selected)
            {
                _classService.DeleteClass(c.Id);
            }
            LoadClasses();
        }

        private void dgvClasses_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c) return;
            txtClassName.Text = c.ClassName;
            // Auto-fill "Chọn lớp" combobox
            cmbClassAddStudent.SelectedValue = c.Id;
            LoadClassStudents();
        }

        private void LoadClassStudents()
        {
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c)
            {
                dgvClassStudents.DataSource = null;
                return;
            }

            var students = _classService.GetStudentsInClass(c.Id)
                .Select(s => new { s.StudentId, s.FullName }).ToList();
            dgvClassStudents.DataSource = students;

            if (dgvClassStudents.Columns.Count > 0)
            {
                dgvClassStudents.Columns["StudentId"].HeaderText = "Mã học viên";
                dgvClassStudents.Columns["FullName"].HeaderText = "Họ và tên";
            }
        }

        private void btnRemoveStudentFromClass_Click(object sender, EventArgs e)
        {
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c) return;

            var selected = dgvClassStudents.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.Cells["StudentId"].Value is int)
                .Select(r => (int)r.Cells["StudentId"].Value)
                .ToList();

            if (selected.Count == 0) return;

            var confirm = MessageBox.Show(
                selected.Count == 1
                    ? "Xóa học viên đã chọn khỏi lớp?"
                    : $"Xóa {selected.Count} học viên đã chọn khỏi lớp?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            foreach (var studentId in selected)
            {
                _classService.RemoveStudentFromClass(c.Id, studentId);
            }
            LoadClassesAndRestore(c.Id);
        }

        private void btnClassSchedule_Click(object sender, EventArgs e)
        {
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c) return;
            using var form = new ClassScheduleForm(c.Id, c.ClassName);
            form.ShowDialog();
            LoadClasses();
        }

        private void btnAddTeacher_Click(object sender, EventArgs e)
        {
            try
            {
                var (username, password) = _teacherService.Add(new Teacher
                {
                    FullName = txtTeacherName.Text.Trim(),
                    Phone = txtTeacherPhone.Text.Trim(),
                    Email = txtTeacherEmail.Text.Trim(),
                    Status = cmbTeacherStatus.Text
                });

                MessageBox.Show(
                    $"Đã thêm giáo viên thành công!\n\nTài khoản đăng nhập:\n  Username: {username}\n  Password: {password}",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadTeacherManagement();
                LoadTeachers();
                LoadClasses();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnUpdateTeacher_Click(object sender, EventArgs e)
        {
            if (dgvTeachers.CurrentRow?.DataBoundItem is not Teacher teacher)
            {
                return;
            }

            try
            {
                teacher.FullName = txtTeacherName.Text.Trim();
                teacher.Phone = txtTeacherPhone.Text.Trim();
                teacher.Email = txtTeacherEmail.Text.Trim();
                teacher.Status = cmbTeacherStatus.Text;
                _teacherService.Update(teacher);

                LoadTeacherManagement();
                LoadTeachers();
                LoadClasses();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnDeleteTeacher_Click(object sender, EventArgs e)
        {
            var selected = dgvTeachers.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => r.DataBoundItem is Teacher)
                .Select(r => (Teacher)r.DataBoundItem!)
                .ToList();

            if (selected.Count == 0) return;

            var confirm = MessageBox.Show(
                selected.Count == 1
                    ? $"Xóa giáo viên '{selected[0].FullName}'?"
                    : $"Xóa {selected.Count} giáo viên đã chọn?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            var errors = new List<string>();
            foreach (var teacher in selected)
            {
                try
                {
                    _teacherService.Delete(teacher.Id);
                }
                catch
                {
                    errors.Add(teacher.FullName);
                }
            }

            LoadTeacherManagement();
            LoadTeachers();
            LoadClasses();

            if (errors.Count > 0)
            {
                MessageBox.Show($"Không thể xóa {errors.Count} giáo viên đang được sử dụng trong hệ thống:\n{string.Join("\n", errors)}");
            }
        }

        private void dgvTeachers_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvTeachers.CurrentRow?.DataBoundItem is not Teacher teacher)
            {
                txtTeacherUsername.Text = string.Empty;
                txtTeacherPassword.Text = string.Empty;
                return;
            }

            txtTeacherName.Text = teacher.FullName;
            txtTeacherPhone.Text = teacher.Phone;
            txtTeacherEmail.Text = teacher.Email;
            cmbTeacherStatus.Text = teacher.Status;

            var account = _teacherService.GetAccountInfo(teacher.Id);
            txtTeacherUsername.Text = account?.Username ?? string.Empty;
            txtTeacherPassword.Text = account?.Password ?? string.Empty;
        }

        private void btnUpdateTeacherPassword_Click(object sender, EventArgs e)
        {
            if (dgvTeachers.CurrentRow?.DataBoundItem is not Teacher teacher)
            {
                MessageBox.Show("Vui lòng chọn giáo viên.");
                return;
            }

            try
            {
                _teacherService.UpdatePassword(teacher.Id, txtTeacherPassword.Text);
                MessageBox.Show("Đã cập nhật mật khẩu.");

                var account = _teacherService.GetAccountInfo(teacher.Id);
                txtTeacherUsername.Text = account?.Username ?? string.Empty;
                txtTeacherPassword.Text = account?.Password ?? string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadSelectedPaymentInfo()
        {
            if (cmbStudentPayment.SelectedValue is not int studentId)
            {
                ClearPaymentInfo();
                return;
            }

            var total = _paymentService.GetTotalTuitionByStudent(studentId);
            var paid = _paymentService.GetPaidAmount(studentId);
            var remaining = total - paid;
            var (totalSessions, attended, absent) = _paymentService.GetAttendanceSummary(studentId);

            lblPaymentNeed.Text = $"Cần đóng: {FormatCurrency(total)} | Buổi học: {totalSessions} (Có mặt: {attended}, Vắng: {absent})";
            lblPaymentPaid.Text = $"Đã đóng: {FormatCurrency(paid)}";
            lblPaymentRemain.Text = $"Còn lại: {FormatCurrency(remaining)}";
            btnCollectPayment.Enabled = remaining > 0;

            var history = _paymentService.GetPaymentHistory(studentId);
            dgvAttendanceDetail.DataSource = null;
            dgvAttendanceDetail.DataSource = history;
            if (dgvAttendanceDetail.Columns.Count > 0)
            {
                dgvAttendanceDetail.Columns["PaymentId"].Visible = false;
                dgvAttendanceDetail.Columns["NgayThu"].HeaderText = "Ngày thu";
                dgvAttendanceDetail.Columns["SoTien"].HeaderText = "Số tiền";
                dgvAttendanceDetail.Columns["NguoiThu"].HeaderText = "Người thu";
                dgvAttendanceDetail.Columns["GhiChu"].HeaderText = "Ghi chú";
            }
        }

        private void ClearPaymentInfo()
        {
            lblPaymentNeed.Text = "Cần đóng: 0đ";
            lblPaymentPaid.Text = "Đã đóng: 0đ";
            lblPaymentRemain.Text = "Còn lại: 0đ";
            txtPaymentAmount.Text = string.Empty;
            txtPaymentNote.Text = string.Empty;
            btnCollectPayment.Enabled = false;
            dgvAttendanceDetail.DataSource = null;
        }

        private void cmbStudentPayment_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isBindingPaymentStudents)
            {
                return;
            }

            LoadSelectedPaymentInfo();
        }

        private void LoadPaymentClassFilter()
        {
            var allItem = new ClassInfo { Id = 0, ClassName = "-- Tất cả --" };
            var items = new List<ClassInfo> { allItem };
            items.AddRange(_classes);
            cmbPaymentFilterClass.DataSource = null;
            cmbPaymentFilterClass.DataSource = items;
            cmbPaymentFilterClass.DisplayMember = nameof(ClassInfo.ClassName);
            cmbPaymentFilterClass.ValueMember = nameof(ClassInfo.Id);
        }

        private void cmbPaymentFilterClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterPaymentStudents();
        }

        private void FilterPaymentStudents()
        {
            var currentStudentId = cmbStudentPayment.SelectedValue is int selectedStudentId ? selectedStudentId : 0;
            var classId = cmbPaymentFilterClass.SelectedValue is int id ? id : 0;

            List<Student> filtered;
            if (classId > 0)
            {
                var studentsInClass = _classService.GetStudentsInClass(classId);
                var ids = studentsInClass.Select(s => s.StudentId).ToHashSet();
                filtered = _students.Where(s => ids.Contains(s.Id)).ToList();
            }
            else
            {
                filtered = _students.ToList();
            }

            _isBindingPaymentStudents = true;
            cmbStudentPayment.DataSource = null;
            cmbStudentPayment.DataSource = filtered;
            cmbStudentPayment.DisplayMember = nameof(Student.FullName);
            cmbStudentPayment.ValueMember = nameof(Student.Id);

            if (filtered.Count == 0)
            {
                _isBindingPaymentStudents = false;
                ClearPaymentInfo();
                return;
            }

            if (currentStudentId > 0 && filtered.Any(s => s.Id == currentStudentId))
            {
                cmbStudentPayment.SelectedValue = currentStudentId;
            }
            else
            {
                cmbStudentPayment.SelectedIndex = 0;
            }

            _isBindingPaymentStudents = false;
            LoadSelectedPaymentInfo();
        }

        private void btnCollectPayment_Click(object sender, EventArgs e)
        {
            if (cmbStudentPayment.SelectedValue is not int studentId)
            {
                return;
            }

            var remaining = _paymentService.GetRemainingAmount(studentId);
            if (remaining <= 0)
            {
                btnCollectPayment.Enabled = false;
                MessageBox.Show("Học viên đã đóng đủ học phí, không thể thu thêm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryParseMoney(txtPaymentAmount.Text, out var amount) || amount <= 0)
            {
                MessageBox.Show("Số tiền thu bắt buộc phải lớn hơn 0.", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPaymentAmount.Focus();
                txtPaymentAmount.SelectAll();
                return;
            }

            try
            {
                _paymentService.Collect(studentId, amount, _currentUser.Id, txtPaymentNote.Text.Trim());
                LoadSelectedPaymentInfo();
                LoadReports();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnEditPaymentHistory_Click(object sender, EventArgs e)
        {
            if (dgvAttendanceDetail.SelectedRows.Count == 0) return;
            if (dgvAttendanceDetail.SelectedRows[0].Cells["PaymentId"]?.Value is not int paymentId) return;
            if (cmbStudentPayment.SelectedValue is not int studentId) return;

            var currentAmountRaw = dgvAttendanceDetail.SelectedRows[0].Cells["SoTien"]?.Value;
            if (currentAmountRaw == null) return;
            var currentAmount = Convert.ToDecimal(currentAmountRaw);

            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập số tiền mới:", "Sửa lịch sử thu phí",
                FormatMoneyInput(currentAmount));

            if (string.IsNullOrWhiteSpace(input)) return;
            if (!TryParseMoney(input, out var newAmount) || newAmount <= 0)
            {
                MessageBox.Show("Số tiền không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _paymentService.UpdatePaymentHistory(paymentId, studentId, newAmount, null);
                LoadSelectedPaymentInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDeletePaymentHistory_Click(object sender, EventArgs e)
        {
            if (dgvAttendanceDetail.SelectedRows.Count == 0) return;
            if (dgvAttendanceDetail.SelectedRows[0].Cells["PaymentId"]?.Value is not int paymentId) return;
            if (cmbStudentPayment.SelectedValue is not int studentId) return;

            var confirm = MessageBox.Show("Bạn có chắc muốn xoá bản ghi thu phí này?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                _paymentService.DeletePaymentHistory(paymentId, studentId);
                LoadSelectedPaymentInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvAttendanceDetail_SelectionChanged(object sender, EventArgs e)
        {
            bool hasSelection = dgvAttendanceDetail.SelectedRows.Count > 0;
            btnEditPaymentHistory.Enabled = hasSelection;
            btnDeletePaymentHistory.Enabled = hasSelection;
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            RequestLogout = true;
            Close();
        }

        private void btnLoadAttendanceStudents_Click(object sender, EventArgs e)
        {
            if (cmbClassAttendance.SelectedValue is not int classId)
            {
                return;
            }

            var savedRecords = _attendanceService.GetAttendanceByClassAndDate(classId, dtpSessionDate.Value);
            var students = _attendanceService.GetStudentsByClass(classId)
                .Select(x => new AttendanceRow
                {
                    StudentId = x.StudentId,
                    StudentName = x.FullName,
                    Status = savedRecords.TryGetValue(x.StudentId, out var status) ? status : string.Empty
                })
                .ToList();

            dgvAttendance.DataSource = null;
            dgvAttendance.DataSource = students;
            ApplyAttendanceHeaders();
            UpdateTeacherAttendanceSaveButtonState();
        }

        private void cmbAttendanceFilter_Changed(object? sender, EventArgs e)
        {
            btnLoadAttendanceStudents_Click(sender ?? this, e);
        }

        private void btnSaveAttendance_Click(object sender, EventArgs e)
        {
            if (_currentUser.TeacherId is null || cmbClassAttendance.SelectedValue is not int classId)
            {
                MessageBox.Show("Chỉ giáo viên mới được điểm danh.");
                return;
            }

            if (_currentUser.Role == "Teacher" && dtpSessionDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("Tài khoản giáo viên chỉ được điểm danh từ hôm nay trở về sau.");
                return;
            }

            dgvAttendance.EndEdit();

            var rows = dgvAttendance.DataSource as List<AttendanceRow>;
            if (rows is null or { Count: 0 })
            {
                return;
            }

            var invalidRow = rows.FirstOrDefault(x => !IsValidAttendanceStatus(x.Status));
            if (invalidRow is not null)
            {
                MessageBox.Show($"Học viên '{invalidRow.StudentName}' phải nhập điểm danh là C hoặc V.");
                UpdateTeacherAttendanceSaveButtonState();
                return;
            }

            var records = rows.ToDictionary(x => x.StudentId, x => x.Status.Trim().ToUpperInvariant());
            _attendanceService.SaveAttendance(classId, _currentUser.TeacherId.Value, dtpSessionDate.Value, records);
            MessageBox.Show("Đã lưu điểm danh.");
            UpdateTeacherAttendanceSaveButtonState();
        }

        private void btnSaveEvaluation_Click(object sender, EventArgs e)
        {
            if (_currentUser.TeacherId is null)
            {
                MessageBox.Show("Chỉ giáo viên mới nhập nhận xét.");
                return;
            }

            _evaluationService.Save(
                cmbStudentEvaluate.SelectedValue is int studentId ? studentId : 0,
                cmbClassEvaluate.SelectedValue is int classId ? classId : 0,
                _currentUser.TeacherId.Value,
                decimal.TryParse(txtScore.Text, out var score) ? score : null,
                txtComment.Text.Trim());

            MessageBox.Show("Đã lưu nhận xét/điểm.");
        }

        private void InitTimesheetCombos()
        {
            cmbTimesheetMonth.Items.Clear();
            for (int i = 1; i <= 12; i++)
                cmbTimesheetMonth.Items.Add(i);
            cmbTimesheetMonth.SelectedItem = DateTime.Now.Month;

            cmbTimesheetYear.Items.Clear();
            for (int y = DateTime.Now.Year - 2; y <= DateTime.Now.Year + 1; y++)
                cmbTimesheetYear.Items.Add(y);
            cmbTimesheetYear.SelectedItem = DateTime.Now.Year;
        }

        private void btnLoadTimesheet_Click(object sender, EventArgs e)
        {
            LoadTimesheet();
        }

        private void cmbTimesheetFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTimesheet();
        }

        private void LoadTimesheet()
        {
            if (_currentUser.TeacherId is not int teacherId) return;
            if (cmbTimesheetMonth.SelectedItem is not int month) return;
            if (cmbTimesheetYear.SelectedItem is not int year) return;

            var records = _timesheetService.GetTimesheetByMonth(teacherId, year, month);

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var table = new System.Data.DataTable();
            table.Columns.Add("Ngày", typeof(string));
            for (int s = 1; s <= 5; s++)
                table.Columns.Add(Models.TeacherTimesheet.GetShiftDescription(s), typeof(string));
            table.Columns.Add("Ghi chú", typeof(string));

            for (int d = 1; d <= daysInMonth; d++)
            {
                var row = table.NewRow();
                row["Ngày"] = $"{d:D2}/{month:D2}/{year}";
                string dayNote = "";
                for (int s = 1; s <= 5; s++)
                {
                    var rec = records.FirstOrDefault(r => r.WorkDate.Day == d && r.ShiftNumber == s);
                    row[s] = rec != null ? (rec.IsPresent ? "✓" : "✗") : "";
                    if (rec?.Note is not null && rec.Note.Length > 0 && dayNote.Length == 0)
                        dayNote = rec.Note;
                }
                row["Ghi chú"] = dayNote;
                table.Rows.Add(row);
            }

            dgvTimesheet.DataSource = table;

            if (dgvTimesheet.Columns.Count > 0)
                dgvTimesheet.Columns[0].ReadOnly = true;

            var totalShifts = records.Count(r => r.IsPresent);
            lblTimesheetSummary.Text = $"Tổng ca: {totalShifts}";
        }

        private void btnSaveTimesheet_Click(object sender, EventArgs e)
        {
            if (_currentUser.TeacherId is not int teacherId) return;
            if (cmbTimesheetMonth.SelectedItem is not int month) return;
            if (cmbTimesheetYear.SelectedItem is not int year) return;

            var skippedPastDates = 0;

            for (int rowIdx = 0; rowIdx < dgvTimesheet.Rows.Count; rowIdx++)
            {
                int day = rowIdx + 1;
                var workDate = new DateTime(year, month, day);

                if (_currentUser.Role == "Teacher" && workDate.Date < DateTime.Today)
                {
                    skippedPastDates++;
                    continue;
                }

                var note = dgvTimesheet.Rows[rowIdx].Cells["Ghi chú"].Value?.ToString()?.Trim() ?? "";

                for (int shift = 1; shift <= 5; shift++)
                {
                    var cellValue = dgvTimesheet.Rows[rowIdx].Cells[shift].Value?.ToString()?.Trim().ToUpper() ?? "";
                    bool isPresent = cellValue == "C" || cellValue == "✓";
                    if (cellValue == "C" || cellValue == "✓" || cellValue == "✗")
                    {
                        _timesheetService.SaveTimesheet(teacherId, workDate, shift, isPresent, note.Length > 0 ? note : null);
                    }
                }
            }

            if (skippedPastDates > 0)
            {
                MessageBox.Show("Đã lưu chấm công. Các ngày trước hôm nay không được phép sửa bằng tài khoản giáo viên.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Đã lưu chấm công thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            LoadTimesheet();
        }

        private void btnLoadTeacherSchedule_Click(object sender, EventArgs e)
        {
            LoadTeacherWeeklySchedule();
        }

        private void LoadTeacherWeeklySchedule()
        {
            if (_currentUser.TeacherId is not int teacherId) return;

            var monday = Models.ClassWeeklySchedule.GetMondayOfWeek(dtpTeacherWeek.Value);
            var schedule = _classScheduleService.GetTeacherScheduleForWeek(teacherId, monday);

            var table = new System.Data.DataTable();
            table.Columns.Add("Ngày", typeof(string));
            for (int s = 1; s <= 5; s++)
                table.Columns.Add(Models.TeacherTimesheet.GetShiftDescription(s), typeof(string));

            for (int d = 0; d < 7; d++)
            {
                var row = table.NewRow();
                row["Ngày"] = Models.ClassWeeklySchedule.GetDayName(d);
                for (int s = 1; s <= 5; s++)
                {
                    var classes = schedule.Where(x => x.DayOfWeek == d && x.ShiftNumber == s)
                        .Select(x => x.ClassName).ToList();
                    row[s] = classes.Count > 0 ? string.Join(", ", classes) : "";
                }
                table.Rows.Add(row);
            }

            dgvTeacherWeeklySchedule.DataSource = table;
        }

        private void InitPayrollCombos()
        {
            cmbPayrollMonth.Items.Clear();
            for (int i = 1; i <= 12; i++)
                cmbPayrollMonth.Items.Add(i);
            cmbPayrollMonth.SelectedItem = DateTime.Now.Month;

            cmbPayrollYear.Items.Clear();
            for (int y = DateTime.Now.Year - 2; y <= DateTime.Now.Year + 1; y++)
                cmbPayrollYear.Items.Add(y);
            cmbPayrollYear.SelectedItem = DateTime.Now.Year;
        }

        private void btnLoadPayroll_Click(object sender, EventArgs e)
        {
            LoadPayroll();
        }

        private void cmbPayrollFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPayroll();
        }

        private void LoadPayroll()
        {
            if (cmbPayrollMonth.SelectedItem is not int month) return;
            if (cmbPayrollYear.SelectedItem is not int year) return;

            var teachers = _teacherService.GetAll();
            var table = new System.Data.DataTable();
            table.Columns.Add("TeacherId", typeof(int));
            table.Columns.Add("Giáo viên", typeof(string));
            table.Columns.Add("Tổng ca", typeof(int));
            table.Columns.Add("Lương (VNĐ)", typeof(string));

            foreach (var teacher in teachers)
            {
                var totalShifts = _timesheetService.GetTotalShiftsInMonth(teacher.Id, year, month);
                var pay = totalShifts * Models.TeacherTimesheet.PayPerShift;
                var row = table.NewRow();
                row["TeacherId"] = teacher.Id;
                row["Giáo viên"] = teacher.FullName;
                row["Tổng ca"] = totalShifts;
                row["Lương (VNĐ)"] = FormatCurrency(pay);
                table.Rows.Add(row);
            }

            dgvPayroll.DataSource = table;
            if (dgvPayroll.Columns.Contains("TeacherId"))
                dgvPayroll.Columns["TeacherId"].Visible = false;
        }

        private void dgvPayroll_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPayroll.CurrentRow is null) return;
            if (cmbPayrollMonth.SelectedItem is not int month) return;
            if (cmbPayrollYear.SelectedItem is not int year) return;

            var teacherId = Convert.ToInt32(dgvPayroll.CurrentRow.Cells["TeacherId"].Value);
            var records = _timesheetService.GetTimesheetByMonth(teacherId, year, month);

            var detailTable = new System.Data.DataTable();
            detailTable.Columns.Add("Ngày", typeof(string));
            detailTable.Columns.Add("Ca", typeof(int));
            detailTable.Columns.Add("Trạng thái", typeof(string));
            detailTable.Columns.Add("Ghi chú", typeof(string));

            foreach (var r in records)
            {
                var row = detailTable.NewRow();
                row["Ngày"] = r.WorkDate.ToString("yyyy-MM-dd");
                row["Ca"] = r.ShiftNumber;
                row["Trạng thái"] = r.IsPresent ? "Có mặt" : "Vắng";
                row["Ghi chú"] = r.Note ?? "";
                detailTable.Rows.Add(row);
            }

            dgvPayrollDetail.DataSource = detailTable;
        }
    }

    public class AttendanceRow
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
