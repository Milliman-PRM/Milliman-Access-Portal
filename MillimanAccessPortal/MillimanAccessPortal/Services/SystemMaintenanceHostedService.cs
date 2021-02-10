/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An ASP.NET Core hosted service that performs various system maintenance operations
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Services
{
    public class SystemMaintenanceHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Timer _clientAccessReviewNotificationTimer;
        private readonly Timer _userAccountDisableNotificationTimer;
        private readonly TimeSpan _clientReviewNotificationTimeOfDayUtc;
        private readonly TimeSpan _userAccountDisableNotificationTimeOfDayUtc;

        public SystemMaintenanceHostedService(
            IServiceProvider serviceProviderArg,
            IConfiguration appConfigArg)
        {
            _serviceProvider = serviceProviderArg;

            _clientReviewNotificationTimeOfDayUtc = TimeSpan.FromHours(appConfigArg.GetValue("ClientReviewNotificationTimeOfDayHourUtc", 9));
            _userAccountDisableNotificationTimeOfDayUtc = TimeSpan.FromHours(appConfigArg.GetValue("UserAccountDisableNotificationTimeofDayHourUtc", 8));

            _clientAccessReviewNotificationTimer = new Timer(ClientAccessReviewNotificationHandler);
            _clientAccessReviewNotificationTimer.Change(TimeSpanTillNextEvent(_clientReviewNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);

            _userAccountDisableNotificationTimer = new Timer(UserAccountDisableNotificationHandler);
            _userAccountDisableNotificationTimer.Change(TimeSpanTillNextEvent(_userAccountDisableNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        public override void Dispose()
        {
            _clientAccessReviewNotificationTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _userAccountDisableNotificationTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _clientAccessReviewNotificationTimer.Dispose();
            _userAccountDisableNotificationTimer.Dispose();
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
                Thread.Sleep(TimeSpan.FromSeconds(15));
            }
        }

        private void ClientAccessReviewNotificationHandler(object state)
        {
            Timer thisTimer = (Timer)state;

            using (var scope = _serviceProvider.CreateScope())
            {
                string emailSubject = "Action Required, MAP Client Access Review";

                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                IConfiguration appConfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                IMessageQueue messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                IHostEnvironment hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

                TimeSpan clientReviewRenewalPeriodDays = TimeSpan.FromDays(appConfig.GetValue<int>("ClientReviewRenewalPeriodDays"));
                TimeSpan clientReviewEarlyWarningDays = TimeSpan.FromDays(appConfig.GetValue<int>("ClientReviewEarlyWarningDays"));

                List<UserRoleInClient> adminRoleAssignments = dbContext.UserRoleInClient
                                                                       .Include(urc => urc.User)
                                                                       .Include(urc => urc.Client)
                                                                       .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                                                                       .ToList();
                List<ApplicationUser> allClientAdminUsers = adminRoleAssignments.Select(urc => urc.User)
                                                                                .Distinct(new IdPropertyComparer<ApplicationUser>())
                                                                                .ToList();

                foreach (ApplicationUser user in allClientAdminUsers)
                {
                    List<Client> clientsToVerify = adminRoleAssignments.Where(urc => urc.UserId == user.Id)
                                                                       .Select(urc => urc.Client)
                                                                       .Distinct(new IdPropertyComparer<Client>())
                                                                       .ToList();

                    List<Client> relevantClients = clientsToVerify.Where(c => DateTime.UtcNow.Date > c.LastAccessReview.LastReviewDateTimeUtc.Date + clientReviewRenewalPeriodDays - clientReviewEarlyWarningDays)
                                                                  .ToList();

                    if (relevantClients.Any())
                    {
                        string mapUrl = hostEnvironment switch
                        {
                            var env when env.IsProduction() => "https://map.milliman.com",
                            var env when env.IsStaging() => "https://map.milliman.com:44300",
                            var env when env.IsDevelopment() => "https://localhost:44336",
                            var env when env.IsEnvironment("internal") => "https://indy-map.milliman.com",
                            _ => "https://unhandled.environment",
                        };
                        string emailBody = "You have the role of Client Administrator of the below listed Client(s) in Milliman Access Portal (MAP). ";
                        emailBody += "Each of these Clients has an approaching deadline for the required periodic review of access assignments. ";
                        emailBody += "User access to Content published for the Client will be discontinued if the review is not completed before the deadline. " + Environment.NewLine + Environment.NewLine;
                        emailBody += $"Please login to MAP at {mapUrl} and perform the Client Access Review. Thank you for using MAP." + Environment.NewLine;

                        foreach (Client client in relevantClients.OrderBy(c => c.LastAccessReview.LastReviewDateTimeUtc))
                        {
                            DateTime deadline = client.LastAccessReview.LastReviewDateTimeUtc.Date + clientReviewRenewalPeriodDays;
                            emailBody += Environment.NewLine + $"  - Client Name: {client.Name}, deadline for review: {(client.LastAccessReview.LastReviewDateTimeUtc + clientReviewRenewalPeriodDays).ToShortDateString()}";
                        }

                        messageQueue.QueueEmail(user.Email, emailSubject, emailBody);
                    }
                }
            }
           
            thisTimer.Change(TimeSpanTillNextEvent(_clientReviewNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        private void UserAccountDisableNotificationHandler(object state)
        {
            Timer thisTimer = (Timer)state;
            using (var scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                IConfiguration appConfiguration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                IMessageQueue messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                IHostEnvironment hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

                int userAccountDisableNotificationWarningDays = appConfiguration.GetValue("UserAccountDisableNotificationWarningDays", 14);
                int userAccountDisableAfterMonths = appConfiguration.GetValue("DisableInactiveUserMonths", 12);   

                List<IGrouping<ApplicationUser, Client>> usersToNotify = dbContext.UserRoleInClient
                                                                                  .Include(usr => usr.User)
                                                                                  .Include(usr => usr.Client)
                                                                                  .Where(usr => DateTime.UtcNow.Date.AddDays(userAccountDisableNotificationWarningDays).Date == usr.User.LastLoginUtc.Value.AddMonths(userAccountDisableAfterMonths).Date)
                                                                                  .GroupBy(urc => urc.User, urc => urc.Client)
                                                                                  .ToList();
               
                string emailSubject = "Your MAP account will be disabled soon";

                string mapUrl = hostEnvironment switch
                {
                    var env when env.IsProduction() => "https://map.milliman.com",
                    var env when env.IsStaging() => "https://map.milliman.com:44300",
                    var env when env.IsDevelopment() => "https://localhost:44336",
                    var env when env.IsEnvironment("internal") => "https://indy-map.milliman.com",
                    _ => "https://unhandled.environment",
                };

                foreach (IGrouping<ApplicationUser, Client> userClients in usersToNotify)
                {
                    string emailBody = "We have noticed you haven't logged into your MAP account for a long time. ";
                    emailBody += $"As a result, you MAP account will be disabled unless you login within {userAccountDisableNotificationWarningDays} days";
                    emailBody += $"Please login to MAP at {mapUrl} if you would like your account to stay active.";

                    List<Guid> clientIDs = userClients.Select(c => c.Id).ToList();
                    List<string> clientAdminsEmails = dbContext.UserRoleInClient
                                                               .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                                                               .Where(urc => clientIDs.Contains(urc.ClientId))
                                                               .Select(urc => urc.User.Email)
                                                               .Distinct()
                                                               .ToList();
                    
                    List<string> recepients = new List<string>{userClients.Key.Email};

                    recepients.AddRange(clientAdminsEmails);    //TODO: Refactor using CC feature once available.
                    
                    messageQueue.QueueEmail(recepients, emailSubject, emailBody);                    
                }
            }
            thisTimer.Change(TimeSpanTillNextEvent(_userAccountDisableNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        private TimeSpan TimeSpanTillNextEvent(TimeSpan eventTimeAfterMidnightUtc)
        {
            DateTime nextEventUtc = DateTime.UtcNow.Date + eventTimeAfterMidnightUtc;
            while (nextEventUtc < DateTime.UtcNow)
            {
                nextEventUtc += TimeSpan.FromDays(1);
            }

            return nextEventUtc - DateTime.UtcNow;
        }
    }
}
