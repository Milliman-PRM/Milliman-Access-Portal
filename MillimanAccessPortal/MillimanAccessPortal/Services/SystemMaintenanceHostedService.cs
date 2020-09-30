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
        private Timer _clientAccessReviewNotificationTimer;

        public SystemMaintenanceHostedService(
            IServiceProvider serviceProviderArg)
        {
            _serviceProvider = serviceProviderArg;

            _clientAccessReviewNotificationTimer = new Timer(ClientAccessReviewNotificationHandler);
            _clientAccessReviewNotificationTimer.Change(0,Timeout.Infinite);
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        private void ClientAccessReviewNotificationHandler(object state)
        {
            Timer thisTimer = (Timer)state;

            using (var scope = _serviceProvider.CreateScope())
            {
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                IConfiguration appConfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                IMessageQueue messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();

                TimeSpan clientReviewRenewalPeriodDays = TimeSpan.FromDays(appConfig.GetValue<int>("ClientReviewRenewalPeriodDays"));
                TimeSpan clientReviewGracePeriodDays = TimeSpan.FromDays(appConfig.GetValue<int>("ClientReviewGracePeriodDays"));

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

                    List<Client> expiringClients = clientsToVerify.Where(c => DateTime.UtcNow > c.LastAccessReview.LastReviewDateTimeUtc + clientReviewRenewalPeriodDays + clientReviewGracePeriodDays)
                                                                  .ToList();

                    // this is intended as a place to filter the expiring client so that notifications are sent only on certain days (e.g. 14 days before, 7, 2, 1) but that 
                    // might not make sense when we are combining notifications for clients with various expirations.  Discuss...
                    expiringClients = expiringClients.Where(c => ).ToList();  

                    if (expiringClients.Any())
                    {

                        // Build a notification email to user
                        string emailSubject = "Action Required, client access review";

                        string emailBody = "You have the role of administrator of the below listed client(s) in Milliman Access Portal. ";
                        emailBody += "Each of these clients has an approaching deadline for the required periodic review of access assignments. ";
                        emailBody += "User access to content published for the client will be discontinued if the deadline passes. " + Environment.NewLine + Environment.NewLine;
                        emailBody += "Please login to MAP at https://map.milliman.com and perform the client review. Thank you for using MAP." + Environment.NewLine + Environment.NewLine;

                        foreach (Client client in expiringClients)
                        {
                            DateTime deadline = client.LastAccessReview.LastReviewDateTimeUtc + clientReviewRenewalPeriodDays;
                            emailBody += $"  - Client Name: {client.Name}, deadline for review: {(client.LastAccessReview.LastReviewDateTimeUtc + clientReviewRenewalPeriodDays).ToShortDateString()}";
                        }

                        messageQueue.QueueEmail(user.Email, emailSubject, emailBody);
                    }
                }
            }

            // Next timer callback should be the next day at time 09:00 UTC
            DateTime nextCall = DateTime.Today + TimeSpan.FromHours(24 + 9);
            thisTimer.Change(nextCall - DateTime.UtcNow, Timeout.InfiniteTimeSpan);
        }
    }
}
