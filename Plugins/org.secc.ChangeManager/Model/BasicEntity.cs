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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using HashidsNet;
using Rock;
using Rock.Data;

namespace org.secc.ChangeManager.Model
{
    public class BasicEntity : IEntity
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public int? ForeignId { get; set; }
        public Guid? ForeignGuid { get; set; }
        public string ForeignKey { get; set; }

        public int TypeId { get { return 0; } }

        public string TypeName { get { return "BasicEntity"; } }

        public string EncryptedKey { get { return ""; } }

        public string ContextKey { get { return ""; } }

        public List<ValidationResult> ValidationResults { get { return new List<ValidationResult>(); } }

        public bool IsValid { get { return true; } }

        public Dictionary<string, object> AdditionalLavaFields { get; set; }


        public string IdKey
        {
            get
            {
                try
                {
                    return GetHash( Id );
                }
                catch
                {
                    return string.Empty;
                }
            }
            private set { /* Make DataContract happy. */ }
        }

        public IEntity Clone()
        {
            return this;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>();
        }

        private string GetHash(int id)
        {
            var salt = ConfigurationManager.AppSettings["DataEncryptionKey"].Left( 40 );
            var hasher = new Hashids( salt, 10 );
            return hasher.Encode( id );
        }

        private int? GetId(string hashedKey)
        {
            var salt = ConfigurationManager.AppSettings["DataEncryptionKey"].Left( 40 );
            var hasher = new Hashids( salt, 10 );
            var ids = hasher.Decode( hashedKey );
            return ids.Length == 1 ? ( int? ) ids[0] : null;
        }
    }
}
