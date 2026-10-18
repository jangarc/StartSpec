using Hosts.Base;
using Hosts.HQ;
using Iam.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostBase();
builder.Services.AddIam(
    builder.Configuration.GetConnectionString("Iam")!,
    builder.Configuration["Iam:JwtSigningKey"] ?? throw new InvalidOperationException("Iam:JwtSigningKey is not configured."));

var app = builder.Build();

app.UseHostBasePipeline();
app.MapTenantEndpoints();
app.MapUserAuthEndpoints();
app.MapAuthorizationEndpoints();
app.MapGet("/healthz", () => Results.Ok(new { status = "Ok" }));

app.Run();

// Exports the implicit Program so ArchTests can inspect this assembly.
public partial class Program { }
