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
    /// <summary>Locally stored description of courses in Rise</summary>
    /// <seealso cref="Rock.Data.Model{org.secc.Rise.Model.Course}" />
    /// <seealso cref="Rock.Data.IRockEntity" />
    [Table( "_org_secc_Rise_Course" )]
    [DataContract]
    public class Course : Model<Course>, IRockEntity
    {
        /// <summary>Gets or sets the URL.</summary>
        /// <value>The URL.</value>
        [DataMember]
        [MaxLength( 200 )]
        [Index]
        public string Url { get; set; }

        /// <summary>Gets or sets the course identifier.</summary>
        /// <value>The course identifier.</value>
        [DataMember]
        [MaxLength( 200 )]
        [Index]
        public string CourseId { get; set; }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        ///   <para>
        ///  Gets or sets the if course is available to all.
        /// (In the learning library)</para>
        /// </summary>
        /// <value>The available to all.</value>
        [DataMember]
        public bool? AvailableToAll { get; set; }

        /// <summary>Gets or sets the categories.</summary>
        /// <value>The categories.</value>
        [LavaInclude]
        public virtual ICollection<Category> Categories
        {
            get { return _categories ?? ( _categories = new Collection<Category>() ); }
            set { _categories = value; }
        }
        private ICollection<Category> _categories;

        /// <summary>Gets or sets the enrolled groups.</summary>
        /// <value>The enrolled groups.</value>
        [LavaInclude]
        public virtual ICollection<Group> EnrolledGroups
        {
            get { return _enrolledGroups ?? ( _enrolledGroups = new Collection<Group>() ); }
            set { _enrolledGroups = value; }
        }
        private ICollection<Group> _enrolledGroups;
    }

    public partial class CourseConfiguration : EntityTypeConfiguration<Course>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public CourseConfiguration()
        {
            this.HasMany( kt => kt.Categories ).WithMany().Map( kt => { kt.MapLeftKey( "CourseId" ); kt.MapRightKey( "CategoryId" ); kt.ToTable( "_org_secc_Rise_CourseCategory" ); } );
            this.HasMany( kt => kt.EnrolledGroups ).WithMany().Map( kt => { kt.MapLeftKey( "CourseId" ); kt.MapRightKey( "GroupId" ); kt.ToTable( "_org_secc_Rise_CourseEnrolledGroup" ); } );
            this.HasEntitySetName( "Courses" );
        }
    }
}
