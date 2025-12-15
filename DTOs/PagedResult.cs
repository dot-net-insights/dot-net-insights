using System;
using System.Collections.Generic;

namespace DotNetInsights.DTOs
{
    /// <summary>
    /// Represents a paginated response containing a collection of items with pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the result set.</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Gets or sets the collection of items for the current page.
        /// </summary>
        public IEnumerable<T> Items { get; set; }

        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets a value indicating whether there are more pages after the current page.
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Gets a value indicating whether there are pages before the current page.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
        /// </summary>
        public PagedResult()
        {
            Items = new List<T>();
            PageNumber = 1;
            PageSize = 10;
            TotalItems = 0;
            TotalPages = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedResult{T}"/> class with specified parameters.
        /// </summary>
        /// <param name="items">The collection of items for the current page.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalItems">The total number of items across all pages.</param>
        public PagedResult(IEnumerable<T> items, int pageNumber, int pageSize, int totalItems)
        {
            Items = items ?? new List<T>();
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalItems = totalItems;
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        }
    }
}
