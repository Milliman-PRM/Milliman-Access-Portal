using System;
using System.Collections.Generic;
using MapDbContextLib.Context;

namespace MillimanAccessPortal.Models.ClientAdminViewModels
{
    public class SelectableNamedThing
    {
        public long Id { get; set; }
        public string DisplayName { get; set; }
        public bool Selected { get; set; }
    }

    public class EditClientViewModel
    {
        public Client ThisClient { get; set; }
        public List<SelectableNamedThing> ApplicableUsers { get; set; }
    }
}
