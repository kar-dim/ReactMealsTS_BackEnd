using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Repositories;
using ReactMeals_WebApi.Services.Implementations;
using ReactMeals_WebApi.Services.Interfaces;
using RestSharp;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

//db context (read connection string from appsettings)
builder.Services
    .AddDbContext<MainDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("JimmysFoodzillaConnectionString")));
//reposistories
builder.Services.AddScoped<TokenRepository>();
builder.Services.AddScoped<DishRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<UserRepository>();

// Add required services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//cors (test only + frontend with VERCEL)
var allowFrontendOnly = "allowFrontendOnly";
builder.Services
    .AddCors(options =>
    {
        options.AddPolicy(name: allowFrontendOnly, policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://react-meals-ts-front-end.vercel.app")
                .AllowAnyMethod()
                .WithHeaders("X-Requested-With", "Content-Type", "Authorization", "ngrok-skip-browser-warning");
        });
    });

// JWT (Auth0)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    // Default authorization scheme
    .AddJwtBearer("Default", options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier
        };
    })
    // Authorization scheme for M2M Auth0 API sending post-register action data
    // It uses different access tokens than the main application
    .AddJwtBearer("M2M_UserRegister", options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:M2M_Domain"]}/";
        options.Audience = builder.Configuration["Auth0:M2M_Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

//authorization for policies (admin etc)
//check the access token's "permission" scope and search for the "admin:admin" claim
builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy =>
    {
        policy.RequireClaim("permissions", "admin:admin");
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
builder.Services.AddSingleton(provider => new RestClient("https://" + provider.GetRequiredService<IConfiguration>()["Auth0:M2M_Domain"]));

var app = builder.Build();

/*
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
*/

app.UseCors(allowFrontendOnly);

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