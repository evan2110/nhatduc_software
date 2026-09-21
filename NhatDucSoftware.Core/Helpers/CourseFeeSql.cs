namespace NhatDucSoftware.Core.Helpers;

public static class CourseFeeSql
{
    /// <summary>
    /// Requires aliases: c = Classes, ats = AttendanceSessions, co = Courses.
    /// </summary>
    public const string SessionTuitionFee = @"
COALESCE((
    SELECT h.TuitionFee
    FROM CourseFeeHistory h
    WHERE h.CourseId = c.CourseId
      AND h.EffectiveFrom <= CAST(ats.SessionDate AS date)
    ORDER BY h.EffectiveFrom DESC, h.Id DESC
    LIMIT 1
), co.TuitionFee)";
}
