/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Defines an API for common ContentType specific functionality
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;
using Microsoft.AspNetCore.Http;

namespace MapCommonLib.ContentTypeSpecific
{
    public abstract class ContentTypeSpecificApiBase
    {
        public abstract Task<UriBuilder> GetContentUri(SelectionGroup GroupEntity, HttpContext Context, object ConfigInfo);
    }
}
