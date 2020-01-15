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
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Rock;

namespace org.secc.Attributes.Helpers
{
    public class KeyValueMatrix : List<KeyValueList>
    {

        public KeyValueMatrix()
        {
        }

        public KeyValueMatrix( IEnumerable<KeyValueList> keyValueLists )
        {
            foreach ( var row in keyValueLists )
            {
                this.Add( row );
            }
        }

        public KeyValueMatrix( string configurationString )
        {
            try
            {
                this.AddRange( JsonConvert.DeserializeObject<KeyValueMatrix>( configurationString ) );
            }
            catch
            {
                BuildFromConfiguration( configurationString );
            }
        }

        public string FormatValue( string controlValue )
        {
            var values = controlValue.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            var matrix = this;

            var value = "";

            for ( var i = 0; i < values.Count; i++ )
            {
                matrix = new KeyValueMatrix( matrix.Where( r => r.Count > i && r[i].Key == values[i] ) );

                var kv = matrix.Where( r => r[i].Key == values[i] ).FirstOrDefault();
                if ( kv != null)
                {
                    value = kv[i].Value;
                }
                else
                {
                    value = "";
                }

            }

            return value;
        }

        private void BuildFromConfiguration( string configurationString )
        {
            if ( configurationString.IsNullOrWhiteSpace() )
            {
                return;
            }

            if ( configurationString.ToUpper().Contains( "SELECT" ) && configurationString.ToUpper().Contains( "FROM" ) )
            {
                DataTable dataTable = Rock.Data.DbService.GetDataTable( configurationString, CommandType.Text, null );
                foreach ( DataRow row in dataTable.Rows )
                {
                    var matrixRow = new KeyValueList();
                    foreach ( var itemObj in row.ItemArray )
                    {
                        var item = itemObj as string;
                        var kv = item.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                        if ( kv.Length > 1 )
                        {
                            matrixRow.Add( kv[0], kv[1] );
                        }
                        else
                        {
                            matrixRow.Add( item, item );
                        }
                    }
                    if ( matrixRow.Count > 0 )
                    {
                        Add( matrixRow );
                    }
                }
            }
            else
            {
                var rows = configurationString.Split( new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries );
                foreach ( var row in rows )
                {
                    var matrixRow = new KeyValueList();
                    var itemList = row.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
                    {
                        foreach ( var item in itemList )
                        {
                            if ( item.IsNullOrWhiteSpace() )
                            {
                                continue;
                            }
                            var kv = item.Split( new char[] { '^' }, StringSplitOptions.RemoveEmptyEntries );
                            if ( kv.Length > 1 )
                            {
                                matrixRow.Add( kv[0], kv[1] );
                            }
                            else
                            {
                                matrixRow.Add( item, item );
                            }
                        }
                    }
                    if ( matrixRow.Count > 0 )
                    {
                        Add( matrixRow );
                    }
                }
            }
        }
    }
}
