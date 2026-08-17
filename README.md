# WebWayCMS.TestHost
integratino testing host for primary project

### Make Scripts Executable
```
chmod +x ./scripts/*
```

### Setup Local Database
```
./scripts/SetupLocalPostgresDBContainer.sh
```

This creates a `postgres:18-alpine` Docker container. It prompts for database name, user,
password, and host port, defaulting to the values already hardcoded in
`WebWayCMS.TestHost/appsettings.json` (`integration-host` on port `16196`).

### Pack the CMS packages
A host project consumes the CMS as the `WebWayCMS` NuGet package from a local feed. Pack the
libraries after cloning (and after changing CMS source) before building or running such a host:
```
./scripts/PackLocalPackages.sh
```

> The integration host (`WebWayCMS.TestHost`) uses **project references** rather than the
> package feed, so it does not need this step.

### Development - Hot Reload
```
./scripts/HotReloadRun.sh
```

Runs `dotnet watch run` against the integration host with `ASPNETCORE_ENVIRONMENT=Development`, so
C# and Razor changes are picked up without a manual rebuild.

## Test widgets

Four host-owned content-zone widgets exercise the whole widget system end-to-end through the real
admin UI and public rendering path. They are the surface a Playwright suite drives; each widget
renders a stable, assertable empty state so no fixture seeding is required — Playwright creates the
FAQs and the page through the admin UI itself.

| Widget | Type | Category | What it covers |
|---|---|---|---|
| `FaqList` | non-routing | `FAQ Test` | full config matrix — one property per `EditorType` (all 15), `EntityPicker` against the host `faqs` type, `ViewPicker` over 3 views |
| `FaqCount` | non-routing | `FAQ Test` | the zero-config path (`HasConfiguration == false`, modal renders no fields) |
| `FaqDetail` | routing (1 route) | `FAQ Test Routing` | `{slug}` route, slug → `GetBySlugAsync` resolution |
| `FaqTopic` | routing (2 routes) | `FAQ Test Routing` | `topics/{topic}` + `topics/{topic}/page/{page:int}` — multi-route, literal segments, inline `int` constraint |

Zones exercised:

- **`Main`** — page-scoped, via `GenericPage/Index.cshtml`.
- **`Main` + `Sidebar`** — nested, via the CMS `Layout` widget's `TwoColumnSidebar` view.
- **`SiteFooter`** — global (`IsGlobal = true`), in `Views/Shared/_Layout.cshtml`.

`GET /_diag/extensibility` asserts all four widgets are seeded and that the two routable widgets are
in the `IRoutableViewComponent` collection (`widget_seeded`, `routable_widgets_registered`).

### Manual walkthrough

Run against a **fresh** database (widget seeding only inserts, so a previously-seeded
configuration-less `FaqList` row would mask the new `FaqListConfiguration`):

```bash
./scripts/TearDownIntegrationhost.sh -v    # drop the DB volume
./scripts/StartIntegrationHost.sh
```

Then at `http://localhost:45847`:

1. `GET /_diag/extensibility` — all checks pass, including `widget_seeded` and
   `routable_widgets_registered`.
2. `/wadmin/widgets` — all four widgets listed, in categories `FAQ Test` and `FAQ Test Routing`, in
   `Order`, with their icons.
3. `/wadmin/faqs` — create and **publish** 3-4 FAQs with lowercase slugs.
4. `/wadmin/pages` — create and publish a `GenericPage` at slug `faq-test`, view `Default`.
5. **Visit `/faq-test` once.** Zones are created lazily: the `Main` zone row does not exist until
   `ContentZoneViewComponent` renders it (`GetOrCreateViewModelByPageSlotAsync`), so it will not
   appear in `/wadmin/contentzones` until the page has been served at least once. Loading `/` at
   the same time materialises the global `SiteFooter` zone needed in step 11.
6. Edit the `faq-test` page's `Main` zone (`/wadmin/contentzones` → the page's `Main` zone → zone-edit):
   - add **FaqCount** — the modal shows no fields; the widget renders a count;
   - add **FaqList** — the modal shows all 15 fields; confirm the entity picker for *Featured FAQ*
     populates from `/wadmin/faqs/api/list`, pick each `ViewName` in turn and confirm the render
     changes, and confirm every value survives a re-open of the edit modal;
   - add **FaqDetail** and **FaqTopic**.
7. `/wadmin/routes` — three widget routes exist, prefixed by the page route:
   `/faq-test/{slug:regex(^[a-z0-9-]+$)}`, `/faq-test/topics/{topic}`,
   `/faq-test/topics/{topic}/page/{page:int}`.
8. Public URLs render the right widget and the list yields:
   `/faq-test`, `/faq-test/<a-faq-slug>`, `/faq-test/topics/general`,
   `/faq-test/topics/general/page/2`. Both inline constraints hold:
   `/faq-test/topics/general/page/abc` → 404 (`:int`), and `/faq-test/Not_A_Slug` → 404 (the slug
   regex; a matching-but-unknown slug like `/faq-test/nope` renders the `NotFound` view instead).
9. Delete the `FaqTopic` zone item → its two route rows are swept from `/wadmin/routes` and the URLs
   404. Re-add it → the routes come back.
10. Nested zones: add the CMS **Layout** widget to `Main`, pick `TwoColumnSidebar`, and confirm the
    inner `Main` and `Sidebar` zones appear and accept a `FaqList`.
11. Global zone: add a `FaqCount` to the footer `SiteFooter` zone and confirm it renders on
    `/faq-test`, `/code-test`, and `/`.
12. Read context: leave one FAQ as an unpublished draft — absent from the public page, present via
    `/wadmin/faqs/preview/{nodeId}` (preview cookie path).
13. Deactivate `FaqCount` at `/wadmin/widgets` → it disappears from the add-widget picker while
    existing placements keep rendering.

Assertion surfaces: `data-testid="faq-list"` (with `data-view`, `data-faq-count`, a
`data-testid="faq-list-empty"` empty state, and one `data-cfg-{property}` attribute per config value —
dates are emitted in `yyyy-MM-dd` / `yyyy-MM-ddTHH:mm` via `FormValueFormatter`, matching what the
form submitted), `data-testid="faq-count"` (with `data-count`), `data-testid="faq-detail"` (with
`data-slug` and `data-source="route|config|missing"` — the widget renders nothing at all when no slug
is in the URL and no fallback FAQ is configured), and `data-testid="faq-topic"` (with `data-topic`,
`data-page`, `data-page-size` (the *effective* size, after the fallback), `data-match-count`).