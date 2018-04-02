// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Rock.Data;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.Finance
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Bank Account Encoder" )]
    [Category( "SECC > Finance" )]
    [Description( "Block for encoding bank routing and account number using SHA1 with HMAC." )]

    public partial class BankAccountEncoder : Rock.Web.UI.RockBlock
    {
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                string connString = System.Configuration.ConfigurationManager.ConnectionStrings["TellerContext"].ConnectionString;
                string sqlStmt = @"
                SELECT TOP 100000 * FROM
                (
                SELECT DISTINCT CheckAcctNo, RoutingNo
                FROM General_Fund.dbo.Docs
                WHERE CheckAcctNo <> '' AND RoutingNo <> ''
                UNION
                SELECT DISTINCT CheckAcctNo, RoutingNo
                FROM Building_Fund.dbo.Docs
                WHERE CheckAcctNo <> '' AND RoutingNo <> ''
                ) A
                ";

                DataTable dtTellerAcctRtng = new DataTable();
                using ( SqlConnection conn = new SqlConnection( connString ) )
                {
                    using ( SqlCommand comm = new SqlCommand( sqlStmt, conn ) )
                    {
                        conn.Open();
                        using ( SqlDataReader dr = comm.ExecuteReader() )
                        {
                            dtTellerAcctRtng.Load( dr );
                        }
                    }
                }
                dtTellerAcctRtng.Columns.Add( "Hashed", typeof( String ) );

                foreach (DataRow dr in dtTellerAcctRtng.Rows)
                {
                    dr["Hashed"] = EncodeAccountNumber( dr["RoutingNo"].ToString(), dr["CheckAcctNo"].ToString() );
                }

                var qry = dtTellerAcctRtng.AsEnumerable()
                    .Select( i => new {
                        RoutingNo = i.Field<string>( "RoutingNo" ),
                        CheckAcctNo = i.Field<string>( "CheckAcctNo" ),
                        Hashed = i.Field<string>( "Hashed" ) } );

                RockContext rockContext = new RockContext();
                var fpbaService = new FinancialPersonBankAccountService( rockContext );
                var lstRockAcctRtng = fpbaService.Queryable()
                    .Select( a => new { a.AccountNumberSecured, a.PersonAliasId } )
                    .ToList();

                var matchingRows = lstRockAcctRtng
                    .Join( qry, x => x.AccountNumberSecured, y => y.Hashed, ( x, y ) => new { y.RoutingNo, y.CheckAcctNo, x.AccountNumberSecured, x.PersonAliasId } )
                    .GroupBy( g => new { g.RoutingNo, g.CheckAcctNo, g.AccountNumberSecured } )
                    .Select( s => new { s.Key.RoutingNo, s.Key.CheckAcctNo, s.Key.AccountNumberSecured, PersonAliasIds = string.Join( "|", s.Select( i => i.PersonAliasId ) ) } );

                try {
                    string path = "D:\\BankAcctRtng.csv";
                    File.WriteAllText( path, String.Format( "{0},{1},{2}", "RoutingNo", "CheckAcctNo", "PersonAliasIds\r\n" ) );
                    File.AppendAllLines( path, matchingRows.Select( x => String.Format( "{0},{1},{2}", x.RoutingNo, x.CheckAcctNo, x.PersonAliasIds ) ) );
                    lblExportStatus.Text = "File exported at " + path;
                }
                catch {
                    lblExportStatus.Text = "File was not exported";
                }
            }
        }
        public static string EncodeAccountNumber( string routingNumber, string accountNumber )
        {
            string toHash = string.Format( "{0}|{1}", routingNumber.Trim(), accountNumber.Trim() );
            return Rock.Security.Encryption.GetSHA1Hash( toHash );
        }

    }

}


