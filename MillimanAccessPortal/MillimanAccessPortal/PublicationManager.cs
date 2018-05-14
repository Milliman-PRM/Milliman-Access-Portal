using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using AuditLogLib;
using AuditLogLib.Services;
using MapDbContextLib.Models;

namespace MillimanAccessPortal
{
    internal class PublicationManager
    {
        internal static Dictionary<long, CancellationToken> ActivePublicationManagers = new Dictionary<long, CancellationToken>();

        #region Instance members
        private CancellationToken _CancellationToken;
        private PublishRequest _RequestDetails;
        private IAuditLogger AuditLog;
        #endregion

        internal PublicationManager(PublishRequest Request)
        {
            _RequestDetails = Request;
        }

        internal void Execute(CancellationToken cancellationToken)
        {
            ActivePublicationManagers.Add(_RequestDetails.RootContentItemId, cancellationToken);
            if (AuditLog == null)
            {
                AuditLog = new AuditLogger();
            }

            _CancellationToken = cancellationToken;
            MethodBase Method = MethodBase.GetCurrentMethod();

        }
    }
}
