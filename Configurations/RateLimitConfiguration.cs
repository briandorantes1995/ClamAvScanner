using Microsoft.AspNetCore.RateLimiting;

namespace ClamScanner.Configurations;

public static class RateLimitConfiguration
{
    public static IServiceCollection AddRateLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(
                "public",
                config =>
                {
                    config.PermitLimit = 5;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 0;
                });

            options.AddFixedWindowLimiter(
                "authenticated",
                config =>
                {
                    config.PermitLimit = 100;
                    config.Window = TimeSpan.FromMinutes(1);
                    config.QueueLimit = 20;
                });
        });

        return services;
    }
}