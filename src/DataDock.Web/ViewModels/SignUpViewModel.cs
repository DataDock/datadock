using System.ComponentModel.DataAnnotations;

namespace DataDock.Web.ViewModels
{
    public class SignUpViewModel : BaseLayoutViewModel
    {
        [Required]
        [Display(Name = "Agree to Terms & Conditions")]
        [Compare("MustAgreeTerms", ErrorMessage = "Please agree to Terms and Conditions")]
        public bool AgreeTerms { get; set; }

        public bool MustAgreeTerms => true;
    }
}
