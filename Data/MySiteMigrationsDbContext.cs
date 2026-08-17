using Microsoft.EntityFrameworkCore;

using WebWayCMS.Data.DbContexts;

namespace WebWayCMS.TestHost.Data;

/// <summary>
/// Migrations-only context for this host's own tables. It inherits
/// <see cref="CmsExtensionDbContext{TSelf}"/>, which excludes every CMS-owned and Identity table, so
/// this context only ever emits the tables this host contributed (e.g. <c>Faqs</c>). Never injected
/// at runtime; it exists so <c>dotnet ef migrations add</c> has a context to diff against.
/// </summary>
public sealed class MySiteMigrationsDbContext : CmsExtensionDbContext<MySiteMigrationsDbContext>
{
    public MySiteMigrationsDbContext(
        DbContextOptions<MySiteMigrationsDbContext> options,
        IEnumerable<ICmsModelExtension> modelExtensions)
        : base(options, modelExtensions)
    {
    }
}
