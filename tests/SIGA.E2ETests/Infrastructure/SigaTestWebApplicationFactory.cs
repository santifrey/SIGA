using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SIGA.Infrastructure.Persistence;

namespace SIGA.E2ETests.Infrastructure;

public class SigaTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string TestDbFileName = "siga_e2e_tests.db";
    private static bool _databaseInitialized = false;
    private static readonly object _initLock = new();

    public string TestDbPath { get; }

    public SigaTestWebApplicationFactory()
    {
        TestDbPath = Path.Combine(AppContext.BaseDirectory, TestDbFileName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["UseSqlite"] = "true",
                ["ConnectionStrings:SqliteConnection"] = $"Data Source={TestDbPath};Mode=ReadWriteCreate;Cache=Shared;",
                ["JwtSettings:SecretKey"] = "SIGA_Local_Development_Only_Secret_Key_2026_Azafran_Mediterraneo!",
                ["JwtSettings:Issuer"] = "SIGA.API",
                ["JwtSettings:Audience"] = "SIGA.Frontend",
                ["JwtSettings:ExpirationHours"] = "24",
                ["BootstrapAdmin:Username"] = "admin",
                ["BootstrapAdmin:Password"] = "Admin123!",
                ["BootstrapAdmin:Documento"] = "00000000",
                ["BootstrapAdmin:Nombre"] = "Administrador",
                ["BootstrapAdmin:Apellido"] = "SIGA",
                ["BootstrapAdmin:Email"] = "admin@azafranmediterraneo.com",
                ["BootstrapAdmin:Telefono"] = "+54 351 0000000",
                ["SeedSampleSucursal"] = "true"
            };

            configBuilder.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            // Relajar el Rate Limiter LoginPolicy para evitar 429 durante la ejecución de pruebas E2E masivas
            services.Configure<RateLimiterOptions>(options =>
            {
                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "e2e_test_runner",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100000,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        host.Start();

        lock (_initLock)
        {
            if (!_databaseInitialized)
            {
                using var scope = host.Services.CreateScope();
                DatabaseSeeder.InitializeDatabaseAsync(scope.ServiceProvider).GetAwaiter().GetResult();
                _databaseInitialized = true;
            }
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
