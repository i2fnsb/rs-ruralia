var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication().AddIdentityCookies();
builder.Services.AddAuthorization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerRenderMode();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Identity endpoints ONCE
app.MapAdditionalIdentityEndpoints();

app.Run();