using MailSender.Application.Interfaces;
using MailSender.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MailSender.IntegrationTests.Infrastructure;

public class MailSenderApiFactory : WebApplicationFactory<Program>
{
    public const string RegistrationPassword = "dwa42";
    private readonly string _databaseName = $"MailSenderIntegrationTests-{Guid.NewGuid()}";

    public TestMailSenderProvider MailProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var settings = new Dictionary<string, string?>
        {
            ["Students:0:Surname"] = "Testowski",
            ["Students:0:IndexSuffix"] = "42",
            ["Jwt:SecretKey"] = "IntegrationTests-Only-Signing-Key-At-Least-32-Characters",
            ["Jwt:Issuer"] = "MailSender.IntegrationTests",
            ["Jwt:Audience"] = "MailSender.IntegrationTests.Clients",
            ["Jwt:ExpirationDays"] = "1",
            ["MailProvider:SelectedProvider"] = "Fake",
            ["Logging:LogLevel:Default"] = "Warning"
        };

        // Startup reads JWT/provider settings before registering services.
        foreach (var setting in settings)
        {
            builder.UseSetting(setting.Key, setting.Value);
        }

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // Do not inherit local credentials, environment settings or extra students.
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MailSenderDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<MailSenderDbContext>>();
            services.AddDbContext<MailSenderDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Only the external delivery boundary is replaced; all other layers are real.
            services.RemoveAll<IMailSenderProvider>();
            services.AddSingleton<IMailSenderProvider>(MailProvider);
        });
    }

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}
