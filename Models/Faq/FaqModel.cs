using Microsoft.AspNetCore.Http;

using WebWayCMS.Controllers.Admin.Handlers;
using WebWayCMS.Data.Models;
using WebWayCMS.Data.Services;
using WebWayCMS.Mapping;
using WebWayCMS.Models.Shared;
using WebWayCMS.Services.Relations;

using WebWayCMS.TestHost.Data;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Host-owned <see cref="AdminCrudModel{TDto}"/> for FAQs. Registered as
/// <see cref="WebWayCMS.Controllers.Admin.Handlers.IAdminCrudHandler"/> so the generic admin
/// controller and MCP toolsets pick it up automatically.
/// </summary>
public sealed class FaqModel : AdminCrudModel<FaqDTO>
{
    /// <summary>The FAQ → topic containment: the name of the declaring property.</summary>
    public const string TopicRelation = nameof(FaqUpsertViewModel.FaqTopicId);

    private readonly IContentStore<FaqDTO> _store;
    private readonly IContentStore<FaqTopicDTO> _topicStore;
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

    public FaqModel(IContentStore<FaqDTO> store, IContentStore<FaqTopicDTO> topicStore, IMapper mapper,
        IChangeSetScope changeSetScope, IRelationBinder relationBinder, IContentDeleteService deletes)
        : base(changeSetScope, relationBinder, deletes)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _topicStore = topicStore ?? throw new ArgumentNullException(nameof(topicStore));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public override async Task<object> GetIndexViewModelAsync(CancellationToken ct = default)
    {
        var dtos = await _store.GetAllCurrentDraftsAsync(ct);
        var topics = (await _topicStore.GetAllCurrentDraftsAsync(ct))
            .ToDictionary(t => t.Version.Node.Id, t => t.Version.Title ?? string.Empty);
        return new FaqIndexViewModel
        {
            Faqs = dtos.Select(d =>
            {
                var item = _mapper.Map<FaqItemViewModel>(d);
                var topicId = d.Version.TargetOf(TopicRelation);
                item.TopicTitle = topicId is { } id ? topics.GetValueOrDefault(id, string.Empty) : string.Empty;
                return item;
            }).ToList()
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

    protected override async Task<bool> DeleteSelfAsync(Guid id, CancellationToken ct = default)
        => await _store.DeleteAsync(id, softDelete: false, ct);

    public override async Task<IEnumerable<object>> GetApiListAsync(CancellationToken ct = default)
    {
        var dtos = await _store.GetAllCurrentDraftsAsync(ct);
        return dtos.Select(d => (object)new { id = d.Version.Node.Id, title = d.Version.Title });
    }
}
