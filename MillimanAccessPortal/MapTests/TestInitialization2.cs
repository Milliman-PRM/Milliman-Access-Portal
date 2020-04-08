/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Initialization of resources used by MAP controllers, especially injected services that MAP initializes through ASP architecture
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using TestResourcesLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MillimanAccessPortal.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapTests
{
    internal class TestInitialization2
    {
        public IServiceProvider serviceProvider = default;
        public IServiceScope _scope = default;
        public IServiceScopeFactory _serviceScopeFactory = default;


        public TestInitialization2()
        {
            InitializeInjectedServices();

            _scope = _serviceScopeFactory.CreateScope();
        }

        ~TestInitialization2()
        {
            _scope.Dispose();
        }

        public void InitializeInjectedServices()
        {
            string tokenProviderName = "MAPResetToken";

            var services = new ServiceCollection();

            services.AddDbContext<MockableMapDbContext>();

            services.AddDataProtection();

            services.AddIdentityCore<ApplicationUser>()
                .AddRoles<ApplicationRole>()
                .AddSignInManager()
                .AddEntityFrameworkStores<MockableMapDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<PasswordResetSecurityTokenProvider<ApplicationUser>>(tokenProviderName)
                .AddTop100000PasswordValidator<ApplicationUser>()
                .AddRecentPasswordInDaysValidator<ApplicationUser>(30)
                .AddPasswordValidator<PasswordIsNotEmailValidator<ApplicationUser>>()
                .AddCommonWordsValidator<ApplicationUser>(new List<string>())
                ;

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 10;
                options.Password.RequiredUniqueChars = 6;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;

                // Enable custom token provider for password resets
                options.Tokens.PasswordResetTokenProvider = tokenProviderName;
            });

            // Configure custom security token provider
            services.Configure<PasswordResetSecurityTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromHours(3);
            });

            // Configure the default token provider used for account activation
            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(7);
            });

            serviceProvider = services.BuildServiceProvider();

            _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            // Test
            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                var processor = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
            }
        }
    }
}
