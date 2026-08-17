using WebWayCMS;
using WebWayCMS.Controllers.Admin.Handlers;
using WebWayCMS.Logging;

using WebWayCMS.TestHost.Data;
using WebWayCMS.TestHost.Diagnostics;
using WebWayCMS.TestHost.Models.Faq;

var builder = WebApplication.CreateBuilder(args);

// Host extensibility: contribute a host-owned EF content type (FaqDTO), its migrations context,
// mapping profile, and content store — entirely from this project, no CMS source changes.
builder.Services.AddWebWayCms(builder.Configuration, cms =>
{
    cms.AddApplicationAssembly(typeof(FaqDTO).Assembly);
    cms.AddMappingProfile(new FaqMappingProfile());
    cms.AddContentType<FaqDTO>("faqs");
    cms.AddMigrationsContext<MySiteMigrationsDbContext>("__EFMigrationsHistory_MySite");
});

builder.Services.AddScoped<FaqModel>();
builder.Services.AddScoped<IAdminCrudHandler>(sp => sp.GetRequiredService<FaqModel>());

builder.Host.UseCmsSerilog();

var mvc = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
    mvc.AddRazorRuntimeCompilation();
}

var app = builder.Build();

app.MapGet("/_diag/extensibility", (IServiceProvider services, CancellationToken ct) =>
    ExtensibilityDiagnostics.RunAsync(services, ct));

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseWebWayCms(); // applies migrations, seeds roles/admin + the default Home page,
                     // and configures the middleware pipeline + dynamic page routing
app.Run();
