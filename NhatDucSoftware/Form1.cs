using System.Drawing.Drawing2D;
using NhatDucSoftware.Models;
using NhatDucSoftware.Services;

namespace NhatDucSoftware
{
    public partial class Form1 : Form
    {
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

        private List<Student> _students = new();
        private List<Course> _courses = new();
        private List<ClassInfo> _classes = new();
        private List<Teacher> _teachers = new();
        private List<RevenueByYearStat> _revenueByYear = new();

        public bool RequestLogout { get; private set; }

        public Form1(AuthenticatedUser user)
        {
            _currentUser = user;
            InitializeComponent();
            Text = $"Nhat Duc Software - {_currentUser.Role}: {_currentUser.Username}";
            UiBackgroundHelper.ApplyBackground(this);
            AddCopyrightLabel();
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
            }
            else
            {
                tabTeacher.Parent = null;
            }

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
            dgvTeacherClasses.DataSource = teacherClasses;
            ApplyClassHeaders(dgvTeacherClasses);
        }

        private void LoadReports()
        {
            var summary = _reportService.GetSummary();
            lblTotalStudents.Text = $"Tổng học viên: {summary.TotalStudents}";
            lblTotalRevenue.Text = $"Doanh thu: {summary.TotalRevenue:N0}";
            lblActiveClasses.Text = $"Lớp hoạt động: {summary.ActiveClasses}";

            _revenueByYear = _reportService.GetRevenueByYear();
            dgvRevenueByYear.DataSource = null;
            dgvRevenueByYear.DataSource = _revenueByYear;
            ApplyRevenueByYearHeaders();
            pnlRevenueChart.Invalidate();
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

            if (_revenueByYear.Count == 0)
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

            var data = _revenueByYear.OrderBy(x => x.Year).ToList();

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
                var yLabel = $"{value:N0}";
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

                var yearText = item.Year.ToString();
                var yearSize = g.MeasureString(yearText, axisFont);
                g.DrawString(yearText, axisFont, labelBrush, x + (barWidth - yearSize.Width) / 2, plot.Bottom + 6);

                var valueText = item.TotalRevenue.ToString("N0");
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
            if (dgvStudents.CurrentRow?.DataBoundItem is not Student s)
            {
                return;
            }

            _studentService.Delete(s.Id);
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
                    TuitionFee = decimal.TryParse(txtCourseFee.Text, out var fee) ? fee : 0,
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
            c.TuitionFee = decimal.TryParse(txtCourseFee.Text, out var fee) ? fee : 0;

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
            txtCourseFee.Text = c.TuitionFee.ToString();
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
                LoadClasses();
                LoadClassStudents();
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
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c) return;
            if (MessageBox.Show($"Xóa lớp '{c.ClassName}'?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            _classService.DeleteClass(c.Id);
            LoadClasses();
        }

        private void dgvClasses_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c) return;
            txtClassName.Text = c.ClassName;
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
        }

        private void btnRemoveStudentFromClass_Click(object sender, EventArgs e)
        {
            if (dgvClasses.CurrentRow?.DataBoundItem is not ClassInfo c) return;
            if (dgvClassStudents.CurrentRow is null) return;

            var studentId = (int)dgvClassStudents.CurrentRow.Cells["StudentId"].Value;
            _classService.RemoveStudentFromClass(c.Id, studentId);
            LoadClasses();
            LoadClassStudents();
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
            if (dgvTeachers.CurrentRow?.DataBoundItem is not Teacher teacher)
            {
                return;
            }

            try
            {
                _teacherService.Delete(teacher.Id);
                LoadTeacherManagement();
                LoadTeachers();
                LoadClasses();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

        private void btnLoadPayment_Click(object sender, EventArgs e)
        {
            if (cmbStudentPayment.SelectedValue is not int studentId)
            {
                return;
            }

            var total = _paymentService.GetTotalTuitionByStudent(studentId);
            var paid = _paymentService.GetPaidAmount(studentId);
            var remaining = total - paid;
            var (totalSessions, attended, absent) = _paymentService.GetAttendanceSummary(studentId);

            lblPaymentNeed.Text = $"Cần đóng: {total:N0} | Buổi học: {totalSessions} (Có mặt: {attended}, Vắng: {absent})";
            lblPaymentPaid.Text = $"Đã đóng: {paid:N0}";
            lblPaymentRemain.Text = $"Còn lại: {remaining:N0}";

            var history = _paymentService.GetPaymentHistory(studentId);
            dgvAttendanceDetail.DataSource = null;
            dgvAttendanceDetail.DataSource = history;
            if (dgvAttendanceDetail.Columns.Count > 0)
            {
                dgvAttendanceDetail.Columns["NgayThu"].HeaderText = "Ngày thu";
                dgvAttendanceDetail.Columns["SoTien"].HeaderText = "Số tiền";
                dgvAttendanceDetail.Columns["NguoiThu"].HeaderText = "Người thu";
                dgvAttendanceDetail.Columns["GhiChu"].HeaderText = "Ghi chú";
                dgvAttendanceDetail.Columns["SoTien"].DefaultCellStyle.Format = "N0";
            }
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

        private void txtSearchStudent_TextChanged(object sender, EventArgs e)
        {
            FilterPaymentStudents();
        }

        private void FilterPaymentStudents()
        {
            var classId = cmbPaymentFilterClass.SelectedValue is int id ? id : 0;
            var search = txtSearchStudent.Text.Trim().ToLower();

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

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(s => s.FullName.ToLower().Contains(search)).ToList();
            }

            cmbStudentPayment.DataSource = null;
            cmbStudentPayment.DataSource = filtered;
            cmbStudentPayment.DisplayMember = nameof(Student.FullName);
            cmbStudentPayment.ValueMember = nameof(Student.Id);
        }

        private void btnCollectPayment_Click(object sender, EventArgs e)
        {
            if (cmbStudentPayment.SelectedValue is not int studentId)
            {
                return;
            }

            var amount = decimal.TryParse(txtPaymentAmount.Text, out var value) ? value : 0;
            _paymentService.Collect(studentId, amount, _currentUser.Id, txtPaymentNote.Text.Trim());
            btnLoadPayment_Click(sender, e);
            LoadReports();
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
        }

        private void btnSaveAttendance_Click(object sender, EventArgs e)
        {
            if (_currentUser.TeacherId is null || cmbClassAttendance.SelectedValue is not int classId)
            {
                MessageBox.Show("Chỉ giáo viên mới được điểm danh.");
                return;
            }

            var rows = dgvAttendance.DataSource as List<AttendanceRow>;
            if (rows is null || rows.Count == 0)
            {
                return;
            }

            var records = rows.ToDictionary(x => x.StudentId, x => x.Status);
            _attendanceService.SaveAttendance(classId, _currentUser.TeacherId.Value, dtpSessionDate.Value, records);
            MessageBox.Show("Đã lưu điểm danh.");
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

        private void LoadTimesheet()
        {
            if (_currentUser.TeacherId is not int teacherId) return;
            if (cmbTimesheetMonth.SelectedItem is not int month) return;
            if (cmbTimesheetYear.SelectedItem is not int year) return;

            var records = _timesheetService.GetTimesheetByMonth(teacherId, year, month);

            // Build pivot table: rows = dates, columns = Ca 1..5
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

            // Make "Ngày" column readonly
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

            for (int rowIdx = 0; rowIdx < dgvTimesheet.Rows.Count; rowIdx++)
            {
                int day = rowIdx + 1;
                var workDate = new DateTime(year, month, day);
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

            MessageBox.Show("Đã lưu chấm công thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Build pivot table: rows = days, columns = shifts
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
                row["Lương (VNĐ)"] = pay.ToString("N0");
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

        private void btnLogout_Click(object sender, EventArgs e)
        {
            RequestLogout = true;
            Close();
        }
    }

    public class AttendanceRow
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
