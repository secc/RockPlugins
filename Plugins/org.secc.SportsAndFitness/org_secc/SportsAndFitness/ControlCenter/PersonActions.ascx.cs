using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Ajax.Utilities;
using org.secc.Microframe;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security.Authentication;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{
    [DisplayName("Control Center Person Actions")]
    [Category("Sports and Fitness > Control Center")]
    [Description("Person Actions for Sports and Fitness Control Center")]

    [GroupField("Sports and Fitness Group",
        Description = "The group that contains all sports and fitness members.",
        IsRequired = true,
        Order = 0,
        Key = AttributeKeys.SFGroup)]

    [GroupField("Group Fitness Group",
        Description = "The group that all Group Fitness participants belong to",
        IsRequired = true,
        Order = 1,
        Key = AttributeKeys.GroupFitnessGroup)]
    [TextField("Group Fitness Sessions Attribute Key",
        Description = "The key of the attribute that contains the group fitness credits.",
        IsRequired = true,
        Order = 2,
        Key = AttributeKeys.GroupFitnessCreditKey)]
    [TextField("Childcare Credit Attribute Key",
        Description = "The family group attribute key that contains the S&F childcare credits.",
        IsRequired = true,
        Order = 3,
        Key = AttributeKeys.ChildcareCreditKey )]
    [AttributeField("PIN Purpose Attribute",
        Description = "Attribute that contains the Purpose of a Login PIN",
        AllowMultiple = false, 
        EntityTypeGuid = "0fa592f1-728c-4885-be38-60ed6c0d834f",
        IsRequired = true,
        Order = 4,
        Key = AttributeKeys.LoginPINPurpose)]

    public partial class PersonActions : RockBlock
    {

        private string _sportsAndFitnessPINPurposeGuid = "e98517ec-1805-456b-8453-ef8480bd487f";

        public static class AttributeKeys
        {
            public const string GroupFitnessGroup = "GroupFitnessGroup";
            public const string GroupFitnessCreditKey = "GroupFitnessSessions";
            public const string SFGroup = "SportsAndFitnessGroup";
            public const string ChildcareCreditKey = "ChildcareCredits";
            public const string LoginPINPurpose = "LoginPINPurpose";
        }

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            lbUpdatePin.Click += personAction_Click;
            lbChildcareCredits.Click += personAction_Click;
            lbGroupFitnessCredit.Click += personAction_Click;

        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if(!Page.IsPostBack)
            {
                LoadPINBadge();
                LoadChildcareCreditBadge();
                LoadGroupFitnessSessionsBadge();
            }
        }

        #endregion Base Control Methods

        #region Events

        private void personAction_Click( object sender, EventArgs e )
        {
            var commandName = ((LinkButton) sender).CommandName;

            switch (commandName)
            {
                case "update-pin":
                    LoadPinModal();
                    break;
                case "add-childcare-credit":
                    LoadChildcareCreditModal();
                    break;
                case "add-groupfitness-sessions":
                    LoadGroupFitnessModal();
                    break;
                default:
                    break;
            }
        }

        #endregion Events

        #region Internal Methods

        private void LoadPINBadge()
        {
            var pinAuthenticationEntityType = EntityTypeCache.Get( typeof( PINAuthentication ) );
            var SportsPINPurposeDV = DefinedValueCache.Get( _sportsAndFitnessPINPurposeGuid.AsGuid() );
            var personId = PageParameter( "Person" ).AsInteger();

            var purposeAttributeGuid = GetAttributeValue( AttributeKeys.LoginPINPurpose ).AsGuid();

            using (var rockContext = new RockContext())
            {
                var attributeValue = new AttributeValueService( rockContext ).Queryable()
                    .Where( av => av.Attribute.Guid == purposeAttributeGuid );

                var loginCount = new UserLoginService( rockContext ).Queryable()
                    .Join( attributeValue, u => u.Id, av => av.EntityId,
                        ( u, av ) => new { UserLoginId = u.Id, u.UserName, u.PersonId, u.EntityTypeId, purposeIds = av.Value } )
                    .Where( l => l.EntityTypeId == pinAuthenticationEntityType.Id )
                    .Where( l => l.PersonId == personId )
                    .ToList()
                    .Select( l => new { l.UserLoginId, l.UserName, purposes = l.purposeIds.SplitDelimitedValues().AsIntegerList() } )
                    .Where( l => l.purposes.Contains( SportsPINPurposeDV.Id ) )
                    .Count();

                hlblPIN.Text = string.Format( "{0} {1}", loginCount, "Login".PluralizeIf( loginCount != 1 ) );
                if(loginCount == 0)
                {
                    hlblPIN.LabelType = LabelType.Default;
                }
                else
                {
                    hlblPIN.LabelType = LabelType.Success;
                }
                hlblPIN.Visible = true;
            }
        }

        private void LoadPinModal()
        {
            throw new NotImplementedException();
        }

        private void LoadChildcareCreditBadge()
        {
            var personId = PageParameter( "Person" ).AsInteger();
            var creditsAttributeKey = GetAttributeValue( AttributeKeys.ChildcareCreditKey );

            using (var rockContext = new RockContext())
            {
                var primaryFamilyId = new PersonService( rockContext ).Get( personId ).PrimaryFamilyId;

                var familyGroup = new GroupService( rockContext ).Get( primaryFamilyId ?? 0 );

                familyGroup.LoadAttributes( rockContext );
                var credits = familyGroup.GetAttributeValue( creditsAttributeKey ).AsInteger();

                hlblChildcare.Text = string.Format( "{0} {1} remaining", credits, "Credit".PluralizeIf( Math.Abs( credits ) != 1 ) );
                if(credits >0)
                {
                    hlblChildcare.LabelType = LabelType.Success;
                }
                else if(credits < 0)
                {
                    hlblChildcare.LabelType = LabelType.Danger;
                }
                else
                {
                    hlblChildcare.LabelType = LabelType.Default;
                }

                hlblChildcare.Visible = true;
            }
        }

        private void LoadChildcareCreditModal()
        {
            throw new NotImplementedException();
        }

        private void LoadGroupFitnessSessionsBadge()
        {
            var groupFitnessGroupGuid = GetAttributeValue( AttributeKeys.GroupFitnessGroup ).AsGuid();
            var personId = PageParameter( "Person" ).AsInteger();
            var groupFitnessSessionKey = GetAttributeValue( AttributeKeys.GroupFitnessCreditKey );
            var groupMemberEntityType = EntityTypeCache.Get( typeof( GroupMember ) );

            using (var rockContext = new RockContext())
            {
                var groupService = new GroupService( rockContext );

                var group = groupService.Get( groupFitnessGroupGuid );

                if(group == null)
                {
                    throw new Exception( "Group Fitness Group is not found." );
                }

                var groupIdAsString = group.Id.ToString();
                var attribute = new AttributeService( rockContext ).Queryable().AsNoTracking()
                    .Where( a => a.EntityTypeId == groupMemberEntityType.Id )
                    .Where( a => a.EntityTypeQualifierColumn == "GroupId" )
                    .Where( a => a.EntityTypeQualifierValue == groupIdAsString )
                    .Where( a => a.Key == groupFitnessSessionKey )
                    .FirstOrDefault();

                if(attribute == null)
                {
                    throw new Exception( "Group Fitness Session Attribute not found." );
                }

                var attributeValues = new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                    .Where( a => a.AttributeId == attribute.Id )
                    .Where( a => a.Value != null && a.Value != "" );

                var groupMember = new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                    .GroupJoin( attributeValues, gm => gm.Id, av => av.EntityId,
                        ( gm, av ) => new { gm.Id, gm.PersonId, gm.GroupId, gm.GroupMemberStatus, gm.IsArchived, Sessions = av.Select( av1 => av1.ValueAsNumeric ).FirstOrDefault() } )
                    .Where( gm => gm.GroupId == group.Id )
                    .Where( gm => gm.PersonId == personId )
                    .FirstOrDefault();

                if (groupMember == null)
                {
                    hlblGroupFitness.Text = "Not Enrolled";
                    hlblGroupFitness.LabelType = LabelType.Danger;
                }
                else if(groupMember.GroupMemberStatus == GroupMemberStatus.Inactive)
                {
                    hlblGroupFitness.Text = "Inactive Member";
                    hlblGroupFitness.LabelType = LabelType.Warning;
                }
                else if(groupMember.IsArchived)
                {
                    hlblGroupFitness.Text = "Archived Member";
                    hlblGroupFitness.LabelType = LabelType.Warning;
                }
                else
                {
                    if(!groupMember.Sessions.HasValue || groupMember.Sessions.Value == 0)
                    {
                        hlblGroupFitness.Text = "No Sessions Remaining";
                        hlblGroupFitness.LabelType = LabelType.Default;
                    }
                    else
                    {
                        hlblGroupFitness.Text = string.Format( "{0:0} {1} remaining", groupMember.Sessions.Value, "Session".PluralizeIf( groupMember.Sessions.Value != 1 ) );
                        hlblGroupFitness.LabelType = LabelType.Success;
                    }
                }

                hlblGroupFitness.Visible = true;

            }

        }

        private void LoadGroupFitnessModal()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}   