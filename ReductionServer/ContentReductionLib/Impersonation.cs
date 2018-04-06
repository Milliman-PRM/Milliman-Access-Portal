/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Class to encapsulate the authentication and impersonation of another user
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace ContentReductionLib
{

    public class Impersonation : IDisposable
    {
        // These support various types of LogonUser() requests.  From winbase.h
        const int LOGON32_PROVIDER_DEFAULT = 0;
        const int LOGON32_PROVIDER_WINNT35 = 1;
        const int LOGON32_PROVIDER_WINNT40 = 2;
        const int LOGON32_PROVIDER_WINNT50 = 3;
        const int LOGON32_PROVIDER_VIRTUAL = 4;
        const int LOGON32_LOGON_INTERACTIVE = 2;
        const int LOGON32_LOGON_NETWORK = 3;
        const int LOGON32_LOGON_BATCH = 4;
        const int LOGON32_LOGON_SERVICE = 5;
        const int LOGON32_LOGON_UNLOCK = 7;
        const int LOGON32_LOGON_NETWORK_CLEARTEXT = 8;
        const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, 
                                            String lpszDomain, 
                                            String lpszPassword, 
                                            int dwLogonType, 
                                            int dwLogonProvider, 
                                            out SafeAccessTokenHandle phToken);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetLastError();

        private SafeAccessTokenHandle _SafeAccessTokenHandle = null;

        /// <summary>
        /// Authenticates the specified credentials and retains an active handle to the resulting identity
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="DomainName"></param>
        /// <param name="Password"></param>
        public Impersonation(string UserName, string DomainName, string Password)
        {
            bool returnValue = LogonUser(UserName, DomainName, Password,
                                         LOGON32_LOGON_NEW_CREDENTIALS,
                                         LOGON32_PROVIDER_DEFAULT,
                                         out _SafeAccessTokenHandle);

            //WindowsIdentity ImpersonatedIdentity = new WindowsIdentity(_SafeAccessTokenHandle.DangerousGetHandle());

            if (!returnValue)
            {
                int result = GetLastError();
                Console.WriteLine($"Login failed, GetLastError returned {result}");
                throw new ApplicationException("Impersonation login failed");
            }
        }

        /// <summary>
        /// Executes an Action with the impersonated user's identity
        /// </summary>
        /// <param name="A"></param>
        public void UsingImpersonatedIdentity(Action A)
        {
            WindowsIdentity.RunImpersonated(_SafeAccessTokenHandle, A);
        }

        /// <summary>
        /// Executes a Func that returns a value with the impersonated user's identity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="F"></param>
        /// <returns></returns>
        public T UsingImpersonatedIdentity<T>(Func<T> F)
        {
            return WindowsIdentity.RunImpersonated(_SafeAccessTokenHandle, F);
        }

        public void Test()
        {
            Console.WriteLine("Before impersonation: " + WindowsIdentity.GetCurrent().Name);

            // Note: To run unimpersonated, pass 'SafeAccessTokenHandle.InvalidHandle' instead of variable 'safeAccessTokenHandle'
            WindowsIdentity.RunImpersonated(_SafeAccessTokenHandle, () =>
            {
                // Check the identity.
                Console.WriteLine("During impersonation: " + WindowsIdentity.GetCurrent().Name);
            });

            // Check the identity again.
            Console.WriteLine("After impersonation: " + WindowsIdentity.GetCurrent().Name);
        }

        public void Dispose()
        {
            this._SafeAccessTokenHandle.Dispose();
        }
    }
}
