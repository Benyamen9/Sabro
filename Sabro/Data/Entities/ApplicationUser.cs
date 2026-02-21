using Microsoft.AspNetCore.Identity;

namespace Sabro.Data.Entities
{
    public class ApplicationUser : IdentityUser<int>
    {
        public UserType Type { get; set; } = UserType.Reader;

        public bool Active { get; set; } = true;

        // Navigation properties
        public ICollection<Annotation>? CreatedAnnotations { get; set; }
        public ICollection<Annotation>? UpdatedAnnotations { get; set; }
        public ICollection<UserFavorite>? Favorites { get; set; }
        public ICollection<UserNote>? Notes { get; set; }
        public ICollection<ReadingList>? ReadingLists { get; set; }
        public ICollection<ReadingHistory>? ReadingHistory { get; set; }
    }

    public enum UserType
    {
        Reader,
        Translator,
        Admin
    }
}
