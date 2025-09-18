using Rock.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace org.secc.OAuth.Model
{
    internal class AuthTokenService
    {
        internal static void AddToken(AuthToken t)
        {
            using (var context = new RockContext())
            {
                var sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@Token", t.Token));
                sqlParams.Add(new SqlParameter("@Ticket", t.Ticket));

                context.Database.ExecuteSqlCommand("[dbo].[_org_secc_OAuth_spAddToken] @Token, @Ticket",
                    sqlParams.ToArray());

            }
        }

        internal static void DeleteToken(string token)
        {
            using (var context = new RockContext())
            {
                context.Database.ExecuteSqlCommand("[dbo].[_org_secc_OAuth_spDeleteToken] @Token",
                    new SqlParameter("@Token", token));
            }
        }

        internal static string GetTicket(string token)
        {
            using (var context = new RockContext())
            {
                return context.Database.SqlQuery<string>("[dbo].[_org_secc_OAuth_spGetTicket] @Token",
                    new SqlParameter("@Token", token)).FirstOrDefault();
            }
        }

    }
}
