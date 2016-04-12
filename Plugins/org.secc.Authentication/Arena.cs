using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Security.Authentication;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock.Security;
using Rock.Model;
using System.Security.Cryptography;
using Rock;

namespace org.secc.Authentication
{
    /// <summary>
    /// Authenticates a username/password using the Rock database
    /// </summary>
    [Description("Arena Authentication Provider")]
    [Export(typeof(AuthenticationComponent))]
    [ExportMetadata("ComponentName", "Arena")]
    public class Arena : Database
    {

        /// <summary>
        /// Encodes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override String EncodePassword(UserLogin user, string password)
        {
            byte[] bytes = (new UnicodeEncoding()).GetBytes(password);
            var hash = SHA1.Create().ComputeHash(bytes);
            return "0x" + BitConverter.ToString(hash).Replace("-", String.Empty).ToLower();
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public override void SetPassword(UserLogin user, string password)
        {
            // When we set a password, we encode it using the database type and switch them away
            // to a standard Rock.Security.Authentication.Database login type (entity 27)
            user.Password = base.EncodePassword(user, password);
            user.EntityTypeId = 27;
            user.LastPasswordChangedDateTime = RockDateTime.Now;
            user.IsPasswordChangeRequired = false;
        }
    }
}
