using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sabro.Data;
using Sabro.Data.Entities;
using Sabro.Services;
using Sabro.Services.Interfaces;
using System.Security.Cryptography.X509Certificates;

namespace Sabro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add identity roles
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFramework<ApplicationDbContext>();

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddScoped<IAnnotationService, AnnotationService>();
            builder.Services.AddScoped<IMarkdownService, MarkdownService>();

            // Swagger/OpenAPI configuration
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Dependency Injection
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
