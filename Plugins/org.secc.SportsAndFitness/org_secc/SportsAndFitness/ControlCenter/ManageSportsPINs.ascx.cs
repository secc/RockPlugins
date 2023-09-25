using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
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

            hfPersonId.Value = personId.ToString();

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
                    .Select( l => l.UserLogin )
                    .ToList();

                rSportsPins.DataSource = userLogins;
                rSportsPins.DataBind();

                pnlMain.Visible = true;
                
            }

        }

        private void AddPIN()
        {
            var personId = hfPersonId.Value.AsInteger();

            if(personId <= 0)
            {
                return;
            }

            var pin = tbPIN.Text.Trim();
            if(pin.IsNullOrWhiteSpace() || pin.Length <= minPinLength)
            {
                ShowAlert( "PIN Not Valid", $"PIN must be at least {minPinLength} long." );
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

            var personId = hfPersonId.Value.AsInteger();

            using (var rockContext = new RockContext())
            {
                var userLoginService = new UserLoginService( rockContext );
                var login = userLoginService.Queryable()
                    .Where( l => l.Id == loginId )
                    .Where( l => l.PersonId == personId )
                    .SingleOrDefault();

                userLoginService.Delete( login );
                rockContext.SaveChanges();
                    
            }
        }

        private void ShowAlert(string title, string message)
        {
            lAlertTitle.Text = title;
            lAlertText.Text = message;
            pnlAlert.Visible = true;
           
        }
    }
}