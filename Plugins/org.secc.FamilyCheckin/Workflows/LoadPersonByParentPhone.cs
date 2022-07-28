using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Finds Children by parents phone number
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Searches and loads person by Parent's phone number." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Load Person By Parent's phone number." )]
    [IntegerField( "Minimum Phone Length", "The minimum number of digits for a phone number.", false, 7 )]
    public class LoadPersonByParentPhone : CheckInActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            var checkinState = GetCheckInState( entity, out errorMessages );
            if ( checkinState != null )
            {
                var searchValue = checkinState.CheckIn.SearchValue.Trim();
                long n;
                var isNumeric = long.TryParse( searchValue, out n );

                if ( isNumeric )
                {
                    if ( searchValue.Length < ( GetAttributeValue( action, "MinimumPhoneLength" ).AsIntegerOrNull() ?? 7 ) )
                    {
                        return false;
                    }
                    var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid() );
                    var adultRole = familyGroupType.Roles
                        .FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() );

                    var childRole = familyGroupType.Roles
                        .FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() );

                    var personRecordType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() );




                    var familyMemberQry = new GroupMemberService( rockContext ).Queryable()
                        .Include( fm => fm.Group )
                        .Include( fm => fm.Person )
                        .Where( fm => fm.Group.GroupTypeId == familyGroupType.Id )
                        .Where( fm => fm.Group.IsActive )
                        .Where( fm => fm.GroupMemberStatus == GroupMemberStatus.Active );

                    var adultQry = familyMemberQry.Where( a => a.GroupRoleId == adultRole.Id ).AsQueryable();
                    var childQry = familyMemberQry.Where( a => a.GroupRoleId == childRole.Id ).AsQueryable();

                    var phoneNumberService = new PhoneNumberService( rockContext );

                    List<GroupMember> familyChildren = phoneNumberService.GetBySearchterm( searchValue )
                        .Where( pn => !pn.Person.IsDeceased )
                        .Where( pn => pn.Person.RecordTypeValueId == personRecordType.Id )
                        .Join( adultQry, pn => pn.PersonId, fm => fm.PersonId,
                           ( pn, fm ) => fm )
                        .Join( childQry, parent => parent.GroupId, child => child.GroupId,
                            ( parent, child ) => child )
                        .ToList();

                    foreach ( var child in familyChildren )
                    {
                        var family = checkinState.CheckIn.Families.FirstOrDefault( f => f.Group.Id == child.GroupId );
                        if ( family == null )
                        {
                            family = new CheckInFamily();
                            family.Group = child.Group.Clone( false );
                            family.Group.LoadAttributes( rockContext );
                            family.Caption = family.ToString();
                            family.SubCaption = string.Empty;
                            family.People = new List<CheckInPerson>();
                            checkinState.CheckIn.Families.Add( family );
                        }

                        if ( family.People.FirstOrDefault( p => p.Person.Id == child.PersonId ) == null )
                        {
                            var cPerson = new CheckInPerson();
                            cPerson.Person = child.Person;
                            family.People.Add( cPerson );
                        }
                    }

                    return true;
                }
                return true;
            }
            errorMessages.Add( $"Attempted to run {this.GetType().GetFriendlyTypeName()} in check-in, but the check-in state was null." );
            return false;
        }
    }
}
