/*
 * CODE OWNERS: Tom Puckett, 
 * OBJECTIVE: The reduction runner class for Qlikview content
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MapDbContextLib.Context;
using QmsApi;

namespace ContentReductionLib.ReductionRunners
{
    internal class QvReductionRunner : ReductionRunnerBase
    {
        internal QvReductionRunner()
        {
            TestQvConnection().Wait();
        }

        private async Task TestQvConnection()
        {
            QmsApi.IQMS c;
            // not providing an address uses default http://indy-qvtest01:4799/QMS/Service
            c = new QMSClient(QMSClient.EndpointConfiguration.BasicHttpBinding_IQMS);
            //c = new QMSClient(QMSClient.EndpointConfiguration.BasicHttpBinding_IQMS, "http://QmsApiInAzure");

            var sk = await c.GetTimeLimitedServiceKeyAsync();

            Task<ServiceInfo[]> s = c.GetServicesAsync(ServiceTypes.QlikViewDistributionService);
            s.Wait();
            var z = s.Result;
        }

        #region Member properties
        internal ContentReductionTask QueueTask
        {
            set;
            private get;
        }

        internal DbContextOptions<ApplicationDbContext> ContextOptions
        {
            set;
            private get;
        }

        #endregion

        internal override bool ValidateInstance()
        {
            return
                QueueTask != null &&
                ContextOptions != null
                ;
        }

        internal override bool ExecuteReduction()
        {
            if (!ValidateInstance())
            {
                return false;
            }

            Guid G = QueueTask.Id;
            for (int i = 0; i < 25; i++)
            {
                Thread.Sleep(200);
                Trace.WriteLine($"Qv reduction task {G.ToString()} on iteration {i}");
            }
            using (ApplicationDbContext Db = new ApplicationDbContext(ContextOptions))
            {
                Db.ContentReductionTask.Find(G).ReductionStatus = ReductionStatusEnum.Reduced;
                Db.SaveChanges();
            }

            return true;
        }
    }
}
