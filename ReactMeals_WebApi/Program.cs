using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using ReactMeals_WebApi.Contexts;
using ReactMeals_WebApi.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

//db context (read connection string from appsettings)
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JimmysFoodzillaConnectionString")));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//cors (test only + frontend with VERCEL)
var allowFrontendOnly = "allowFrontendOnly";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowFrontendOnly, 
        policy => {  
            policy.WithOrigins("http://localhost:3000", "https://react-meals-ts-front-end.vercel.app");
            policy.AllowAnyMethod();
            policy.WithHeaders("X-Requested-With", "Content-Type", "Authorization", "ngrok-skip-browser-warning");
        });
});

//JWT (Auth0)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//default authorization scheme
.AddJwtBearer("Default", options =>
{
    options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
    options.Audience = builder.Configuration["Auth0:Audience"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimTypes.NameIdentifier
    };
//authorization scheme when the M2M Auth0 API sends us the post-register action data
//it uses different access tokens than the main application
}).AddJwtBearer("M2M_UserRegister", options =>
{
    options.Authority = $"https://{builder.Configuration["Auth0:M2M_Domain"]}/";
    options.Audience = builder.Configuration["Auth0:M2M_Audience"];
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

//authorization for policies (admin etc)
builder.Services.AddAuthorization(options =>
{
    //check the access token's "permission" scope and search for the "admin:admin" claim
    options.AddPolicy("AdminPolicy", policy =>
          policy.RequireClaim("permissions", "admin:admin"));
});

//our custom services
builder.Services.AddSingleton<IImageValidationService, ImageValidationService>();
//ngrok
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<NgrokTunnelService>();
//Auth0 Management API JWT Token Renewal Service
builder.Services.AddScoped<JwtService>(); // Add this line to register JwtService
builder.Services.AddSingleton<JwtValidationAndRenewalService>();
builder.Services.AddHostedService<JwtValidationAndRenewalService>(provider => provider.GetService<JwtValidationAndRenewalService>());

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
    FileProvider = new PhysicalFileProvider(
                           Path.Combine(Directory.GetCurrentDirectory(), @"Images")),
    RequestPath = new PathString("/dishimages")
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
