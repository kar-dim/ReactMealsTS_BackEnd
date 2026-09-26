using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Implementations;
using ReactMeals_WebApi.Services.Interfaces;
using RestSharp;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

//common configuration parameters
var corsPolicyName = "allowFrontendOnly";
var m2mDomain = builder.Configuration["Auth0:M2M_Domain"];
var defaultDomain = builder.Configuration["Auth0:Domain"];
var m2mAudience = builder.Configuration["Auth0:M2M_Audience"];
var defaultAudience = builder.Configuration["Auth0:Audience"];
var registrationClientId = builder.Configuration["Auth0:M2M_RegistrationClientID"] ?? builder.Configuration["Auth0:M2M_ClientID"];
var imagesPath = Path.GetFullPath(builder.Configuration["Images:Directory"] ?? "Images", builder.Environment.ContentRootPath);
// Prefer an environment secret, retain the local secret file for development.
var secretPath = Path.Combine(builder.Environment.ContentRootPath, "m2m_secret.txt");
var m2mClientSecret = builder.Configuration["Auth0:M2M_ClientSecret"] ??
    (File.Exists(secretPath) ? File.ReadAllText(secretPath).Trim() : null);
if (string.IsNullOrWhiteSpace(m2mClientSecret))
    throw new InvalidOperationException("Auth0:M2M_ClientSecret is required");
builder.Configuration["Auth0:M2M_ClientSecret"] = m2mClientSecret;
string[] allowedCorsOrigins = ["http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:3000", "https://react-meals-ts-front-end.vercel.app"];
string[] allowedCorsHeaders = ["X-Requested-With", "Content-Type", "Authorization", "ngrok-skip-browser-warning"];

//db context (read connection string from appsettings)
builder.Services.AddDbContext<MainDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("JimmysFoodzillaConnectionString")));
//repositories
builder.Services.AddScoped<DishRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<UserRepository>();

// Add required services
builder.Services.AddControllers();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("orderPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

//cors (test only + frontend with VERCEL)
builder.Services.AddCors(options => options.AddPolicy(name: corsPolicyName, policy =>
        policy.WithOrigins(allowedCorsOrigins).AllowAnyMethod().WithHeaders(allowedCorsHeaders)));

// JWT (Auth0), Default authorization scheme + M2M Auth0 API sending post-register action data
builder.Services.AddAuthentication("Default")
    .AddJwtBearer("Default", options => ConfigureJwt(options, defaultDomain, defaultAudience))
    .AddJwtBearer("M2M_UserRegister", options => ConfigureJwt(options, m2mDomain, m2mAudience));

//authorization for policies (admin etc)
//check the access token's "permission" scope and search for the "admin:admin" claim
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder("Default").RequireAuthenticatedUser().Build();
    options.AddPolicy("AdminPolicy", policy => policy.RequireClaim("permissions", "admin:admin"));
    options.AddPolicy("RegistrationClientPolicy", policy => policy.RequireAssertion(context =>
        !string.IsNullOrWhiteSpace(registrationClientId) &&
        context.User.Claims.Any(claim =>
            (claim.Type == "azp" || claim.Type == "client_id") && claim.Value == registrationClientId)));
});

/* custom services */
//ngrok
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ITunnelService, NgrokTunnelService>();
    builder.Services.AddHostedService(provider => provider.GetService<ITunnelService>());
}
//internal services that implement business logic
builder.Services.AddScoped<IDishService, DishService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDishImageService, DishImageService>();
//Auth0 Management API JWT Token Renewal Service
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<IJwtRenewalService, JwtRenewalService>();
builder.Services.AddHostedService(provider => provider.GetService<IJwtRenewalService>());
//in-memory dishes service
builder.Services.AddSingleton<IDishesCacheService, DishesCacheService>();
builder.Services.AddHostedService(provider => provider.GetService<IDishesCacheService>());
//RestSharp singleton client
builder.Services.AddSingleton(provider => new RestClient(new RestClientOptions("https://" + m2mDomain) { Timeout = TimeSpan.FromSeconds(10) }));

var app = builder.Build();
app.UseCors(corsPolicyName);

//for static images
Directory.CreateDirectory(imagesPath);
var imageContentTypes = new FileExtensionContentTypeProvider();
imageContentTypes.Mappings.Clear();
imageContentTypes.Mappings[".jpg"] = "image/jpeg";
imageContentTypes.Mappings[".png"] = "image/png";
imageContentTypes.Mappings[".gif"] = "image/gif";
imageContentTypes.Mappings[".bmp"] = "image/bmp";
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = new PathString("/dishimages"),
    ContentTypeProvider = imageContentTypes,
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Context.Response.Headers["Cache-Control"] = "public, max-age=604800, immutable";
    }
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();

void ConfigureJwt(JwtBearerOptions options, string domain, string audience)
{
    options.Authority = $"https://{domain}/";
    options.Audience = audience;
    options.TokenValidationParameters = new() { NameClaimType = ClaimTypes.NameIdentifier };
}
