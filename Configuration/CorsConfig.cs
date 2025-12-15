using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetInsights.Configuration
{
    /// <summary>
    /// CORS (Cross-Origin Resource Sharing) configuration for the application.
    /// This class contains methods to configure CORS policies and middleware.
    /// </summary>
    public static class CorsConfig
    {
        /// <summary>
        /// The name of the default CORS policy.
        /// </summary>
        public const string DefaultCorsPolicy = "DefaultCorsPolicy";

        /// <summary>
        /// Adds CORS services to the dependency injection container.
        /// Configures the default CORS policy with allowed origins, methods, and headers.
        /// </summary>
        /// <param name="services">The IServiceCollection to add CORS services to.</param>
        /// <returns>The modified IServiceCollection for method chaining.</returns>
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicy, builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            return services;
        }

        /// <summary>
        /// Adds CORS services with a specific set of allowed origins.
        /// </summary>
        /// <param name="services">The IServiceCollection to add CORS services to.</param>
        /// <param name="allowedOrigins">Array of allowed origins (e.g., "https://example.com").</param>
        /// <returns>The modified IServiceCollection for method chaining.</returns>
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, string[] allowedOrigins)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicy, builder =>
                {
                    builder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            return services;
        }

        /// <summary>
        /// Uses the CORS middleware with the default policy.
        /// This method should be called in the Configure method of Startup.cs.
        /// </summary>
        /// <param name="app">The IApplicationBuilder to add CORS middleware to.</param>
        /// <returns>The modified IApplicationBuilder for method chaining.</returns>
        public static IApplicationBuilder UseCorsConfiguration(this IApplicationBuilder app)
        {
            app.UseCors(DefaultCorsPolicy);
            return app;
        }
    }
}
