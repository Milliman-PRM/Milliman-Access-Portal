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
        private readonly TimeSpan _clientReviewNotificationTimeOfDayUtc;

        public SystemMaintenanceHostedService(
            IServiceProvider serviceProviderArg,
            IConfiguration appConfigArg)
        {
            _serviceProvider = serviceProviderArg;

            _clientReviewNotificationTimeOfDayUtc = TimeSpan.FromHours(appConfigArg.GetValue("ClientReviewNotificationTimeOfDayHourUtc", 9));

            _clientAccessReviewNotificationTimer = new Timer(ClientAccessReviewNotificationHandler);
            _clientAccessReviewNotificationTimer.Change(TimeSpanTillNextEvent(_clientReviewNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // await Task.Yield();
            for (;;)
            {
                await Task.Delay(TimeSpan.FromMinutes(0.5));
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

                TimeSpan clientReviewRenewalPeriodDays = TimeSpan.FromDays(appConfig.GetValue<int>("ClientReviewRenewalPeriodDays"));

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

                    List<Client> expiringClients = clientsToVerify.Where(c => DateTime.UtcNow.Date > c.LastAccessReview.LastReviewDateTimeUtc.Date + clientReviewRenewalPeriodDays)
                                                                  .ToList();

                    if (expiringClients.Any())
                    {
                        string emailBody = "You have the role of administrator of the below listed client(s) in Milliman Access Portal. ";
                        emailBody += "Each of these clients has an approaching deadline for the required periodic review of access assignments. ";
                        emailBody += "User access to content published for the client will be discontinued if the deadline passes. " + Environment.NewLine + Environment.NewLine;
                        emailBody += "Please login to MAP at https://map.milliman.com and perform the client review. Thank you for using MAP." + Environment.NewLine + Environment.NewLine;

                        foreach (Client client in expiringClients)
                        {
                            DateTime deadline = client.LastAccessReview.LastReviewDateTimeUtc.Date + clientReviewRenewalPeriodDays;
                            emailBody += $"  - Client Name: {client.Name}, deadline for review: {(client.LastAccessReview.LastReviewDateTimeUtc + clientReviewRenewalPeriodDays).ToShortDateString()}";
                        }

                        messageQueue.QueueEmail(user.Email, emailSubject, emailBody);
                    }
                }
            }

            thisTimer.Change(TimeSpanTillNextEvent(_clientReviewNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        private TimeSpan TimeSpanTillNextEvent(TimeSpan eventTimeAfterMidnightUtc)
        {
            DateTime nextEventUtc = DateTime.Today + eventTimeAfterMidnightUtc;
            if (nextEventUtc < DateTime.UtcNow)
            {
                nextEventUtc += TimeSpan.FromDays(1);
            }

            return nextEventUtc - DateTime.UtcNow;
        }
    }
}
