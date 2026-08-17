using Microsoft.AspNetCore.Http;

using WebWayCMS.Controllers.Admin.Handlers;
using WebWayCMS.Data.Services;
using WebWayCMS.Mapping;
using WebWayCMS.Models.Shared;

using WebWayCMS.TestHost.Data;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Host-owned <see cref="AdminCrudModel{TDto}"/> for FAQs. Registered as
/// <see cref="WebWayCMS.Controllers.Admin.Handlers.IAdminCrudHandler"/> so the generic admin
/// controller and MCP toolsets pick it up automatically.
/// </summary>
public sealed class FaqModel : AdminCrudModel<FaqDTO>
{
    private readonly IContentStore<FaqDTO> _store;
    private readonly IMapper _mapper;

    protected override IContentStore<FaqDTO> Store => _store;
    protected override string VersionHistoryContentType => "faqs";
    protected override string GetVersionHistoryBackUrl(string? parentKey = null) => "/wadmin/faqs";
    protected override Task<List<FaqDTO>> GetAllVersionsAsync(Guid nodeId, CancellationToken ct) => _store.GetAllVersionsAsync(nodeId, ct);
    protected override Task<bool> DeleteVersionCoreAsync(Guid id, CancellationToken ct) => _store.DeleteVersionAsync(id, ct);

    public override string ContentType => "faqs";
    public override string DisplayName => "FAQ";
    public override string IndexViewPath => "~/Views/AdminFaq/Faqs.cshtml";
    public override string UpsertViewPath => "~/Views/AdminFaq/FaqUpsert.cshtml";

    public FaqModel(IContentStore<FaqDTO> store, IMapper mapper, IChangeSetScope changeSetScope)
        : base(changeSetScope)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public override async Task<object> GetIndexViewModelAsync(CancellationToken ct = default)
    {
        var dtos = await _store.GetAllCurrentDraftsAsync(ct);
        return new FaqIndexViewModel
        {
            Faqs = dtos.Select(d => _mapper.Map<FaqItemViewModel>(d)).ToList()
        };
    }

    public override async Task<object?> GetUpsertViewModelAsync(Guid? id, IQueryCollection query, CancellationToken ct = default)
    {
        if (id == null || id == Guid.Empty)
            return new FaqUpsertViewModel();

        var dto = await _store.GetCurrentDraftAsync(id.Value, ct);
        return dto == null ? null : _mapper.Map<FaqUpsertViewModel>(dto);
    }

    public override object CreateEmptyUpsertViewModel() => new FaqUpsertViewModel();

    protected override async Task<AdminSaveResult> SaveUpsertCoreAsync(object model, CancellationToken ct = default)
    {
        var vm = (FaqUpsertViewModel)model;
        var dto = _mapper.Map<FaqDTO>(vm);
        var result = await _store.SaveDraftAsync(dto, vm.ExpectedVersionNumber, ct);
        return result.Success
            ? new AdminSaveResult(true, NodeId: result.NodeId)
            : new AdminSaveResult(false, result.ErrorMessage ?? "Save failed.");
    }

    public override async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => await _store.DeleteAsync(id, softDelete: false, ct);

    public override async Task<IEnumerable<object>> GetApiListAsync(CancellationToken ct = default)
    {
        var dtos = await _store.GetAllCurrentDraftsAsync(ct);
        return dtos.Select(d => (object)new { id = d.Version.Node.Id, title = d.Version.Title });
    }
}
