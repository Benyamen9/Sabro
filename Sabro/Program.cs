using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Sabro.Data;
using Sabro.Data.Entities;
using Sabro.Services;
using Sabro.Services.Interfaces;
using Scalar.AspNetCore;
using System.Text;

namespace Sabro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Identity
            builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // JWT authentication
            var jwtSecret = builder.Configuration["Jwt:Secret"]!;
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                };
            });

            // Application services
            builder.Services.AddControllers();
            builder.Services.AddScoped<IAnnotationService, AnnotationService>();
            builder.Services.AddScoped<ISegmentService, SegmentService>();
            builder.Services.AddScoped<IMarkdownService, MarkdownService>();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            // TODO: Add the following services and controllers (MVP Phase 1-4):
            // - TextVersionsController + service + DTOs  (PRIORITY: critical, needed in production)
            // - AuthorsController      + service + DTOs
            // - SourcesController      + service + DTOs
            // - SuggestedEditsController + service + DTOs  (reviewer workflow)
            // - ChapterValidationController + service + DTOs
            // - GET /api/segments/{id}/history      (read-only)
            // - GET /api/annotations/{id}/history   (read-only)
            // - IUserService implementation (stub exists, body is empty)

            // OpenAPI + Scalar
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info.Title = "Sabro API";
                    document.Info.Version = "v1";
                    document.Info.Description = "API for the Sabro biblical commentary platform.";
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                    {
                        ["Bearer"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT"
                        }
                    };
                    return Task.CompletedTask;
                });
                options.AddOperationTransformer((operation, context, ct) =>
                {
                    var hasAuth = context.Description.ActionDescriptor.EndpointMetadata
                        .OfType<IAuthorizeData>().Any();
                    if (hasAuth)
                    {
                        operation.Security = [new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer")] = []
                        }];
                    }
                    return Task.CompletedTask;
                });
            });

            // Database
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.Title = "Sabro API";
                    options.AddHttpAuthentication("Bearer", auth =>
                    {
                        auth.Token = "paste-your-jwt-token-here";
                    });
                });
            }

            // Seed dev data
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!db.TextVersions.Any())
                {
                    db.TextVersions.Add(new TextVersion { Name = "English", Language = "EN", Published = true });
                    db.SaveChanges();
                }
            }

            app.UseHttpsRedirection();
            app.UseMiddleware<Sabro.Middleware.ExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
