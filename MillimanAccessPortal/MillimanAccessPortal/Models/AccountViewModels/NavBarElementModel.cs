using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class NavBarElementModel
    {
        [Required]
        public long Order { get; set; }

        [Required]
        public string Label { get; set; }

        [Required]
        public string URL { get; set; }

        [Required]
        public string Icon { get; set; }
    }
}
