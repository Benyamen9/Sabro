using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReviewerQualifications = table.Column<string>(type: "TEXT", nullable: true),
                    IsTrusted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsReviewer = table.Column<bool>(type: "INTEGER", nullable: false),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AlsoKnownAs = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BirthYear = table.Column<int>(type: "INTEGER", nullable: true),
                    DeathYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Century = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Biography = table.Column<string>(type: "TEXT", nullable: true),
                    FeastDay = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SourceType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Century = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TextVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Published = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingLists_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Annotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceLocation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ContentMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    ContentHtml = table.Column<string>(type: "TEXT", nullable: false),
                    ParentAnnotationId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    Published = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOfficial = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Annotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Annotations_Annotations_ParentAnnotationId",
                        column: x => x.ParentAnnotationId,
                        principalTable: "Annotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Annotations_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Annotations_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Annotations_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Annotations_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AuthorSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthorSources_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthorSources_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChapterValidations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Book = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Chapter = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsManualOverride = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterValidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterValidations_TextVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "TextVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Segments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CanonicalRef = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Book = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Chapter = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Verse = table.Column<int>(type: "INTEGER", nullable: false),
                    SegmentOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    ValidationStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Segments_TextVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "TextVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnnotationCrossReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnnotationId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetCanonicalRef = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReferenceType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnotationCrossReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnotationCrossReferences_Annotations_AnnotationId",
                        column: x => x.AnnotationId,
                        principalTable: "Annotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnnotationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnnotationId = table.Column<int>(type: "INTEGER", nullable: false),
                    OldContentMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    NewContentMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    ChangedById = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnotationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnotationHistories_Annotations_AnnotationId",
                        column: x => x.AnnotationId,
                        principalTable: "Annotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnnotationHistories_AspNetUsers_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnnotationAnchors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AnnotationId = table.Column<int>(type: "INTEGER", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartOffset = table.Column<int>(type: "INTEGER", nullable: true),
                    EndOffset = table.Column<int>(type: "INTEGER", nullable: true),
                    AnchorText = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayText = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnotationAnchors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnotationAnchors_Annotations_AnnotationId",
                        column: x => x.AnnotationId,
                        principalTable: "Annotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnnotationAnchors_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReadingHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingHistory_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingListItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReadingListId = table.Column<int>(type: "INTEGER", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingListItems_ReadingLists_ReadingListId",
                        column: x => x.ReadingListId,
                        principalTable: "ReadingLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingListItems_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SegmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    OldContent = table.Column<string>(type: "TEXT", nullable: false),
                    NewContent = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    ChangedById = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SegmentHistories_AspNetUsers_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SegmentHistories_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuggestedEdits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SuggestedContent = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: true),
                    ResolvedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuggestedEdits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuggestedEdits_AspNetUsers_ResolvedById",
                        column: x => x.ResolvedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuggestedEdits_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuggestedEdits_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFavorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    AnnotationId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites", x => x.Id);
                    table.CheckConstraint("CK_UserFavorite_Target", "SegmentId IS NOT NULL OR AnnotationId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_UserFavorites_Annotations_AnnotationId",
                        column: x => x.AnnotationId,
                        principalTable: "Annotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavorites_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SegmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    ContentHtml = table.Column<string>(type: "TEXT", nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmittedForReview = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotes_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationAnchors_AnnotationId",
                table: "AnnotationAnchors",
                column: "AnnotationId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationAnchors_SegmentId",
                table: "AnnotationAnchors",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationCrossReferences_AnnotationId",
                table: "AnnotationCrossReferences",
                column: "AnnotationId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationCrossReferences_TargetCanonicalRef",
                table: "AnnotationCrossReferences",
                column: "TargetCanonicalRef");

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationHistories_AnnotationId",
                table: "AnnotationHistories",
                column: "AnnotationId");

            migrationBuilder.CreateIndex(
                name: "IX_AnnotationHistories_ChangedById",
                table: "AnnotationHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_AuthorId",
                table: "Annotations",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_CreatedById",
                table: "Annotations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_ParentAnnotationId",
                table: "Annotations",
                column: "ParentAnnotationId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_Published",
                table: "Annotations",
                column: "Published");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_SourceId",
                table: "Annotations",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Annotations_UpdatedById",
                table: "Annotations",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Century",
                table: "Authors",
                column: "Century");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Language",
                table: "Authors",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_Name",
                table: "Authors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorSources_AuthorId_SourceId_Role",
                table: "AuthorSources",
                columns: new[] { "AuthorId", "SourceId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthorSources_SourceId",
                table: "AuthorSources",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterValidations_VersionId_Book_Chapter",
                table: "ChapterValidations",
                columns: new[] { "VersionId", "Book", "Chapter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistory_SegmentId",
                table: "ReadingHistory",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistory_UserId_LastReadAt",
                table: "ReadingHistory",
                columns: new[] { "UserId", "LastReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistory_UserId_SegmentId",
                table: "ReadingHistory",
                columns: new[] { "UserId", "SegmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingListItems_ReadingListId_Order",
                table: "ReadingListItems",
                columns: new[] { "ReadingListId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingListItems_SegmentId",
                table: "ReadingListItems",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingLists_UserId",
                table: "ReadingLists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SegmentHistories_ChangedById",
                table: "SegmentHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_SegmentHistories_SegmentId",
                table: "SegmentHistories",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Segments_Book_Chapter",
                table: "Segments",
                columns: new[] { "Book", "Chapter" });

            migrationBuilder.CreateIndex(
                name: "IX_Segments_VersionId_CanonicalRef",
                table: "Segments",
                columns: new[] { "VersionId", "CanonicalRef" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sources_Century",
                table: "Sources",
                column: "Century");

            migrationBuilder.CreateIndex(
                name: "IX_Sources_Name",
                table: "Sources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sources_SourceType",
                table: "Sources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedEdits_ResolvedById",
                table: "SuggestedEdits",
                column: "ResolvedById");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedEdits_ReviewerId",
                table: "SuggestedEdits",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_SuggestedEdits_SegmentId_Status",
                table: "SuggestedEdits",
                columns: new[] { "SegmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_AnnotationId",
                table: "UserFavorites",
                column: "AnnotationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_SegmentId",
                table: "UserFavorites",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_UserId_AnnotationId",
                table: "UserFavorites",
                columns: new[] { "UserId", "AnnotationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_UserId_SegmentId",
                table: "UserFavorites",
                columns: new[] { "UserId", "SegmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_SegmentId",
                table: "UserNotes",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotes_UserId_SegmentId",
                table: "UserNotes",
                columns: new[] { "UserId", "SegmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnotationAnchors");

            migrationBuilder.DropTable(
                name: "AnnotationCrossReferences");

            migrationBuilder.DropTable(
                name: "AnnotationHistories");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuthorSources");

            migrationBuilder.DropTable(
                name: "ChapterValidations");

            migrationBuilder.DropTable(
                name: "ReadingHistory");

            migrationBuilder.DropTable(
                name: "ReadingListItems");

            migrationBuilder.DropTable(
                name: "SegmentHistories");

            migrationBuilder.DropTable(
                name: "SuggestedEdits");

            migrationBuilder.DropTable(
                name: "UserFavorites");

            migrationBuilder.DropTable(
                name: "UserNotes");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ReadingLists");

            migrationBuilder.DropTable(
                name: "Annotations");

            migrationBuilder.DropTable(
                name: "Segments");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "TextVersions");
        }
    }
}
