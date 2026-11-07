namespace ei_back.Core.Application.Utils
{
    public class PagedSearchDto<T>
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalResults { get; set; }
        public string SortDirection { get; set; }
        public List<T> Items { get; set; }

    }
}
