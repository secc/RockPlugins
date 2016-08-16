using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using System.Reflection;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Rock.Data;
using org.secc.FamilyCheckin.Utilities;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.SportsAndFitness
{
    [DisplayName( "PIN Adder Modal" )]
    [Category( "SECC > Check-in" )]
    [Description( "Block for adding PIN numbers to a person" )]
    [DataViewField( "Security Role Dataview", "Data view which people who are in a security role. It will not allow adding PINs for people in this group.", entityTypeName: "Rock.Model.Person", required: false )]

    public partial class PinAdderModal : RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
        }

        protected override void OnLoad( EventArgs e )
        {
        }


        protected void btnRequest_Click( object sender, EventArgs e )
        {
            pnlErrors.Visible = false;
            tbPin.Text = "";
            ppPerson.SelectedValue = 0;
            ppPerson.PersonName = "";
            mdPin.Show();
        }

        protected void mdPin_SaveClick( object sender, EventArgs e )
        {
            var pin = tbPin.Text.AsNumeric();
            if ( !string.IsNullOrWhiteSpace( pin ) && pin.Length > 7 )
            {

                var personId = ppPerson.PersonId;
                if ( personId != null )
                {
                    //check to see if person is in a security role and disallow if in security role
                    var securityRoleGuid = GetAttributeValue( "SecurityRoleDataview" );

                    if ( string.IsNullOrWhiteSpace( securityRoleGuid ) )
                    {
                        alert( "Security role dataview attribute not set." );
                        return;
                    }
                    var rockContext = new RockContext();

                    var securityMembers = new DataViewService( rockContext ).Get( securityRoleGuid.AsGuid() );

                    if ( securityMembers == null )
                    {
                        alert( "Security role dataview not found." );
                        return;
                    }
                    var errorMessages = new List<string>();
                    var securityMembersQry = securityMembers.GetQuery( null, 30, out errorMessages );
                    if ( securityMembersQry.Where( p => p.Id == personId ).Any() )
                    {
                        alert( "Unable to add PIN to person. This person is in a security role and cannot have a PIN added from this tool." );
                        return;
                    }

                    var userLoginService = new UserLoginService( rockContext );
                    var userLogin = userLoginService.GetByUserName( pin );
                    if ( userLogin == null )
                    {
                        userLogin = new UserLogin();
                        userLogin.UserName = pin;
                        userLogin.IsConfirmed = true;
                        userLogin.PersonId = personId;
                        userLogin.EntityTypeId = EntityTypeCache.Read( "Rock.Security.Authentication.PINAuthentication" ).Id;
                        userLoginService.Add( userLogin );
                        rockContext.SaveChanges();
                        mdPin.Hide();
                        maInfo.Show( "PIN has been added.", ModalAlertType.Information );
                    }
                    else
                    {
                        alert( "This PIN has already been assigned to a person, and cannot be assigned." );
                    }
                }
            }
            else
            {
                alert( "PIN number was of invalid length" );
            }
        }

        void alert( string text )
        {
            pnlErrors.Visible = true;
            ltErrors.Text = text;
        }
    }
}