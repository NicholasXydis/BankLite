namespace BankLite.Application.DTOs
{
    public class PagedResultDto<T>
    {
        /// <summary>The list of items for the current page.</summary>
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        /// <summary>The total number of items across all pages.</summary>
        public int TotalCount { get; set; }
        /// <summary>The current page number.</summary>
        public int Page { get; set; }
        /// <summary>The number of items per page.</summary>
        public int PageSize { get; set; }
    }
}
