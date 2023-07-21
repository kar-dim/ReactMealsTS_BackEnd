using Microsoft.EntityFrameworkCore;
using ReactMeals_WebApi.Contexts;

var builder = WebApplication.CreateBuilder(args);

//db context (read connection string from appsettings)
builder.Services.AddDbContext<OrdersDbContext>(options =>
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
