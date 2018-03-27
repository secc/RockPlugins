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
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace org.secc.Panel.Utilities
{
    class DBConnection
    {

        private string databaseName = string.Empty;
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string Password { get; set; }
        private MySqlConnection connection = null;
        public MySqlConnection Connection
        {
            get { return connection; }
        }

        private static DBConnection _instance = null;
        public static DBConnection Instance()
        {
            if ( _instance == null )
                _instance = new DBConnection();
            return _instance;
        }

        public bool IsConnect()
        {
            bool result = true;
            if ( Connection == null )
            {
                if ( String.IsNullOrEmpty( databaseName ) )
                {
                    result = false;
                }
                string connstring = string.Format( ConfigurationManager.ConnectionStrings["PanelContext"].ConnectionString, databaseName );
                connection = new MySqlConnection( connstring );
                connection.Open();
                result = true;
            }

            return result;
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
