using AdmissionSystem.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from configuration. In Docker it is set with the
// ConnectionStrings__DefaultConnection environment variable.
// It is read when the first DbContext is created (not here) so the test project
// can supply its own value.
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "The database connection string is missing. Set ConnectionStrings__DefaultConnection.");
    }
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

// Create or update the database tables when the API starts.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.MapGet("/", () => "School Admission API is running.");

app.Run();
