using NhatDucSoftware.Core.Models;
using NhatDucSoftware.Core.Services;

if (args.Length > 0 && args[0] == "month")
{
    RunMonthAnalysis();
}
else
{
    var teacherName = args.Length > 0 ? args[0] : "Thuận";
    var workDate = args.Length > 1 ? DateTime.Parse(args[1]) : new DateTime(2026, 6, 26);
    RunDayAnalysis(teacherName, workDate);
}

static void RunDayAnalysis(string teacherName, DateTime workDate)
{
    var teacherService = new TeacherService();
    var timesheetService = new TeacherTimesheetService();
    var scheduleService = new ClassScheduleService();

    var teacher = teacherService.GetAll().FirstOrDefault(t =>
        t.FullName.Contains(teacherName, StringComparison.OrdinalIgnoreCase));

    if (teacher is null)
    {
        Console.WriteLine($"Không tìm thấy giáo viên chứa '{teacherName}'.");
        return;
    }

    Console.WriteLine($"Giáo viên: {teacher.Id} - {teacher.FullName}");
    Console.WriteLine($"Ngày điều tra: {workDate:yyyy-MM-dd} ({workDate.DayOfWeek})");
    Console.WriteLine();

    var paySettings = timesheetService.GetClassPaySettings(teacher.Id);
    var classService = new ClassService();
    var allClasses = classService.GetAll().Where(c => c.TeacherId == teacher.Id).ToDictionary(c => c.Id);
    Console.WriteLine("=== Cài đặt lương theo lớp ===");
    foreach (var s in paySettings)
    {
        var info = allClasses.GetValueOrDefault(s.ClassId);
        var status = info is null ? "?" : $"{info.Status}{(info.InactiveFromWeekStart is not null ? $" từ tuần {info.InactiveFromWeekStart}" : "")}";
        Console.WriteLine($"  Lớp {s.ClassId} ({s.ClassName}): {s.PayPerShift:N0}đ | Trạng thái: {status}");
    }

    var monday = ClassWeeklySchedule.GetMondayOfWeek(workDate);
    var scheduleDay = ToScheduleDay(workDate);

    Console.WriteLine();
    Console.WriteLine($"=== Lịch tuần bắt đầu {monday:yyyy-MM-dd}, thứ {scheduleDay} ===");
    foreach (var s in paySettings)
    {
        var schedule = scheduleService.GetScheduleForWeek(s.ClassId, monday);
        var slots = schedule.Where(x => x.DayOfWeek == scheduleDay).OrderBy(x => x.ShiftNumber).ToList();
        if (slots.Count == 0) continue;

        Console.WriteLine($"  Lớp {s.ClassId} ({s.ClassName}):");
        foreach (var slot in slots)
        {
            Console.WriteLine($"    Ca {slot.ShiftNumber} (week {slot.WeekStartDate})");
        }
    }

    Console.WriteLine();
    Console.WriteLine("=== Chấm công & lương ca ===");
    var records = timesheetService.GetTimesheetByMonth(teacher.Id, workDate.Year, workDate.Month)
        .Where(r => r.WorkDate.Date == workDate.Date)
        .OrderBy(r => r.ShiftNumber);

    foreach (var r in records)
    {
        var pay = r.IsPresent ? timesheetService.GetShiftPay(teacher.Id, r.WorkDate, r.ShiftNumber) : 0;
        Console.WriteLine($"  Ca {r.ShiftNumber}: {(r.IsPresent ? "Có mặt" : "Vắng")} | Lương: {pay:N0}đ");

        if (r.IsPresent)
        {
            var matchedClasses = GetMatchedClasses(paySettings, scheduleService, monday, scheduleDay, r.ShiftNumber);
            foreach (var c in matchedClasses)
            {
                Console.WriteLine($"    -> Khớp lớp {c.ClassId} ({c.ClassName}): {c.PayPerShift:N0}đ");
            }

            if (matchedClasses.Count == 0)
            {
                Console.WriteLine("    -> Không khớp lịch lớp nào (dùng fallback)");
            }
        }
    }
}

static void RunMonthAnalysis()
{
    var teacher = new TeacherService().GetAll().First(t => t.FullName.Contains("Thuận"));
    var ts = new TeacherTimesheetService();
    var ss = new ClassScheduleService();
    var records = ts.GetTimesheetByMonth(teacher.Id, 2026, 6).Where(r => r.IsPresent).ToList();
    var paySettings = ts.GetClassPaySettings(teacher.Id);

    int doubleCount = 0;
    decimal extra = 0;
    foreach (var r in records)
    {
        var monday = ClassWeeklySchedule.GetMondayOfWeek(r.WorkDate);
        var scheduleDay = ToScheduleDay(r.WorkDate);
        var classes = GetMatchedClasses(paySettings, ss, monday, scheduleDay, r.ShiftNumber);
        var pay = ts.GetShiftPay(teacher.Id, r.WorkDate, r.ShiftNumber);
        if (classes.Count > 1)
        {
            doubleCount++;
            extra += pay - 100000;
            Console.WriteLine($"{r.WorkDate:yyyy-MM-dd} ca {r.ShiftNumber}: {classes.Count} lớp -> {pay:N0} ({string.Join(" + ", classes.Select(c => c.ClassName))})");
        }
    }

    Console.WriteLine($"Tổng ca có >1 lớp khớp: {doubleCount}, phụ thu: {extra:N0}đ");
    Console.WriteLine($"Lương tháng: {ts.CalculateMonthlyPay(teacher.Id, 2026, 6):N0}đ / {records.Count} ca (nếu 100k/ca = {records.Count * 100000:N0}đ)");
}

static List<TeacherClassPaySetting> GetMatchedClasses(
    List<TeacherClassPaySetting> paySettings,
    ClassScheduleService scheduleService,
    DateTime monday,
    int scheduleDay,
    int shiftNumber) =>
    paySettings
        .Where(c => scheduleService.GetScheduleForWeek(c.ClassId, monday)
            .Any(x => x.DayOfWeek == scheduleDay && x.ShiftNumber == shiftNumber))
        .ToList();

static int ToScheduleDay(DateTime date) => date.DayOfWeek switch
{
    DayOfWeek.Monday => 0,
    DayOfWeek.Tuesday => 1,
    DayOfWeek.Wednesday => 2,
    DayOfWeek.Thursday => 3,
    DayOfWeek.Friday => 4,
    DayOfWeek.Saturday => 5,
    DayOfWeek.Sunday => 6,
    _ => 0
};
