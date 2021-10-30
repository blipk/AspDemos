using AspDemos.Models.Identity;

namespace AspDemos.Areas.Admin.Models {
    public class UsersViewModel {
        public ApplicationUser ApplicationUser { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}