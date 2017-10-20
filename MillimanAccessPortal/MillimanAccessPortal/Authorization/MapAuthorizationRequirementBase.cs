using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MapDbContextLib.Context;
using MapDbContextLib.Identity;

namespace MillimanAccessPortal.Authorization
{
    internal enum MapAuthorizationRequirementResult
    {
        /// <summary>
        /// Requirement satisfied, though another requirement may still force failure
        /// </summary>
        Pass,

        /// <summary>
        /// Requirement should force the authorization to fail despite any other requirement result
        /// </summary>
        Fail,

        /// <summary>
        /// Requirement should force the authorization to succeed despite any other requirement result
        /// </summary>
        Succeed,
    }

    public class MapAuthorizationRequirementBase : IAuthorizationRequirement
    {
        /// <summary>
        /// Expected to be overridden in derived classes
        /// </summary>
        /// <param name="User">The ApplicationUser to be authorized</param>
        /// <param name="DataContext">ApplicationDbContext to run needed queries</param>
        /// <returns></returns>
        internal virtual MapAuthorizationRequirementResult EvaluateRequirement(ApplicationUser User, ApplicationDbContext DataContext)
        {
            throw new NotImplementedException("Error. Called the base class implementation of MapAuthorizationRequirementBase.EvaluateRule");
        }
    }
}
