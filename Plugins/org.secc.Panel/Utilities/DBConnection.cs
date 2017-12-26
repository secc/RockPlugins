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
