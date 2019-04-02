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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Search
{
    [DisplayName( "Bulk Search" )]
    [Category( "SECC > Search" )]
    [Description( "Takes the ten two csv and puts it in a grid of people." )]
    public partial class BulkSearch : RockBlock
    {

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gGrid.GridRebind += GGrid_GridRebind;
        }

        private void GGrid_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
        }


        protected void btnNumbers_Click( object sender, EventArgs e )
        {
            BindGrid();
        }

        public void BindGrid()
        {
            RockContext rockContext = new RockContext();
            var personService = new PersonService( rockContext );

            // Handle all the phone numbers
            var emails = tbNumbers.Text.Split( new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries )
                .Where(e => Regex.IsMatch( e,
                        @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                        @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                        RegexOptions.IgnoreCase )
                ).Select( e => e.ToLower() ).ToList();

            // Handle the Phone Numbers
            var numbers = tbNumbers.Text.Split( new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries )
                .Where( e => Regex.IsMatch( e,
                         @"^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}$",
                        RegexOptions.IgnoreCase )
                ).Select(n => Regex.Replace( n, "[^0-9]", "" )).ToList();

            var people = personService.Queryable().Where( p => p.PhoneNumbers.Any(pn => numbers.Contains( pn.Number ) ) || emails.Contains( p.Email.ToLower() ) )
                .DistinctBy( p => p.Id )
                .ToList();



            gGrid.DataSource = people;
            gGrid.PersonIdField = "Id";
            gGrid.DataBind();
        }
    }
}
