using org.secc.WebsitePageCleanup.App.Data;

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


    public void LoadInteractionCount(int channelId, DateTime startDate, DateTime endDate, string connectionString)
    {
        var pageData = new PageData( connectionString );
        var filter = new InteractionFilter
        {
            ChannelId = channelId,
            PageId = PageId,
            StartDate = startDate,
            EndDate = endDate
        };

        InteractionCount = pageData.GetInteractionCount( filter );
    }

    public void SaveLogItem( string connectionString)
    {
        var pageData = new PageData( connectionString );
        Id = pageData.InsertWebpageLogItem( this );


    }

    public static int? GetInteractionChannel(int siteId, string connectionString)
    {
        var pageData = new PageData( connectionString );
        return pageData.GetInteractionChannelIdBySiteId( siteId );
    }

    public static List<PageCleanupItem> GetSitePages(int siteId, string connectionString)
    {
        var data = new PageData( connectionString );
        return data.GetSitePages( siteId );
    }
}


public enum PageStatus
{
    NOT_PROCESSED,
    SAVED,
    SAVED_HAS_CHILDREN,
    REMOVED
}