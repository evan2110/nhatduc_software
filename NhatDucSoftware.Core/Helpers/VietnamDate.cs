namespace NhatDucSoftware.Core.Helpers;

public static class VietnamDate
{
    public static DateTime Today
    {
        get
        {
            try
            {
                var timeZoneId = OperatingSystem.IsWindows()
                    ? "SE Asia Standard Time"
                    : "Asia/Ho_Chi_Minh";
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTime.UtcNow.AddHours(7).Date;
            }
        }
    }
}
