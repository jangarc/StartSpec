// Org-unit API surface for Consumer: shell placeholder only (routes stubbed, nothing implemented) —证明拆分点 per F2 / ADR-006.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "Ok" }));

app.Run();

// Exports the implicit Program so ArchTests can inspect this assembly.
public partial class Program { }
