using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Model;

namespace org.secc.Rise.Model
{
    [Table( "_org_secc_Rise_Course" )]
    [DataContract]
    public class Course : Model<Course>
    {
        [DataMember]
        [MaxLength(200)]
        [Index]
        public string Url { get; set; }

        [DataMember]
        [MaxLength( 200 )]
        [Index]
        public string CourseId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [LavaInclude]
        public virtual ICollection<Category> Categories
        {
            get { return _categories ?? ( _categories = new Collection<Category>() ); }
            set { _categories = value; }
        }
        private ICollection<Category> _categories;

        [LavaInclude]
        public virtual ICollection<Group> EnrolledGroups
        {
            get { return _enrolledGroups ?? ( _enrolledGroups = new Collection<Group>() ); }
            set { _enrolledGroups = value; }
        }
        private ICollection<Group> _enrolledGroups;
    }

    public partial class RiseCourseConfiguration : EntityTypeConfiguration<Course>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public RiseCourseConfiguration()
        {
            this.HasMany( kt => kt.Categories ).WithMany().Map( kt => { kt.MapLeftKey( "CourseId" ); kt.MapRightKey( "CategoryId" ); kt.ToTable( "_org_secc_Rise_CourseCategory" ); } );
            this.HasMany( kt => kt.EnrolledGroups ).WithMany().Map( kt => { kt.MapLeftKey( "CourseId" ); kt.MapRightKey( "GroupId" ); kt.ToTable( "_org_secc_Rise_CourseEnrolledGroup" ); } );
            this.HasEntitySetName( "Courses" );
        }
    }
}
