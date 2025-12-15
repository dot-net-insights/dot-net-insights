namespace DotNetInsights.DTOs
{
    /// <summary>
    /// Pagination parameters for query string binding.
    /// </summary>
    public class PaginationParams
    {
        /// <summary>
        /// Gets or sets the page number (1-based indexing).
        /// Default value is 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size (number of items per page).
        /// Default value is 10.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum allowed page size.
        /// Default value is 50.
        /// </summary>
        public const int MaxPageSize = 50;

        /// <summary>
        /// Validates the pagination parameters.
        /// </summary>
        /// <returns>True if parameters are valid; otherwise, false.</returns>
        public bool IsValid()
        {
            if (PageNumber < 1)
                return false;

            if (PageSize < 1 || PageSize > MaxPageSize)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the skip count for database queries.
        /// </summary>
        /// <returns>Number of records to skip.</returns>
        public int GetSkip()
        {
            return (PageNumber - 1) * PageSize;
        }
    }
}
