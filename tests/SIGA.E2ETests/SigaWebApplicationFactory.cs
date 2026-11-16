using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SIGA.E2ETests;

public class SigaWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _testDbPath;

    public string TestDatabasePath => _testDbPath;

    public SigaWebApplicationFactory()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"siga_e2e_{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseSqlite"] = "true",
                ["ConnectionStrings:SqliteConnection"] = $"Data Source={_testDbPath}",
                ["JwtSettings:SecretKey"] = "SIGA_E2E_Test_Secret_Key_At_Least_32_Chars_Long!",
                ["JwtSettings:Issuer"] = "SIGA.API",
                ["JwtSettings:Audience"] = "SIGA.Frontend",
                ["JwtSettings:ExpirationHours"] = "24",
                ["BootstrapAdmin:Username"] = "admin",
                ["BootstrapAdmin:Password"] = "Admin123!",
                ["SeedSampleSucursal"] = "true"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Override Rate Limiter to prevent HTTP 429 during automated bulk runs
            services.Configure<RateLimiterOptions>(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "test",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100000,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // Ignore cleanup lock if still in use by finalizer
            }
        }
    }
}
