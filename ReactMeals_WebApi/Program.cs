using ReactMeals_WebApi.Models;
using ReactMeals_WebApi.Services;
using System.Reflection.PortableExecutable;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<JimmysFoodzillaDatabaseSettings>(builder.Configuration.GetSection("JimmysFoodzillaDatabase"));
builder.Services.AddSingleton<JimmysFoodzillaService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var allowFrontendOnly = "allowFrontendOnly";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowFrontendOnly, 
        policy => {  
            policy.WithOrigins("http://localhost:3001");
            policy.AllowAnyMethod();
            policy.WithHeaders("X-Requested-With", "Content-Type");
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(allowFrontendOnly);


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
