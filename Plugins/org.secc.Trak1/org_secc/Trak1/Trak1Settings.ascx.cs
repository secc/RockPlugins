using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI;

using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.secc.Trak1.BackgroundCheck;
using System.Reflection;
using org.secc.Trak1.Helpers;
using System.Net.Http;
using Newtonsoft.Json;

namespace RockWeb.Plugins.org_secc.Trak1
{
    [DisplayName( "Trak1 Packages" )]
    [Category( "SECC > Background Check" )]
    [Description( "Block for updating the settings used by the Trak-1 integration." )]

    public partial class Trak1Settings : Rock.Web.UI.RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            RockContext rockContext = new RockContext();
            EntityTypeService entityTypeService = new EntityTypeService( rockContext );
            var assembly = typeof( org.secc.Trak1.BackgroundCheck.Trak1 ).AssemblyQualifiedName;
            var entity = entityTypeService.Queryable().Where( et => et.AssemblyName == assembly ).FirstOrDefault();
            var providerGuid = entity.Guid;
            var provider = BackgroundCheckContainer.GetComponent( providerGuid.ToString() );

            var trak1 = ( org.secc.Trak1.BackgroundCheck.Trak1 ) provider;
            var packages = trak1.GetPackageList();

            foreach ( var package in packages )
            {
                PanelWidget panelWidget = new PanelWidget()
                {
                    Title = package.PackageName,
                    Expanded = true
                };
                phPackages.Controls.Add( panelWidget );
                var sb = new StringBuilder();
                sb.Append( "<ul>" );
                sb.AppendFormat( "<li>Price: {0}</li>", package.PackagePrice );
                sb.AppendFormat( "<li>Description: {0}</li>", package.PackageDescription );
                sb.Append( "<li><b>Components:</b></li>" );
                sb.Append( "<ul>" );
                foreach ( var component in package.Components )
                {
                    sb.AppendFormat( "<li><b>{0}</b></li>", component.ComponentName );
                    sb.AppendFormat( "<li>Description: {0}</li>", component.ComponentDescription );
                    if ( component.RequiredFields.Any() )
                    {
                        sb.Append( "<li><b>Required Fields:</b></li>" );
                        sb.Append( "<ul>" );
                        foreach ( var requiredField in component.RequiredFields )
                        {
                            sb.AppendFormat( "<li>Name: {0}</li>", requiredField.Name );
                            sb.Append( "<ul>" );
                            sb.AppendFormat( "<li>Type: {0}</li>", requiredField.Type );
                            sb.AppendFormat( "<li>Length: {0}</li>", requiredField.Length );
                            sb.AppendFormat( "<li>Description: {0}</li>", requiredField.Description );
                            sb.Append( "</ul>" );
                        }
                        sb.Append( "</ul>" );
                    }
                }
                sb.Append( "</ul>" );
                sb.Append( "</ul>" );
                LiteralControl ltInfo = new LiteralControl
                {
                    Text = sb.ToString()
                };
                panelWidget.Controls.Add( ltInfo );
            }
        }
    }
}