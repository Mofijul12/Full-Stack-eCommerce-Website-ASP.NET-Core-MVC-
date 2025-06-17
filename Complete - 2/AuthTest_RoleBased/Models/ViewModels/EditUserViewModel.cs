using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AuthTest_RoleBased.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Email { get; set; }

        public string CellPhone { get; set; }

        [DisplayName("Delivery Address")]
        public string Country { get; set; }
    }
}
