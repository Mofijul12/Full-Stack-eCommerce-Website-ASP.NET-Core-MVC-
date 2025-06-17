namespace AuthTest_RoleBased.Models.ViewModels
{
    public class ShowCartViewModel
    {
        public IEnumerable<Product> Products { get; set; }
        public string Country { get; set; }
        public string CellPhone { get; set; } = default!;
    }
}
