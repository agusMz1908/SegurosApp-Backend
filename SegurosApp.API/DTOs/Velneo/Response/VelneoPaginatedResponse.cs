namespace SegurosApp.API.DTOs.Velneo.Response
{
    public class VelneoPaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Count { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
        public int StartIndex => TotalPages > 0 ? ((Page - 1) * PageSize) + 1 : 0;
        public int EndIndex => Math.Min(Page * PageSize, TotalCount);
    }
}
