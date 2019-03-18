using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapDbContextLib.Context;
using MapDbContextLib.Models;

namespace MillimanAccessPortal.Models.SystemAdmin
{
    public class AllAuthenticationSchemes
    {
        public class AuthenticationScheme
        {
            public string Name { get; set; }

            public string DisplayName { get; set; }

            public AuthenticationType Type { get; set; }

            public List<string> DomainList { get; set; }

            /// <summary>
            /// Value should be an instance of a derived class corresponding to the Type property
            /// </summary>
            public AuthenticationSchemeProperties Properties { get; set; }


            public static explicit operator AuthenticationScheme(MapDbContextLib.Context.AuthenticationScheme dbScheme)
            {
                AuthenticationScheme returnVal = new AuthenticationScheme
                {
                    Name = dbScheme.Name,
                    DisplayName = dbScheme.DisplayName,
                    Type = dbScheme.Type,
                    DomainList = dbScheme.DomainList,
                };
                switch (dbScheme.Type)
                {
                    case AuthenticationType.Default:
                        returnVal.Properties = null;
                        break;
                    case AuthenticationType.WsFederation:
                        returnVal.Properties = (WsFederationSchemeProperties)dbScheme.SchemePropertiesObj;
                        break;
                    default:
                        throw new ApplicationException("Unsupported scheme type");
                }

                return returnVal;
            }
        }

        public List<AuthenticationScheme> Schemes { get; set; }
    }
}
