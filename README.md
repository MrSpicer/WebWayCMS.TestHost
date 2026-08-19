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

## Seeded fixtures

A fresh `./scripts/StartIntegrationHost.sh` boot seeds the whole test site automatically through the
JSON content seeder — no manual `/wadmin` creation is required before anything is assertable. Two
files carry the fixtures, one per source provider so a single boot exercises both:

| File | Provider | Contents |
|---|---|---|
| `Fixtures/faqs.contentseed.json` | embedded resource (`.contentseed.json` suffix) | 7 `faqs` items |
| `contentseed/pages.json` | disk folder (`ContentSeed:Path` defaults to `contentseed`) | 6 `pages` items |

FAQ slugs:

| Slug | Topic (slug prefix) | Rating | Published |
|---|---|---|---|
| `general-reset-password` | `general` | 5 | yes |
| `general-account-page` | `general` | 4 | yes |
| `general-contact-support` | `general` | 3 | yes |
| `general-support-hours` | `general` | 2 | yes |
| `billing-update-card` | `billing` | 1 | yes |
| `billing-refunds` | `billing` (sanitization probe) | 0 | yes |
| `draft-unpublished` | — | 5 | no |

`FaqCount` renders **6**; `FaqTopic` on `general` matches **4** (page size 2 ⇒ two full pages) and on
`billing` matches **2**. Ratings are all distinct so `data-faq-rating` assertions pin the right row.

Page slugs: `faq-test`, `page-test` (+ its child `page-test/nested`), `zones`, `zones-landing`,
`configured`.

The seed `id` in each file is a **stable key, not a node id** — the ledger maps it to a CMS-generated
`ContentNode.Id`, so Playwright keys off **slugs**, not seed guids. Cross-item references
(`ConfiguredPage.FeaturedFaqId` → the `general-reset-password` FAQ, and the `nested` page's
`ParentNodeId` → the `page-test` page) use `@seed:{guid}` tokens resolved through the ledger.

Seeding is **idempotent**: an admin edit to a seeded FAQ survives reboots, and changing a fixture file
re-applies it as a new version on the same node. To build the site by hand instead, disable seeding
with `WEBWAYCMS_SKIP_CONTENTSEED=true` (or `ContentSeed:Enabled=false`).

## Test widgets

Four host-owned content-zone widgets exercise the whole widget system end-to-end through the real
admin UI and public rendering path. They are the surface a Playwright suite drives. The FAQ and page
*content* is seeded (see Seeded fixtures), but *widget placements* are not — each widget renders a
stable, assertable empty state, and Playwright places the widgets into zones through the admin UI
itself.

| Widget | Type | Category | What it covers |
|---|---|---|---|
| `FaqList` | non-routing | `FAQ Test` | full config matrix — one property per `EditorType` (all 15), `EntityPicker` against the host `faqs` type, `ViewPicker` over 3 views, plus a `HeadingIcon` property on the host-contributed `IconPicker` form component (17 properties total) |
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

Run against a **fresh** database. Dropping the volume (`-v`) is **mandatory**: the previously
committed migration `20260817014733_AddFaqs` was replaced by `20260817185956_AddFaqs`, so a volume
that ran the old one has it in the host history table and the new ID's `CreateTable("Faqs")` hits
Postgres `42P07 relation "Faqs" already exists` at startup. (Widget seeding also only inserts, so a
previously-seeded configuration-less `FaqList` row would mask the new `FaqListConfiguration`.)
`StartIntegrationHost.sh` only offers to tear down when a stack is *already running*, and defaults to
**N** — a stopped-but-not-`-v`-torn-down stack reaches the `42P07` failure with no prompt at all.

```bash
./scripts/TearDownIntegrationhost.sh -v    # drop the DB volume
./scripts/StartIntegrationHost.sh
```

Then at `http://localhost:45847`:

1. `GET /_diag/extensibility` — all checks pass, including `widget_seeded` and
   `routable_widgets_registered`.
2. `/wadmin/widgets` — all four widgets listed, in categories `FAQ Test` and `FAQ Test Routing`, in
   `Order`, with their icons.
3. `/wadmin/faqs` — the seven FAQs are already seeded: six published plus a `draft-unpublished` draft.
4. `/wadmin/pages` — already seeded: `faq-test` (`GenericPage`, view `Default`) plus the page-controller
   pages below.
5. **Visit `/faq-test` once.** Zones are created lazily: the `Main` zone row does not exist until
   `ContentZoneViewComponent` renders it (`GetOrCreateViewModelByPageSlotAsync`), so it will not
   appear in `/wadmin/contentzones` until the page has been served at least once. Loading `/` at
   the same time materialises the global `SiteFooter` zone needed in step 11.
6. Edit the `faq-test` page's `Main` zone (`/wadmin/contentzones` → the page's `Main` zone → zone-edit):
   - add **FaqCount** — the modal shows no fields; the widget renders a count;
   - add **FaqList** — the modal shows all 16 fields; confirm the entity picker for *Featured FAQ*
     populates from `/wadmin/faqs/api/list`, pick each `ViewName` in turn and confirm the render
     changes, confirm the *Heading Icon* field renders as a `<select>` + server-rendered preview (the
     host-contributed `IconPicker` form component), and confirm every value survives a re-open of the
     edit modal;
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

Every element that represents one FAQ also carries `data-faq-rating` — the `HelpfulnessRating` column
on the host `faqs` table, edited through the host-contributed `StarRating` form component. It is on the
per-item element in each list view (`<dt>` in `FaqList/Default`, `.accordion-item` in `Accordion`, `<li>`
in `Compact` and `FaqTopic/Default`, `<tr>` in `FaqTopic/Table`), on `.faq-list-featured`, and on the
`FaqDetail` root (omitted there when no FAQ resolved). That closes the loop the `data-cfg-*` attributes
close for widget config: it is the only public-side surface proving a *content-type* field round-tripped
through the custom form component to the database and back.

## Host-Contributed Form Components

Two `[CMSFormComponent]` ViewComponents in this project exercise the host form-component seam
(`WebWayCMS.TestHost/ViewComponents/Forms/`), seeded automatically by `CmsFormComponentSeeder` — no
`Program.cs` registration needed, since the host already calls
`cms.AddApplicationAssembly(typeof(FaqDTO).Assembly)`. Both are selected by name
(`[FormProperty(FormComponent = "…")]`), not by `EditorType`.

| Component | Used by | Binding | Shape |
|---|---|---|---|
| `FormStarRating` (registry name `StarRating`) | `faqs` upsert form, field `HelpfulnessRating` (`int`, 0-5) | Model-bound | Six radios sharing one `name` |
| `FormIconPicker` (registry name `IconPicker`) | `FaqList` widget config and `ConfiguredPage` page config, field `HeadingIcon` (`string?`) | Model- or JSON-bound | One `<select>` + a server-rendered `<i class="…">` preview |

This split is deliberate, not arbitrary: `form-components.js` serializes every `[data-prop]` element
in a JSON-bound (widget/page) configuration sub-form, and `ContentZoneModel.DeserializePropertiesToConfigType`
deserializes with no tolerance for a malformed shape — a throw there silently drops the *entire* saved
configuration. A control that emits several inputs sharing one `name` (a radio group, like
`StarRating`) is therefore safe only on a **model-bound** form; `IconPicker` emits exactly one
`data-prop` element, so it works in both places. Both components render static HTML only (no
JavaScript, no CSP changes) — `IconPicker`'s preview reflects the value as of the last save/reload,
not a live update as the `<select>` changes.

`GET /_diag/extensibility`'s `form_components_seeded` check asserts both registry rows exist with the
expected `ViewComponentName`.

## Page Controller Tests

Three host-owned `[PageController]` page types exercise the editor-managed page pipeline —
page-type seeding → the admin page-type dropdown → `ConfigurationJson` round-trip → publish →
`CMSRouteTransformer` dispatch → `CMS:PageData`/`CMS:PageConfig` → per-page view override →
page-scoped content zones. They are discovered from the host assembly by the page-type seeder
(`CmsStartupHelpers.SeedAssemblies` includes `Assembly.GetEntryAssembly()`), so **no `Program.cs`
change and no DI registration** is needed.

| Page type | Views | What it covers |
|---|---|---|
| `PageTestHub` | `Index` | Index of the whole suite (a live specimen: `_PageDiagnostics` + a `HubNotice` zone) |
| `ZoneShowcasePage` | `Index`, `TwoColumn`, `Landing`, `Bare` | Multiple views over one page; page-scoped vs global zones; nested zones (drop a `Layout` widget into `PageMain`); view-switch persistence; zero-zone view |
| `ConfiguredPage` | `Index` | One config field per `EditorType` (16) plus a `HeadingIcon` field on the host `IconPicker` component (17 total), echoed back as `data-cfg-*` attributes to prove the `ConfigurationJson` → `CMS:PageConfig` round-trip |

`ZoneShowcasePage` slot names are prefixed `Page*` (`PageHeader`, `PageMain`, `PageAside`,
`PageFooter`) on purpose: the host `Layout` widget renders *nested* zones named
`Main`/`Sidebar`/`Header`/`Content`/`Footer`, and unprefixed page slots would produce duplicate
`data-zone-name` values in the DOM once a Layout widget is dropped in. `ShowcaseGlobal` is
`IsGlobal = true`.

### Discovery (no navbar link — the pages are created by the JSON content seeder)

These pages are seeded at startup (see Seeded fixtures), so they already exist at `/wadmin/pages`
(plus `faq-test` and a `nested` child page under `page-test`):

1. `page-test` → **Page Test Hub** (leave View Name empty) — the index of the suite.
2. `zones` → **Zone Showcase Page**, View Name **`TwoColumn`**.
3. `zones-landing` → **Zone Showcase Page**, View Name **`Landing`**.
4. `configured` → **Configured Page** (leave View Name empty).

Then the manual walkthrough at `http://localhost:45847`:

1. `GET /_diag/extensibility` — still 10/10; the new page types must not disturb the existing checks.
2. `/wadmin/pagetypes` — the three types are listed under category **Page Tests** with their
   `ConfigurationTypeName` populated.
3. `/wadmin/pages` → new page → **Zone Showcase Page** — the **View Name** dropdown offers
   `Bare`, `Index`, `Landing`, `TwoColumn`. Switch to **Configured Page** and the list collapses to
   that type's views and the stale selection clears. Save `zones` with `TwoColumn`, reopen for edit,
   and `TwoColumn` is still selected (the `FormViewPicker` retained-value fix).
4. **Preview before publish** — with `zones` saved as a draft only, use the admin Preview action
   (`/_preview/{nodeId}`): it renders `TwoColumn` with no route row in `CMSRoutes`.
5. **Publish and dispatch** — publish `zones` → `GET /zones` renders;
   `data-testid="active-view"` reads `TwoColumn`, `data-testid="route-pattern"` reads `/zones`.
6. **Zones** — logged in as admin, the four page zones (`PageHeader`, `PageMain`, `PageAside`,
   `PageFooter`) plus `ShowcaseGlobal` render their add-widget affordance. Add a FAQ List to
   `PageAside` and a `Layout` widget (`TwoColumnSidebar`) to `PageMain`; the nested `Main`/`Sidebar`
   zones appear inside the Layout widget and stay distinct from the page-scoped `data-zone-name`s.
7. **View switch preserves zone content** — change View Name to `Landing`, republish, reload: the
   `PageMain` widget is still there, `LandingHero` is newly created, and `PageAside` content is
   dormant — not deleted. Switch back to `TwoColumn` and `PageAside` returns.
8. **Zero-zone view** — switch to `Bare`; the page renders with no zone markup and no error.
9. **Config round-trip** — publish `configured`, fill every field in the admin config form, save,
   reload the edit form (values persist), then load `/configured` and confirm each `data-cfg-*`
   attribute matches. Leave a numeric field empty — the rest of the config must survive (numeric
   properties are declared `int?` for exactly this reason).
10. **Logged out** — hit `/zones`, `/zones-landing`, `/configured` anonymously: pages render, zone
    edit affordances are absent.

Assertion surfaces: `data-testid="showcase-view-{index|two-column|landing|bare}"` on each view root,
`data-testid="zone-{page-header|page-main|page-aside|page-footer|showcase-global|landing-hero|landing-cta|hub-notice}"`
on each zone wrapper, the `_PageDiagnostics` block (`active-view`, `page-controller`,
`page-viewname`, `page-slug`, `page-node-id`, `page-version`, `route-pattern`), and one
`data-cfg-{property}` attribute per `ConfiguredPage` config value (dates via `FormValueFormatter`).

> **Page-type seeding is insert-only.** Editing a `[PageController]` display attribute after the
> first run has no effect — the database row is authoritative. Change metadata at `/wadmin/pagetypes`,
> or reset the DB with `./scripts/TearDownIntegrationhost.sh -v` to re-seed.

### Gaps the suite is built to expose

1. **Rich text inside `ConfigurationJson` is not sanitized.** `RichTextSanitizer` runs at
   `AdminCrudModel.SaveUpsertAsync` over top-level `string` properties marked
   `[FormProperty(EditorType = RichText)]`; a page's rich-text config lives *inside* the
   `ConfigurationJson` hidden string, so it is stored raw. `ConfiguredPage/Index.cshtml` and
   `PageTestHub/Index.cshtml` render their rich-text config with `@Html.Raw` and flag it in a code
   comment (the same class of gap as the widget-config one recorded in
   `docs/architecture/04-content-zone-framework.md` §10.1).
2. **The View Name dropdown was dead data (now fixed).** `PageModel.GetProperties` already returned
   `availableViews` from the registry endpoint, but `page-upsert.js` only called the sibling `/form`
   endpoint, so the dropdown always offered just `Default`. It now populates from
   `GET /wadmin/pages/registry/{name}/properties`, and `FormViewPicker` retains an unknown stored
   value as an option so an edit load keeps the selection.