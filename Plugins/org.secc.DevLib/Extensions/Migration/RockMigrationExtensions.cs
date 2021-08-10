using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using Rock;

namespace org.secc.DevLib.Extensions.Migration
{
    public static class RockMigrationExtensions
    {
        public static void AddPrimaryKey( this Rock.Plugin.Migration migration, string table, string column, string name = null, bool clustered = true, object anonymousArguments = null )
        {
            var mini = new MiniMigration();
            mini.AddPrimaryKey( table, column, name, clustered, anonymousArguments );
            migration.Sql( mini.GetMigrationSql( migration.SqlConnection ) );
        }

        public static void AddPrimaryKey( this Rock.Plugin.Migration migration, string table, string[] columns, string name = null, bool clustered = true, object anonymousArguments = null )
        {
            var mini = new MiniMigration();
            mini.AddPrimaryKey( table, columns, name, clustered, anonymousArguments );
            migration.Sql( mini.GetMigrationSql( migration.SqlConnection ) );
        }

        public static void AddForeignKey( this Rock.Plugin.Migration migration, string dependentTable, string dependentColumn, string principalTable, string principalColumn = null, bool cascadeDelete = false, string name = null, object anonymousArguments = null )
        {
            var mini = new MiniMigration();
            mini.AddForeignKey( dependentTable, dependentColumn, principalTable, principalColumn, cascadeDelete, name, anonymousArguments );
            migration.Sql( mini.GetMigrationSql( migration.SqlConnection ) );
        }

        public static void AddForeignKey( this Rock.Plugin.Migration migration, string dependentTable, string[] dependentColumns, string principalTable, string[] principalColumns = null, bool cascadeDelete = false, string name = null, object anonymousArguments = null )
        {
            var mini = new MiniMigration();
            mini.AddForeignKey( dependentTable, dependentColumns, principalTable, principalColumns, cascadeDelete, name, anonymousArguments );
            migration.Sql( mini.GetMigrationSql( migration.SqlConnection ) );
        }

        public static void CreateIndex( this Rock.Plugin.Migration migration, string table, string column, bool unique = false, string name = null, bool clustered = false, object anonymousArguments = null )
        {
            var mini = new MiniMigration();
            mini.CreateIndex( table, column, unique, name, clustered, anonymousArguments );
            migration.Sql( mini.GetMigrationSql( migration.SqlConnection ) );
        }

        public static void CreateIndex( this Rock.Plugin.Migration migration, string table, string[] columns, bool unique = false, string name = null, bool clustered = false, object anonymousArguments = null )
        {
            var mini = new MiniMigration();
            mini.CreateIndex( table, columns, unique, name, clustered, anonymousArguments );
            migration.Sql( mini.GetMigrationSql( migration.SqlConnection ) );
        }
    }

    class MiniMigration : System.Data.Entity.Migrations.DbMigration
    {
        internal new void AddPrimaryKey( string table, string column, string name = null, bool clustered = true, object anonymousArguments = null )
        {
            base.AddPrimaryKey( table, column, name, clustered, anonymousArguments );
        }

        internal new void AddPrimaryKey( string table, string[] columns, string name = null, bool clustered = true, object anonymousArguments = null )
        {
            base.AddPrimaryKey( table, columns, name, clustered, anonymousArguments );
        }

        internal new void AddForeignKey( string dependentTable, string dependentColumn, string principalTable, string principalColumn = null, bool cascadeDelete = false, string name = null, object anonymousArguments = null )
        {
            base.AddForeignKey( dependentTable, dependentColumn, principalTable, principalColumn, cascadeDelete, name, anonymousArguments );
        }

        internal new void AddForeignKey( string dependentTable, string[] dependentColumns, string principalTable, string[] principalColumns = null, bool cascadeDelete = false, string name = null, object anonymousArguments = null )
        {
            base.AddForeignKey( dependentTable, dependentColumns, principalTable, principalColumns, cascadeDelete, name, anonymousArguments );
        }

        internal new void CreateIndex( string table, string column, bool unique = false, string name = null, bool clustered = false, object anonymousArguments = null )
        {
            base.CreateIndex( table, column, unique, name, clustered, anonymousArguments );
        }

        internal new void CreateIndex( string table, string[] columns, bool unique = false, string name = null, bool clustered = false, object anonymousArguments = null )
        {
            base.CreateIndex( table, columns, unique, name, clustered, anonymousArguments );
        }


        public override void Up()
        {
            throw new NotImplementedException();
        }

        internal string GetMigrationSql( SqlConnection sqlConnection )
        {
            StringBuilder sql = new StringBuilder();
            var prop = this.GetType().GetProperty( "Operations", BindingFlags.NonPublic | BindingFlags.Instance );
            if ( prop != null )
            {
                IEnumerable<MigrationOperation> operations = prop.GetValue( this ) as IEnumerable<MigrationOperation>;
                foreach ( var operation in operations )
                {
                    if ( operation is AddForeignKeyOperation && ( ( AddForeignKeyOperation ) operation ).PrincipalColumns.Count == 0 )
                    {
                        ( ( AddForeignKeyOperation ) operation ).PrincipalColumns.Add( "Id" );
                    }
                }
                var generator = new SqlServerMigrationSqlGenerator();
                var statements = generator.Generate( operations, sqlConnection.ServerVersion.AsInteger() > 10 ? "2008" : "2005" );
                foreach ( MigrationStatement item in statements )
                {
                    sql.Append( item.Sql );
                }
            }
            return sql.ToString();
        }
    }
}
