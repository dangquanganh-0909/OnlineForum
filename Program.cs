using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Kestrel to handle longer URLs and larger files
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestLineSize = 32768; // 32KB instead of 16KB
                options.Limits.MaxRequestHeadersTotalSize = 65536; // 64KB instead of 32KB
                options.Limits.MaxRequestBodySize = 104857600; // 100MB for file uploads
            });

            // Add services
            builder.Services.AddDbContext<ForumDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                // Cho phép đăng nhập bằng email
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ForumDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.LogoutPath = "/Identity/Account/Logout";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
            });

            // Add Google Authentication
            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            
            // Only add Google authentication if both ClientId and ClientSecret are configured
            // and not the placeholder values
            if (!string.IsNullOrEmpty(googleClientId) 
                && !string.IsNullOrEmpty(googleClientSecret)
                && !googleClientId.Contains("YOUR_GOOGLE_CLIENT_ID")
                && !googleClientSecret.Contains("YOUR_GOOGLE_CLIENT_SECRET"))
            {
                try
                {
                    builder.Services.AddAuthentication()
                        .AddGoogle(options =>
                        {
                            options.ClientId = googleClientId;
                            options.ClientSecret = googleClientSecret;
                            options.Scope.Add("email");
                            options.Scope.Add("profile");
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to configure Google Authentication: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Info: Google Authentication not configured. Please set Authentication:Google:ClientId and Authentication:Google:ClientSecret in appsettings.json or user-secrets.");
            }

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            
            // Register custom services
            builder.Services.AddScoped<IFileUploadService, FileUploadService>();
            builder.Services.AddScoped<IAIContentService, AIContentService>();
            builder.Services.AddScoped<ISimilarContentService, SimilarContentService>();

            // Configure form options
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.KeyLengthLimit = int.MaxValue;
            });

            // Antiforgery config
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // Authorization: Allow anonymous access by default, protect specific actions with [Authorize]
            /*
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            */

            var app = builder.Build();

            // Pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Map Razor Pages first for Identity
            app.MapRazorPages();

            // Redirect /Post to Home (since we combined the functionality)
            app.MapGet("/Post", () => Results.Redirect("/"));
            app.MapPost("/Post", () => Results.Redirect("/"));

            // Map default MVC route
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Initialize database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ForumDbContext>();
                
                try
                {
                    // Initialize Users & Roles
                    await DbInitializer.InitializeAsync(services);
                    
                    // Seed 3-Tier Categories (vOz Style)
                    // Note: This might fail if migrations are not applied yet
                    ThreeTierCategorySeed.SeedData(context);
                    
                    // Seed Category Admins (after 3-tier categories exist)
                    var userManager = services.GetRequiredService<UserManager<User>>();
                    var adminUser = await userManager.FindByEmailAsync("admin@forum.com");
                    if (adminUser != null)
                    {
                        await DbInitializer.SeedCategoryAdminsAsync(context, userManager, adminUser);
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            app.Run();
        }
    }
}
