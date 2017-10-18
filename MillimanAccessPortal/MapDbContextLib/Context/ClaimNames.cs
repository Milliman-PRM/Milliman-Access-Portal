using System;
using System.Collections.Generic;
using System.Text;

namespace MapDbContextLib.Context
{
    public enum ClaimNames
    {
        /// <summary>
        /// Values for ClientMembership claims are Client Name
        /// </summary>
        ClientMembership,

        /// <summary>
        /// Values for ProfitCenterManager claims are PK of the referenced ProfitCenter
        /// </summary>
        ProfitCenterManager,
    }
}
