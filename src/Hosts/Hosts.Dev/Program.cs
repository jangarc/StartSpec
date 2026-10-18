// One-button development host (G4): boots everything locally for rehearsal; dev-only — production deploys use the org-unit hosts exclusively.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "Ok", env = "dev" }));

app.Run();

// Exports the implicit Program so ArchTests can inspect this assembly.
public partial class Program { }
