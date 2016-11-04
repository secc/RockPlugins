using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.secc.Microframe.Model;

namespace RockWeb.Plugins.org_secc.Microframe
{
    [DisplayName( "Sign Detail" )]
    [Category( "SECC > Microframe" )]
    [Description( "Displays the details of the given sign." )]
    public partial class SignDetail : RockBlock, IDetailBlock
    {

        private Dictionary<int, string> SignCategories
        {
            get
            {
                var signCategories = ViewState["SignCategories"] as Dictionary<int, string>;
                if ( signCategories == null )
                {
                    signCategories = new Dictionary<int, string>();
                    ViewState["SignCategories"] = signCategories;
                }
                return signCategories;
            }
            set
            {
                ViewState["SignCategories"] = value;
            }
        }

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.AddConfigurationUpdateTrigger( upnlDevice );

            gCategories.DataKeyNames = new string[] { "Id" };
            gCategories.Actions.ShowAdd = true;
            gCategories.Actions.AddClick += gCategories_AddClick;
            gCategories.GridRebind += gCategories_GridRebind;
        }

        private void gCategories_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindSignCategories();
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
                ShowDetail( PageParameter( "SignId" ).AsInteger() );
            }
        }

        #endregion

        #region Events


        protected void btnSave_Click( object sender, EventArgs e )
        {
            Sign sign = null;

            var rockContext = new RockContext();
            var signService = new SignService( rockContext );
            var attributeService = new AttributeService( rockContext );

            int signId = int.Parse( hfSignId.Value );

            if ( signId != 0 )
            {
                sign = signService.Get( signId );
            }

            if ( sign == null )
            {
                // Check for existing
                var existingDevice = signService.Queryable()
                    .Where( k => k.Name == tbName.Text )
                    .FirstOrDefault();
                if ( existingDevice != null )
                {
                    nbDuplicateSign.Text = string.Format( "A sign already exists with the name '{0}'. Please use a different device name.", existingDevice.Name );
                    nbDuplicateSign.Visible = true;
                }
                else
                {
                    sign = new Sign();
                    signService.Add( sign );
                }
            }


            if ( sign != null )
            {
                sign.Name = tbName.Text;
                sign.PIN = tbPIN.Text;
                sign.Description = tbDescription.Text;
                sign.IPAddress = tbIPAddress.Text;

                if ( !sign.IsValid || !Page.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                var keys = SignCategories.Select( sc => sc.Key ).ToList();
                foreach (var signCategory in sign.SignCategories.ToList() )
                {
                    if (!keys.Contains( signCategory.Id ) )
                    {
                        sign.SignCategories.Remove( signCategory );
                    }
                }
                var newSignCategories = new SignCategoryService( rockContext ).GetByIds( keys ).ToList();
                foreach (var newSignCategory in newSignCategories )
                {
                    if (!sign.SignCategories.Select(sc => sc.Id ).Contains(newSignCategory.Id) )
                    {
                        sign.SignCategories.Add( newSignCategory );
                    }
                }


                rockContext.SaveChanges();

                NavigateToParentPage();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        #endregion

        #region Methods


        /// <summary>
        /// Shows the edit.
        /// </summary>
        /// <param name="DeviceId">The device identifier.</param>
        public void ShowDetail( int signId )
        {
            pnlDetails.Visible = true;
            Sign sign = null;

            var rockContext = new RockContext();

            if ( !signId.Equals( 0 ) )
            {
                sign = new SignService( rockContext ).Get( signId );
                lActionTitle.Text = ActionTitle.Edit( Sign.FriendlyTypeName ).FormatAsHtmlTitle();
                SignCategories = new Dictionary<int, string>();
                foreach ( var signCategory in sign.SignCategories )
                {
                    SignCategories.Add( signCategory.Id, signCategory.Name );
                }
                
            }

            if ( sign == null )
            {
                sign = new Sign { Id = 0 };
                lActionTitle.Text = ActionTitle.Add( Sign.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            hfSignId.Value = sign.Id.ToString();

            tbName.Text = sign.Name;
            tbPIN.Text = sign.PIN;
            tbDescription.Text = sign.Description;
            tbIPAddress.Text = sign.IPAddress;

            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !IsUserAuthorized( Authorization.EDIT ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Sign.FriendlyTypeName );
            }

            if ( readOnly )
            {
                lActionTitle.Text = ActionTitle.View( Sign.FriendlyTypeName );
                btnCancel.Text = "Close";
            }

            tbName.ReadOnly = readOnly;
            tbPIN.ReadOnly = readOnly;
            tbDescription.ReadOnly = readOnly;

            btnSave.Visible = !readOnly;

            BindSignCategories();
        }

        private void BindSignCategories()
        {
            gCategories.DataSource = SignCategories
                .OrderBy( l => l.Value )
                .Select( l => new
                {
                    Id = l.Key,
                    Name = l.Value
                } )
                .ToList();
            gCategories.DataBind();
        }

        protected void gCategories_AddClick( object sender, EventArgs e )
        {
            ddlSignCategories.DataSource = new SignCategoryService( new RockContext() )
                .Queryable()
                .Where( sc => !SignCategories.Keys.Contains( sc.Id ) )
                .ToList();
            ddlSignCategories.DataBind();
            mdSignCategories.Show();
        }

        protected void gCategories_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            if ( SignCategories.ContainsKey( e.RowKeyId ) )
            {
                SignCategories.Remove( e.RowKeyId );
            }
            BindSignCategories();
        }
        #endregion

        protected void mdSignCategories_SaveClick( object sender, EventArgs e )
        {
            var signCategoryId = ddlSignCategories.SelectedValue.AsInteger();
            var signCategory = new SignCategoryService( new RockContext() ).Get( signCategoryId );
            if ( signCategory != null & !SignCategories.ContainsKey( signCategory.Id ) )
            {
                SignCategories.Add( signCategory.Id, signCategory.Name );
            }
            BindSignCategories();
            mdSignCategories.Hide();
        }
    }
}
