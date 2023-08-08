using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReactMeals_WebApi.Contexts;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

//db context (read connection string from appsettings)
builder.Services.AddDbContext<MainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JimmysFoodzillaConnectionString")));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

//cors (test only)
var allowFrontendOnly = "allowFrontendOnly";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowFrontendOnly, 
        policy => {  
            policy.WithOrigins("http://192.168.1.2:3000", "http://localhost:3001", "http://localhost:3000");
            policy.AllowAnyMethod();
            policy.WithHeaders("X-Requested-With", "Content-Type", "Authorization");
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

//Authorization (needed??)
/*
builder.Services.AddAuthorization(options =>
{
    var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
        JwtBearerDefaults.AuthenticationScheme,
        "M2M_UserRegister");
    defaultAuthorizationPolicyBuilder =
        defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
    options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
});
*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(allowFrontendOnly);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
