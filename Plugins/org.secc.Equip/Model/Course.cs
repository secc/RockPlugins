using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="Rock.Data.Entity{org.secc.Equip.Model.Course}" />
    [Table( "_org_secc_Equip_Course" )]
    [DataContract]
    public class Course : Model<Course>, IRockEntity, ICategorized, IHasActiveFlag, ISecured
    {
        /// <summary>Converts to string.</summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [DataMember]
        public string Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Slug { get; set; }

        [DataMember]
        public int? ImageId { get; set; }

        /// <summary>Gets or sets the category identifier.</summary>
        /// <value>The category identifier.</value>
        [DataMember]
        public int? CategoryId { get; set; }

        [DataMember]
        [MaxLength( 100 )]
        public string IconCssClass { get; set; }

        [DataMember]
        public bool AllowDocumentationMode { get; set; }

        [DataMember]
        public bool IsActive { get; set; }

        [DataMember]
        public int? AllowedGroupId { get; set; }

        [LavaInclude]
        public virtual Group AllowedGroup { get; set; }

        [DataMember]
        public int? AllowedDataViewId { get; set; }

        [LavaInclude]
        public virtual DataView AllowedDataView { get; set; }

        [LavaInclude]
        public virtual BinaryFile Image { get; set; }

        /// <summary>Gets or sets the category.</summary>
        /// <value>The category.</value>
        [LavaInclude]
        public virtual Category Category { get; set; }

        [DataMember]
        public string ExternalCourseUrl { get; set; }

        /// <summary>Gets or sets the course chapters.</summary>
        /// <value>The course chapters.</value>
        [LavaInclude]
        public virtual ICollection<Chapter> Chapters { get; set; }

        [NotMapped]
        public override ISecured ParentAuthority
        {
            get
            {
                if ( this.CategoryId.HasValue )
                {
                    return this.Category;
                }
                else
                {
                    return new GlobalDefault();
                }
            }
        }

        public bool PersonCanView( Person person )
        {
            if ( this.AllowedGroup != null )
            {
                if ( AllowedGroup.Members.Any( gm => gm.PersonId == person.Id ) )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if ( this.AllowedDataView != null )
            {
                var errorMessages = new List<string>();
                var qry = AllowedDataView.GetQuery( null, new RockContext(), 300, out errorMessages );
                if ( qry.Any( e => e.Id == person.Id ) )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

    }

    /// <summary></summary>
    /// <seealso cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{org.secc.Equip.Model.Course}" />
    public partial class CourseConfiguration : EntityTypeConfiguration<Course>
    {
        /// <summary>Initializes a new instance of the <see cref="CourseConfiguration"/> class.</summary>
        public CourseConfiguration()
        {
            this.HasEntitySetName( "Course" );
        }
    }
}