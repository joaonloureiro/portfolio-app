using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using JoaoLoureiro.Portfolio.Core.Posts;
using JoaoLoureiro.Portfolio.Core.Projects;
using JoaoLoureiro.Portfolio.Core.Locales;
using JoaoLoureiro.Portfolio.Core.EmailDelivery;
using JoaoLoureiro.Portfolio.Core.Interfaces;
using JoaoLoureiro.Portfolio.Infrastructure.Services;
using JoaoLoureiro.Portfolio.Infrastructure.Settings;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public static class ContentPersistenceExtensions
{
    public static IServiceCollection AddContentPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ContentStoreOptions>, ContentStoreOptionsValidator>();
        services.AddOptions<ContentStoreOptions>()
            .Bind(configuration.GetSection(ContentStoreOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<EmailDeliveryPolicySeedOptions>, EmailDeliveryPolicySeedOptionsValidator>();
        services.AddOptions<EmailDeliveryPolicySeedOptions>()
            .Bind(configuration.GetSection(EmailDeliveryPolicySeedOptions.SectionName))
            .ValidateOnStart();

        services.AddDbContext<PortfolioDbContext>((serviceProvider, options) =>
        {
            var storeOptions = serviceProvider
                .GetRequiredService<IOptions<ContentStoreOptions>>()
                .Value;

            options.UseSqlite(
                ContentStorePath.CreateConnectionString(storeOptions.Path),
                sqlite => sqlite.MigrationsAssembly(typeof(PortfolioDbContext).Assembly.FullName));
        });

        services.AddScoped<IPostStore, EfPostStore>();
        services.AddScoped<IProjectStore, EfProjectStore>();
        services.AddScoped<ILocaleCatalogStore, EfLocaleCatalogStore>();
        services.AddScoped<IEmailDeliveryPolicyStore, EfEmailDeliveryPolicyStore>();
        services.AddSingleton<IEmailTransportReadiness, SmtpTransportReadiness>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<PostApplicationService>();
        services.AddScoped<ProjectApplicationService>();
        services.AddScoped<LocaleCatalogApplicationService>();
        services.AddScoped<EmailDeliveryApplicationService>();

        return services;
    }
}
