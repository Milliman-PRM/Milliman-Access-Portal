/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An ASP.NET Core hosted service that performs various system maintenance operations
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapCommonLib;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
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
        private readonly Timer _quarterlyClientAccessReviewSummaryNotificationTimer;

        private readonly TimeSpan _clientReviewNotificationTimeOfDayUtc;
        private readonly TimeSpan _userAccountDisableNotificationTimeOfDayUtc;
        private readonly TimeSpan _quarterlyClientAccessReviewSummaryNotificationTimeOfDayUtc;

        public SystemMaintenanceHostedService(
            IServiceProvider serviceProviderArg,
            IConfiguration appConfigArg)
        {
            _serviceProvider = serviceProviderArg;

            _clientReviewNotificationTimeOfDayUtc = TimeSpan.FromHours(appConfigArg.GetValue("ClientReviewNotificationTimeOfDayHourUtc", 9));
            _clientAccessReviewNotificationTimer = new Timer(ClientAccessReviewNotificationHandler);
            _clientAccessReviewNotificationTimer.Change(TimeSpanTillNextDailyEvent(_clientReviewNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);

            _userAccountDisableNotificationTimeOfDayUtc = TimeSpan.FromHours(appConfigArg.GetValue("UserAccountDisableNotificationTimeofDayHourUtc", 8));
            _userAccountDisableNotificationTimer = new Timer(UserAccountDisableNotificationHandler);
            _userAccountDisableNotificationTimer.Change(TimeSpanTillNextDailyEvent(_userAccountDisableNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);

            _quarterlyClientAccessReviewSummaryNotificationTimeOfDayUtc = TimeSpan.FromHours(appConfigArg.GetValue("QuarterlyClientAccessReviewSummaryNotificationTimeOfDayHourUtc", 8));
            _quarterlyClientAccessReviewSummaryNotificationTimer = new Timer(QuarterlyClientAccessReviewSummaryNotificationHandler);
            _quarterlyClientAccessReviewSummaryNotificationTimer.Change(TimeSpanTillNextDailyEvent(_quarterlyClientAccessReviewSummaryNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        public override void Dispose()
        {
            _clientAccessReviewNotificationTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _userAccountDisableNotificationTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _quarterlyClientAccessReviewSummaryNotificationTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            _clientAccessReviewNotificationTimer.Dispose();
            _userAccountDisableNotificationTimer.Dispose();
            _quarterlyClientAccessReviewSummaryNotificationTimer.Dispose();
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

                string mapUrl = GetMapRootUrl(hostEnvironment, appConfig);
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
                        var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);

                        string emailBody = "You have the role of Client Administrator of the below listed Client(s) in Milliman Access Portal (MAP). ";
                        emailBody += "Each of these Clients has an approaching deadline for the required periodic review of access assignments. ";
                        emailBody += "User access to Content published for the Client will be discontinued if the review is not completed before the deadline. " + Environment.NewLine + Environment.NewLine;
                        emailBody += $"Please login to MAP at {mapUrl} and perform the Client Access Review. Thank you for using MAP." + Environment.NewLine + Environment.NewLine;
                        emailBody += $"Note that the time zone reported for your user account can be adjusted in the Account Settings view of MAP." + Environment.NewLine + Environment.NewLine;
                        emailBody += $"Thank you for using MAP." + Environment.NewLine;

                        foreach (Client client in relevantClients.OrderBy(c => c.LastAccessReview.LastReviewDateTimeUtc))
                        {
                            DateTime deadline = client.LastAccessReview.LastReviewDateTimeUtc + clientReviewRenewalPeriodDays;  // UTC
                            emailBody += Environment.NewLine + $"  - Client Name: {client.Name}, deadline for review: {GlobalFunctions.UtcToLocalString(deadline, user.TimeZoneId)}";
                        }

                        messageQueue.QueueEmail(user.Email, emailSubject, emailBody);
                    }
                }
            }
           
            thisTimer.Change(TimeSpanTillNextDailyEvent(_clientReviewNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
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

                string mapUrl = GetMapRootUrl(hostEnvironment, appConfiguration);
                int userAccountDisableNotificationWarningDays = appConfiguration.GetValue("UserAccountDisableNotificationWarningDays", 14);
                int userAccountDisableAfterMonths = appConfiguration.GetValue("DisableInactiveUserMonths", 12);   
                string mapSupportEmail = appConfiguration.GetValue<string>("SupportEmailAddress");
                DateTime notifyIfLastLoginWas = DateTime.UtcNow.Date.AddDays(userAccountDisableNotificationWarningDays).AddMonths(-userAccountDisableAfterMonths).Date;

                List<IGrouping<ApplicationUser, Client>> usersToNotify = dbContext.UserRoleInClient
                                                                                  .Include(usr => usr.User)
                                                                                  .Include(usr => usr.Client)
                                                                                  .Where(usr => usr.User.LastLoginUtc.Value.Date == notifyIfLastLoginWas)
                                                                                  .AsEnumerable()
                                                                                  .GroupBy(urc => urc.User, urc => urc.Client)
                                                                                  .ToList();
               
                string emailSubject = "Your MAP account status";

                foreach (IGrouping<ApplicationUser, Client> userClients in usersToNotify)
                {
                    string emailBody = $"In accordance with Milliman Access Portal (MAP) policies, your account will expire soon due to inactivity. ";
                    emailBody += $"If you would like your MAP user account to remain enabled, please login to MAP at {mapUrl} within {userAccountDisableNotificationWarningDays} days.{Environment.NewLine}{Environment.NewLine}";
                    emailBody += $"If you have any questions regarding this email, please contact us at map.support@milliman.com.{Environment.NewLine}{Environment.NewLine}";
                    emailBody += $"Thanks.{Environment.NewLine}{Environment.NewLine}MAP team";

                    List<Guid> clientIDs = userClients.Select(c => c.Id).ToList();
                    List<string> clientAdminsEmails = dbContext.UserRoleInClient
                                                               .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                                                               .Where(urc => clientIDs.Contains(urc.ClientId))
                                                               .Select(urc => urc.User.Email)
                                                               .Distinct()
                                                               .ToList();
                    
                    List<string> recipients = new List<string>{userClients.Key.Email};

                    clientAdminsEmails = clientAdminsEmails.Except(recipients).ToList();

                    messageQueue.QueueMessage(recipients, null, clientAdminsEmails, emailSubject, emailBody, null, null);
                }
            }
            thisTimer.Change(TimeSpanTillNextDailyEvent(_userAccountDisableNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        private void QuarterlyClientAccessReviewSummaryNotificationHandler(object state)
        {
            Timer thisTimer = (Timer)state;

            using (var scope = _serviceProvider.CreateScope())
            {
                string emailSubject = "Quarterly Client Access Review Summary for Milliman Access Portal";

                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                IConfiguration appConfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                IMessageQueue messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
                IHostEnvironment hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

                DateTime clientReviewExpiredIfBefore = DateTime.UtcNow - TimeSpan.FromDays(appConfig.GetValue<int>("ClientReviewRenewalPeriodDays"));

                List<ProfitCenter> profitCentersToNotify = dbContext.ProfitCenter
                                                                    .Where(pc => pc.QuarterlyMaintenanceNotificationList != null)
                                                                    .AsEnumerable()
                                                                    .Where(pc => pc.QuarterlyMaintenanceNotificationList.Any())
                                                                    .ToList();

                foreach (ProfitCenter profitCenter in profitCentersToNotify)
                {
                    // Only proceed if notification has not been done for this profit center this quarter
                    if (profitCenter.LastQuarterlyMaintenanceNotificationUtc.HasValue &&
                        QuartersSinceDateTimeMinValue(DateTime.UtcNow) <= QuartersSinceDateTimeMinValue(profitCenter.LastQuarterlyMaintenanceNotificationUtc.Value))
                    {
                        continue;
                    }

                    // first get current clients, then expired
                    List<Client> clients = dbContext.Client
                                                    .Where(c => c.ProfitCenterId == profitCenter.Id)
                                                    .Where(c => c.LastAccessReview.LastReviewDateTimeUtc > clientReviewExpiredIfBefore)
                                                    .OrderBy(c => c.Name)
                                                    .ToList()
                                                    .Concat(dbContext.Client
                                                                     .Where(c => c.ProfitCenterId == profitCenter.Id)
                                                                     .Where(c => c.LastAccessReview.LastReviewDateTimeUtc < clientReviewExpiredIfBefore)
                                                                     .OrderBy(c => c.Name))
                                                    .ToList();

                    string emailBody = $"Below is a summary of Client Access Review information for all clients associated with profit center \"{profitCenter.Name}\".{Environment.NewLine}{Environment.NewLine}";

                    clients.ForEach(c => 
                    {
                        var lastReview = c.LastAccessReview;

                        string lastReviewerName = lastReview?.UserName ?? "N/A";
                        if (!string.IsNullOrWhiteSpace(lastReview?.UserName))
                        {
                            ApplicationUser reviewerRecord = dbContext.ApplicationUser.SingleOrDefault(u => EF.Functions.ILike(u.UserName, lastReview.UserName ?? "N/A"));
                            if (reviewerRecord != null) lastReviewerName = $"{reviewerRecord.FirstName} {reviewerRecord.LastName}"; 
                        }

                        string reviewStatus = lastReview?.LastReviewDateTimeUtc switch
                        {
                            null => "OVERDUE",
                            DateTime lastreview when lastreview < clientReviewExpiredIfBefore => "OVERDUE",
                            DateTime lastreview when lastreview < clientReviewExpiredIfBefore + TimeSpan.FromDays(14) => "Due within 2 weeks",
                            _ => "Active",
                        };
                        List<ApplicationUser> allClientAdmins = dbContext.UserRoleInClient
                                                                         .Where(urc => urc.Role.RoleEnum == RoleEnum.Admin)
                                                                         .Where(urc => urc.ClientId == c.Id)
                                                                         .Select(urc => urc.User)
                                                                         .AsEnumerable()
                                                                         .Distinct(new IdPropertyComparer<ApplicationUser>())
                                                                         .ToList();
                        emailBody += $"- Client: {c.Name}{Environment.NewLine}";
                        emailBody += $"  - Last review date (UTC): {c.LastAccessReview?.LastReviewDateTimeUtc.ToString("yyyy-MM-dd")}{Environment.NewLine}";
                        emailBody += $"  - Last review by: {lastReviewerName}{Environment.NewLine}";
                        emailBody += $"  - Status: {reviewStatus}{Environment.NewLine}";
                        emailBody += $"  - Client admins:{Environment.NewLine}";
                        allClientAdmins.ForEach(u => 
                        {
                            string personName = u.EmailConfirmed
                                              ? $"{ u.FirstName} { u.LastName}"
                                              : "<account not activated>";
                            emailBody += $"    - {personName}, {u.Email}{Environment.NewLine}";
                        });
                        emailBody += Environment.NewLine;
                    });

                    emailBody += $"{Environment.NewLine}Please contact {appConfig.GetValue<string>("SupportEmailAddress")} if you have any questions.{Environment.NewLine}{Environment.NewLine}";
                    emailBody += $"Thanks,{Environment.NewLine}{Environment.NewLine}";
                    emailBody += $"MAP Support team{Environment.NewLine}";

                    if (messageQueue.QueueEmail(profitCenter.QuarterlyMaintenanceNotificationList, emailSubject, emailBody))
                    {
                        Log.Information($"Quarterly summary email sent for profit center <{profitCenter.Name}> to recipients <{string.Join(", ", profitCenter.QuarterlyMaintenanceNotificationList)}>");
                        profitCenter.LastQuarterlyMaintenanceNotificationUtc = DateTime.UtcNow;
                        dbContext.SaveChanges();
                    }
                }
            }

            thisTimer.Change(TimeSpanTillNextDailyEvent(_quarterlyClientAccessReviewSummaryNotificationTimeOfDayUtc), Timeout.InfiniteTimeSpan);
        }

        private TimeSpan TimeSpanTillNextDailyEvent(TimeSpan eventTimeAfterMidnightUtc)
        {
            DateTime nextEventUtc = DateTime.UtcNow.Date + eventTimeAfterMidnightUtc;
            while (nextEventUtc < DateTime.UtcNow)
            {
                nextEventUtc += TimeSpan.FromDays(1);
            }

            return nextEventUtc - DateTime.UtcNow;
        }

        private string GetMapRootUrl(IHostEnvironment hostEnvironment, IConfiguration appConfig)
        {
            return hostEnvironment switch
            {
                var env when env.IsProduction() => "https://map.milliman.com",
                var env when env.IsStaging() => "https://map.milliman.com:44300",
                var env when env.IsDevelopment() => "https://localhost:44336",
                var env when env.IsEnvironment("internal") => "https://indy-map.milliman.com",
                _ => appConfig.GetValue("MapRootUrl", $"https://unconfigured.MapRootUrl.forEnvironment.[{hostEnvironment.EnvironmentName}]"),
            };

        }
        
        private int QuartersSinceDateTimeMinValue(DateTime date)
        {
            return (date.Year - DateTime.MinValue.Year) * 4
                   + (date.Month switch
                      {
                          int m when m < 4 => 1,
                          int m when m < 7 => 2,
                          int m when m < 10 => 3,
                          _ => 4
                      });
        }

    }
}
