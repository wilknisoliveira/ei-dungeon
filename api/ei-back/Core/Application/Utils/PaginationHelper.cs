namespace ei_back.Core.Application.Utils
{
    public static class PaginationHelper
    {
        public static string ValidateSort(string sortDirection)
        {
            return (!string.IsNullOrWhiteSpace(sortDirection) && !sortDirection.Equals("desc")) ? "asc" : "desc";
        }

        public static int ValidateSize(int pageSize)
        {
            return (pageSize < 1) ? 10 : pageSize;
        }

        public static int ValidateOffset(int page, int size)
        {
            return page > 0 ? (page - 1) * ValidateSize(size) : 0;
        }
    }
}
