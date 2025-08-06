using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Model;
using Rock.Plugin;

namespace org.secc.Reporting.Migrations
{
    [MigrationNumber(4, "1.12.0")]
    public partial class DecisonFormReportTable : Migration
    {
        public override void Up()
        {
            var dropTable = @"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.Tables WHERE TABLE_NAME = '_org_secc_Reporting_DecisionForm')
                BEGIN
                    DROP TABLE dbo._org_secc_Reporting_DecisionForm
                END";

            base.Sql( dropTable );

            var createTable = @"
				CREATE TABLE dbo._org_secc_Reporting_DecisionForm
				(
					Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
					RecordType NVARCHAR(10) NOT NULL,
					RecordId INT NOT NULL,
					PersonAliasId INT NOT NULL,
					PersonId INT NOT NULL,
					LastName NVARCHAR(50) NULL,
					FirstName NVARCHAR(50) NULL,
					NickName NVARCHAR(50) NULL,
					Age INT NULL,
					IsMinor BIT DEFAULT(0) NOT NULL,
					Gender NVARCHAR(1) NULL,
					Email NVARCHAR(75) NULL,
					MobilePhone NVARCHAR(50) NULL,
					GraduationYear INT NULL,
					HomeLocationId INT NULL,
					ConnectionStatusValueId INT NOT NULL,
					ConnectionStatusValue NVARCHAR(50) NULL,
					BaptismDate DATETIME NULL,
					DecisionType NVARCHAR(MAX),
					FormDate DATETIME NULL,
					DecisionCampusId INT NULL,
					DecisionCampusName NVARCHAR(100) NULL,
					FamilyCampusId INT NULL,
					FamilyCampusName NVARCHAR(100) NULL,
					EventName NVARCHAR(250) NULL,
					BaptismTypeValueId INT NULL,
					BaptismTypeValue NVARCHAR(250) NULL,
					ParentGuardianName NVARCHAR(200) NULL,
					ParentEmail NVARCHAR(200) NULL,
					ParentPhone NVARCHAR(500) NULL,
					StatementOfFaithSignedDate DATETIME NULL,
					MembershipDate DATETIME NULL,
					MembershipClassDate DATETIME NULL,
					HomeStreet1 NVARCHAR(100) NULL,
					HomeStreet2 NVARCHAR(100) NULL,
					HomeCity NVARCHAR(50) NULL,
					HomeState NVARCHAR(50) NULL,
					HomePostalCode NVARCHAR(50) NULL, 
					HomeCountry NVARCHAR(50) NULL,
					CreatedDateTime DATETIME NULL,
					CreatedByPersonAliasId INT NULL,
					Guid UNIQUEIDENTIFIER NOT NULL,
					ForeignId INT NULL,
					ForeignGuid UNIQUEIDENTIFIER NULL,
					ForeignKey NVARCHAR(100),
					ModifiedDateTime DATETIME NULL,
					ModifiedByPersonAliasId INT NULL,
					Birthdate DATETIME NULL
				)";

			base.Sql( createTable );

			
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }

    }
}
