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

        private List<Student> _students = new();
        private List<Course> _courses = new();
        private List<ClassInfo> _classes = new();
        private List<Teacher> _teachers = new();

        public bool RequestLogout { get; private set; }

        public Form1(AuthenticatedUser user)
        {
            _currentUser = user;
            InitializeComponent();
            Text = $"Nhat Duc Software - {_currentUser.Role}: {_currentUser.Username}";
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
                [nameof(Student.Language)] = "Ngôn ngữ",
                [nameof(Student.Status)] = "Trạng thái"
            });
        }

        private void ApplyCourseHeaders()
        {
            SetGridHeaders(dgvCourses, new Dictionary<string, string>
            {
                [nameof(Course.Id)] = "Mã khóa",
                [nameof(Course.Code)] = "Mã cấp độ",
                [nameof(Course.Name)] = "Tên khóa học",
                [nameof(Course.Language)] = "Ngôn ngữ",
                [nameof(Course.TuitionFee)] = "Học phí",
                [nameof(Course.DurationHours)] = "Thời lượng (giờ)",
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
                [nameof(ClassInfo.MaxSize)] = "Sĩ số tối đa",
                [nameof(ClassInfo.CurrentSize)] = "Sĩ số hiện tại",
                [nameof(ClassInfo.Status)] = "Trạng thái",
                [nameof(ClassInfo.ScheduleText)] = "Lịch học"
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
            cmbCourseClass.DisplayMember = nameof(Course.Code);
            cmbCourseClass.ValueMember = nameof(Course.Id);

            cmbCourseAssign.DataSource = null;
            cmbCourseAssign.DataSource = _courses.ToList();
            cmbCourseAssign.DisplayMember = nameof(Course.Code);
            cmbCourseAssign.ValueMember = nameof(Course.Id);
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

            cmbClassEvaluate.DataSource = null;
            cmbClassEvaluate.DataSource = _classes.ToList();
            cmbClassEvaluate.DisplayMember = nameof(ClassInfo.ClassName);
            cmbClassEvaluate.ValueMember = nameof(ClassInfo.Id);

            cmbClassAddStudent.DataSource = null;
            cmbClassAddStudent.DataSource = _classes.ToList();
            cmbClassAddStudent.DisplayMember = nameof(ClassInfo.ClassName);
            cmbClassAddStudent.ValueMember = nameof(ClassInfo.Id);

            var teacherClasses = _currentUser.Role == "Teacher"
                ? _classes.Where(c => c.TeacherId == _currentUser.TeacherId).ToList()
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
                    Language = cmbStudentLanguage.Text,
                    Status = cmbStudentStatus.Text
                });
                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                s.Language = cmbStudentLanguage.Text;
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
            cmbStudentLanguage.Text = s.Language;
            cmbStudentStatus.Text = s.Status;
        }

        private void btnAddCourse_Click(object sender, EventArgs e)
        {
            _courseService.Add(new Course
            {
                Code = txtCourseCode.Text.Trim(),
                Name = txtCourseName.Text.Trim(),
                Language = cmbCourseLanguage.Text,
                TuitionFee = decimal.TryParse(txtCourseFee.Text, out var fee) ? fee : 0,
                DurationHours = int.TryParse(txtCourseDuration.Text, out var duration) ? duration : 0,
                Status = "Active"
            });
            LoadCoursesToCombos();
        }

        private void btnAssignCourse_Click(object sender, EventArgs e)
        {
            if (dgvStudents.CurrentRow?.DataBoundItem is not Student s || cmbCourseAssign.SelectedValue is not int courseId)
            {
                return;
            }

            _studentService.AssignCourse(s.Id, courseId);
            MessageBox.Show("Đã gán khóa học cho học viên.");
        }

        private void btnCreateClass_Click(object sender, EventArgs e)
        {
            int? teacherId = cmbTeacherClass.SelectedValue is int selectedTeacherId ? selectedTeacherId : null;

            _classService.AddClass(
                txtClassName.Text.Trim(),
                cmbCourseClass.SelectedValue is int courseId ? courseId : 0,
                teacherId,
                int.TryParse(txtClassMaxSize.Text, out var maxSize) ? maxSize : 20,
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnAddTeacher_Click(object sender, EventArgs e)
        {
            try
            {
                _teacherService.Add(new Teacher
                {
                    FullName = txtTeacherName.Text.Trim(),
                    Phone = txtTeacherPhone.Text.Trim(),
                    Email = txtTeacherEmail.Text.Trim(),
                    Status = cmbTeacherStatus.Text
                });

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
                return;
            }

            txtTeacherName.Text = teacher.FullName;
            txtTeacherPhone.Text = teacher.Phone;
            txtTeacherEmail.Text = teacher.Email;
            cmbTeacherStatus.Text = teacher.Status;
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

            lblPaymentNeed.Text = $"Cần đóng: {total:N0}";
            lblPaymentPaid.Text = $"Đã đóng: {paid:N0}";
            lblPaymentRemain.Text = $"Còn lại: {remaining:N0}";
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

            var students = _attendanceService.GetStudentsByClass(classId)
                .Select(x => new AttendanceRow { StudentId = x.StudentId, StudentName = x.FullName, Status = "Present" })
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
        public string Status { get; set; } = "Present";
    }
}
