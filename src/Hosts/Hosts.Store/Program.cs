// Org-unit API surface for Store (ADR-002). Thin adapter over canonical Application handlers — reusable composition arrives with tenant endpoints (iam 2.4+).
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "Ok" }));

app.Run();

// Exports the implicit Program so ArchTests can inspect this assembly.
public partial class Program { }
