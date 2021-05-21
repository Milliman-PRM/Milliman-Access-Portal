/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace PowerBiLib
{
    public class PowerBiEmbedModel
    {
        public Guid WorkspaceId { get; set; }

        public Guid ReportId { get; set; }

        public string EmbedToken { get; set; }

        public string EmbedUrl { get; set; }

        public bool EditableEnabled { get; set; }

        public bool NavigationPaneEnabled { get; set; }

        public bool FilterPaneEnabled { get; set; }

        public bool BookmarksPaneEnabled { get; set; }
    }
}
