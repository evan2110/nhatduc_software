namespace NhatDucSoftware.Web.Helpers;

public static class PaginationHelper
{
    public static int GetTotalPages(int totalItems, int pageSize) =>
        totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);

    public static List<T> GetPage<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        if (items.Count == 0)
        {
            return new List<T>();
        }

        var safePage = Math.Clamp(page, 1, GetTotalPages(items.Count, pageSize));
        return items.Skip((safePage - 1) * pageSize).Take(pageSize).ToList();
    }
}
