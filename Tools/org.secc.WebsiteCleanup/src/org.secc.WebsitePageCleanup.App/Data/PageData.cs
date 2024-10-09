using Microsoft.Data.SqlClient;
using org.secc.WebsitePageCleanup.App.Model;

namespace org.secc.WebsitePageCleanup.App.Data
{
    public class PageData
    {
        private readonly string connectionString;

        public PageData( string connectionString )
        {
            this.connectionString = connectionString;
        }


        public int? GetInteractionChannelIdBySiteId( int siteId )
        {
            string SP_NAME = "[dbo].[_org_secc_WebPageCleanup_InteractionChannelBySiteId]";
            int? channelId = null;
            using (var sqlConn = new SqlConnection( connectionString ))
            {
                var sqlCmd = new SqlCommand( SP_NAME, sqlConn );
                sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCmd.Parameters.Add( new SqlParameter( "@SiteId", siteId ) );
                sqlConn.Open();
                channelId = sqlCmd.ExecuteScalar() as int?;
                sqlConn.Close();
            }

            return channelId;
        }

        public List<PageCleanupItem> GetSitePages( int siteId )
        {
            string SP_NAME = "[dbo].[_org_secc_WebPageCleanup_Get_SitePages]";
            List<Model.PageCleanupItem> sitePages = new List<Model.PageCleanupItem>();

            using (var sqlConn = new SqlConnection( connectionString ))
            {
                var sqlCmd = new SqlCommand( SP_NAME, sqlConn );
                sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCmd.Parameters.Add( new SqlParameter( "@SiteId", siteId ) );
                sqlConn.Open();

                var dr = sqlCmd.ExecuteReader();

                while (dr.Read())
                {
                    var page = new Model.PageCleanupItem();
                    page.PageId = (int) dr["PageId"];

                    if (!dr.IsDBNull( dr.GetOrdinal( "ParentPageId" ) ))
                    {
                        page.ParentPageId = (int) dr["ParentPageId"];
                    }

                    page.LayoutId = (int) dr["LayoutId"];

                    if (!dr.IsDBNull( dr.GetOrdinal( "InternalName" ) ))
                    {
                        page.InternalName = dr["InternalName"].ToString()!;
                    }
                    sitePages.Add( page );
                }

                dr.Close();
                sqlConn.Close();
            }
            return sitePages;
        }

        public int InsertWebpageLogItem( PageCleanupItem p )
        {
            string SP_NAME = "[dbo].[_org_secc_WebPageCleanupLog_Insert]";
            int id = 0;

            using (var sqlConn = new SqlConnection( connectionString ))
            {
                var sqlCmd = new SqlCommand( SP_NAME, sqlConn );
                sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;
                var idParam = new SqlParameter( "@Id", 0 );
                idParam.DbType = System.Data.DbType.Int32;
                idParam.Direction = System.Data.ParameterDirection.Output;

                sqlCmd.Parameters.Add( idParam );
                sqlCmd.Parameters.Add( new SqlParameter( "@PageId", p.PageId ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@InternalName", p.InternalName ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@LayoutId", p.LayoutId ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@PageStatus", p.PageStatus ) );


                if (p.ParentPageId.HasValue)
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@ParentPageId", p.ParentPageId.Value ) );
                }
                else
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@ParentPageId", DBNull.Value ) );
                }

                if (p.InteractionCount.HasValue)
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@InteractionCount", p.InteractionCount ) );
                }
                else
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@InteractionCount", DBNull.Value ) );
                }

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
                sqlConn.Close();
                id = (int) idParam.Value;

            }
            return id;
        }

        public void TruncateWebpageLog()
        {
            string sql = @"TRUNCATE TABLE [dbo].[_org_secc_WebPageCleanupLog]";

            using (var sqlConn = new SqlConnection( connectionString ))
            {
                var sqlCmd = new SqlCommand( sql );
                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
                sqlConn.Close();
            }
        }

        public void UpdateWebpageLogItem( PageCleanupItem p )
        {
            string SP_NAME = "[dbo].[_org_secc_WebPageCleanup_LogUpdate]";

            using (var sqlConn = new SqlConnection( connectionString ))
            {
                var sqlCmd = new SqlCommand( SP_NAME, sqlConn );
                sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;

                sqlCmd.Parameters.Add( new SqlParameter( "@Id", p.Id ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@PageId", p.PageId ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@IntenalName", p.InternalName ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@LayoutId", p.LayoutId ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@PageStatus", p.PageStatus ) );


                if (p.ParentPageId.HasValue)
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@ParentPageId", p.ParentPageId.Value ) );
                }
                else
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@ParentPageID", DBNull.Value ) );
                }

                if (p.InteractionCount.HasValue)
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@InteractionCount", p.InteractionCount.Value ) );
                }
                else
                {
                    sqlCmd.Parameters.Add( new SqlParameter( "@InteractionCount", DBNull.Value ) );
                }

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
                sqlConn.Close();
            }
        }

        public int GetInteractionCount(InteractionFilter filter)
        {
            string SP_NAME = "[dbo].[_org_secc_WebPageCleanup_GetPageInteractionCount]";
            int interactionCount = 0;
            using (var sqlConn = new SqlConnection( connectionString ))
            {
                var sqlCmd = new SqlCommand( SP_NAME, sqlConn );
                sqlCmd.CommandType = System.Data.CommandType.StoredProcedure;

                sqlCmd.Parameters.Add( new SqlParameter( "@ChannelId", filter.ChannelId ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@PageId", filter.PageId ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@StartDate", filter.StartDate ) );
                sqlCmd.Parameters.Add( new SqlParameter( "@EndDate", filter.EndDate ) );

                sqlConn.Open();
                interactionCount = (int) sqlCmd.ExecuteScalar();
                sqlConn.Close();
            }

            return interactionCount;
        }


    }
}
