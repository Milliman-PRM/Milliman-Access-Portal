/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines an API for common ContentType specific functionality
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading.Tasks;

namespace MapCommonLib.ContentTypeSpecific
{
    public abstract class ContentTypeSpecificApiBase
    {
        public abstract Task<UriBuilder> GetContentUri(string SelectionGroupUrl, string UserName, object ConfigInfo);
    }
}
