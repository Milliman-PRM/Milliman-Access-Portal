/*
 * CODE OWNERS: Tom Puckett,
 * OBJECTIVE: Provides get/set access to configuration information for use throughout the application
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace ContentPublishingLib
{
    public static class Configuration
    {
        public static ConfigurationRoot ApplicationConfiguration { get; set; } = null;
    }
}
