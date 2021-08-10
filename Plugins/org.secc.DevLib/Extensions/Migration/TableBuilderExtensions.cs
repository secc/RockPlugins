using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Builders;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using Rock;

namespace org.secc.DevLib.Extensions.Migration
{
    public static class TableBuilderExtensions
    {
        public static void Run<TColumns>( this TableBuilder<TColumns> tableBuilder, Rock.Plugin.Migration migration )
        {
            FieldInfo fiMigration = tableBuilder.GetType().GetField( "_migration", BindingFlags.NonPublic | BindingFlags.Instance );
            DbMigration dbMigration = fiMigration.GetValue( tableBuilder ) as DbMigration;

            migration.Sql( GetMigrationSql( dbMigration, migration.SqlConnection ) );
        }

        internal static string GetMigrationSql( DbMigration dbMigration, SqlConnection sqlConnection )
        {
            StringBuilder sql = new StringBuilder();
            var prop = dbMigration.GetType().GetProperty( "Operations", BindingFlags.NonPublic | BindingFlags.Instance );
            if ( prop != null )
            {
                IEnumerable<MigrationOperation> operations = prop.GetValue( dbMigration ) as IEnumerable<MigrationOperation>;
                foreach ( var operation in operations )
                {
                    if ( operation is AddForeignKeyOperation && ( ( AddForeignKeyOperation ) operation ).PrincipalColumns.Count == 0 )
                    {
                        // In Rock, the principal column should always be the Id.  This isn't always the case . . . .
                        ( ( AddForeignKeyOperation ) operation ).PrincipalColumns.Add( "Id" );
                    }
                }
                var generator = new SqlServerMigrationSqlGenerator();
                var statements = generator.Generate( operations, sqlConnection.ServerVersion.AsInteger() > 10 ? "2008" : "2005" );
                foreach ( MigrationStatement item in statements )
                {
                    if ( item.Sql.StartsWith( "CREATE TABLE" ) )
                    {
                        //So the way this works is ROCK makes our table for us.
                        //But table builder just tacks on the Primary Key constraint
                        //So remove most of the table text and change it to an alter table instead
                        var start = item.Sql.IndexOf( "CREATE TABLE" ) + 12;
                        var end = item.Sql.IndexOf( "(" );
                        var tableName = item.Sql.Substring( start, end - start );
                        var constraint = item.Sql.Substring( item.Sql.IndexOf( "CONSTRAINT" ) );
                        constraint = constraint.Substring( 0, constraint.Length - 1 );
                        sql.Append( string.Format( $"ALTER TABLE {tableName} ADD {constraint};" ) );
                    }
                    else
                    {
                        sql.Append( item.Sql + ";" );
                    }
                }
            }
            return sql.ToString();
        }



    }
}
