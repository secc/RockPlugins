using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace org.secc.Finance.Rest.Controllers
{
    public partial class FinancialStatementController : ApiControllerBase
    {

        private const string CONTRIBUTION_STATEMENT_FILETYPE_GUID = "01630dc6-66dd-4e26-b51e-6f70216847ae";

        [Authenticate, Secured]
        [HttpDelete]
        [System.Web.Http.Route( "api/FinancialStatement/{binaryFileId}" )]
        public HttpResponseMessage DeleteStatement( int binaryFileId )
        {
            try
            {
                var fileType = BinaryFileTypeCache.Get( CONTRIBUTION_STATEMENT_FILETYPE_GUID.AsGuid() );
                var rockContext = new RockContext();

                var binaryFileService = new BinaryFileService( rockContext );

                var file = binaryFileService.Queryable().AsNoTracking()
                    .Where( f => f.BinaryFileTypeId == fileType.Id )
                    .Where( f => f.Id == binaryFileId )
                    .FirstOrDefault();

                if ( file == null )
                {
                    return new HttpResponseMessage( HttpStatusCode.NotFound )
                    {
                        Content = new StringContent( "Contribution Statement Not Found" )
                    };

                }

                var sqlParameter = new SqlParameter( "@BinaryFileId", binaryFileId );
                var sql = "DELETE FROM dbo.[Document] WHERE BinaryFileId = @BinaryFileId";

                rockContext.Database.ExecuteSqlCommand( sql, sqlParameter );

                var binaryFile = binaryFileService.Get( binaryFileId );
                binaryFileService.Delete( binaryFile );

                rockContext.SaveChanges();

                return new HttpResponseMessage( HttpStatusCode.NoContent );


            }
            catch ( HttpResponseException exception )
            {
                return exception.Response;
            }
            catch
            {
                return new HttpResponseMessage( HttpStatusCode.InternalServerError )
                {
                    Content = new StringContent( "Unhandled Exception" )
                };
            }
        }

    }

}
