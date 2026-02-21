using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(UserId))]
    public class ReadingList : BaseEntity
    {
        public int UserId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Option for future use
        public bool IsPublic { get; set; } = false;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ICollection<ReadingListItem>? Items { get; set; }
    }
}
