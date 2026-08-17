using Microsoft.EntityFrameworkCore;

using WebWayCMS.ContentZones;
using WebWayCMS.Controllers.Admin.Handlers;
using WebWayCMS.Data.DbContexts;
using WebWayCMS.Data.Services;
using WebWayCMS.Mapping;

using WebWayCMS.TestHost.Data;
using WebWayCMS.TestHost.Models.Faq;

namespace WebWayCMS.TestHost.Diagnostics;

/// <summary>
/// Repeatable startup check for the host-extensibility seam. Each check maps to a row in the
/// acceptance table; a silent no-op in any one of them is exactly the failure mode this seam guards
/// against.
/// </summary>
public sealed class ExtensibilityDiagnostics
{
    private sealed record CheckResult(string Name, bool Passed, string Detail);

    public static async Task<IResult> RunAsync(IServiceProvider services, CancellationToken ct)
    {
        var results = new List<CheckResult>();

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        results.Add(await CheckFaqEntityInModelAsync(sp, ct));
        results.Add(await CheckFaqTableExistsAsync(sp, ct));
        results.Add(await CheckMigrationHistoriesAsync(sp, ct));
        results.Add(CheckContentStore(sp));
        results.Add(CheckHandlerInRegistry(sp));
        results.Add(CheckMapper(sp));
        results.Add(CheckWidgetSeeded(sp));
        results.Add(CheckRouteSeeded(sp));

        var passed = results.Count(r => r.Passed);
        return Results.Json(new
        {
            passed,
            total = results.Count,
            checks = results
        });
    }

    private static async Task<CheckResult> CheckFaqEntityInModelAsync(IServiceProvider sp, CancellationToken ct)
    {
        try
        {
            var db = sp.GetRequiredService<CmsDbContext>();
            var entityType = db.Model.FindEntityType(typeof(FaqDTO));
            var setType = db.Set<FaqDTO>().EntityType;
            return entityType != null && setType.ClrType == typeof(FaqDTO)
                ? new CheckResult("faq_entity_in_model", true, entityType.GetTableName() ?? "Faqs")
                : new CheckResult("faq_entity_in_model", false, "FaqDTO missing from CmsDbContext model");
        }
        catch (Exception ex)
        {
            return new CheckResult("faq_entity_in_model", false, ex.Message);
        }
    }

    private static async Task<CheckResult> CheckFaqTableExistsAsync(IServiceProvider sp, CancellationToken ct)
    {
        try
        {
            var db = sp.GetRequiredService<CmsDbContext>();
            var count = await db.Set<FaqDTO>().CountAsync(ct);
            return new CheckResult("faq_table_exists", true, $"Faqs has {count} rows");
        }
        catch (Exception ex)
        {
            return new CheckResult("faq_table_exists", false, ex.Message);
        }
    }

    private static async Task<CheckResult> CheckMigrationHistoriesAsync(IServiceProvider sp, CancellationToken ct)
    {
        try
        {
            var cms = sp.GetRequiredService<CmsDbContext>();
            var cmsApplied = await cms.Database.GetAppliedMigrationsAsync(ct);

            var host = sp.GetRequiredService<MySiteMigrationsDbContext>();
            var hostApplied = await host.Database.GetAppliedMigrationsAsync(ct);

            var cmsUntouched = !cmsApplied.Any(m => m.Contains("AddFaqs", StringComparison.Ordinal));
            var hostHasAddFaqs = hostApplied.Any(m => m.Contains("AddFaqs", StringComparison.Ordinal));

            return cmsUntouched && hostHasAddFaqs
                ? new CheckResult("migration_histories", true, $"host: [{string.Join(", ", hostApplied)}]")
                : new CheckResult("migration_histories", false, $"cmsUntouched={cmsUntouched}, hostHasAddFaqs={hostHasAddFaqs}");
        }
        catch (Exception ex)
        {
            return new CheckResult("migration_histories", false, ex.Message);
        }
    }

    private static CheckResult CheckContentStore(IServiceProvider sp)
    {
        try
        {
            var store = sp.GetService<IContentStore<FaqDTO>>();
            return store != null
                ? new CheckResult("content_store_registered", true, store.GetType().Name)
                : new CheckResult("content_store_registered", false, "IContentStore<FaqDTO> not registered");
        }
        catch (Exception ex)
        {
            return new CheckResult("content_store_registered", false, ex.Message);
        }
    }

    private static CheckResult CheckHandlerInRegistry(IServiceProvider sp)
    {
        try
        {
            var registry = sp.GetRequiredService<IAdminHandlerRegistry>();
            var handler = registry.GetHandler("faqs");
            return handler is FaqModel
                ? new CheckResult("handler_in_registry", true, handler.GetType().Name)
                : new CheckResult("handler_in_registry", false, $"GetHandler(\"faqs\") = {handler?.GetType().Name ?? "null"}");
        }
        catch (Exception ex)
        {
            return new CheckResult("handler_in_registry", false, ex.Message);
        }
    }

    private static CheckResult CheckMapper(IServiceProvider sp)
    {
        try
        {
            var mapper = sp.GetRequiredService<IMapper>();
            var dto = new FaqDTO
            {
                Question = "Q",
                Answer = "A",
                Version = new WebWayCMS.Data.Models.ContentVersion
                {
                    Node = new WebWayCMS.Data.Models.ContentNode { Id = Guid.NewGuid(), ContentTypeKey = "faqs" },
                    Title = "Test FAQ",
                },
            };
            var vm = mapper.Map<FaqUpsertViewModel>(dto);
            return vm != null
                ? new CheckResult("mapper_works", true, $"Question={vm.Question}, Answer={vm.Answer}")
                : new CheckResult("mapper_works", false, "Map returned null");
        }
        catch (Exception ex)
        {
            return new CheckResult("mapper_works", false, ex.Message);
        }
    }

    private static CheckResult CheckWidgetSeeded(IServiceProvider sp)
    {
        try
        {
            var registry = sp.GetRequiredService<IWidgetRegistry>();
            var widget = registry.GetByName("FaqList");
            return widget != null
                ? new CheckResult("widget_seeded", true, widget.DisplayName)
                : new CheckResult("widget_seeded", false, "FaqList widget not seeded");
        }
        catch (Exception ex)
        {
            return new CheckResult("widget_seeded", false, ex.Message);
        }
    }

    private static CheckResult CheckRouteSeeded(IServiceProvider sp)
    {
        try
        {
            var registry = sp.GetRequiredService<ICMSRouteRegistry>();
            var hasCodeTest = registry.GetActiveRoutes().Any(r => r.Pattern == "/code-test");
            return hasCodeTest
                ? new CheckResult("route_seeded", true, "/code-test route present")
                : new CheckResult("route_seeded", false, "/code-test route missing");
        }
        catch (Exception ex)
        {
            return new CheckResult("route_seeded", false, ex.Message);
        }
    }
}
