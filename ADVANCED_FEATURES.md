# Advanced Features Guide

This comprehensive guide covers advanced features available in the dot-net-insights API, including pagination, search & filtering, sorting, rate limiting, and CORS configuration.

## Table of Contents

1. [Pagination](#pagination)
2. [Search & Filtering](#search--filtering)
3. [Sorting](#sorting)
4. [Rate Limiting](#rate-limiting)
5. [CORS Configuration](#cors-configuration)
6. [Best Practices](#best-practices)

---

## Pagination

Pagination allows you to retrieve large datasets in manageable chunks, improving performance and reducing bandwidth usage.

### Overview

The API supports cursor-based and offset-based pagination strategies. Cursor-based pagination is recommended for better performance with large datasets.

### Query Parameters

- `pageSize` (integer, optional): Number of items per page. Default: 20, Max: 100
- `pageNumber` (integer, optional): Page number for offset-based pagination. Default: 1
- `cursor` (string, optional): Cursor token for cursor-based pagination
- `nextPageCursor` (string, optional): Token to fetch the next page

### Code Examples

#### C# - Offset-Based Pagination

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class PaginationExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";

    public PaginationExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<T> GetPageAsync<T>(string endpoint, int pageNumber, int pageSize)
    {
        var query = $"{endpoint}?pageNumber={pageNumber}&pageSize={pageSize}";
        var response = await _httpClient.GetAsync($"{BaseUrl}{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content);
    }

    public async Task FetchAllPagesAsync()
    {
        int pageNumber = 1;
        int pageSize = 50;
        bool hasMorePages = true;

        while (hasMorePages)
        {
            var result = await GetPageAsync<PagedResult>("/api/insights", pageNumber, pageSize);
            
            Console.WriteLine($"Page {pageNumber}: {result.Items.Count} items");
            
            // Process items
            foreach (var item in result.Items)
            {
                Console.WriteLine($"  - {item.Name}");
            }

            hasMorePages = result.HasNextPage;
            pageNumber++;
        }
    }
}

public class PagedResult
{
    [JsonProperty("items")]
    public List<InsightItem> Items { get; set; }

    [JsonProperty("totalCount")]
    public int TotalCount { get; set; }

    [JsonProperty("pageNumber")]
    public int PageNumber { get; set; }

    [JsonProperty("pageSize")]
    public int PageSize { get; set; }

    [JsonProperty("totalPages")]
    public int TotalPages { get; set; }

    [JsonProperty("hasNextPage")]
    public bool HasNextPage { get; set; }

    [JsonProperty("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }
}

public class InsightItem
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}
```

#### C# - Cursor-Based Pagination

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CursorPaginationExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";

    public CursorPaginationExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<CursorPagedResult> GetPageAsync(string endpoint, string cursor = null, int pageSize = 50)
    {
        var query = $"{endpoint}?pageSize={pageSize}";
        if (!string.IsNullOrEmpty(cursor))
        {
            query += $"&cursor={Uri.EscapeDataString(cursor)}";
        }

        var response = await _httpClient.GetAsync($"{BaseUrl}{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<CursorPagedResult>(content);
    }

    public async Task FetchAllWithCursorAsync()
    {
        string currentCursor = null;
        int pageCount = 0;

        while (true)
        {
            var result = await GetPageAsync("/api/insights", currentCursor, 100);
            pageCount++;

            Console.WriteLine($"Page {pageCount}: {result.Items.Count} items");

            // Process items
            foreach (var item in result.Items)
            {
                Console.WriteLine($"  - {item.Name}");
            }

            // Check if there are more pages
            if (string.IsNullOrEmpty(result.NextCursor))
            {
                break;
            }

            currentCursor = result.NextCursor;
        }

        Console.WriteLine($"Total pages processed: {pageCount}");
    }
}

public class CursorPagedResult
{
    [JsonProperty("items")]
    public List<InsightItem> Items { get; set; }

    [JsonProperty("nextCursor")]
    public string NextCursor { get; set; }

    [JsonProperty("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }

    [JsonProperty("totalCount")]
    public int? TotalCount { get; set; }
}
```

### Usage Examples

```csharp
// Example 1: Simple offset-based pagination
var example = new PaginationExample();
await example.FetchAllPagesAsync();

// Example 2: Cursor-based pagination
var cursorExample = new CursorPaginationExample();
await cursorExample.FetchAllWithCursorAsync();

// Example 3: Fetch specific page
var firstPage = await example.GetPageAsync<PagedResult>("/api/insights", pageNumber: 1, pageSize: 25);
Console.WriteLine($"Total items available: {firstPage.TotalCount}");
Console.WriteLine($"Total pages: {firstPage.TotalPages}");
```

### Best Practices

- **Use cursor-based pagination** for production environments with large datasets
- **Optimize page size**: Balance between payload size and number of requests (50-100 items recommended)
- **Cache results** when possible to reduce API calls
- **Handle pagination metadata** to show user progress
- **Implement retry logic** for failed pagination requests

---

## Search & Filtering

Search and filtering capabilities enable you to retrieve specific data subsets efficiently.

### Query Parameters

- `search` (string, optional): Full-text search query
- `filter` (string, optional): Advanced filtering using field:value syntax
- `tags` (string[], optional): Filter by multiple tags
- `dateFrom` (datetime, optional): Filter items created after this date
- `dateTo` (datetime, optional): Filter items created before this date
- `status` (string, optional): Filter by status (active, inactive, archived)

### Code Examples

#### C# - Basic Search

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class SearchExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";

    public SearchExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<SearchResult> SearchAsync(string query, int pageSize = 20)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"{BaseUrl}/api/search?search={encodedQuery}&pageSize={pageSize}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<SearchResult>(content);
    }

    public async Task PerformSearchAsync()
    {
        var results = await SearchAsync(".NET core performance");

        Console.WriteLine($"Found {results.Items.Count} results");
        foreach (var item in results.Items)
        {
            Console.WriteLine($"Title: {item.Title}");
            Console.WriteLine($"Relevance Score: {item.RelevanceScore}");
            Console.WriteLine($"Summary: {item.Summary}\n");
        }
    }
}

public class SearchResult
{
    [JsonProperty("items")]
    public List<SearchItem> Items { get; set; }

    [JsonProperty("totalCount")]
    public int TotalCount { get; set; }

    [JsonProperty("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }
}

public class SearchItem
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("summary")]
    public string Summary { get; set; }

    [JsonProperty("relevanceScore")]
    public double RelevanceScore { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }
}
```

#### C# - Advanced Filtering

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class FilteringExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";

    public FilteringExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<FilteredResult> FilterAsync(FilterCriteria criteria)
    {
        var url = BuildFilterUrl(criteria);
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<FilteredResult>(content);
    }

    private string BuildFilterUrl(FilterCriteria criteria)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(criteria.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(criteria.Search)}");
        }

        if (criteria.Tags?.Any() == true)
        {
            var tags = string.Join(",", criteria.Tags.Select(Uri.EscapeDataString));
            queryParams.Add($"tags={tags}");
        }

        if (criteria.DateFrom.HasValue)
        {
            queryParams.Add($"dateFrom={criteria.DateFrom:yyyy-MM-dd}");
        }

        if (criteria.DateTo.HasValue)
        {
            queryParams.Add($"dateTo={criteria.DateTo:yyyy-MM-dd}");
        }

        if (!string.IsNullOrEmpty(criteria.Status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(criteria.Status)}");
        }

        if (criteria.PageSize.HasValue)
        {
            queryParams.Add($"pageSize={criteria.PageSize}");
        }

        var query = string.Join("&", queryParams);
        return string.IsNullOrEmpty(query) ? $"{BaseUrl}/api/insights" : $"{BaseUrl}/api/insights?{query}";
    }

    public async Task PerformFilterAsync()
    {
        var criteria = new FilterCriteria
        {
            Search = "performance optimization",
            Tags = new[] { ".NET", "performance", "caching" },
            DateFrom = DateTime.Now.AddMonths(-3),
            DateTo = DateTime.Now,
            Status = "active",
            PageSize = 50
        };

        var results = await FilterAsync(criteria);

        Console.WriteLine($"Found {results.TotalCount} items matching criteria");
        foreach (var item in results.Items)
        {
            Console.WriteLine($"Title: {item.Title}");
            Console.WriteLine($"Status: {item.Status}");
            Console.WriteLine($"Tags: {string.Join(", ", item.Tags)}\n");
        }
    }
}

public class FilterCriteria
{
    public string Search { get; set; }
    public string[] Tags { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string Status { get; set; }
    public int? PageSize { get; set; }
}

public class FilteredResult
{
    [JsonProperty("items")]
    public List<FilteredItem> Items { get; set; }

    [JsonProperty("totalCount")]
    public int TotalCount { get; set; }

    [JsonProperty("appliedFilters")]
    public Dictionary<string, object> AppliedFilters { get; set; }
}

public class FilteredItem
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; }

    [JsonProperty("createdDate")]
    public DateTime CreatedDate { get; set; }
}
```

### Usage Examples

```csharp
// Example 1: Simple search
var searchExample = new SearchExample();
await searchExample.PerformSearchAsync();

// Example 2: Advanced filtering
var filterExample = new FilteringExample();
await filterExample.PerformFilterAsync();

// Example 3: Combined search and filtering
var criteria = new FilterCriteria
{
    Search = "async patterns",
    Tags = new[] { "async", "await" },
    Status = "active"
};
var results = await filterExample.FilterAsync(criteria);
```

### Best Practices

- **Use specific filters** to reduce result sets
- **Implement search suggestions** with autocomplete
- **Cache filter options** (tags, statuses) for quick access
- **Validate filter inputs** before sending to API
- **Use pagination with filters** for large result sets
- **Monitor search performance** and optimize queries

---

## Sorting

Sorting enables you to organize results by specific fields and order preferences.

### Query Parameters

- `sortBy` (string): Field to sort by (e.g., "name", "date", "relevance")
- `sortOrder` (string): Sort direction - "asc" (ascending) or "desc" (descending). Default: "asc"
- `sortByMultiple` (string[]): Sort by multiple fields with priority

### Code Examples

#### C# - Basic Sorting

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class SortingExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";

    public SortingExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<SortedResult> GetSortedItemsAsync(string sortBy, string sortOrder = "asc", int pageSize = 20)
    {
        var url = $"{BaseUrl}/api/insights?sortBy={Uri.EscapeDataString(sortBy)}&sortOrder={sortOrder}&pageSize={pageSize}";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<SortedResult>(content);
    }

    public async Task DemonstrateSortingAsync()
    {
        // Sort by date (newest first)
        var byDate = await GetSortedItemsAsync("createdDate", "desc");
        Console.WriteLine("=== Sorted by Date (Newest First) ===");
        foreach (var item in byDate.Items)
        {
            Console.WriteLine($"{item.Title} - {item.CreatedDate:yyyy-MM-dd}");
        }

        // Sort by name (A-Z)
        var byName = await GetSortedItemsAsync("title", "asc");
        Console.WriteLine("\n=== Sorted by Title (A-Z) ===");
        foreach (var item in byName.Items)
        {
            Console.WriteLine(item.Title);
        }

        // Sort by relevance (if applicable)
        var byRelevance = await GetSortedItemsAsync("relevance", "desc");
        Console.WriteLine("\n=== Sorted by Relevance (Most Relevant First) ===");
        foreach (var item in byRelevance.Items)
        {
            Console.WriteLine($"{item.Title} - Score: {item.RelevanceScore}");
        }
    }
}

public class SortedResult
{
    [JsonProperty("items")]
    public List<SortedItem> Items { get; set; }

    [JsonProperty("sortBy")]
    public string SortBy { get; set; }

    [JsonProperty("sortOrder")]
    public string SortOrder { get; set; }
}

public class SortedItem
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonProperty("relevanceScore")]
    public double? RelevanceScore { get; set; }
}
```

#### C# - Multi-Field Sorting

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class MultiFieldSortingExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";

    public MultiFieldSortingExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<SortedResult> GetMultiSortedItemsAsync(List<SortField> sortFields, int pageSize = 20)
    {
        var sortParams = string.Join(",", sortFields.Select(sf => $"{sf.Field}:{sf.Order.ToLower()}"));
        var url = $"{BaseUrl}/api/insights?sortBy={Uri.EscapeDataString(sortParams)}&pageSize={pageSize}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<SortedResult>(content);
    }

    public async Task DemonstrateMultiSortAsync()
    {
        var sortFields = new List<SortField>
        {
            new SortField { Field = "status", Order = "asc" },      // First sort by status
            new SortField { Field = "createdDate", Order = "desc" } // Then by date
        };

        var results = await GetMultiSortedItemsAsync(sortFields);

        Console.WriteLine("=== Multi-Field Sorting (Status, then Date) ===");
        foreach (var item in results.Items)
        {
            Console.WriteLine($"{item.Title} - Status: {item.Status}, Date: {item.CreatedDate:yyyy-MM-dd}");
        }
    }
}

public class SortField
{
    public string Field { get; set; }
    public string Order { get; set; } // "asc" or "desc"
}
```

### Usage Examples

```csharp
// Example 1: Sort by creation date (newest first)
var sortingExample = new SortingExample();
await sortingExample.DemonstrateSortingAsync();

// Example 2: Multi-field sorting
var multiSortExample = new MultiFieldSortingExample();
await multiSortExample.DemonstrateMultiSortAsync();

// Example 3: Sort with pagination and filtering
var combined = await sortingExample.GetSortedItemsAsync("createdDate", "desc", 50);
```

### Best Practices

- **Default to relevant sorting**: Use "relevance" for search results
- **Use date sorting** for chronological data
- **Implement multi-field sorting** for complex data structures
- **Cache sort options** for user preferences
- **Validate sort fields** against available options
- **Monitor sort performance** on large datasets

---

## Rate Limiting

Rate limiting ensures fair API usage and maintains service stability.

### Overview

The API implements rate limiting based on:
- **Requests per minute**: 60 requests/minute for standard tier
- **Requests per hour**: 3,600 requests/hour for standard tier
- **Requests per day**: 50,000 requests/day for standard tier

### Response Headers

All API responses include rate limit information:

```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1639580000
X-RateLimit-RetryAfter: 30
```

### Code Examples

#### C# - Respecting Rate Limits

```csharp
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class RateLimitingExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";
    
    private int _rateLimitRemaining = 60;
    private long _rateLimitResetTime = 0;

    public RateLimitingExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetWithRateLimitHandlingAsync(string endpoint)
    {
        // Check if we're near the rate limit
        if (_rateLimitRemaining < 5)
        {
            var waitTime = _rateLimitResetTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (waitTime > 0)
            {
                Console.WriteLine($"Rate limit near. Waiting {waitTime} seconds...");
                await Task.Delay(TimeSpan.FromSeconds(waitTime));
            }
        }

        var response = await _httpClient.GetAsync($"{BaseUrl}{endpoint}");

        // Extract rate limit information from response headers
        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
        {
            _rateLimitRemaining = int.Parse(remainingValues.First());
        }

        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
        {
            _rateLimitResetTime = long.Parse(resetValues.First());
        }

        // Handle 429 (Too Many Requests) responses
        if ((int)response.StatusCode == 429)
        {
            var retryAfter = 60; // Default to 60 seconds
            if (response.Headers.TryGetValues("X-RateLimit-RetryAfter", out var retryValues))
            {
                retryAfter = int.Parse(retryValues.First());
            }

            Console.WriteLine($"Rate limit exceeded. Retrying after {retryAfter} seconds...");
            await Task.Delay(TimeSpan.FromSeconds(retryAfter));
            
            // Retry the request
            return await GetWithRateLimitHandlingAsync(endpoint);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task DisplayRateLimitStatusAsync()
    {
        Console.WriteLine($"Rate Limit Remaining: {_rateLimitRemaining}");
        var resetDateTime = DateTimeOffset.FromUnixTimeSeconds(_rateLimitResetTime);
        Console.WriteLine($"Rate Limit Resets At: {resetDateTime:yyyy-MM-dd HH:mm:ss}");
    }
}
```

#### C# - Exponential Backoff with Retry

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public class ExponentialBackoffExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";
    private const int MaxRetries = 5;
    private const int InitialBackoffMs = 1000; // Start with 1 second

    public ExponentialBackoffExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetWithExponentialBackoffAsync(string endpoint)
    {
        int retryCount = 0;
        int backoffMs = InitialBackoffMs;

        while (retryCount < MaxRetries)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}{endpoint}");

                // Handle rate limiting
                if ((int)response.StatusCode == 429)
                {
                    int retryAfter = CalculateRetryAfter(response);
                    Console.WriteLine($"Rate limited. Attempt {retryCount + 1}/{MaxRetries}. Waiting {retryAfter}ms...");
                    await Task.Delay(retryAfter);
                    retryCount++;
                    backoffMs = (int)(backoffMs * 1.5); // Exponential increase
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable || 
                                                    ex.StatusCode == HttpStatusCode.GatewayTimeout)
            {
                if (retryCount >= MaxRetries - 1)
                    throw;

                Console.WriteLine($"Service unavailable. Attempt {retryCount + 1}/{MaxRetries}. Waiting {backoffMs}ms...");
                await Task.Delay(backoffMs);
                retryCount++;
                backoffMs = (int)(backoffMs * 1.5);
            }
        }

        throw new HttpRequestException("Max retries exceeded");
    }

    private int CalculateRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-RateLimit-RetryAfter", out var retryValues))
        {
            if (int.TryParse(retryValues.First(), out int seconds))
            {
                return seconds * 1000;
            }
        }

        return InitialBackoffMs;
    }
}
```

#### C# - Request Queuing

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class RequestQueueExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";
    private readonly Queue<string> _requestQueue;
    private readonly SemaphoreSlim _semaphore;
    private const int RequestsPerSecond = 10;

    public RequestQueueExample()
    {
        _httpClient = new HttpClient();
        _requestQueue = new Queue<string>();
        _semaphore = new SemaphoreSlim(RequestsPerSecond);
    }

    public async Task QueueRequestAsync(string endpoint)
    {
        _requestQueue.Enqueue(endpoint);
    }

    public async Task ProcessQueueAsync()
    {
        while (_requestQueue.Count > 0)
        {
            await _semaphore.WaitAsync();

            try
            {
                var endpoint = _requestQueue.Dequeue();
                Console.WriteLine($"Processing: {endpoint}");

                var response = await _httpClient.GetAsync($"{BaseUrl}{endpoint}");
                response.EnsureSuccessStatusCode();

                Console.WriteLine($"Completed: {endpoint}");
            }
            finally
            {
                // Release semaphore and add delay between requests
                _ = Task.Delay(TimeSpan.FromMilliseconds(1000 / RequestsPerSecond)).ContinueWith(_ => _semaphore.Release());
            }
        }
    }

    public async Task DemoQueueProcessingAsync()
    {
        // Queue multiple requests
        await QueueRequestAsync("/api/insights?page=1");
        await QueueRequestAsync("/api/insights?page=2");
        await QueueRequestAsync("/api/insights?page=3");
        await QueueRequestAsync("/api/insights?page=4");

        // Process queue with rate limiting
        await ProcessQueueAsync();
    }
}
```

### Usage Examples

```csharp
// Example 1: Handle rate limiting with retries
var rateLimitExample = new RateLimitingExample();
var data = await rateLimitExample.GetWithRateLimitHandlingAsync("/api/insights");
await rateLimitExample.DisplayRateLimitStatusAsync();

// Example 2: Exponential backoff strategy
var backoffExample = new ExponentialBackoffExample();
var result = await backoffExample.GetWithExponentialBackoffAsync("/api/insights");

// Example 3: Queue-based request management
var queueExample = new RequestQueueExample();
await queueExample.DemoQueueProcessingAsync();
```

### Best Practices

- **Monitor X-RateLimit headers** in every response
- **Implement exponential backoff** for retries
- **Use request queuing** for batch operations
- **Cache responses** to reduce redundant requests
- **Implement progressive backoff** when approaching limits
- **Upgrade tier** if consistently hitting limits
- **Distribute requests** across time periods
- **Handle 429 responses gracefully** with user-friendly messages

---

## CORS Configuration

Cross-Origin Resource Sharing (CORS) allows controlled access to the API from different domains.

### Overview

CORS headers control which domains can access the API:

```
Access-Control-Allow-Origin: https://yourdomain.com
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Allow-Credentials: true
Access-Control-Max-Age: 86400
```

### Code Examples

#### C# - CORS Configuration on Server

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class CorsConfigurationStartup
{
    public IConfiguration Configuration { get; }

    public CorsConfigurationStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Define CORS policy
        services.AddCors(options =>
        {
            options.AddPolicy("ProductionPolicy", builder =>
            {
                builder
                    .WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("X-RateLimit-Remaining", "X-RateLimit-Reset");
            });

            options.AddPolicy("DevelopmentPolicy", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseCors("DevelopmentPolicy");
        }
        else
        {
            app.UseCors("ProductionPolicy");
        }

        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

#### C# - CORS with Custom Headers

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

public class AdvancedCorsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AdvancedPolicy", builder =>
            {
                var allowedOrigins = new List<string>
                {
                    "https://yourdomain.com",
                    "https://api.yourdomain.com",
                    "https://admin.yourdomain.com"
                };

                builder
                    .WithOrigins(allowedOrigins.ToArray())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders(
                        "X-RateLimit-Limit",
                        "X-RateLimit-Remaining",
                        "X-RateLimit-Reset",
                        "X-Request-Id",
                        "X-Total-Count"
                    )
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .SetPreflightMaxAge(System.TimeSpan.FromHours(24));
            });
        });

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseCors("AdvancedPolicy");
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

#### C# - Client-Side CORS Handling

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CorsClientExample
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.dot-net-insights.com";

    public CorsClientExample()
    {
        _httpClient = new HttpClient();
    }

    public async Task<T> GetWithCorsAsync<T>(string endpoint, Dictionary<string, string> customHeaders = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{endpoint}");

        // Add custom headers
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        if (customHeaders != null)
        {
            foreach (var header in customHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        try
        {
            var response = await _httpClient.SendAsync(request);

            // Check for CORS-related errors
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Request failed: {response.StatusCode}");
                Console.WriteLine($"Reason: {response.ReasonPhrase}");
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"CORS Error: {ex.Message}");
            throw;
        }
    }

    public async Task HandlePreflight()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, $"{BaseUrl}/api/insights");
        
        // Preflight headers
        request.Headers.Add("Origin", "https://yourdomain.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type, Authorization");

        try
        {
            var response = await _httpClient.SendAsync(request);
            
            Console.WriteLine("Preflight Response Headers:");
            foreach (var header in response.Headers)
            {
                if (header.Key.StartsWith("Access-Control"))
                {
                    Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Preflight failed: {ex.Message}");
        }
    }
}
```

#### JavaScript/TypeScript - CORS Client Example

```javascript
// CORS-aware fetch wrapper
class CorsApiClient {
  constructor(baseUrl = 'https://api.dot-net-insights.com') {
    this.baseUrl = baseUrl;
    this.rateLimitRemaining = 60;
    this.rateLimitResetTime = 0;
  }

  async fetch(endpoint, options = {}) {
    const url = `${this.baseUrl}${endpoint}`;
    
    const fetchOptions = {
      method: options.method || 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        ...options.headers,
      },
      credentials: 'include', // Include cookies for CORS
      mode: 'cors', // Explicitly set CORS mode
      ...options,
    };

    try {
      const response = await fetch(url, fetchOptions);
      
      // Extract CORS headers
      this.extractRateLimitHeaders(response);
      
      if (!response.ok) {
        if (response.status === 429) {
          const retryAfter = response.headers.get('X-RateLimit-RetryAfter') || 60;
          console.warn(`Rate limited. Retry after ${retryAfter} seconds`);
        }
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`CORS Fetch Error: ${error.message}`);
      throw error;
    }
  }

  extractRateLimitHeaders(response) {
    const remaining = response.headers.get('X-RateLimit-Remaining');
    const reset = response.headers.get('X-RateLimit-Reset');
    
    if (remaining) this.rateLimitRemaining = parseInt(remaining);
    if (reset) this.rateLimitResetTime = parseInt(reset);
  }

  async get(endpoint, options = {}) {
    return this.fetch(endpoint, { ...options, method: 'GET' });
  }

  async post(endpoint, data, options = {}) {
    return this.fetch(endpoint, {
      ...options,
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  getRateLimitStatus() {
    return {
      remaining: this.rateLimitRemaining,
      resetTime: new Date(this.rateLimitResetTime * 1000),
    };
  }
}

// Usage example
const client = new CorsApiClient();

// Fetch with CORS
client.get('/api/insights?pageSize=20')
  .then(data => console.log('Success:', data))
  .catch(error => console.error('Error:', error));

// Check rate limit status
console.log(client.getRateLimitStatus());
```

### Usage Examples

```csharp
// Server-side CORS configuration
var startup = new AdvancedCorsStartup();

// Client-side CORS handling
var corsClient = new CorsClientExample();
var data = await corsClient.GetWithCorsAsync<object>("/api/insights");
await corsClient.HandlePreflight();
```

### Best Practices

- **Whitelist specific origins** in production
- **Use credentials carefully**: Only when necessary
- **Expose only necessary headers**: Use `Access-Control-Expose-Headers`
- **Set appropriate Max-Age**: Cache preflight responses (24 hours recommended)
- **Implement origin validation**: Verify origin headers server-side
- **Use HTTPS**: Always in production
- **Test preflight requests**: Especially for non-simple requests
- **Document CORS requirements** for API consumers
- **Monitor CORS errors** in client applications

---

## Best Practices

### General API Usage

1. **Connection Management**
   ```csharp
   // Reuse HttpClient instances
   private static readonly HttpClient _httpClient = new HttpClient();
   
   // Don't dispose HttpClient in loops
   using var response = await _httpClient.GetAsync(url);
   ```

2. **Error Handling**
   ```csharp
   try
   {
       var response = await _httpClient.GetAsync(url);
       response.EnsureSuccessStatusCode();
   }
   catch (HttpRequestException ex)
   {
       // Log and handle appropriately
       Console.WriteLine($"API Error: {ex.Message}");
   }
   ```

3. **Timeout Configuration**
   ```csharp
   _httpClient.Timeout = TimeSpan.FromSeconds(30);
   ```

### Performance Optimization

- **Enable response compression**: Accept gzip encoding
- **Use ETags**: Cache responses with If-None-Match headers
- **Implement connection pooling**: Reuse connections
- **Async/await pattern**: Use asynchronous operations
- **Batch requests**: Combine related queries

### Security

- **Always use HTTPS** in production
- **Validate SSL certificates**: Don't disable certificate validation
- **Protect API keys**: Use environment variables
- **Implement request signing**: For sensitive operations
- **Validate all inputs**: Before sending to API

### Monitoring & Logging

- **Log all API requests**: For debugging and auditing
- **Track performance metrics**: Response times and error rates
- **Monitor rate limit usage**: Implement alerting
- **Sample logging**: For high-volume scenarios
- **Structured logging**: Use JSON or similar formats

### Testing

- **Mock API responses**: For unit tests
- **Test error scenarios**: 429, 5xx, network errors
- **Load testing**: Verify rate limit handling
- **Integration testing**: With staging environment
- **Test pagination**: Edge cases (first, last page)

---

## Additional Resources

- [API Documentation](https://docs.dot-net-insights.com)
- [SDKs & Libraries](https://github.com/dot-net-insights)
- [Support & FAQ](https://support.dot-net-insights.com)
- [Community Forum](https://forum.dot-net-insights.com)

---

## Contributing

Found an issue or want to improve this documentation? Please contribute:

1. Fork the repository
2. Create a feature branch
3. Make your improvements
4. Submit a pull request

---

## License

This documentation is provided under the MIT License. See LICENSE file for details.

---

**Last Updated:** 2025-12-15
**Documentation Version:** 1.0
**Status:** Production Ready
