
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Text.RegularExpressions;

namespace Milliman.Common {
    public class ServiceSupport {
        public static class PrincipalProvider {
            [ThreadStatic]
            private static IPrincipal _userPrincipal;

            public static IPrincipal UserPrincipal {
                get {return ServiceSupport.PrincipalProvider._userPrincipal;}
                set {ServiceSupport.PrincipalProvider._userPrincipal = value;}
            }
        }
    }
}
