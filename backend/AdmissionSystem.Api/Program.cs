var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Sprint 0: this only proves that the API starts. The real endpoints come in later sprints.
app.MapGet("/", () => "School Admission API is running.");

app.Run();
