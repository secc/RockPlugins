

namespace org.secc.WebsitePageCleanup.App.Model;

public class PageCleanupItem
{
    public int Id { get; set; }
    public int PageId { get; set; }
    public int LayoutId { get; set; }
    public string InternalName { get; set; } = string.Empty;
    public int? ParentPageId { get; set; } = null;
    public Guid Guid { get; set; }
    public int? InteractionCount { get; set; }
    public PageStatus PageStatus { get; set; } = PageStatus.NOT_PROCESSED;
}


public enum PageStatus
{
    NOT_PROCESSED,
    SAVED,
    SAVED_HAS_CHILDREN,
    REMOVED
}