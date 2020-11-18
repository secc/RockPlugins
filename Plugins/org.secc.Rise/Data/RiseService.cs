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
using Rock.Data;

namespace org.secc.Rise.Data
{
    /// <summary>Base Class for Rise Services</summary>
    /// <typeparam name="T">Rise Entity</typeparam>
    /// <seealso cref="Rock.Data.Service{T}" />
    public class RiseService<T> : Rock.Data.Service<T> where T : Rock.Data.Entity<T>, new()
    {
        public RiseService( RockContext context )
            : base( context )
        {
        }

        public virtual bool CanDelete( T item, out string errorMessage )
        {
            errorMessage = string.Empty;
            return true;
        }
    }
}
