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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.ChangeManager.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.ChangeManager
{
    [DisplayName( "Change Request Detail" )]
    [Category( "SECC > CRM" )]
    [Description( "View requests" )]

    [DataViewField( "Blacklist Data View",
        "Data View of people who should never have their data automatically updated such as staff members, VIPs or other people you wish to have reviewed before updating.",
        false, key: "BlacklistDataView", order: 1 )]
    [WorkflowTypeField( "Workflow", "Workflow to run after completing request." )]
    public partial class ChangeRequestDetail : Rock.Web.UI.RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            var changeId = hfChangeId.ValueAsInt();
            if ( changeId == 0 )
            {
                changeId = PageParameter( "ChangeRequest" ).AsInteger();
                hfChangeId.SetValue( changeId );
            }
            RockContext rockContext = new RockContext();
            ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
            ChangeRequest changeRequest = changeRequestService.Get( changeId );

            if ( changeRequest == null )
            {
                return;
            }

            if ( !IsUserAuthorized( Rock.Security.Authorization.EDIT )
                && ( CurrentPerson == null || !CurrentPerson.Aliases.Select( a => a.Id ).Contains( changeRequest.RequestorAliasId ) ) )
            {
                this.Visible = false;
                return;
            }

            CheckForBlacklist( changeRequest );

            var link = "";
            if ( changeRequest.EntityTypeId == EntityTypeCache.Get( typeof( PersonAlias ) ).Id )
            {
                PersonAliasService personAliasService = new PersonAliasService( rockContext );
                var personAlias = personAliasService.Get( changeRequest.EntityId );
                if ( personAlias != null )
                {
                    link = string.Format( "<a href='/Person/{0}' target='_blank' class='btn btn-default btn-sm'><i class='fa fa-user'></i></a>", personAlias.Person.Id );
                }
            }

            lName.Text = string.Format( @"
<h1 class='panel-title'>{0} {1}</h1>
<div class='panel-labels'>
    <span class='label label-default'>
        Requested by: <a href='/Person/{2}' target='_blank'>{3}</a>
    </span>
    <span class='label label-{4}'>
        {5}
    </span>
</div>",
                link,
                changeRequest.Name,
                changeRequest.RequestorAlias.PersonId,
                changeRequest.RequestorAlias.Person.FullName,
                changeRequest.IsComplete ? "primary" : "success",
                changeRequest.IsComplete ? "Complete" : "Active" );

            var changeRecords = changeRequest.ChangeRecords.ToList();

            var errors = new List<string>();
            var entity = ChangeRequest.GetEntity( changeRequest.EntityTypeId, changeRequest.EntityId, rockContext, changeRequest.FamilyGroupOfPersonAliasId, errors );

            foreach ( var changeRecord in changeRecords )
            {
                FormatValues( changeRequest.EntityTypeId, entity, changeRecord, rockContext );
            }

            if ( changeRecords.Any() )
            {
                gRecords.DataSource = changeRecords;
                gRecords.DataBind();
            }
            else
            {
                gRecords.Visible = false;
            }

            if ( changeRequest.RequestorComment.IsNotNullOrWhiteSpace() )
            {
                ltRequestComments.Visible = true;
                ltRequestComments.Text = changeRequest.RequestorComment;
            }

            ltApproverComment.Text = changeRequest.ApproverComment;
            tbApproverComment.Text = changeRequest.ApproverComment;

            if ( !IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                btnComplete.Visible = false;
                tbApproverComment.Visible = false;
                ltApproverComment.Visible = true;
                ( ( DataControlField ) gRecords.Columns
               .Cast<DataControlField>()
               .Where( fld => ( fld.HeaderText == "Is Rejected" ) )
               .SingleOrDefault() ).Visible = false;
            }
        }

        private void CheckForBlacklist( ChangeRequest changeRequest )
        {
            var blackListDV = GetAttributeValue( "BlacklistDataView" ).AsGuidOrNull();
            if ( blackListDV.HasValue )
            {


                RockContext rockContext = new RockContext();
                List<int> relatedPersonIds = new List<int>();

                if ( changeRequest.EntityTypeId == EntityTypeCache.Get( typeof( PersonAlias ) ).Id )
                {
                    PersonAliasService personAliasService = new PersonAliasService( rockContext );
                    var personAlias = personAliasService.Get( changeRequest.EntityId );
                    if ( personAlias != null )
                    {
                        relatedPersonIds.Add( personAlias.PersonId );
                    }
                }
                else if ( changeRequest.EntityTypeId == EntityTypeCache.Get( typeof( Person ) ).Id )
                {
                    relatedPersonIds.Add( changeRequest.EntityId );
                }
                else if ( changeRequest.EntityTypeId == EntityTypeCache.Get( typeof( Group ) ).Id )
                {
                    GroupService groupService = new GroupService( rockContext );
                    var family = groupService.Get( changeRequest.EntityId );
                    if ( family != null )
                    {
                        relatedPersonIds.AddRange( family.Members.Select( m => m.PersonId ) );
                    }
                }
                DataViewService dataViewService = new DataViewService( rockContext );
                var dv = dataViewService.Get( blackListDV.Value );
                if ( dv != null )
                {
                    var qry = ( IQueryable<Person> ) dv.GetQuery( new DataViewGetQueryArgs { DatabaseTimeoutSeconds = 30 } );

                    if ( qry.Where( p => relatedPersonIds.Contains( p.Id ) ).Any() )
                    {
                        nbBlacklistWarning.Visible = true;
                    }
                }
            }
        }

        private void FormatValues( int entityTypeId, IEntity targetEntity, ChangeRecord changeRecord, RockContext rockContext )
        {
            var errors = new List<string>();
            //Get the target Entity
            if ( changeRecord.RelatedEntityId.HasValue
                    && changeRecord.RelatedEntityId.Value != 0
                    && changeRecord.RelatedEntityTypeId.HasValue )
            {
                entityTypeId = changeRecord.RelatedEntityTypeId.Value;

                switch ( changeRecord.Action )
                {
                    case ChangeRecordAction.Create:
                        targetEntity = ChangeRequest.CreateNewEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.NewValue, rockContext, errors, false );
                        break;
                    case ChangeRecordAction.Delete:
                        targetEntity = ChangeRequest.CreateNewEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.OldValue, rockContext, errors, false );
                        break;
                    default:
                        targetEntity = ChangeRequest.GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext, null, errors );
                        break;
                }
            }

            if ( targetEntity == null )
            {
                return;
            }

            //Enums
            if ( changeRecord.Property.IsNotNullOrWhiteSpace() )
            {
                PropertyInfo enumProp = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );
                if ( enumProp != null && enumProp.PropertyType != null && enumProp.PropertyType.IsEnum )
                {
                    enumProp.PropertyType.GetEnumUnderlyingType();
                    changeRecord.NewValue = System.Enum.GetName( enumProp.PropertyType, changeRecord.NewValue.AsInteger() ).SplitCase();
                    changeRecord.OldValue = System.Enum.GetName( enumProp.PropertyType, changeRecord.OldValue.AsInteger() ).SplitCase();
                }
            }

            //Format new value
            var newObject = changeRecord.NewValue.FromJsonOrNull<BasicEntity>();
            if ( newObject != null )
            {
                if ( changeRecord.Property.IsNullOrWhiteSpace() )
                {
                    changeRecord.NewValue = targetEntity.ToString();
                }
                else
                {
                    PropertyInfo prop = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );

                    if ( prop.PropertyType.GetInterfaces().Any( i => i.IsInterface && i.GetInterfaces().Contains( typeof( IEntity ) ) ) )
                    {
                        var entityTypeCache = EntityTypeCache.Get( prop.PropertyType );

                        var entityType = entityTypeCache.GetEntityType();
                        var dyn = changeRecord.NewValue.FromJsonOrNull<Dictionary<string, object>>();
                        var entity = ( ( IEntity ) Activator.CreateInstance( entityType ) );
                        foreach ( var key in dyn.Keys )
                        {
                            var eleProp = entity.GetType().GetProperty( key );
                            if ( eleProp != null )
                            {
                                ChangeRequest.SetProperty( entity, eleProp, dyn[key].ToStringSafe(), errors );
                            }
                        }
                        if ( entity != null )
                        {
                            changeRecord.NewValue = entity.ToString();
                        }
                    }
                }
            }

            //Format old Value
            var oldObject = changeRecord.OldValue.FromJsonOrNull<BasicEntity>();
            if ( oldObject != null )
            {
                if ( changeRecord.Property.IsNullOrWhiteSpace() )
                {
                    changeRecord.OldValue = targetEntity.ToString();
                }
                else
                {
                    PropertyInfo prop = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );

                    if ( prop.PropertyType.GetInterfaces().Any( i => i.IsInterface && i.GetInterfaces().Contains( typeof( IEntity ) ) ) )
                    {
                        var entityTypeCache = EntityTypeCache.Get( prop.PropertyType );

                        var entityType = entityTypeCache.GetEntityType();
                        var dyn = changeRecord.OldValue.FromJsonOrNull<Dictionary<string, object>>();
                        var entity = ( ( IEntity ) Activator.CreateInstance( entityType ) );
                        foreach ( var key in dyn.Keys )
                        {
                            var eleProp = entity.GetType().GetProperty( key );
                            if ( eleProp != null )
                            {
                                ChangeRequest.SetProperty( entity, eleProp, dyn[key].ToStringSafe(), errors );
                            }
                        }
                        if ( entity != null )
                        {
                            changeRecord.OldValue = entity.ToString();
                        }
                    }
                }
            }

            // Attributes
            if ( changeRecord.Property.IsNotNullOrWhiteSpace() )
            {
                PropertyInfo prop = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );
                if ( prop == null )
                {
                    ( ( IHasAttributes ) targetEntity ).LoadAttributes();
                    if ( targetEntity is IHasAttributes && ( ( IHasAttributes ) targetEntity ).Attributes.Keys.Contains( changeRecord.Property ) )
                    {
                        // Just set the attribute to the appropriate value and then pull the formatted value.
                        ( ( IHasAttributes ) targetEntity ).SetAttributeValue( changeRecord.Property, changeRecord.OldValue );
                        changeRecord.OldValue = ( ( IHasAttributes ) targetEntity ).AttributeValues[changeRecord.Property].ValueFormatted;

                        ( ( IHasAttributes ) targetEntity ).SetAttributeValue( changeRecord.Property, changeRecord.NewValue );
                        changeRecord.NewValue = ( ( IHasAttributes ) targetEntity ).AttributeValues[changeRecord.Property].ValueFormatted;
                    }
                }
            }


            //Special Dispensation for the photo.
            if ( changeRecord.Property == "PhotoId" )
            {
                if ( changeRecord.NewValue.AsInteger() != 0 )
                {
                    changeRecord.NewValue = string.Format( "<a href='/GetImage.ashx?id={0}' target='_blank'><img src='/GetImage.ashx?id={0}' height=50></a>",
                        changeRecord.NewValue );
                }
                if ( changeRecord.OldValue.AsInteger() != 0 )
                {
                    changeRecord.OldValue = string.Format( "<a href='/GetImage.ashx?id={0}' target='_blank'><img src='/GetImage.ashx?id={0}' height=50></a>",
                        changeRecord.OldValue );
                }
            }
            else
            {
                //Format Property Name

                if ( changeRecord.RelatedEntityType != null )
                {
                    changeRecord.Property = changeRecord.RelatedEntityType.Name.Split( '.' ).Last() + ": " + changeRecord.Property.SplitCase();
                }
                else
                {
                    changeRecord.Property = changeRecord.Property.SplitCase();
                }
                if ( changeRecord.Comment.IsNotNullOrWhiteSpace() )
                {
                    changeRecord.Property += "<br>(" + changeRecord.Comment + ")";
                }
            }
        }

        protected void gRecords_CheckedChanged( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            //Only change value if user edit
            if ( IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                RockContext rockContext = new RockContext();
                ChangeRecordService changeRecordService = new ChangeRecordService( rockContext );
                var changeRecord = changeRecordService.Get( e.RowKeyId );
                if ( changeRecord != null )
                {
                    if ( changeRecord.IsRejected )
                    {
                        changeRecord.IsRejected = false;
                    }
                    else
                    {
                        changeRecord.IsRejected = true;
                    }
                }
                rockContext.SaveChanges();
            }
            BindGrid();
        }

        protected void btnComplete_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
            var changeRequest = changeRequestService.Get( hfChangeId.ValueAsInt() );
            changeRequest.ApproverComment = tbApproverComment.Text;

            List<string> errors;
            changeRequest.CompleteChanges( new RockContext(), out errors );

            if ( errors.Any() )
            {
                nbError.Visible = true;
                nbError.Text = "<ul>" + string.Join( "", errors.Select( ex => string.Format( "<li>{0}</li>", ex ) ) ) + "</ul>";
            }
            else
            {
                changeRequest.IsComplete = true;
                changeRequest.ApproverAliasId = CurrentPersonAliasId ?? 0;
                rockContext.SaveChanges();
                changeRequest.LaunchWorkflow( GetAttributeValue( "Workflow" ).AsGuidOrNull() );
                NavigateToParentPage();
            }
        }
    }
}