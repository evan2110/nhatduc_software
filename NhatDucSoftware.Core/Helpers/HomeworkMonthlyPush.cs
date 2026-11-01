namespace NhatDucSoftware.Core.Helpers;

public readonly record struct HomeworkMonthlyWrite(decimal Score, string ShortComment, int Year, int Month);

public static class HomeworkMonthlyPush
{
    public static bool TryBuild(decimal? score, string? shortComment, DateTime assignedOn, out HomeworkMonthlyWrite write, out string error)
    {
        write = default;
        if (score is null)
        {
            error = "Hãy lưu phiếu trước khi đưa vào đánh giá tháng.";
            return false;
        }

        if (score < 0 || score > 10)
        {
            error = "Điểm phải từ 0 đến 10.";
            return false;
        }

        write = new HomeworkMonthlyWrite(score.Value, shortComment?.Trim() ?? "", assignedOn.Year, assignedOn.Month);
        error = "";
        return true;
    }
}
