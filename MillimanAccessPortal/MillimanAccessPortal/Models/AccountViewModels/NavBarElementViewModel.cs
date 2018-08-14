using System.ComponentModel.DataAnnotations;

namespace MillimanAccessPortal.Models.AccountViewModels
{
    public class NavBarElement
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
