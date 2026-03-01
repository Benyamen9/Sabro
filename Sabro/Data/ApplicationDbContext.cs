using Microsoft.EntityFrameworkCore;
using Sabro.Data.Entities;

namespace Sabro.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<TextVersion> TextVersions { get; set; } = null!;
        public DbSet<Segment> Segments { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<AuthorSource> AuthorSources { get; set; }
        public DbSet<Annotation> Annotations { get; set; }
        public DbSet<AnnotationAnchor> AnnotationAnchors { get; set; }
        public DbSet<AnnotationCrossReference> AnnotationCrossReferences { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<UserNote> UserNotes { get; set; }
        public DbSet<ReadingList> ReadingLists { get; set; }
        public DbSet<ReadingListItem> ReadingListItems { get; set; }
        public DbSet<ReadingHistory> ReadingHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Segment
            builder.Entity<Segment>()
                .HasOne(s => s.Version)
                .WithMany(v => v.Segments)
                .HasForeignKey(s => s.VersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Annotation
            builder.Entity<Annotation>()
                .HasOne(a => a.Author)
                .WithMany(au => au.Annotations)
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Annotation>()
                .HasOne(a => a.Source)
                .WithMany(s => s.Annotations)
                .HasForeignKey(a => a.SourceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Annotation>()
                .HasOne(a => a.ParentAnnotation)
                .WithMany(a => a.Replies)
                .HasForeignKey(a => a.ParentAnnotationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Annotation>()
                .HasOne(a => a.CreatedBy)
                .WithMany(u => u.CreatedAnnotations)
                .HasForeignKey(a => a.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Annotation>()
                .HasOne(a => a.UpdatedBy)
                .WithMany(u => u.UpdatedAnnotations)
                .HasForeignKey(a => a.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // AnnotationAnchor
            builder.Entity<AnnotationAnchor>()
                .HasOne(aa => aa.Annotation)
                .WithMany(a => a.Anchors)
                .HasForeignKey(aa => aa.AnnotationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AnnotationAnchor>()
                .HasOne(aa => aa.Segment)
                .WithMany(s => s.AnnotationAnchors)
                .HasForeignKey(aa => aa.SegmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // AuthorSource
            builder.Entity<AuthorSource>()
                .HasOne(aus => aus.Author)
                .WithMany(au => au.AuthorSources)
                .HasForeignKey(aus => aus.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AuthorSource>()
                .HasOne(aus => aus.Source)
                .WithMany(s => s.AuthorSources)
                .HasForeignKey(aus => aus.SourceId)
                .OnDelete(DeleteBehavior.Restrict);

            // AnnotationCrossReference
            builder.Entity<AnnotationCrossReference>()
                .HasOne(acr => acr.Annotation)
                .WithMany(a => a.CrossReferences)
                .HasForeignKey(acr => acr.AnnotationId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserFavorite
            builder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserFavorite>()
                .HasOne(uf => uf.Segment)
                .WithMany()
                .HasForeignKey(uf => uf.SegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserFavorite>()
                .HasOne(uf => uf.Annotation)
                .WithMany()
                .HasForeignKey(uf => uf.AnnotationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure that either SegmentId or AnnotationId is set, but not both
            builder.Entity<UserFavorite>()
                .ToTable(t => t.HasCheckConstraint("CK_UserFavorite_Target",
                "SegmentId IS NOT NULL OR AnnotationId IS NOT NULL"));

            // UserNote
            builder.Entity<UserNote>()
                .HasOne(un => un.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(un => un.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserNote>()
                .HasOne(un => un.Segment)
                .WithMany()
                .HasForeignKey(un => un.SegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ReadingList
            builder.Entity<ReadingList>()
                .HasOne(rl => rl.User)
                .WithMany(u => u.ReadingLists)
                .HasForeignKey(rl => rl.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ReadingListItem
            builder.Entity<ReadingListItem>()
                .HasOne(rli => rli.ReadingList)
                .WithMany(rl => rl.Items)
                .HasForeignKey(rli => rli.ReadingListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReadingListItem>()
                .HasOne(rli => rli.Segment)
                .WithMany()
                .HasForeignKey(rli => rli.SegmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ReadingHistory
            builder.Entity<ReadingHistory>()
                .HasOne(rh => rh.User)
                .WithMany(u => u.ReadingHistory)
                .HasForeignKey(rh => rh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReadingHistory>()
                .HasOne(rh => rh.Segment)
                .WithMany()
                .HasForeignKey(rh => rh.SegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed initial data
            SeedData(builder);
        }

        private  void SeedData(ModelBuilder builder)
        {
            // TODO: Add seed data for Authors, Sources, TextVersions, and Segments
        }
    }
}
