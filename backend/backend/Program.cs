using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using backend.Services; 
using backend.Models;
using Microsoft.AspNetCore.Identity;


var builder = WebApplication.CreateBuilder(args);

// Konfiguracja AppContext dla UTC
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);


builder.Services.AddControllers();

//polaczenie z baza
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

//uslugi
builder.Services.AddScoped<GeneringDataService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Inicjalizacja bazy danych
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        if (context.Database.CanConnect() && context.Database.GetAppliedMigrations().Any())
        {
            Console.WriteLine("Database already exists with migrations, skipping migration.");
        }
        else
        {
            context.Database.Migrate();
        }
        
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Wystąpił błąd podczas inicjalizacji bazy danych.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.MapGet("/", ([FromServices] GeneringDataService dataService) =>
{
    var populationData = dataService.LoadPopulationData();
    var interestRates = dataService.GetInterestRates();
    var flatPrices = dataService.GetFlatPrices();
    
    return Results.Json(new
    {
        Population = populationData,
        InterestRates = interestRates,
        FlatPrices = flatPrices
    });
});

app.Run();
