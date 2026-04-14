using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sabro.Data.Entities
{
    [Index(nameof(VersionId), nameof(Book), nameof(Chapter), IsUnique = true)]
    public class ChapterValidation : BaseEntity
    {
        public int VersionId { get; set; }

        [Required, MaxLength(100)]
        public string Book { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Chapter { get; set; } = null!;

        public ValidationStatus Status { get; set; } = ValidationStatus.Draft;

        // When true, the status was set manually rather than auto-calculated from verses
        public bool IsManualOverride { get; set; } = false;

        // Navigation properties
        public TextVersion Version { get; set; } = null!;
    }
}
