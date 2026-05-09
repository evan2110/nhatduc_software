namespace NhatDucSoftware
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            tabMain = new TabControl();
            tabAdmin = new TabPage();
            tabAdminFunctions = new TabControl();
            tabAdminStudents = new TabPage();
            lblStudentName = new Label();
            lblStudentPhone = new Label();
            lblStudentEmail = new Label();
            lblStudentBirthYear = new Label();
            lblStudentAddress = new Label();
            lblStudentStatus = new Label();
            dgvStudents = new DataGridView();
            txtStudentName = new TextBox();
            txtStudentPhone = new TextBox();
            txtStudentEmail = new TextBox();
            txtStudentBirthYear = new TextBox();
            txtStudentAddress = new TextBox();
            cmbStudentStatus = new ComboBox();
            btnAddStudent = new Button();
            btnUpdateStudent = new Button();
            btnDeleteStudent = new Button();
            btnImportStudents = new Button();
            btnExportStudents = new Button();
            tabAdminCourses = new TabPage();
            lblCourseName = new Label();
            lblCourseFee = new Label();
            dgvCourses = new DataGridView();
            txtCourseName = new TextBox();
            txtCourseFee = new TextBox();
            btnAddCourse = new Button();
            btnUpdateCourse = new Button();
            tabAdminClasses = new TabPage();
            lblClassName = new Label();
            lblClassCourse = new Label();
            lblClassTeacher = new Label();
            lblSelectClass = new Label();
            lblSelectStudent = new Label();
            dgvClasses = new DataGridView();
            txtClassName = new TextBox();
            cmbCourseClass = new ComboBox();
            cmbTeacherClass = new ComboBox();
            btnCreateClass = new Button();
            btnUpdateClass = new Button();
            btnDeleteClass = new Button();
            cmbClassAddStudent = new ComboBox();
            cmbStudentClass = new ComboBox();
            btnAddStudentToClass = new Button();
            dgvClassStudents = new DataGridView();
            btnRemoveStudentFromClass = new Button();
            btnClassSchedule = new Button();
            tabAdminPayments = new TabPage();
            lblPaymentFilterClass = new Label();
            cmbPaymentFilterClass = new ComboBox();
            lblPaymentStudent = new Label();
            cmbStudentPayment = new ComboBox();
            lblPaymentNeed = new Label();
            lblPaymentPaid = new Label();
            lblPaymentRemain = new Label();
            lblPaymentAmount = new Label();
            txtPaymentAmount = new TextBox();
            lblPaymentNote = new Label();
            txtPaymentNote = new TextBox();
            btnCollectPayment = new Button();
            btnExportPayment = new Button();
            btnEditPaymentHistory = new Button();
            btnDeletePaymentHistory = new Button();
            btnFinalizePayment = new Button();
            dgvAttendanceDetail = new DataGridView();
            tabAdminReports = new TabPage();
            lblTotalStudents = new Label();
            lblTotalRevenue = new Label();
            lblActiveClasses = new Label();
            dgvRevenueByYear = new DataGridView();
            lblRevenueChartTitle = new Label();
            pnlRevenueChart = new Panel();
            tabAdminTeachers = new TabPage();
            lblTeacherName = new Label();
            lblTeacherPhone = new Label();
            lblTeacherEmail = new Label();
            lblTeacherStatus = new Label();
            dgvTeachers = new DataGridView();
            txtTeacherName = new TextBox();
            txtTeacherPhone = new TextBox();
            txtTeacherEmail = new TextBox();
            cmbTeacherStatus = new ComboBox();
            btnAddTeacher = new Button();
            btnUpdateTeacher = new Button();
            btnDeleteTeacher = new Button();
            lblTeacherAccount = new Label();
            txtTeacherUsername = new TextBox();
            lblTeacherPassword = new Label();
            txtTeacherPassword = new TextBox();
            btnUpdateTeacherPassword = new Button();
            tabAdminPayroll = new TabPage();
            lblPayrollMonth = new Label();
            cmbPayrollMonth = new ComboBox();
            cmbPayrollYear = new ComboBox();
            dgvPayroll = new DataGridView();
            dgvPayrollDetail = new DataGridView();
            btnLogoutAdmin = new Button();
            tabTeacher = new TabPage();
            tabTeacherFunctions = new TabControl();
            tabTeacherTimesheet = new TabPage();
            lblTimesheetMonth = new Label();
            cmbTimesheetMonth = new ComboBox();
            cmbTimesheetYear = new ComboBox();
            btnSaveTimesheet = new Button();
            dgvTimesheet = new DataGridView();
            lblTimesheetSummary = new Label();
            tabTeacherSchedule = new TabPage();
            lblTeacherWeek = new Label();
            dtpTeacherWeek = new DateTimePicker();
            btnLoadTeacherSchedule = new Button();
            dgvTeacherWeeklySchedule = new DataGridView();
            dgvTeacherClasses = new DataGridView();
            tabTeacherAttendance = new TabPage();
            lblAttendanceClass = new Label();
            lblAttendanceDate = new Label();
            cmbClassAttendance = new ComboBox();
            dtpSessionDate = new DateTimePicker();
            dgvAttendance = new DataGridView();
            btnSaveAttendance = new Button();
            tabTeacherEvaluation = new TabPage();
            lblEvaluateClass = new Label();
            lblEvaluateStudent = new Label();
            lblEvaluateScore = new Label();
            lblEvaluateComment = new Label();
            cmbClassEvaluate = new ComboBox();
            cmbStudentEvaluate = new ComboBox();
            txtScore = new TextBox();
            txtComment = new TextBox();
            btnSaveEvaluation = new Button();
            btnLogoutTeacher = new Button();
            btnLogoutGlobal = new Button();
            tabMain.SuspendLayout();
            tabAdmin.SuspendLayout();
            tabAdminFunctions.SuspendLayout();
            tabAdminStudents.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvStudents).BeginInit();
            tabAdminCourses.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCourses).BeginInit();
            tabAdminClasses.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvClasses).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvClassStudents).BeginInit();
            tabAdminPayments.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAttendanceDetail).BeginInit();
            tabAdminReports.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvRevenueByYear).BeginInit();
            tabAdminTeachers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTeachers).BeginInit();
            tabAdminPayroll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPayroll).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvPayrollDetail).BeginInit();
            tabTeacher.SuspendLayout();
            tabTeacherFunctions.SuspendLayout();
            tabTeacherTimesheet.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTimesheet).BeginInit();
            tabTeacherSchedule.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTeacherWeeklySchedule).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTeacherClasses).BeginInit();
            tabTeacherAttendance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAttendance).BeginInit();
            tabTeacherEvaluation.SuspendLayout();
            SuspendLayout();
            // 
            // tabMain
            // 
            tabMain.Controls.Add(tabAdmin);
            tabMain.Controls.Add(tabTeacher);
            tabMain.Dock = DockStyle.Fill;
            tabMain.Location = new Point(0, 0);
            tabMain.Name = "tabMain";
            tabMain.SelectedIndex = 0;
            tabMain.Size = new Size(1040, 560);
            tabMain.TabIndex = 0;
            // 
            // tabAdmin
            // 
            tabAdmin.Controls.Add(tabAdminFunctions);
            tabAdmin.Controls.Add(btnLogoutAdmin);
            tabAdmin.Location = new Point(4, 24);
            tabAdmin.Name = "tabAdmin";
            tabAdmin.Size = new Size(1032, 532);
            tabAdmin.TabIndex = 0;
            tabAdmin.Text = "Admin";
            // 
            // tabAdminFunctions
            // 
            tabAdminFunctions.Controls.Add(tabAdminStudents);
            tabAdminFunctions.Controls.Add(tabAdminCourses);
            tabAdminFunctions.Controls.Add(tabAdminClasses);
            tabAdminFunctions.Controls.Add(tabAdminPayments);
            tabAdminFunctions.Controls.Add(tabAdminReports);
            tabAdminFunctions.Controls.Add(tabAdminTeachers);
            tabAdminFunctions.Controls.Add(tabAdminPayroll);
            tabAdminFunctions.Dock = DockStyle.Fill;
            tabAdminFunctions.Location = new Point(0, 0);
            tabAdminFunctions.Name = "tabAdminFunctions";
            tabAdminFunctions.SelectedIndex = 0;
            tabAdminFunctions.Size = new Size(1032, 532);
            tabAdminFunctions.TabIndex = 0;
            // 
            // tabAdminStudents
            // 
            btnViewEvaluations = new Button();
            tabAdminStudents.Controls.Add(lblStudentName);
            tabAdminStudents.Controls.Add(lblStudentPhone);
            tabAdminStudents.Controls.Add(lblStudentEmail);
            tabAdminStudents.Controls.Add(lblStudentBirthYear);
            tabAdminStudents.Controls.Add(lblStudentAddress);
            tabAdminStudents.Controls.Add(lblStudentStatus);
            tabAdminStudents.Controls.Add(dgvStudents);
            tabAdminStudents.Controls.Add(txtStudentName);
            tabAdminStudents.Controls.Add(txtStudentPhone);
            tabAdminStudents.Controls.Add(txtStudentEmail);
            tabAdminStudents.Controls.Add(txtStudentBirthYear);
            tabAdminStudents.Controls.Add(txtStudentAddress);
            tabAdminStudents.Controls.Add(cmbStudentStatus);
            tabAdminStudents.Controls.Add(btnAddStudent);
            tabAdminStudents.Controls.Add(btnUpdateStudent);
            tabAdminStudents.Controls.Add(btnDeleteStudent);
            tabAdminStudents.Controls.Add(btnImportStudents);
            tabAdminStudents.Controls.Add(btnExportStudents);
            tabAdminStudents.Controls.Add(btnViewEvaluations);
            tabAdminStudents.Location = new Point(4, 24);
            tabAdminStudents.Name = "tabAdminStudents";
            tabAdminStudents.Size = new Size(1024, 504);
            tabAdminStudents.TabIndex = 0;
            tabAdminStudents.Text = "Học viên";
            // 
            // lblStudentName
            // 
            lblStudentName.Location = new Point(680, 12);
            lblStudentName.Name = "lblStudentName";
            lblStudentName.Size = new Size(120, 15);
            lblStudentName.TabIndex = 0;
            lblStudentName.Text = "Họ và tên";
            lblStudentName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblStudentPhone
            // 
            lblStudentPhone.Location = new Point(680, 49);
            lblStudentPhone.Name = "lblStudentPhone";
            lblStudentPhone.Size = new Size(120, 15);
            lblStudentPhone.TabIndex = 1;
            lblStudentPhone.Text = "Số điện thoại";
            lblStudentPhone.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblStudentEmail
            // 
            lblStudentEmail.Location = new Point(680, 95);
            lblStudentEmail.Name = "lblStudentEmail";
            lblStudentEmail.Size = new Size(120, 15);
            lblStudentEmail.TabIndex = 2;
            lblStudentEmail.Text = "Email";
            lblStudentEmail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblStudentBirthYear
            // 
            lblStudentBirthYear.Location = new Point(680, 139);
            lblStudentBirthYear.Name = "lblStudentBirthYear";
            lblStudentBirthYear.Size = new Size(120, 15);
            lblStudentBirthYear.TabIndex = 3;
            lblStudentBirthYear.Text = "Năm sinh";
            lblStudentBirthYear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblStudentAddress
            // 
            lblStudentAddress.Location = new Point(680, 184);
            lblStudentAddress.Name = "lblStudentAddress";
            lblStudentAddress.Size = new Size(120, 15);
            lblStudentAddress.TabIndex = 4;
            lblStudentAddress.Text = "Địa chỉ";
            lblStudentAddress.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblStudentStatus
            // 
            lblStudentStatus.Location = new Point(680, 228);
            lblStudentStatus.Name = "lblStudentStatus";
            lblStudentStatus.Size = new Size(120, 15);
            lblStudentStatus.TabIndex = 5;
            lblStudentStatus.Text = "Trạng thái";
            lblStudentStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // dgvStudents
            // 
            dgvStudents.Location = new Point(10, 10);
            dgvStudents.Name = "dgvStudents";
            dgvStudents.Size = new Size(650, 430);
            dgvStudents.TabIndex = 6;
            dgvStudents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvStudents.SelectionChanged += dgvStudents_SelectionChanged;
            dgvStudents.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // txtStudentName
            // 
            txtStudentName.Location = new Point(680, 26);
            txtStudentName.Name = "txtStudentName";
            txtStudentName.Size = new Size(300, 23);
            txtStudentName.TabIndex = 7;
            txtStudentName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtStudentPhone
            // 
            txtStudentPhone.Location = new Point(680, 67);
            txtStudentPhone.Name = "txtStudentPhone";
            txtStudentPhone.Size = new Size(300, 23);
            txtStudentPhone.TabIndex = 8;
            txtStudentPhone.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtStudentEmail
            // 
            txtStudentEmail.Location = new Point(680, 113);
            txtStudentEmail.Name = "txtStudentEmail";
            txtStudentEmail.Size = new Size(300, 23);
            txtStudentEmail.TabIndex = 9;
            txtStudentEmail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtStudentBirthYear
            // 
            txtStudentBirthYear.Location = new Point(680, 157);
            txtStudentBirthYear.Name = "txtStudentBirthYear";
            txtStudentBirthYear.Size = new Size(300, 23);
            txtStudentBirthYear.TabIndex = 10;
            txtStudentBirthYear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtStudentAddress
            // 
            txtStudentAddress.Location = new Point(680, 202);
            txtStudentAddress.Name = "txtStudentAddress";
            txtStudentAddress.Size = new Size(300, 23);
            txtStudentAddress.TabIndex = 11;
            txtStudentAddress.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // cmbStudentStatus
            // 
            cmbStudentStatus.Items.AddRange(new object[] { "Active", "Inactive" });
            cmbStudentStatus.Location = new Point(682, 246);
            cmbStudentStatus.Name = "cmbStudentStatus";
            cmbStudentStatus.Size = new Size(145, 23);
            cmbStudentStatus.TabIndex = 12;
            cmbStudentStatus.Text = "Active";
            cmbStudentStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnAddStudent
            // 
            btnAddStudent.Location = new Point(680, 281);
            btnAddStudent.Name = "btnAddStudent";
            btnAddStudent.Size = new Size(95, 30);
            btnAddStudent.TabIndex = 13;
            btnAddStudent.Text = "Thêm";
            btnAddStudent.Click += btnAddStudent_Click;
            btnAddStudent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnUpdateStudent
            // 
            btnUpdateStudent.Location = new Point(782, 281);
            btnUpdateStudent.Name = "btnUpdateStudent";
            btnUpdateStudent.Size = new Size(95, 30);
            btnUpdateStudent.TabIndex = 14;
            btnUpdateStudent.Text = "Sửa";
            btnUpdateStudent.Click += btnUpdateStudent_Click;
            btnUpdateStudent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnDeleteStudent
            // 
            btnDeleteStudent.Location = new Point(885, 281);
            btnDeleteStudent.Name = "btnDeleteStudent";
            btnDeleteStudent.Size = new Size(95, 30);
            btnDeleteStudent.TabIndex = 15;
            btnDeleteStudent.Text = "Xóa";
            btnDeleteStudent.Click += btnDeleteStudent_Click;
            btnDeleteStudent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnImportStudents
            // 
            btnImportStudents.Location = new Point(680, 358);
            btnImportStudents.Name = "btnImportStudents";
            btnImportStudents.Size = new Size(300, 30);
            btnImportStudents.TabIndex = 17;
            btnImportStudents.Text = "Nhập hàng loạt từ file TXT";
            btnImportStudents.Click += btnImportStudents_Click;
            btnImportStudents.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnExportStudents
            // 
            btnExportStudents.Location = new Point(680, 394);
            btnExportStudents.Name = "btnExportStudents";
            btnExportStudents.Size = new Size(300, 30);
            btnExportStudents.TabIndex = 18;
            btnExportStudents.Text = "Xuất Excel học viên";
            btnExportStudents.Click += btnExportStudents_Click;
            btnExportStudents.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnViewEvaluations
            // 
            btnViewEvaluations.Location = new Point(680, 322);
            btnViewEvaluations.Name = "btnViewEvaluations";
            btnViewEvaluations.Size = new Size(300, 30);
            btnViewEvaluations.TabIndex = 16;
            btnViewEvaluations.Text = "Xem Điểm / Nhận xét";
            btnViewEvaluations.Click += btnViewEvaluations_Click;
            btnViewEvaluations.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // tabAdminCourses
            // 
            tabAdminCourses.Controls.Add(lblCourseName);
            tabAdminCourses.Controls.Add(lblCourseFee);
            tabAdminCourses.Controls.Add(dgvCourses);
            tabAdminCourses.Controls.Add(txtCourseName);
            tabAdminCourses.Controls.Add(txtCourseFee);
            tabAdminCourses.Controls.Add(btnAddCourse);
            tabAdminCourses.Controls.Add(btnUpdateCourse);
            tabAdminCourses.Location = new Point(4, 24);
            tabAdminCourses.Name = "tabAdminCourses";
            tabAdminCourses.Size = new Size(1024, 504);
            tabAdminCourses.TabIndex = 1;
            tabAdminCourses.Text = "Khóa học";
            // 
            // lblCourseName
            // 
            lblCourseName.Location = new Point(730, 22);
            lblCourseName.Name = "lblCourseName";
            lblCourseName.Size = new Size(120, 15);
            lblCourseName.TabIndex = 0;
            lblCourseName.Text = "Tên khóa học";
            lblCourseName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblCourseFee
            // 
            lblCourseFee.Location = new Point(730, 68);
            lblCourseFee.Name = "lblCourseFee";
            lblCourseFee.Size = new Size(120, 15);
            lblCourseFee.TabIndex = 1;
            lblCourseFee.Text = "Học phí";
            lblCourseFee.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // dgvCourses
            // 
            dgvCourses.Location = new Point(10, 10);
            dgvCourses.Name = "dgvCourses";
            dgvCourses.Size = new Size(700, 430);
            dgvCourses.TabIndex = 2;
            dgvCourses.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCourses.SelectionChanged += dgvCourses_SelectionChanged;
            dgvCourses.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // txtCourseName
            // 
            txtCourseName.Location = new Point(730, 40);
            txtCourseName.Name = "txtCourseName";
            txtCourseName.Size = new Size(270, 23);
            txtCourseName.TabIndex = 3;
            txtCourseName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtCourseFee
            // 
            txtCourseFee.Location = new Point(730, 86);
            txtCourseFee.Name = "txtCourseFee";
            txtCourseFee.Size = new Size(270, 23);
            txtCourseFee.TabIndex = 4;
            txtCourseFee.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnAddCourse
            // 
            btnAddCourse.Location = new Point(730, 120);
            btnAddCourse.Name = "btnAddCourse";
            btnAddCourse.Size = new Size(130, 28);
            btnAddCourse.TabIndex = 5;
            btnAddCourse.Text = "Thêm khóa";
            btnAddCourse.Click += btnAddCourse_Click;
            btnAddCourse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnUpdateCourse
            // 
            btnUpdateCourse.Location = new Point(870, 120);
            btnUpdateCourse.Name = "btnUpdateCourse";
            btnUpdateCourse.Size = new Size(130, 28);
            btnUpdateCourse.TabIndex = 6;
            btnUpdateCourse.Text = "Chỉnh sửa";
            btnUpdateCourse.Click += btnUpdateCourse_Click;
            btnUpdateCourse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // tabAdminClasses
            // 
            tabAdminClasses.Controls.Add(lblClassName);
            tabAdminClasses.Controls.Add(lblClassCourse);
            tabAdminClasses.Controls.Add(lblClassTeacher);
            tabAdminClasses.Controls.Add(lblSelectClass);
            tabAdminClasses.Controls.Add(lblSelectStudent);
            tabAdminClasses.Controls.Add(dgvClasses);
            tabAdminClasses.Controls.Add(txtClassName);
            tabAdminClasses.Controls.Add(cmbCourseClass);
            tabAdminClasses.Controls.Add(cmbTeacherClass);
            tabAdminClasses.Controls.Add(btnCreateClass);
            tabAdminClasses.Controls.Add(btnUpdateClass);
            tabAdminClasses.Controls.Add(btnDeleteClass);
            tabAdminClasses.Controls.Add(cmbClassAddStudent);
            tabAdminClasses.Controls.Add(cmbStudentClass);
            tabAdminClasses.Controls.Add(btnAddStudentToClass);
            tabAdminClasses.Controls.Add(dgvClassStudents);
            tabAdminClasses.Controls.Add(btnRemoveStudentFromClass);
            tabAdminClasses.Controls.Add(btnClassSchedule);
            tabAdminClasses.Location = new Point(4, 24);
            tabAdminClasses.Name = "tabAdminClasses";
            tabAdminClasses.Size = new Size(1024, 504);
            tabAdminClasses.TabIndex = 2;
            tabAdminClasses.Text = "Lớp học";
            // 
            // lblClassName
            // 
            lblClassName.Location = new Point(730, 12);
            lblClassName.Name = "lblClassName";
            lblClassName.Size = new Size(120, 15);
            lblClassName.TabIndex = 0;
            lblClassName.Text = "Tên lớp";
            lblClassName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblClassCourse
            // 
            lblClassCourse.Location = new Point(730, 57);
            lblClassCourse.Name = "lblClassCourse";
            lblClassCourse.Size = new Size(120, 15);
            lblClassCourse.TabIndex = 1;
            lblClassCourse.Text = "Khóa học";
            lblClassCourse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblClassTeacher
            // 
            lblClassTeacher.Location = new Point(870, 57);
            lblClassTeacher.Name = "lblClassTeacher";
            lblClassTeacher.Size = new Size(120, 15);
            lblClassTeacher.TabIndex = 2;
            lblClassTeacher.Text = "Giáo viên";
            lblClassTeacher.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblSelectClass
            // 
            lblSelectClass.Location = new Point(730, 175);
            lblSelectClass.Name = "lblSelectClass";
            lblSelectClass.Size = new Size(120, 15);
            lblSelectClass.TabIndex = 4;
            lblSelectClass.Text = "Chọn lớp";
            lblSelectClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblSelectStudent
            // 
            lblSelectStudent.Location = new Point(870, 175);
            lblSelectStudent.Name = "lblSelectStudent";
            lblSelectStudent.Size = new Size(120, 15);
            lblSelectStudent.TabIndex = 5;
            lblSelectStudent.Text = "Chọn học viên";
            lblSelectStudent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // dgvClasses
            // 
            dgvClasses.Location = new Point(10, 10);
            dgvClasses.Name = "dgvClasses";
            dgvClasses.Size = new Size(700, 230);
            dgvClasses.TabIndex = 6;
            dgvClasses.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvClasses.SelectionChanged += dgvClasses_SelectionChanged;
            dgvClasses.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // txtClassName
            // 
            txtClassName.Location = new Point(730, 30);
            txtClassName.Name = "txtClassName";
            txtClassName.Size = new Size(270, 23);
            txtClassName.TabIndex = 7;
            txtClassName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // cmbCourseClass
            // 
            cmbCourseClass.Location = new Point(730, 75);
            cmbCourseClass.Name = "cmbCourseClass";
            cmbCourseClass.Size = new Size(130, 23);
            cmbCourseClass.TabIndex = 8;
            cmbCourseClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // cmbTeacherClass
            // 
            cmbTeacherClass.Location = new Point(870, 75);
            cmbTeacherClass.Name = "cmbTeacherClass";
            cmbTeacherClass.Size = new Size(130, 23);
            cmbTeacherClass.TabIndex = 9;
            cmbTeacherClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnCreateClass
            // 
            btnCreateClass.Location = new Point(730, 108);
            btnCreateClass.Name = "btnCreateClass";
            btnCreateClass.Size = new Size(85, 28);
            btnCreateClass.TabIndex = 10;
            btnCreateClass.Text = "Tạo lớp";
            btnCreateClass.Click += btnCreateClass_Click;
            btnCreateClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnUpdateClass
            // 
            btnUpdateClass.Location = new Point(823, 108);
            btnUpdateClass.Name = "btnUpdateClass";
            btnUpdateClass.Size = new Size(85, 28);
            btnUpdateClass.TabIndex = 11;
            btnUpdateClass.Text = "Sửa lớp";
            btnUpdateClass.Click += btnUpdateClass_Click;
            btnUpdateClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnDeleteClass
            // 
            btnDeleteClass.Location = new Point(915, 108);
            btnDeleteClass.Name = "btnDeleteClass";
            btnDeleteClass.Size = new Size(85, 28);
            btnDeleteClass.TabIndex = 12;
            btnDeleteClass.Text = "Xóa lớp";
            btnDeleteClass.Click += btnDeleteClass_Click;
            btnDeleteClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // cmbClassAddStudent
            // 
            cmbClassAddStudent.Location = new Point(730, 193);
            cmbClassAddStudent.Name = "cmbClassAddStudent";
            cmbClassAddStudent.Size = new Size(130, 23);
            cmbClassAddStudent.TabIndex = 13;
            cmbClassAddStudent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // cmbStudentClass
            // 
            cmbStudentClass.Location = new Point(870, 193);
            cmbStudentClass.Name = "cmbStudentClass";
            cmbStudentClass.Size = new Size(130, 23);
            cmbStudentClass.TabIndex = 14;
            cmbStudentClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnAddStudentToClass
            // 
            btnAddStudentToClass.Location = new Point(730, 223);
            btnAddStudentToClass.Name = "btnAddStudentToClass";
            btnAddStudentToClass.Size = new Size(270, 28);
            btnAddStudentToClass.TabIndex = 15;
            btnAddStudentToClass.Text = "Thêm học viên vào lớp";
            btnAddStudentToClass.Click += btnAddStudentToClass_Click;
            btnAddStudentToClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // dgvClassStudents
            // 
            dgvClassStudents.AllowUserToAddRows = false;
            dgvClassStudents.Location = new Point(10, 250);
            dgvClassStudents.Name = "dgvClassStudents";
            dgvClassStudents.ReadOnly = true;
            dgvClassStudents.Size = new Size(700, 200);
            dgvClassStudents.TabIndex = 16;
            dgvClassStudents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvClassStudents.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // btnRemoveStudentFromClass
            // 
            btnRemoveStudentFromClass.Location = new Point(730, 260);
            btnRemoveStudentFromClass.Name = "btnRemoveStudentFromClass";
            btnRemoveStudentFromClass.Size = new Size(270, 28);
            btnRemoveStudentFromClass.TabIndex = 17;
            btnRemoveStudentFromClass.Text = "Xóa học viên khỏi lớp";
            btnRemoveStudentFromClass.Click += btnRemoveStudentFromClass_Click;
            btnRemoveStudentFromClass.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnClassSchedule
            // 
            btnClassSchedule.Location = new Point(730, 295);
            btnClassSchedule.Name = "btnClassSchedule";
            btnClassSchedule.Size = new Size(270, 28);
            btnClassSchedule.TabIndex = 18;
            btnClassSchedule.Text = "Thiết lập lịch học";
            btnClassSchedule.Click += btnClassSchedule_Click;
            btnClassSchedule.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // tabAdminPayments
            // 
            tabAdminPayments.Controls.Add(lblPaymentFilterClass);
            tabAdminPayments.Controls.Add(cmbPaymentFilterClass);
            tabAdminPayments.Controls.Add(lblPaymentStudent);
            tabAdminPayments.Controls.Add(cmbStudentPayment);
            tabAdminPayments.Controls.Add(lblPaymentNeed);
            tabAdminPayments.Controls.Add(lblPaymentPaid);
            tabAdminPayments.Controls.Add(lblPaymentRemain);
            tabAdminPayments.Controls.Add(lblPaymentAmount);
            tabAdminPayments.Controls.Add(txtPaymentAmount);
            tabAdminPayments.Controls.Add(lblPaymentNote);
            tabAdminPayments.Controls.Add(txtPaymentNote);
            tabAdminPayments.Controls.Add(btnCollectPayment);
            tabAdminPayments.Controls.Add(btnExportPayment);
            tabAdminPayments.Controls.Add(btnEditPaymentHistory);
            tabAdminPayments.Controls.Add(btnDeletePaymentHistory);
            tabAdminPayments.Controls.Add(btnFinalizePayment);
            tabAdminPayments.Controls.Add(dgvAttendanceDetail);
            tabAdminPayments.Location = new Point(4, 24);
            tabAdminPayments.Name = "tabAdminPayments";
            tabAdminPayments.Size = new Size(1024, 504);
            tabAdminPayments.TabIndex = 3;
            tabAdminPayments.Text = "Học phí";
            // 
            // lblPaymentFilterClass
            // 
            lblPaymentFilterClass.Location = new Point(20, 12);
            lblPaymentFilterClass.Name = "lblPaymentFilterClass";
            lblPaymentFilterClass.Size = new Size(80, 15);
            lblPaymentFilterClass.TabIndex = 0;
            lblPaymentFilterClass.Text = "Lọc theo lớp:";
            lblPaymentFilterClass.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbPaymentFilterClass
            // 
            cmbPaymentFilterClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentFilterClass.Location = new Point(105, 9);
            cmbPaymentFilterClass.Name = "cmbPaymentFilterClass";
            cmbPaymentFilterClass.Size = new Size(180, 23);
            cmbPaymentFilterClass.TabIndex = 1;
            cmbPaymentFilterClass.SelectedIndexChanged += cmbPaymentFilterClass_SelectedIndexChanged;
            cmbPaymentFilterClass.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblPaymentStudent
            // 
            lblPaymentStudent.Location = new Point(300, 12);
            lblPaymentStudent.Name = "lblPaymentStudent";
            lblPaymentStudent.Size = new Size(70, 15);
            lblPaymentStudent.TabIndex = 0;
            lblPaymentStudent.Text = "Học viên:";
            lblPaymentStudent.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbStudentPayment
            // 
            cmbStudentPayment.Location = new Point(375, 9);
            cmbStudentPayment.Name = "cmbStudentPayment";
            cmbStudentPayment.Size = new Size(260, 23);
            cmbStudentPayment.TabIndex = 3;
            cmbStudentPayment.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblPaymentNeed
            // 
            lblPaymentNeed.Location = new Point(20, 75);
            lblPaymentNeed.Name = "lblPaymentNeed";
            lblPaymentNeed.Size = new Size(390, 20);
            lblPaymentNeed.TabIndex = 5;
            lblPaymentNeed.Text = "Cần đóng: 0";
            lblPaymentNeed.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblPaymentPaid
            // 
            lblPaymentPaid.Location = new Point(20, 97);
            lblPaymentPaid.Name = "lblPaymentPaid";
            lblPaymentPaid.Size = new Size(300, 20);
            lblPaymentPaid.TabIndex = 6;
            lblPaymentPaid.Text = "Đã đóng: 0";
            lblPaymentPaid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblPaymentRemain
            // 
            lblPaymentRemain.Location = new Point(20, 119);
            lblPaymentRemain.Name = "lblPaymentRemain";
            lblPaymentRemain.Size = new Size(300, 20);
            lblPaymentRemain.TabIndex = 7;
            lblPaymentRemain.Text = "Còn lại: 0";
            lblPaymentRemain.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblPaymentAmount
            // 
            lblPaymentAmount.Location = new Point(20, 145);
            lblPaymentAmount.Name = "lblPaymentAmount";
            lblPaymentAmount.Size = new Size(160, 15);
            lblPaymentAmount.TabIndex = 1;
            lblPaymentAmount.Text = "Số tiền thu";
            lblPaymentAmount.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // txtPaymentAmount
            // 
            txtPaymentAmount.Location = new Point(20, 163);
            txtPaymentAmount.Name = "txtPaymentAmount";
            txtPaymentAmount.Size = new Size(390, 23);
            txtPaymentAmount.TabIndex = 8;
            txtPaymentAmount.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblPaymentNote
            // 
            lblPaymentNote.Location = new Point(20, 190);
            lblPaymentNote.Name = "lblPaymentNote";
            lblPaymentNote.Size = new Size(160, 15);
            lblPaymentNote.TabIndex = 2;
            lblPaymentNote.Text = "Ghi chú";
            lblPaymentNote.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // txtPaymentNote
            // 
            txtPaymentNote.Location = new Point(20, 208);
            txtPaymentNote.Name = "txtPaymentNote";
            txtPaymentNote.Size = new Size(390, 23);
            txtPaymentNote.TabIndex = 9;
            txtPaymentNote.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // btnCollectPayment
            // 
            btnCollectPayment.Location = new Point(20, 238);
            btnCollectPayment.Name = "btnCollectPayment";
            btnCollectPayment.Size = new Size(390, 30);
            btnCollectPayment.TabIndex = 10;
            btnCollectPayment.Text = "Thu học phí";
            btnCollectPayment.Click += btnCollectPayment_Click;
            btnCollectPayment.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // btnExportPayment
            // 
            btnExportPayment.Location = new Point(20, 274);
            btnExportPayment.Name = "btnExportPayment";
            btnExportPayment.Size = new Size(390, 30);
            btnExportPayment.TabIndex = 11;
            btnExportPayment.Text = "Xuất Excel";
            btnExportPayment.Click += btnExportPayment_Click;
            btnExportPayment.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // btnEditPaymentHistory
            // 
            btnEditPaymentHistory.Location = new Point(20, 274);
            btnEditPaymentHistory.Name = "btnEditPaymentHistory";
            btnEditPaymentHistory.Size = new Size(390, 30);
            btnEditPaymentHistory.TabIndex = 11;
            btnEditPaymentHistory.Text = "Chỉnh sửa lịch sử thu";
            btnEditPaymentHistory.Click += btnEditPaymentHistory_Click;
            btnEditPaymentHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // btnDeletePaymentHistory
            // 
            btnDeletePaymentHistory.Location = new Point(20, 310);
            btnDeletePaymentHistory.Name = "btnDeletePaymentHistory";
            btnDeletePaymentHistory.Size = new Size(390, 30);
            btnDeletePaymentHistory.TabIndex = 12;
            btnDeletePaymentHistory.Text = "Xóa lịch sử thu";
            btnDeletePaymentHistory.Click += btnDeletePaymentHistory_Click;
            btnDeletePaymentHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // btnFinalizePayment
            // 
            btnFinalizePayment.Location = new Point(20, 346);
            btnFinalizePayment.Name = "btnFinalizePayment";
            btnFinalizePayment.Size = new Size(390, 30);
            btnFinalizePayment.TabIndex = 13;
            btnFinalizePayment.Text = "Chốt số liệu";
            btnFinalizePayment.Click += btnFinalizePayment_Click;
            btnFinalizePayment.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dgvAttendanceDetail
            // 
            dgvAttendanceDetail.AllowUserToAddRows = false;
            dgvAttendanceDetail.Location = new Point(430, 40);
            dgvAttendanceDetail.Name = "dgvAttendanceDetail";
            dgvAttendanceDetail.ReadOnly = true;
            dgvAttendanceDetail.Size = new Size(580, 450);
            dgvAttendanceDetail.TabIndex = 13;
            dgvAttendanceDetail.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAttendanceDetail.SelectionChanged += dgvAttendanceDetail_SelectionChanged;
            dgvAttendanceDetail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // tabAdminReports
            // 
            tabAdminReports.Controls.Add(lblTotalStudents);
            tabAdminReports.Controls.Add(lblTotalRevenue);
            tabAdminReports.Controls.Add(lblActiveClasses);
            tabAdminReports.Controls.Add(dgvRevenueByYear);
            tabAdminReports.Controls.Add(lblRevenueChartTitle);
            tabAdminReports.Controls.Add(pnlRevenueChart);
            tabAdminReports.Location = new Point(4, 24);
            tabAdminReports.Name = "tabAdminReports";
            tabAdminReports.Size = new Size(1024, 504);
            tabAdminReports.TabIndex = 4;
            tabAdminReports.Text = "Báo cáo";
            // 
            // lblTotalStudents
            // 
            lblTotalStudents.Location = new Point(20, 40);
            lblTotalStudents.Name = "lblTotalStudents";
            lblTotalStudents.Size = new Size(300, 30);
            lblTotalStudents.TabIndex = 0;
            lblTotalStudents.Text = "Tổng học viên: 0";
            lblTotalStudents.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblTotalRevenue
            // 
            lblTotalRevenue.Location = new Point(20, 80);
            lblTotalRevenue.Name = "lblTotalRevenue";
            lblTotalRevenue.Size = new Size(300, 30);
            lblTotalRevenue.TabIndex = 1;
            lblTotalRevenue.Text = "Doanh thu: 0";
            lblTotalRevenue.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblActiveClasses
            // 
            lblActiveClasses.Location = new Point(20, 120);
            lblActiveClasses.Name = "lblActiveClasses";
            lblActiveClasses.Size = new Size(300, 30);
            lblActiveClasses.TabIndex = 2;
            lblActiveClasses.Text = "Lớp hoạt động: 0";
            lblActiveClasses.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dgvRevenueByYear
            // 
            dgvRevenueByYear.AllowUserToAddRows = false;
            dgvRevenueByYear.Location = new Point(20, 165);
            dgvRevenueByYear.Name = "dgvRevenueByYear";
            dgvRevenueByYear.ReadOnly = true;
            dgvRevenueByYear.Size = new Size(300, 320);
            dgvRevenueByYear.TabIndex = 3;
            dgvRevenueByYear.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRevenueByYear.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            // 
            // lblRevenueChartTitle
            // 
            lblRevenueChartTitle.Location = new Point(340, 145);
            lblRevenueChartTitle.Name = "lblRevenueChartTitle";
            lblRevenueChartTitle.Size = new Size(670, 15);
            lblRevenueChartTitle.TabIndex = 4;
            lblRevenueChartTitle.Text = "Biểu đồ doanh thu theo năm";
            lblRevenueChartTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // pnlRevenueChart
            // 
            pnlRevenueChart.BackColor = Color.White;
            pnlRevenueChart.BorderStyle = BorderStyle.FixedSingle;
            pnlRevenueChart.Location = new Point(340, 165);
            pnlRevenueChart.Name = "pnlRevenueChart";
            pnlRevenueChart.Size = new Size(670, 320);
            pnlRevenueChart.TabIndex = 5;
            pnlRevenueChart.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlRevenueChart.Paint += pnlRevenueChart_Paint;

            // 
            // tabAdminTeachers
            // 
            tabAdminTeachers.Controls.Add(lblTeacherName);
            tabAdminTeachers.Controls.Add(lblTeacherPhone);
            tabAdminTeachers.Controls.Add(lblTeacherEmail);
            tabAdminTeachers.Controls.Add(lblTeacherStatus);
            tabAdminTeachers.Controls.Add(dgvTeachers);
            tabAdminTeachers.Controls.Add(txtTeacherName);
            tabAdminTeachers.Controls.Add(txtTeacherPhone);
            tabAdminTeachers.Controls.Add(txtTeacherEmail);
            tabAdminTeachers.Controls.Add(cmbTeacherStatus);
            tabAdminTeachers.Controls.Add(btnAddTeacher);
            tabAdminTeachers.Controls.Add(btnUpdateTeacher);
            tabAdminTeachers.Controls.Add(btnDeleteTeacher);
            tabAdminTeachers.Controls.Add(lblTeacherAccount);
            tabAdminTeachers.Controls.Add(txtTeacherUsername);
            tabAdminTeachers.Controls.Add(lblTeacherPassword);
            tabAdminTeachers.Controls.Add(txtTeacherPassword);
            tabAdminTeachers.Controls.Add(btnUpdateTeacherPassword);
            tabAdminTeachers.Location = new Point(4, 24);
            tabAdminTeachers.Name = "tabAdminTeachers";
            tabAdminTeachers.Size = new Size(1024, 504);
            tabAdminTeachers.TabIndex = 5;
            tabAdminTeachers.Text = "Giáo viên";
            // 
            // lblTeacherName
            // 
            lblTeacherName.Location = new Point(680, 12);
            lblTeacherName.Name = "lblTeacherName";
            lblTeacherName.Size = new Size(120, 15);
            lblTeacherName.TabIndex = 0;
            lblTeacherName.Text = "Họ và tên giáo viên";
            lblTeacherName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblTeacherPhone
            // 
            lblTeacherPhone.Location = new Point(680, 49);
            lblTeacherPhone.Name = "lblTeacherPhone";
            lblTeacherPhone.Size = new Size(120, 15);
            lblTeacherPhone.TabIndex = 1;
            lblTeacherPhone.Text = "Số điện thoại";
            lblTeacherPhone.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblTeacherEmail
            // 
            lblTeacherEmail.Location = new Point(680, 95);
            lblTeacherEmail.Name = "lblTeacherEmail";
            lblTeacherEmail.Size = new Size(120, 15);
            lblTeacherEmail.TabIndex = 2;
            lblTeacherEmail.Text = "Email";
            lblTeacherEmail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblTeacherStatus
            // 
            lblTeacherStatus.Location = new Point(680, 146);
            lblTeacherStatus.Name = "lblTeacherStatus";
            lblTeacherStatus.Size = new Size(120, 15);
            lblTeacherStatus.TabIndex = 3;
            lblTeacherStatus.Text = "Trạng thái";
            lblTeacherStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // dgvTeachers
            // 
            dgvTeachers.Location = new Point(10, 10);
            dgvTeachers.Name = "dgvTeachers";
            dgvTeachers.Size = new Size(650, 430);
            dgvTeachers.TabIndex = 4;
            dgvTeachers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTeachers.SelectionChanged += dgvTeachers_SelectionChanged;
            dgvTeachers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // txtTeacherName
            // 
            txtTeacherName.Location = new Point(680, 26);
            txtTeacherName.Name = "txtTeacherName";
            txtTeacherName.Size = new Size(300, 23);
            txtTeacherName.TabIndex = 5;
            txtTeacherName.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtTeacherPhone
            // 
            txtTeacherPhone.Location = new Point(680, 67);
            txtTeacherPhone.Name = "txtTeacherPhone";
            txtTeacherPhone.Size = new Size(300, 23);
            txtTeacherPhone.TabIndex = 6;
            txtTeacherPhone.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtTeacherEmail
            // 
            txtTeacherEmail.Location = new Point(680, 113);
            txtTeacherEmail.Name = "txtTeacherEmail";
            txtTeacherEmail.Size = new Size(300, 23);
            txtTeacherEmail.TabIndex = 7;
            txtTeacherEmail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // cmbTeacherStatus
            // 
            cmbTeacherStatus.Items.AddRange(new object[] { "Active", "Inactive" });
            cmbTeacherStatus.Location = new Point(680, 164);
            cmbTeacherStatus.Name = "cmbTeacherStatus";
            cmbTeacherStatus.Size = new Size(145, 23);
            cmbTeacherStatus.TabIndex = 8;
            cmbTeacherStatus.Text = "Active";
            cmbTeacherStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnAddTeacher
            // 
            btnAddTeacher.Location = new Point(680, 199);
            btnAddTeacher.Name = "btnAddTeacher";
            btnAddTeacher.Size = new Size(95, 30);
            btnAddTeacher.TabIndex = 9;
            btnAddTeacher.Text = "Thêm";
            btnAddTeacher.Click += btnAddTeacher_Click;
            btnAddTeacher.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnUpdateTeacher
            // 
            btnUpdateTeacher.Location = new Point(782, 199);
            btnUpdateTeacher.Name = "btnUpdateTeacher";
            btnUpdateTeacher.Size = new Size(95, 30);
            btnUpdateTeacher.TabIndex = 10;
            btnUpdateTeacher.Text = "Sửa";
            btnUpdateTeacher.Click += btnUpdateTeacher_Click;
            btnUpdateTeacher.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnDeleteTeacher
            // 
            btnDeleteTeacher.Location = new Point(885, 199);
            btnDeleteTeacher.Name = "btnDeleteTeacher";
            btnDeleteTeacher.Size = new Size(95, 30);
            btnDeleteTeacher.TabIndex = 11;
            btnDeleteTeacher.Text = "Xóa";
            btnDeleteTeacher.Click += btnDeleteTeacher_Click;
            btnDeleteTeacher.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblTeacherAccount
            // 
            lblTeacherAccount.Location = new Point(680, 245);
            lblTeacherAccount.Name = "lblTeacherAccount";
            lblTeacherAccount.Size = new Size(120, 15);
            lblTeacherAccount.TabIndex = 12;
            lblTeacherAccount.Text = "Tài khoản";
            lblTeacherAccount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtTeacherUsername
            // 
            txtTeacherUsername.Location = new Point(680, 263);
            txtTeacherUsername.Name = "txtTeacherUsername";
            txtTeacherUsername.ReadOnly = true;
            txtTeacherUsername.Size = new Size(300, 23);
            txtTeacherUsername.TabIndex = 13;
            txtTeacherUsername.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // lblTeacherPassword
            // 
            lblTeacherPassword.Location = new Point(680, 292);
            lblTeacherPassword.Name = "lblTeacherPassword";
            lblTeacherPassword.Size = new Size(120, 15);
            lblTeacherPassword.TabIndex = 14;
            lblTeacherPassword.Text = "Mật khẩu";
            lblTeacherPassword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // txtTeacherPassword
            // 
            txtTeacherPassword.Location = new Point(680, 310);
            txtTeacherPassword.Name = "txtTeacherPassword";
            txtTeacherPassword.Size = new Size(300, 23);
            txtTeacherPassword.TabIndex = 15;
            txtTeacherPassword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // btnUpdateTeacherPassword
            // 
            btnUpdateTeacherPassword.Location = new Point(680, 340);
            btnUpdateTeacherPassword.Name = "btnUpdateTeacherPassword";
            btnUpdateTeacherPassword.Size = new Size(300, 30);
            btnUpdateTeacherPassword.TabIndex = 16;
            btnUpdateTeacherPassword.Text = "Cập nhật mật khẩu";
            btnUpdateTeacherPassword.Click += btnUpdateTeacherPassword_Click;
            btnUpdateTeacherPassword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 
            // tabAdminPayroll
            // 
            lblPayrollMonth = new Label();
            cmbPayrollMonth = new ComboBox();
            cmbPayrollYear = new ComboBox();
            dgvPayroll = new DataGridView();
            dgvPayrollDetail = new DataGridView();
            tabAdminPayroll.Controls.Add(lblPayrollMonth);
            tabAdminPayroll.Controls.Add(cmbPayrollMonth);
            tabAdminPayroll.Controls.Add(cmbPayrollYear);
            tabAdminPayroll.Controls.Add(dgvPayroll);
            tabAdminPayroll.Controls.Add(dgvPayrollDetail);
            tabAdminPayroll.Location = new Point(4, 24);
            tabAdminPayroll.Name = "tabAdminPayroll";
            tabAdminPayroll.Size = new Size(1024, 504);
            tabAdminPayroll.TabIndex = 6;
            tabAdminPayroll.Text = "Ngày công";
            // 
            // lblPayrollMonth
            // 
            lblPayrollMonth.Location = new Point(10, 15);
            lblPayrollMonth.Name = "lblPayrollMonth";
            lblPayrollMonth.Size = new Size(50, 15);
            lblPayrollMonth.TabIndex = 0;
            lblPayrollMonth.Text = "Tháng:";
            lblPayrollMonth.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbPayrollMonth
            // 
            cmbPayrollMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPayrollMonth.Location = new Point(65, 12);
            cmbPayrollMonth.Name = "cmbPayrollMonth";
            cmbPayrollMonth.Size = new Size(60, 23);
            cmbPayrollMonth.TabIndex = 1;
            cmbPayrollMonth.SelectedIndexChanged += cmbPayrollFilter_SelectedIndexChanged;
            cmbPayrollMonth.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbPayrollYear
            // 
            cmbPayrollYear.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPayrollYear.Location = new Point(135, 12);
            cmbPayrollYear.Name = "cmbPayrollYear";
            cmbPayrollYear.Size = new Size(80, 23);
            cmbPayrollYear.TabIndex = 2;
            cmbPayrollYear.SelectedIndexChanged += cmbPayrollFilter_SelectedIndexChanged;
            cmbPayrollYear.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dgvPayroll
            // 
            dgvPayroll.AllowUserToAddRows = false;
            dgvPayroll.Location = new Point(10, 45);
            dgvPayroll.Name = "dgvPayroll";
            dgvPayroll.ReadOnly = true;
            dgvPayroll.Size = new Size(360, 440);
            dgvPayroll.TabIndex = 4;
            dgvPayroll.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPayroll.SelectionChanged += dgvPayroll_SelectionChanged;
            dgvPayroll.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            // 
            // dgvPayrollDetail
            // 
            dgvPayrollDetail.AllowUserToAddRows = false;
            dgvPayrollDetail.Location = new Point(380, 45);
            dgvPayrollDetail.Name = "dgvPayrollDetail";
            dgvPayrollDetail.ReadOnly = true;
            dgvPayrollDetail.Size = new Size(630, 440);
            dgvPayrollDetail.TabIndex = 5;
            dgvPayrollDetail.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPayrollDetail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // btnLogoutAdmin
            // 
            btnLogoutAdmin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogoutAdmin.Location = new Point(944, 3);
            btnLogoutAdmin.Name = "btnLogoutAdmin";
            btnLogoutAdmin.Size = new Size(80, 23);
            btnLogoutAdmin.TabIndex = 1;
            btnLogoutAdmin.Text = "Đăng xuất";
            btnLogoutAdmin.Click += btnLogout_Click;
            // 
            // tabTeacher
            // 
            tabTeacher.Controls.Add(tabTeacherFunctions);
            tabTeacher.Controls.Add(btnLogoutTeacher);
            tabTeacher.Location = new Point(4, 24);
            tabTeacher.Name = "tabTeacher";
            tabTeacher.Size = new Size(1032, 532);
            tabTeacher.TabIndex = 1;
            tabTeacher.Text = "Teacher";
            // 
            // tabTeacherFunctions
            // 
            tabTeacherFunctions.Controls.Add(tabTeacherTimesheet);
            tabTeacherFunctions.Controls.Add(tabTeacherSchedule);
            tabTeacherFunctions.Controls.Add(tabTeacherAttendance);
            tabTeacherFunctions.Controls.Add(tabTeacherEvaluation);
            tabTeacherFunctions.Dock = DockStyle.Fill;
            tabTeacherFunctions.Location = new Point(0, 0);
            tabTeacherFunctions.Name = "tabTeacherFunctions";
            tabTeacherFunctions.SelectedIndex = 0;
            tabTeacherFunctions.Size = new Size(1032, 532);
            tabTeacherFunctions.TabIndex = 0;
            // 
            // tabTeacherTimesheet
            // 
            tabTeacherTimesheet.Controls.Add(lblTimesheetMonth);
            tabTeacherTimesheet.Controls.Add(cmbTimesheetMonth);
            tabTeacherTimesheet.Controls.Add(cmbTimesheetYear);
            tabTeacherTimesheet.Controls.Add(btnSaveTimesheet);
            tabTeacherTimesheet.Controls.Add(dgvTimesheet);
            tabTeacherTimesheet.Controls.Add(lblTimesheetSummary);
            tabTeacherTimesheet.Location = new Point(4, 24);
            tabTeacherTimesheet.Name = "tabTeacherTimesheet";
            tabTeacherTimesheet.Size = new Size(1024, 504);
            tabTeacherTimesheet.TabIndex = 3;
            tabTeacherTimesheet.Text = "Bảng chấm công";
            // 
            // lblTimesheetMonth
            // 
            lblTimesheetMonth.AutoSize = true;
            lblTimesheetMonth.Location = new Point(10, 15);
            lblTimesheetMonth.Name = "lblTimesheetMonth";
            lblTimesheetMonth.Size = new Size(44, 15);
            lblTimesheetMonth.TabIndex = 0;
            lblTimesheetMonth.Text = "Tháng:";
            lblTimesheetMonth.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbTimesheetMonth
            // 
            cmbTimesheetMonth.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTimesheetMonth.Location = new Point(65, 12);
            cmbTimesheetMonth.Name = "cmbTimesheetMonth";
            cmbTimesheetMonth.Size = new Size(60, 23);
            cmbTimesheetMonth.TabIndex = 1;
            cmbTimesheetMonth.SelectedIndexChanged += cmbTimesheetFilter_SelectedIndexChanged;
            cmbTimesheetMonth.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbTimesheetYear
            // 
            cmbTimesheetYear.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTimesheetYear.Location = new Point(135, 12);
            cmbTimesheetYear.Name = "cmbTimesheetYear";
            cmbTimesheetYear.Size = new Size(80, 23);
            cmbTimesheetYear.TabIndex = 2;
            cmbTimesheetYear.SelectedIndexChanged += cmbTimesheetFilter_SelectedIndexChanged;
            cmbTimesheetYear.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // btnSaveTimesheet
            // 
            btnSaveTimesheet.Location = new Point(230, 11);
            btnSaveTimesheet.Name = "btnSaveTimesheet";
            btnSaveTimesheet.Size = new Size(100, 25);
            btnSaveTimesheet.TabIndex = 4;
            btnSaveTimesheet.Text = "Lưu chấm công";
            btnSaveTimesheet.Click += btnSaveTimesheet_Click;
            btnSaveTimesheet.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dgvTimesheet
            // 
            dgvTimesheet.AllowUserToAddRows = false;
            dgvTimesheet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTimesheet.Location = new Point(10, 45);
            dgvTimesheet.Name = "dgvTimesheet";
            dgvTimesheet.Size = new Size(1000, 400);
            dgvTimesheet.TabIndex = 5;
            dgvTimesheet.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // lblTimesheetSummary
            // 
            lblTimesheetSummary.AutoSize = true;
            lblTimesheetSummary.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTimesheetSummary.Location = new Point(10, 462);
            lblTimesheetSummary.Name = "lblTimesheetSummary";
            lblTimesheetSummary.Size = new Size(0, 15);
            lblTimesheetSummary.TabIndex = 6;
            lblTimesheetSummary.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            // 
            // tabTeacherSchedule
            // 
            tabTeacherSchedule.Controls.Add(lblTeacherWeek);
            tabTeacherSchedule.Controls.Add(dtpTeacherWeek);
            tabTeacherSchedule.Controls.Add(btnLoadTeacherSchedule);
            tabTeacherSchedule.Controls.Add(dgvTeacherWeeklySchedule);
            tabTeacherSchedule.Controls.Add(dgvTeacherClasses);
            tabTeacherSchedule.Location = new Point(4, 24);
            tabTeacherSchedule.Name = "tabTeacherSchedule";
            tabTeacherSchedule.Size = new Size(1024, 504);
            tabTeacherSchedule.TabIndex = 0;
            tabTeacherSchedule.Text = "Lịch dạy / Lớp";
            // 
            // lblTeacherWeek
            // 
            lblTeacherWeek.Location = new Point(10, 12);
            lblTeacherWeek.Name = "lblTeacherWeek";
            lblTeacherWeek.Size = new Size(50, 15);
            lblTeacherWeek.TabIndex = 0;
            lblTeacherWeek.Text = "Tuần:";
            lblTeacherWeek.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dtpTeacherWeek
            // 
            dtpTeacherWeek.Format = DateTimePickerFormat.Short;
            dtpTeacherWeek.Location = new Point(65, 9);
            dtpTeacherWeek.Name = "dtpTeacherWeek";
            dtpTeacherWeek.Size = new Size(150, 23);
            dtpTeacherWeek.TabIndex = 1;
            dtpTeacherWeek.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // btnLoadTeacherSchedule
            // 
            btnLoadTeacherSchedule.Location = new Point(225, 8);
            btnLoadTeacherSchedule.Name = "btnLoadTeacherSchedule";
            btnLoadTeacherSchedule.Size = new Size(80, 25);
            btnLoadTeacherSchedule.TabIndex = 2;
            btnLoadTeacherSchedule.Text = "Xem";
            btnLoadTeacherSchedule.Click += btnLoadTeacherSchedule_Click;
            btnLoadTeacherSchedule.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dgvTeacherWeeklySchedule
            // 
            dgvTeacherWeeklySchedule.AllowUserToAddRows = false;
            dgvTeacherWeeklySchedule.Location = new Point(10, 40);
            dgvTeacherWeeklySchedule.Name = "dgvTeacherWeeklySchedule";
            dgvTeacherWeeklySchedule.ReadOnly = true;
            dgvTeacherWeeklySchedule.Size = new Size(1000, 230);
            dgvTeacherWeeklySchedule.TabIndex = 3;
            dgvTeacherWeeklySchedule.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTeacherWeeklySchedule.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // dgvTeacherClasses
            // 
            dgvTeacherClasses.Location = new Point(10, 280);
            dgvTeacherClasses.Name = "dgvTeacherClasses";
            dgvTeacherClasses.Size = new Size(1000, 210);
            dgvTeacherClasses.TabIndex = 0;
            dgvTeacherClasses.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTeacherClasses.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // tabTeacherAttendance
            // 
            tabTeacherAttendance.Controls.Add(lblAttendanceClass);
            tabTeacherAttendance.Controls.Add(lblAttendanceDate);
            tabTeacherAttendance.Controls.Add(cmbClassAttendance);
            tabTeacherAttendance.Controls.Add(dtpSessionDate);
            tabTeacherAttendance.Controls.Add(dgvAttendance);
            tabTeacherAttendance.Controls.Add(btnSaveAttendance);
            tabTeacherAttendance.Location = new Point(4, 24);
            tabTeacherAttendance.Name = "tabTeacherAttendance";
            tabTeacherAttendance.Size = new Size(1024, 504);
            tabTeacherAttendance.TabIndex = 1;
            tabTeacherAttendance.Text = "Điểm danh";
            // 
            // lblAttendanceClass
            // 
            lblAttendanceClass.Location = new Point(10, 2);
            lblAttendanceClass.Name = "lblAttendanceClass";
            lblAttendanceClass.Size = new Size(120, 15);
            lblAttendanceClass.TabIndex = 0;
            lblAttendanceClass.Text = "Chọn lớp";
            lblAttendanceClass.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblAttendanceDate
            // 
            lblAttendanceDate.Location = new Point(260, 2);
            lblAttendanceDate.Name = "lblAttendanceDate";
            lblAttendanceDate.Size = new Size(120, 15);
            lblAttendanceDate.TabIndex = 1;
            lblAttendanceDate.Text = "Ngày học";
            lblAttendanceDate.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbClassAttendance
            // 
            cmbClassAttendance.Location = new Point(10, 20);
            cmbClassAttendance.Name = "cmbClassAttendance";
            cmbClassAttendance.Size = new Size(240, 23);
            cmbClassAttendance.TabIndex = 2;
            cmbClassAttendance.SelectedIndexChanged += cmbAttendanceFilter_Changed;
            cmbClassAttendance.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dtpSessionDate
            // 
            dtpSessionDate.Location = new Point(260, 20);
            dtpSessionDate.Name = "dtpSessionDate";
            dtpSessionDate.Size = new Size(240, 23);
            dtpSessionDate.TabIndex = 3;
            dtpSessionDate.ValueChanged += cmbAttendanceFilter_Changed;
            dtpSessionDate.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // dgvAttendance
            // 
            dgvAttendance.Location = new Point(10, 55);
            dgvAttendance.Name = "dgvAttendance";
            dgvAttendance.Size = new Size(1000, 350);
            dgvAttendance.TabIndex = 4;
            dgvAttendance.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAttendance.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // btnSaveAttendance
            // 
            btnSaveAttendance.Location = new Point(10, 410);
            btnSaveAttendance.Name = "btnSaveAttendance";
            btnSaveAttendance.Size = new Size(1000, 30);
            btnSaveAttendance.TabIndex = 6;
            btnSaveAttendance.Text = "Lưu điểm danh";
            btnSaveAttendance.Click += btnSaveAttendance_Click;
            btnSaveAttendance.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // tabTeacherEvaluation
            // 
            tabTeacherEvaluation.Controls.Add(lblEvaluateClass);
            tabTeacherEvaluation.Controls.Add(lblEvaluateStudent);
            tabTeacherEvaluation.Controls.Add(lblEvaluateScore);
            tabTeacherEvaluation.Controls.Add(lblEvaluateComment);
            tabTeacherEvaluation.Controls.Add(cmbClassEvaluate);
            tabTeacherEvaluation.Controls.Add(cmbStudentEvaluate);
            tabTeacherEvaluation.Controls.Add(txtScore);
            tabTeacherEvaluation.Controls.Add(txtComment);
            tabTeacherEvaluation.Controls.Add(btnSaveEvaluation);
            tabTeacherEvaluation.Location = new Point(4, 24);
            tabTeacherEvaluation.Name = "tabTeacherEvaluation";
            tabTeacherEvaluation.Size = new Size(1024, 504);
            tabTeacherEvaluation.TabIndex = 2;
            tabTeacherEvaluation.Text = "Nhận xét / Điểm";
            // 
            // lblEvaluateClass
            // 
            lblEvaluateClass.Location = new Point(20, 12);
            lblEvaluateClass.Name = "lblEvaluateClass";
            lblEvaluateClass.Size = new Size(120, 15);
            lblEvaluateClass.TabIndex = 0;
            lblEvaluateClass.Text = "Lớp học";
            lblEvaluateClass.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblEvaluateStudent
            // 
            lblEvaluateStudent.Location = new Point(280, 12);
            lblEvaluateStudent.Name = "lblEvaluateStudent";
            lblEvaluateStudent.Size = new Size(120, 15);
            lblEvaluateStudent.TabIndex = 1;
            lblEvaluateStudent.Text = "Học viên";
            lblEvaluateStudent.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblEvaluateScore
            // 
            lblEvaluateScore.Location = new Point(20, 57);
            lblEvaluateScore.Name = "lblEvaluateScore";
            lblEvaluateScore.Size = new Size(120, 15);
            lblEvaluateScore.TabIndex = 2;
            lblEvaluateScore.Text = "Điểm";
            lblEvaluateScore.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // lblEvaluateComment
            // 
            lblEvaluateComment.Location = new Point(20, 103);
            lblEvaluateComment.Name = "lblEvaluateComment";
            lblEvaluateComment.Size = new Size(120, 15);
            lblEvaluateComment.TabIndex = 3;
            lblEvaluateComment.Text = "Nhận xét";
            lblEvaluateComment.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // cmbClassEvaluate
            // 
            cmbClassEvaluate.Location = new Point(20, 30);
            cmbClassEvaluate.Name = "cmbClassEvaluate";
            cmbClassEvaluate.Size = new Size(250, 23);
            cmbClassEvaluate.TabIndex = 4;
            cmbClassEvaluate.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            cmbClassEvaluate.SelectedIndexChanged += cmbClassEvaluate_SelectedIndexChanged;
            // 
            // cmbStudentEvaluate
            // 
            cmbStudentEvaluate.Location = new Point(280, 30);
            cmbStudentEvaluate.Name = "cmbStudentEvaluate";
            cmbStudentEvaluate.Size = new Size(250, 23);
            cmbStudentEvaluate.TabIndex = 5;
            cmbStudentEvaluate.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // txtScore
            // 
            txtScore.Location = new Point(20, 75);
            txtScore.Name = "txtScore";
            txtScore.Size = new Size(250, 23);
            txtScore.TabIndex = 6;
            txtScore.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // txtComment
            // 
            txtComment.Location = new Point(20, 121);
            txtComment.Multiline = true;
            txtComment.Name = "txtComment";
            txtComment.Size = new Size(980, 280);
            txtComment.TabIndex = 7;
            txtComment.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // btnSaveEvaluation
            // 
            btnSaveEvaluation.Location = new Point(20, 411);
            btnSaveEvaluation.Name = "btnSaveEvaluation";
            btnSaveEvaluation.Size = new Size(980, 30);
            btnSaveEvaluation.TabIndex = 8;
            btnSaveEvaluation.Text = "Lưu nhận xét/điểm";
            btnSaveEvaluation.Click += btnSaveEvaluation_Click;
            btnSaveEvaluation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // btnLogoutTeacher
            // 
            btnLogoutTeacher.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogoutTeacher.Location = new Point(944, 3);
            btnLogoutTeacher.Name = "btnLogoutTeacher";
            btnLogoutTeacher.Size = new Size(80, 23);
            btnLogoutTeacher.TabIndex = 1;
            btnLogoutTeacher.Text = "Đăng xuất";
            btnLogoutTeacher.Click += btnLogout_Click;
            // 
            // btnLogoutGlobal
            // 
            btnLogoutGlobal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogoutGlobal.Location = new Point(946, 3);
            btnLogoutGlobal.Name = "btnLogoutGlobal";
            btnLogoutGlobal.Size = new Size(90, 23);
            btnLogoutGlobal.TabIndex = 2;
            btnLogoutGlobal.Text = "Đăng xuất";
            btnLogoutGlobal.UseVisualStyleBackColor = true;
            btnLogoutGlobal.Click += btnLogout_Click;
            // 
            // Form1
            // 
            ClientSize = new Size(1040, 560);
            MinimumSize = new Size(900, 520);
            Controls.Add(btnLogoutGlobal);
            Controls.Add(tabMain);
            Name = "Form1";
            Text = "Nhat Duc Software";
            Load += Form1_Load;
            tabMain.ResumeLayout(false);
            tabAdmin.ResumeLayout(false);
            tabAdminFunctions.ResumeLayout(false);
            tabAdminStudents.ResumeLayout(false);
            tabAdminStudents.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvStudents).EndInit();
            tabAdminCourses.ResumeLayout(false);
            tabAdminCourses.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCourses).EndInit();
            tabAdminClasses.ResumeLayout(false);
            tabAdminClasses.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvClasses).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvClassStudents).EndInit();
            tabAdminPayments.ResumeLayout(false);
            tabAdminPayments.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAttendanceDetail).EndInit();
            tabAdminReports.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvRevenueByYear).EndInit();
            tabAdminTeachers.ResumeLayout(false);
            tabAdminTeachers.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTeachers).EndInit();
            tabAdminPayroll.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPayroll).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvPayrollDetail).EndInit();
            tabTeacher.ResumeLayout(false);
            tabTeacherFunctions.ResumeLayout(false);
            tabTeacherTimesheet.ResumeLayout(false);
            tabTeacherTimesheet.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTimesheet).EndInit();
            tabTeacherSchedule.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvTeacherWeeklySchedule).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTeacherClasses).EndInit();
            tabTeacherAttendance.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvAttendance).EndInit();
            tabTeacherEvaluation.ResumeLayout(false);
            tabTeacherEvaluation.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabMain;
        private TabPage tabAdmin;
        private TabPage tabTeacher;
        private TabControl tabAdminFunctions;
        private TabPage tabAdminStudents;
        private TabPage tabAdminCourses;
        private TabPage tabAdminClasses;
        private TabPage tabAdminPayments;
        private TabPage tabAdminReports;
        private TabPage tabAdminTeachers;
        private TabPage tabAdminPayroll;
        private Label lblPayrollMonth;
        private ComboBox cmbPayrollMonth;
        private ComboBox cmbPayrollYear;
        private DataGridView dgvPayroll;
        private TabControl tabTeacherFunctions;
        private TabPage tabTeacherSchedule;
        private TabPage tabTeacherAttendance;
        private TabPage tabTeacherEvaluation;
        private DataGridView dgvStudents;
        private TextBox txtStudentName;
        private TextBox txtStudentPhone;
        private TextBox txtStudentEmail;
        private TextBox txtStudentBirthYear;
        private TextBox txtStudentAddress;
        private ComboBox cmbStudentStatus;
        private Button btnAddStudent;
        private Button btnUpdateStudent;
        private Button btnDeleteStudent;
        private Button btnImportStudents;
        private Button btnExportStudents;
        private DataGridView dgvCourses;
        private TextBox txtCourseName;
        private TextBox txtCourseFee;
        private Button btnAddCourse;
        private Button btnUpdateCourse;
        private DataGridView dgvClasses;
        private TextBox txtClassName;
        private ComboBox cmbCourseClass;
        private ComboBox cmbTeacherClass;
        private Button btnCreateClass;
        private Button btnUpdateClass;
        private Button btnDeleteClass;
        private ComboBox cmbClassAddStudent;
        private ComboBox cmbStudentClass;
        private Button btnAddStudentToClass;
        private ComboBox cmbStudentPayment;
        private Label lblPaymentNeed;
        private Label lblPaymentPaid;
        private Label lblPaymentRemain;
        private TextBox txtPaymentAmount;
        private TextBox txtPaymentNote;
        private Button btnCollectPayment;
        private Button btnExportPayment;
        private Button btnEditPaymentHistory;
        private Button btnDeletePaymentHistory;
        private Button btnFinalizePayment;
        private Label lblPaymentFilterClass;
        private ComboBox cmbPaymentFilterClass;
        private DataGridView dgvAttendanceDetail;
        private Label lblTotalStudents;
        private Label lblTotalRevenue;
        private Label lblActiveClasses;
        private DataGridView dgvRevenueByYear;
        private Label lblRevenueChartTitle;
        private Panel pnlRevenueChart;
        private DataGridView dgvTeacherClasses;
        private Label lblTeacherWeek;
        private DateTimePicker dtpTeacherWeek;
        private Button btnLoadTeacherSchedule;
        private DataGridView dgvTeacherWeeklySchedule;
        private ComboBox cmbClassAttendance;
        private DateTimePicker dtpSessionDate;
        private DataGridView dgvAttendance;
        private Button btnSaveAttendance;
        private ComboBox cmbClassEvaluate;
        private ComboBox cmbStudentEvaluate;
        private TextBox txtScore;
        private TextBox txtComment;
        private Button btnSaveEvaluation;
        private Label lblStudentName;
        private Label lblStudentPhone;
        private Label lblStudentEmail;
        private Label lblStudentBirthYear;
        private Label lblStudentAddress;
        private Label lblStudentStatus;
        private Label lblCourseName;
        private Label lblCourseFee;
        private Label lblClassName;
        private Label lblClassCourse;
        private Label lblClassTeacher;
        private Label lblSelectClass;
        private Label lblSelectStudent;
        private DataGridView dgvClassStudents;
        private Button btnRemoveStudentFromClass;
        private Button btnClassSchedule;
        private Label lblPaymentStudent;
        private Label lblPaymentAmount;
        private Label lblPaymentNote;
        private Label lblTeacherName;
        private Label lblTeacherPhone;
        private Label lblTeacherEmail;
        private Label lblTeacherStatus;
        private DataGridView dgvTeachers;
        private TextBox txtTeacherName;
        private TextBox txtTeacherPhone;
        private TextBox txtTeacherEmail;
        private ComboBox cmbTeacherStatus;
        private Button btnAddTeacher;
        private Button btnUpdateTeacher;
        private Button btnDeleteTeacher;
        private Label lblTeacherAccount;
        private TextBox txtTeacherUsername;
        private Label lblTeacherPassword;
        private TextBox txtTeacherPassword;
        private Button btnUpdateTeacherPassword;
        private TabPage tabTeacherTimesheet;
        private Label lblTimesheetMonth;
        private ComboBox cmbTimesheetMonth; 
        private ComboBox cmbTimesheetYear;
        private Button btnSaveTimesheet;
        private DataGridView dgvTimesheet;
        private Label lblTimesheetSummary;
        private Label lblAttendanceClass;
        private Label lblAttendanceDate;
        private Label lblEvaluateClass;
        private Label lblEvaluateStudent;
        private Label lblEvaluateScore;
        private Label lblEvaluateComment;
        private Button btnLogoutAdmin;
        private Button btnLogoutTeacher;
        private Button btnLogoutGlobal;
        private DataGridView dgvPayrollDetail;
        private Button btnViewEvaluations;
    }
}
