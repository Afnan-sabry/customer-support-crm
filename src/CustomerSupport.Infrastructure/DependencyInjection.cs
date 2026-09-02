using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Interfaces;
using CustomerSupport.Infrastructure.Identity;
using CustomerSupport.Infrastructure.Persistence;
using CustomerSupport.Infrastructure.Persistence.Interceptors;
using CustomerSupport.Infrastructure.Repositories;
using CustomerSupport.Infrastructure.Services;
using CustomerSupport.Infrastructure.Services.Channels;
using CustomerSupport.Infrastructure.Services.MockProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPortalTokenService, PortalTokenService>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ISlaRepository, SlaRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IChannelProviderFactory, ChannelProviderFactory>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddScoped<EscalationService>();
        services.AddScoped<AssignmentService>();
        services.AddHostedService<SlaMonitoringService>();

        services.AddScoped<IEmailSender, MockEmailSender>();
        services.AddScoped<IChannelProvider, EmailChannelProvider>();

        services.AddScoped<IWhatsAppClient, MockWhatsAppClient>();
        services.AddScoped<IChannelProvider, WhatsAppChannelProvider>();

        services.AddScoped<IChatSessionService, ChatSessionService>();

        // Note: LiveChatChannelProvider (IChannelProvider for ChannelType.LiveChat) is registered
        // in the API project's Program.cs, not here, because it depends on IHubContext<ChatHub>
        // and ChatHub is defined in the API project (Infrastructure cannot reference API).

        services.AddScoped<ISmsClient, MockSmsClient>();
        services.AddScoped<IChannelProvider, SmsChannelProvider>();

        return services;
    }
}
