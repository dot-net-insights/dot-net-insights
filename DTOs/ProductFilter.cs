namespace dot_net_insights.DTOs
{
    /// <summary>
    /// Data Transfer Object for filtering product search criteria.
    /// Provides multiple filter options for querying products.
    /// </summary>
    public class ProductFilter
    {
        /// <summary>
        /// Gets or sets the search keyword for product name or description.
        /// </summary>
        public string SearchKeyword { get; set; }

        /// <summary>
        /// Gets or sets the minimum price filter.
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Gets or sets the maximum price filter.
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Gets or sets the category ID for filtering products by category.
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the product status filter (e.g., "Active", "Inactive", "Discontinued").
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the minimum stock level filter.
        /// </summary>
        public int? MinStock { get; set; }

        /// <summary>
        /// Gets or sets the maximum stock level filter.
        /// </summary>
        public int? MaxStock { get; set; }

        /// <summary>
        /// Gets or sets the brand filter for product filtering.
        /// </summary>
        public string Brand { get; set; }

        /// <summary>
        /// Gets or sets the supplier ID for filtering products by supplier.
        /// </summary>
        public int? SupplierId { get; set; }

        /// <summary>
        /// Gets or sets the rating threshold for filtering products by minimum rating.
        /// </summary>
        public double? MinRating { get; set; }

        /// <summary>
        /// Gets or sets the page number for pagination (1-based indexing).
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Gets or sets the page size for pagination.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the sort column for ordering results.
        /// </summary>
        public string SortBy { get; set; } = "Name";

        /// <summary>
        /// Gets or sets the sort direction (ascending/descending).
        /// </summary>
        public string SortDirection { get; set; } = "asc";

        /// <summary>
        /// Gets or sets a value indicating whether to include discontinued products.
        /// </summary>
        public bool IncludeDiscontinued { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to include out-of-stock products.
        /// </summary>
        public bool IncludeOutOfStock { get; set; } = false;

        /// <summary>
        /// Gets or sets the creation date filter (from date).
        /// </summary>
        public DateTime? CreatedFromDate { get; set; }

        /// <summary>
        /// Gets or sets the creation date filter (to date).
        /// </summary>
        public DateTime? CreatedToDate { get; set; }
    }
}
