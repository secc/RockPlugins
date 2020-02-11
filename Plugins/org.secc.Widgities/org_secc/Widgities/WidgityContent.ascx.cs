using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.Widgities.Cache;
using org.secc.Widgities.Controls;
using org.secc.Widgities.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org.secc.Widgities
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Widgity Content" )]
    [Category( "SECC > Widgities" )]
    [Description( "Block for displaying widgities on a webpage" )]
    public partial class WidgityContent : Rock.Web.UI.RockBlockCustomSettings
    {
        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        WidgityControl widgityControl;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            if ( Page.IsPostBack )
            {
                widgityControl = new WidgityControl
                {
                    ID = ID = this.ID + "_widgityControl",
                    EntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id,
                    EntityGuid = BlockCache.Guid,
                };
                phPlaceholder.Controls.Add( widgityControl );
            }
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
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
                widgityControl = new WidgityControl
                {
                    ID = ID = this.ID + "_widgityControl",
                    EntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id,
                    EntityGuid = BlockCache.Guid
                };
                phPlaceholder.Controls.Add( widgityControl );
                widgityControl.DataBind();
            }

        }

        protected override void ShowSettings()
        {
            if ( UserCanEdit )
            {
                widgityControl.ShowSettings();
            }
        }
    }
}