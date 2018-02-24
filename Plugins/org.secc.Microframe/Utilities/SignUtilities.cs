// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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
using System.Linq;
using org.secc.Microframe.Model;
using Rock.Data;

namespace org.secc.Microframe.Utilities
{
    public class SignUtilities
    {
        public static void UpdateSignCategorySigns( int signCategoryId )
        {
            using ( RockContext rockContext = new RockContext() )
            {
                SignCategory signCategory = new SignCategoryService( rockContext ).Get( signCategoryId );
                if ( signCategory != null )
                {
                    var signs = new SignService( rockContext )
                        .Queryable()
                        .Where( s => s.SignCategories.Select( sc => sc.Id ).Contains( signCategoryId ) )
                        .ToList();
                    foreach ( var sign in signs )
                    {
                        List<string> codes = new List<string>();
                        var categoriesCodes = sign.SignCategories
                            .Select( sc => sc.Codes )
                            .ToList();
                        foreach ( var code in categoriesCodes )
                        {
                            codes.AddRange( ( code ?? "" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                                .ToList()
                                );
                        }
                        MicroframeConnection signConnection = new MicroframeConnection( sign.IPAddress, sign.PIN );
                        signConnection.UpdateMessages( codes );
                    }
                }
            }
        }

        public static void UpdateAllSigns()
        {
            using ( RockContext rockContext = new RockContext() )
            {
                var signs = new SignService( rockContext )
                            .Queryable()
                            .Select( s => s );
                foreach ( var sign in signs )
                {
                    List<string> codes = new List<string>();
                    var categoriesCodes = sign.SignCategories
                        .Select( sc => sc.Codes )
                        .ToList();
                    foreach ( var code in categoriesCodes )
                    {
                        codes.AddRange(
                            ( code ?? "" )
                            .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                            .ToList()
                            );
                    }
                    MicroframeConnection signConnection = new MicroframeConnection( sign.IPAddress, sign.PIN );
                    signConnection.UpdateMessages( codes );
                }
            }
        }
    }
}
