using Microsoft.AspNetCore.Authentication.JwtBearer;
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
string[] allowedCorsOrigins = ["http://localhost:3000", "https://react-meals-ts-front-end.vercel.app"];
string[] allowedHeaders = ["X-Requested-With", "Content-Type", "Authorization", "ngrok-skip-browser-warning"];

//db context (read connection string from appsettings)
builder.Services.AddDbContext<MainDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("JimmysFoodzillaConnectionString")));
//reposistories
builder.Services.AddScoped<TokenRepository>();
builder.Services.AddScoped<DishRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<UserRepository>();

// Add required services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//cors (test only + frontend with VERCEL)
builder.Services.AddCors(options => options.AddPolicy(name: corsPolicyName, policy => 
        policy.WithOrigins(allowedCorsOrigins).AllowAnyMethod().WithHeaders(allowedHeaders)));

// JWT (Auth0), Default authorization scheme + M2M Auth0 API sending post-register action data
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Default", options => ConfigureJwt(options, defaultDomain, defaultAudience))
    .AddJwtBearer("M2M_UserRegister", options => ConfigureJwt(options, m2mDomain, m2mAudience));

//authorization for policies (admin etc)
//check the access token's "permission" scope and search for the "admin:admin" claim
builder.Services.AddAuthorizationBuilder().AddPolicy("AdminPolicy", policy => policy.RequireClaim("permissions", "admin:admin"));

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
builder.Services.AddSingleton(provider => new RestClient("https://" + m2mDomain));

var app = builder.Build();
app.UseCors(corsPolicyName);

//for static images
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Images")),
    RequestPath = new PathString("/dishimages")
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

void ConfigureJwt(JwtBearerOptions options, string domain, string audience)
{
    options.Authority = $"https://{domain}/";
    options.Audience = audience;
    options.TokenValidationParameters = new() { NameClaimType = ClaimTypes.NameIdentifier };
}