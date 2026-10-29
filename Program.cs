using WebWayCMS;
using WebWayCMS.Controllers.Admin.Handlers;
using WebWayCMS.Interfaces;
using WebWayCMS.Logging;

using WebWayCMS.TestHost.Data;
using WebWayCMS.TestHost.Diagnostics;
using WebWayCMS.TestHost.Models.Faq;
using WebWayCMS.TestHost.ViewComponents;

var builder = WebApplication.CreateBuilder(args);

// Host extensibility: contribute host-owned EF content types (FaqDTO, FaqTopicDTO), their migrations
// context, mapping profile, and content stores — entirely from this project, no CMS source changes. The
// FAQ → topic link is one [ContentRelation] on FaqUpsertViewModel.FaqTopicId: no FK, nothing to register.
builder.Services.AddWebWayCms(builder.Configuration, cms =>
{
    cms.AddApplicationAssembly(typeof(FaqDTO).Assembly);
    cms.AddMappingProfile(new FaqMappingProfile());
    cms.AddContentType<FaqDTO>("faqs");
    cms.AddContentType<FaqTopicDTO>("faqtopics");
    cms.AddMigrationsContext<MySiteMigrationsDbContext>("__EFMigrationsHistory_MySite");
});

builder.Services.AddScoped<FaqModel>();
builder.Services.AddScoped<IAdminCrudHandler>(sp => sp.GetRequiredService<FaqModel>());
builder.Services.AddScoped<FaqTopicModel>();
builder.Services.AddScoped<IAdminCrudHandler>(sp => sp.GetRequiredService<FaqTopicModel>());

// Routable widgets are collected through IEnumerable<IRoutableViewComponent>, so each one needs an
// explicit registration alongside its concrete ViewComponent registration.
builder.Services.AddScoped<FaqViewComponent>();
builder.Services.AddScoped<IRoutableViewComponent>(sp => sp.GetRequiredService<FaqViewComponent>());
builder.Services.AddScoped<FaqTopicViewComponent>();
builder.Services.AddScoped<IRoutableViewComponent>(sp => sp.GetRequiredService<FaqTopicViewComponent>());

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
