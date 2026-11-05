using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using org.secc.Security.Utility;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;


namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{
    [DisplayName("Sports and Fitness PIN Manager")]
    [Category("Sports and Fitness > Control Center")]
    [Description("Helps manage user PINS, should only be used in conjuction with the control center person actions block.")]
    public partial class ManageSportsPINs : RockBlock
    {
        string SFPurposeGuid = "e98517ec-1805-456b-8453-ef8480bd487f";
        string PinPurposeAttributeGuid = "aae4d6b6-88dc-427f-bda9-60ec7a75db65";
        int minPinLength = 4;

        /// <summary>
        /// True when the current person can add and remove the PINs shown in the list.
        /// </summary>
        protected bool CanManagePins { get; private set; }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            rSportsPins.ItemCommand += rSportsPins_ItemCommand;
            lbAdd.Click += lbAdd_Click;
        }



        protected override void OnLoad( EventArgs e )
        {
            HideAlert();
            if(!Page.IsPostBack)
            {
                LoadPins();
            }
        }

        private void lbAdd_Click( object sender, EventArgs e )
        {
            AddPIN();

        }

        private void rSportsPins_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            if (e.CommandName.Equals( "RemovePIN", StringComparison.InvariantCultureIgnoreCase ))
            {
                RemovePIN( e.CommandArgument.ToString().AsInteger() );
                LoadPins();
            }
        }

        private void LoadPins()
        {
            pnlMain.Visible = false;
            var personId = PageParameter( "Person" ).AsIntegerOrNull();

            if(!personId.HasValue)
            {
                return;
            }

            var userLoginEntityType = EntityTypeCache.Get( typeof( UserLogin ) );
            var pinLoginType = EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_PIN );
            var SFPurpose = DefinedValueCache.Get( SFPurposeGuid );

            using (var rockContext = new RockContext())
            {
                var userLoginService = new UserLoginService( rockContext );
                var attributeValueService = new AttributeValueService( rockContext );

                var canAdministratePins = CanAdministratePins( rockContext );
                var person = new PersonService( rockContext ).Get( personId.Value );
                CanManagePins = CanManagePinsFor( person, canAdministratePins );
                tbPIN.Visible = CanManagePins;
                lbAdd.Visible = CanManagePins;

                var attributeGuid = PinPurposeAttributeGuid.AsGuid();
                var attributeQry = attributeValueService.Queryable()
                    .Where( a => a.Attribute.Guid == attributeGuid );


                var userLogins = userLoginService.Queryable()
                    .Join( attributeQry, u => u.Id, a => a.EntityId,
                        ( u, a ) => new { UserLogin = u, PurposeValue = a.Value } )
                    .Where( l => l.UserLogin.PersonId == personId.Value )
                    .Where( l => l.UserLogin.EntityTypeId == pinLoginType.Id )
                    .ToList()
                    .Where( l => l.PurposeValue.SplitDelimitedValues().Contains( SFPurpose.Id.ToString() ) )
                    .Select( l => new { l.UserLogin.Id, UserName = PinManagerRules.DisplayPin( l.UserLogin.UserName, canAdministratePins ) } )
                    .ToList();

                rSportsPins.DataSource = userLogins;
                rSportsPins.DataBind();

                pnlMain.Visible = true;
                
            }

        }

        private void AddPIN()
        {
            // Derive the target person from the server-side page parameter and
            // verify the current user is authorized to edit them. The client-posted
            // hidden field must never be trusted for this (ROCK-8766).
            var person = GetAuthorizedPerson();

            if(person == null)
            {
                return;
            }

            var personId = person.Id;

            var pin = tbPIN.Text.Trim();
            if(pin.IsNullOrWhiteSpace() || pin.Length < minPinLength)
            {
                ShowAlert( "PIN Not Valid", $"PIN must be at least {minPinLength} characters long." );
                return;
            }

            if ( !pin.All( c => c >= '0' && c <= '9' ) )
            {
                ShowAlert( "PIN Not Valid", "PIN must contain only the numbers 0-9." );
                return;
            }

            if ( pin.All( c => c == '0' ) )
            {
                ShowAlert( "PIN Not Valid", "PIN cannot be all zeros." );
                return;
            }

            using (var userContext = new RockContext())
            {
                var userLoginService = new UserLoginService( userContext );

                var loginExists = userLoginService.Queryable()
                    .Where( l => l.UserName == pin )
                    .Any();

                if(loginExists)
                {
                    ShowAlert( "PIN Not Valid", "Entered PIN is not available" );
                    return;
                }

                var userLogin = new UserLogin()
                {
                    PersonId = personId,
                    UserName = pin,
                    EntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_PIN ).Id,
                    IsConfirmed = true,
                    IsLockedOut = false,

                };
                userLoginService.Add( userLogin );
                userContext.SaveChanges();

                userLogin.LoadAttributes();
                var attribute = AttributeCache.Get( PinPurposeAttributeGuid );
                var spPurpose = DefinedValueCache.Get( SFPurposeGuid );

                userLogin.SetAttributeValue( attribute.Key, spPurpose.Id.ToString() );
                userLogin.SaveAttributeValues();

                userContext.SaveChanges();
            }

            tbPIN.Text = string.Empty;
            LoadPins();
            
        }

        private void HideAlert()
        {
            lAlertTitle.Text = string.Empty;
            lAlertText.Text = string.Empty;
            pnlAlert.Visible = false;
        }

        private void RemovePIN(int loginId)
        {
            // Same access-control requirement as AddPIN: resolve the person from the
            // server-side page parameter and confirm the current user can edit them,
            // rather than trusting the client-posted hidden field (ROCK-8766).
            var person = GetAuthorizedPerson();

            if(person == null)
            {
                return;
            }

            var personId = person.Id;

            using (var rockContext = new RockContext())
            {
                var userLoginService = new UserLoginService( rockContext );
                var login = userLoginService.Get( loginId );

                if ( !PinManagerRules.IsPersonPin( login, personId, EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_PIN ).Id ) )
                {
                    return;
                }

                // Same as Person Actions: remove only the Sports and Fitness purpose from a PIN with other
                // purposes, delete a PIN whose only purpose is Sports and Fitness, and leave other PINs alone.
                var sportsPINDV = DefinedValueCache.Get( SFPurposeGuid );
                login.LoadAttributes( rockContext );
                var pinTypesIds = login.GetAttributeValue( "PINPurpose" ).SplitDelimitedValues()
                    .Select( v => v.AsInteger() )
                    .ToList();

                if ( pinTypesIds.Count() > 1 && pinTypesIds.Contains( sportsPINDV.Id ) )
                {
                    pinTypesIds.Remove( sportsPINDV.Id );
                    login.SetAttributeValue( "PINPurpose", string.Join( "|", pinTypesIds ) );
                    login.SaveAttributeValue( "PINPurpose" );
                }
                else if ( pinTypesIds.Contains( sportsPINDV.Id ) )
                {
                    userLoginService.Delete( login );
                }

                rockContext.SaveChanges();

            }
        }

        /// <summary>
        /// Resolves the target person from the server-side "Person" page parameter and
        /// verifies the current user can administrate PIN Authentication and is authorized
        /// to edit that person before any PIN login is created or removed. Returns null
        /// (showing an alert when appropriate) if no person is specified or the current
        /// user is not authorized.
        /// </summary>
        private Person GetAuthorizedPerson()
        {
            var personId = PageParameter( "Person" ).AsIntegerOrNull();

            if(!personId.HasValue)
            {
                return null;
            }

            using (var rockContext = new RockContext())
            {
                var person = new PersonService( rockContext ).Get( personId.Value );

                if(person == null)
                {
                    return null;
                }

                if ( !CanManagePinsFor( person, CanAdministratePins( rockContext ) ) )
                {
                    ShowAlert( "Not Authorized", "You are not authorized to manage PINs for this person." );
                    return null;
                }

                return person;
            }
        }

        /// <summary>
        /// Returns true when the current person can add and remove the person's PINs: they can administrate
        /// PIN Authentication (<paramref name="canAdministratePins"/>, from CanAdministratePins) and edit the person.
        /// </summary>
        private bool CanManagePinsFor( Person person, bool canAdministratePins )
        {
            return person != null
                && canAdministratePins
                && person.IsAuthorized( Authorization.EDIT, CurrentPerson );
        }

        /// <summary>
        /// Returns true when the current person can administrate PIN Authentication, the same check as the PIN Manager block.
        /// </summary>
        private bool CanAdministratePins( RockContext rockContext )
        {
            return new EntityTypeService( rockContext ).Get( EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_PIN ).Id )
                .IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );
        }

        private void ShowAlert(string title, string message)
        {
            lAlertTitle.Text = title;
            lAlertText.Text = message;
            pnlAlert.Visible = true;
           
        }
    }
}