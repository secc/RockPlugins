using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Security
{
    /// <summary>
    /// User control for managing PINs
    /// </summary>
    [DisplayName( "PIN Manager" )]
    [Category( "SECC > Security" )]
    [Description( "Tool to manage the PINs of a person" )]
    [DefinedTypeField( "Purpose Defined Type", "The defined type containing the purposes for PIN numbers.", true )]

    partial class PINManager : PersonBlock
    {

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindCheckBoxList();
            }

            if ( Person != null && Person.Id != 0 )
            {
                DisplayPINs();

            }
            else
            {
                upnlPIN.Visible = false;
            }
        }



        private void DisplayPINs()
        {
            int pinEntityId = EntityTypeCache.Read( "Rock.Security.Authentication.PINAuthentication" ).Id;

            RockContext rockContext = new RockContext();
            var userLoginEntity = new EntityTypeService( rockContext ).Get( pinEntityId );
            var userAuthorized = userLoginEntity.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );
            if ( userAuthorized )
            {
                lbAdd.Visible = true;
            }

            var pins = new UserLoginService( rockContext ).GetByPersonId( Person.Id ).Where( ul => ul.EntityTypeId == pinEntityId ).ToList();

            phPin.Controls.Clear();
            foreach ( var pin in pins )
            {
                var rowPanel = new Panel();
                rowPanel.CssClass = "row rollover-container";
                phPin.Controls.Add( rowPanel );

                var firstPanel = new Panel();
                firstPanel.CssClass = "col-xs-1";
                rowPanel.Controls.Add( firstPanel );

                var secondPanel = new Panel();
                secondPanel.CssClass = "col-xs-10";
                rowPanel.Controls.Add( secondPanel );

                pin.LoadAttributes();
                List<string> purpose = new List<string>();
                if ( pin.GetAttributeValue( "PINPurpose" ) != null )
                {
                    var purposeIds = pin.GetAttributeValue( "PINPurpose" )
                        .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                        .ToList();
                    if ( purposeIds.Any() )
                    {
                        foreach ( var purposeId in purposeIds )
                        {
                            var dvPurpose = DefinedValueCache.Read( purposeId.AsInteger() );
                            if ( dvPurpose != null )
                            {
                                purpose.Add( dvPurpose.Value );
                            }
                        }
                    }
                    else
                    {
                        purpose.Add( "[No Purpose]" );
                    }
                }
                else
                {
                    purpose.Add( "[No Purpose]" );
                }

                RockLiteral rockLiteral = new RockLiteral();
                rockLiteral.Label = pin.UserName;
                rockLiteral.Text = string.Join( ", ", purpose );
                secondPanel.Controls.Add( rockLiteral );

                var thirdPanel = new Panel();
                thirdPanel.CssClass = "col-xs-1 rollover-item";
                rowPanel.Controls.Add( thirdPanel );

                if ( userAuthorized )
                {
                    LinkButton lbEdit = new LinkButton();
                    lbEdit.ID = pin.Guid.ToString();
                    lbEdit.Text = "<i class='fa fa-pencil'></i>";
                    lbEdit.Click += ( s, ee ) => EditPin( pin.Id, rockContext );
                    thirdPanel.Controls.Add( lbEdit );
                }
            }
        }

        private void BindCheckBoxList()
        {
            var definedTypeGuid = GetAttributeValue( "PurposeDefinedType" ).AsGuid();
            var definedType = DefinedTypeCache.Read( definedTypeGuid );
            cblPurpose.DataSource = definedType.DefinedValues;
            cblPurpose.DataTextField = "Value";
            cblPurpose.DataValueField = "Id";
            cblPurpose.DataBind();

        }


        protected void EditPin( int UserLoginId, RockContext rockContext = null )
        {
            nbError.Visible = false;
            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }
            UserLogin pin;

            if ( UserLoginId != 0 )
            {
                pin = new UserLoginService( rockContext ).Get( UserLoginId );

                //Check to see if non-PIN is trying to be edited
                //This should never be hit, but let's make sure
                if ( pin.EntityTypeId != EntityTypeCache.Read( "Rock.Security.Authentication.PINAuthentication" ).Id )
                {
                    LogException( new Exception( "Atempted edit on non-PIN in PIN manager" ) );
                    return;
                }
                if ( pin != null )
                {
                    hfPinID.Value = pin.Id.ToString();
                    tbPin.Text = pin.UserName;

                    pin.LoadAttributes();
                    if ( pin.GetAttributeValue( "PINPurpose" ) != null )
                    {
                        var purposeIds = pin.GetAttributeValue( "PINPurpose" )
                            .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                            .ToList();
                        foreach ( var checkbox in cblPurpose.Items.Cast<ListItem>() )
                        {
                            checkbox.Selected = false;
                            if ( purposeIds.Contains( checkbox.Value ) )
                            {
                                checkbox.Selected = true;
                            }
                        }
                    }

                }
                btnDelete.Visible = true;
                hfConfirmDelete.Value = "";
                btnDelete.CssClass = "btn btn-block btn-warning";
                btnDelete.Text = "Remove PIN";
            }
            else
            {
                hfPinID.Value = "0";
                tbPin.Text = "";
                btnDelete.Visible = false;
            }

            mdEditPin.Show();
        }


        protected void lbAdd_Click( object sender, EventArgs e )
        {
            EditPin( 0 );
        }

        protected void mdEditPin_SaveClick( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();

            int pinEntityId = EntityTypeCache.Read( "Rock.Security.Authentication.PINAuthentication" ).Id;
            var userLoginEntity = new EntityTypeService( rockContext ).Get( pinEntityId );
            var userAuthorized = userLoginEntity.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );
            if ( !userAuthorized )
            {
                mdEditPin.Hide();
                return;
            }

            int pinId = hfPinID.ValueAsInt();
            UserLoginService userLoginService = new UserLoginService( rockContext );
            UserLogin pin = userLoginService.Get( pinId );

            var number = tbPin.Text.AsDouble();
            if ( number == 0 )
            {
                nbError.Visible = true;
                nbError.Text = "Error: Requested PIN could not be converted to number.";
                return;
            }

            if ( number.ToString().Length < 6 )
            {
                nbError.Visible = true;
                nbError.Text = "Error: Requested PIN must be 6 digits or greater.";
                return;
            }

            //Check to see if this PIN already exists in the system
            if ( pin == null || number.ToString() != pin.UserName )
            {
                if ( userLoginService.GetByUserName( number.ToString() ) != null )
                {
                    nbError.Visible = true;
                    nbError.Text = "This PIN has already been taken.";
                    return;
                }
            }


            if ( pin != null )
            {
                //Check to see if we are accidentally overwriting someones username
                if ( pin.EntityTypeId != pinEntityId )
                {
                    LogException( new Exception( "Atempted edit on non-PIN in PIN manager" ) );
                    return;
                }
            }
            else
            {
                pin = new UserLogin();
                pin.Id = pinId;
                pin.IsConfirmed = true;
                pin.PersonId = Person.Id;
                pin.EntityTypeId = pinEntityId;
                userLoginService.Add( pin );
            }

            if ( number.ToString() != pin.UserName )
            {
                pin.UserName = tbPin.Text;
                rockContext.SaveChanges();
            }
            pin.LoadAttributes();
            pin.SetAttributeValue( "PINPurpose", string.Join( "|", cblPurpose.SelectedValues ) );
            pin.SaveAttributeValues();
            mdEditPin.Hide();

            DisplayPINs();
        }


        protected void btnDelete_Click( object sender, EventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( hfConfirmDelete.Value ) )
            {
                hfConfirmDelete.Value = "DELETE";
                btnDelete.CssClass = "btn btn-block btn-danger";
                btnDelete.Text = "Confirm Delete";
            }
            else
            {
                RockContext rockContext = new RockContext();

                int pinEntityId = EntityTypeCache.Read( "Rock.Security.Authentication.PINAuthentication" ).Id;
                var userLoginEntity = new EntityTypeService( rockContext ).Get( pinEntityId );
                var userAuthorized = userLoginEntity.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );
                if ( !userAuthorized )
                {
                    mdEditPin.Hide();
                    return;
                }

                int pinId = hfPinID.ValueAsInt();
                UserLoginService userLoginService = new UserLoginService( rockContext );
                UserLogin pin = userLoginService.Get( pinId );
                userLoginService.Delete( pin );
                rockContext.SaveChanges();
                mdEditPin.Hide();
                DisplayPINs();
            }
        }
    }
}
