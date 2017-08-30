using System;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;

namespace MapCommonLib.ContentTypeSpecific
{
    public abstract class ContentTypeSpecificApiBase
    {
        public abstract UriBuilder GetContentUri(ContentItemUserGroup GroupEntity, string UserName, object ConfigInfo);
    }
}
