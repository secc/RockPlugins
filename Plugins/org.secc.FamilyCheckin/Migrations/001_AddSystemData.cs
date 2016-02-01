using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.FamilyCheckin.Migrations
{
    [MigrationNumber(1, "1.2.0")]
    public class AddSystemData : Migration
    {
        public const string LabelFamilyAttributeGuid = "660BFD6E-3DE2-4F6F-AB5F-D0AC6342B201";
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            //Create new attribute to flag if family label.
            Sql(string.Format(@"
                INSERT INTO [dbo].[Attribute] ( [IsSystem],[FieldTypeId],[EntityTypeId],[EntityTypeQualifierColumn],[EntityTypeQualifierValue]
                    ,[Key],[Name],[Description],[Order]
                    ,[IsGridColumn],[IsMultiValue],[IsRequired],[DefaultValue],[Guid] )
                VALUES (
                    0 -- isSystem
                    ,(SELECT [Id] FROM [FieldType] WHERE [Class] = 'Rock.Field.Types.BooleanFieldType') --FieldTypeID
                    ,(SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.BinaryFile') -- EntityTypeId
                    ,'BinaryFileTypeId' --EntityTypeQualifierColumn
                    ,(SELECT [Id] FROM [BinaryFileType] WHERE [Guid] = 'DE0E5C50-234B-474C-940C-C571F385E65F')--EntityTypeQualifierValue
                    ,'IsFamilyLabel'--Key
                    ,'Is Family Label'--Name
                    ,'Flag to show if label prints once per family.'--Description
                    ,0--Order
                    ,0--IsGridColumn
                    ,0--IsMultiValue
                    ,0--IsRequired
                    ,'False'--DefaultValue
                    ,'{0}' --Guid
                    )", LabelFamilyAttributeGuid));

            Sql(string.Format(@"
"
                )
                );
        }

        public override void Down()
        {
            Sql(string.Format(@"DELETE from [dbo].[Attribute] Where [Guid] = {0}",
                LabelFamilyAttributeGuid ));
        }
    }
}
