using System.ComponentModel.DataAnnotations;

namespace DataDock.Web.ViewModels
{
    public class DeleteAccountViewModel : BaseLayoutViewModel
    {
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm before you can delete your account.")]
        [Display(Name = "Yes, I want to delete my account permanently.")]
        public bool Confirm { get; set; }
    }
}
