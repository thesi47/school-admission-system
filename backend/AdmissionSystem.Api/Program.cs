using AdmissionSystem.Api.Data;
using AdmissionSystem.Api.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Bad JSON or bad route values return the same { "error": "..." } shape as every other error.
        options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new ErrorResponseDto("The request is not valid. Please check the values and try again."));
    });

var app = builder.Build();

// Unexpected errors become a safe 500 message. No stack trace or connection string is shown.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature != null)
        {
            logger.LogError(feature.Error, "Unhandled error while processing {Path}", context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            new ErrorResponseDto("Something went wrong on the server. Please try again."));
    });
});

app.MapControllers();

// Create or update the database tables when the API starts.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
