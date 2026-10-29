using Microsoft.AspNetCore.Http;

using WebWayCMS.Controllers.Admin.Handlers;
using WebWayCMS.Data.Services;
using WebWayCMS.Mapping;
using WebWayCMS.Models.Shared;
using WebWayCMS.Services.Relations;

using WebWayCMS.TestHost.Data;

namespace WebWayCMS.TestHost.Models.Faq;

/// <summary>
/// Host-owned <see cref="AdminCrudModel{TDto}"/> for FAQ topics. Nothing here knows about FAQs beyond the
/// index's per-topic count: the FAQ → topic link is one <c>[ContentRelation]</c> on
/// <see cref="FaqUpsertViewModel.FaqTopicId"/>, and the relation engine supplies the rest — the children
/// panel on this type's edit screen, the MCP child tools, containment visibility and the delete cascade.
/// </summary>
public sealed class FaqTopicModel : AdminCrudModel<FaqTopicDTO>
{
    private readonly IContentStore<FaqTopicDTO> _store;
    private readonly IContentStore<FaqDTO> _faqStore;
    private readonly IMapper _mapper;

    protected override IContentStore<FaqTopicDTO> Store => _store;
    protected override string VersionHistoryContentType => "faqtopics";
    protected override string GetVersionHistoryBackUrl(string? parentKey = null) => "/wadmin/faqtopics";
    protected override Task<List<FaqTopicDTO>> GetAllVersionsAsync(Guid nodeId, CancellationToken ct) => _store.GetAllVersionsAsync(nodeId, ct);
    protected override Task<bool> DeleteVersionCoreAsync(Guid id, CancellationToken ct) => _store.DeleteVersionAsync(id, ct);

    public override string ContentType => "faqtopics";
    public override string DisplayName => "FAQ Topic";
    public override string IndexViewPath => "~/Views/AdminFaq/FaqTopics.cshtml";
    public override string UpsertViewPath => "~/Views/AdminFaq/FaqTopicUpsert.cshtml";

    public FaqTopicModel(IContentStore<FaqTopicDTO> store, IContentStore<FaqDTO> faqStore, IMapper mapper,
        IChangeSetScope changeSetScope, IRelationBinder relationBinder, IContentDeleteService deletes)
        : base(changeSetScope, relationBinder, deletes)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _faqStore = faqStore ?? throw new ArgumentNullException(nameof(faqStore));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public override async Task<object> GetIndexViewModelAsync(CancellationToken ct = default)
    {
        var topics = await _store.GetAllCurrentDraftsAsync(ct);
        var counts = await _faqStore.CountCurrentDraftsByRelationAsync(FaqModel.TopicRelation, ct);
        return new FaqTopicIndexViewModel
        {
            Topics = topics.Select(t =>
            {
                var item = _mapper.Map<FaqTopicItemViewModel>(t);
                item.FaqCount = counts.GetValueOrDefault(t.Version.Node.Id);
                return item;
            }).ToList()
        };
    }

    public override async Task<object?> GetUpsertViewModelAsync(Guid? id, IQueryCollection query, CancellationToken ct = default)
    {
        if (id == null || id == Guid.Empty)
            return new FaqTopicUpsertViewModel();

        var dto = await _store.GetCurrentDraftAsync(id.Value, ct);
        return dto == null ? null : _mapper.Map<FaqTopicUpsertViewModel>(dto);
    }

    public override object CreateEmptyUpsertViewModel() => new FaqTopicUpsertViewModel();

    protected override async Task<AdminSaveResult> SaveUpsertCoreAsync(object model, CancellationToken ct = default)
    {
        var vm = (FaqTopicUpsertViewModel)model;
        var dto = _mapper.Map<FaqTopicDTO>(vm);
        var result = await _store.SaveDraftAsync(dto, vm.ExpectedVersionNumber, ct);
        return result.Success
            ? new AdminSaveResult(true, NodeId: result.NodeId)
            : new AdminSaveResult(false, result.ErrorMessage ?? "Save failed.");
    }

    /// <summary>The topic alone; its FAQs are deleted first by the containment cascade.</summary>
    protected override async Task<bool> DeleteSelfAsync(Guid id, CancellationToken ct = default)
        => await _store.DeleteAsync(id, softDelete: false, ct);

    public override async Task<IEnumerable<object>> GetApiListAsync(CancellationToken ct = default)
    {
        var dtos = await _store.GetAllCurrentDraftsAsync(ct);
        return dtos.Select(d => (object)new { id = d.Version.Node.Id, title = d.Version.Title });
    }
}
