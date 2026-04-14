using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

            // OpenAPI + Scalar
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info.Title = "Sabro API";
                    document.Info.Version = "v1";
                    document.Info.Description = "API for the Sabro biblical commentary platform.";
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

            app.UseHttpsRedirection();
            app.UseMiddleware<Sabro.Middleware.ExceptionMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
