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
using System.ComponentModel;
using Rock;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using Rock.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using org.secc.ChangeManager.Model;
using System.Reflection;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.ChangeManager
{
    [DisplayName( "Change Entry" )]
    [Category( "SECC > CRM" )]
    [Description( "Allows people to enter changes which can later be reviewed." )]
    public partial class ChangeEntry : Rock.Web.UI.RockBlock
    {


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                Person person = GetPerson();
                if ( person == null )
                {
                    throw new Exception( "A person is needed." );
                }
                BindDropDown();
                DisplayForm( person );
            }
        }

        private void BindDropDown()
        {
            ddlTitle.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_TITLE.AsGuid() ), true );
            ddlGender.BindToEnum<Gender>( true );
            ddlMaritalStatus.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS.AsGuid() ), true );
        }

        private void DisplayForm( Person person )
        {
            ddlTitle.SetValue( person.TitleValueId );
            iuPhoto.BinaryFileId = person.PhotoId;
            tbNickName.Text = person.NickName;
            tbFirstName.Text = person.FirstName;
            tbLastName.Text = person.LastName;

            //PhoneNumber
            var mobilePhoneType = DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ) );

            var phoneNumbers = new List<PhoneNumber>();
            var phoneNumberTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ) );
            if ( phoneNumberTypes.DefinedValues.Any() )
            {
                foreach ( var phoneNumberType in phoneNumberTypes.DefinedValues )
                {
                    var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberType.Id );
                    if ( phoneNumber == null )
                    {
                        var numberType = new DefinedValue();
                        numberType.Id = phoneNumberType.Id;
                        numberType.Value = phoneNumberType.Value;

                        phoneNumber = new PhoneNumber { NumberTypeValueId = numberType.Id, NumberTypeValue = numberType };
                        phoneNumber.IsMessagingEnabled = mobilePhoneType != null && phoneNumberType.Id == mobilePhoneType.Id;
                    }
                    else
                    {
                        // Update number format, just in case it wasn't saved correctly
                        phoneNumber.NumberFormatted = PhoneNumber.FormattedNumber( phoneNumber.CountryCode, phoneNumber.Number );
                    }

                    phoneNumbers.Add( phoneNumber );
                }
            }
            rContactInfo.DataSource = phoneNumbers;
            rContactInfo.DataBind();


            //email
            tbEmail.Text = person.Email;
            cbIsEmailActive.Checked = person.IsEmailActive;

            rblEmailPreference.SetValue( person.EmailPreference.ConvertToString( false ) );
            rblCommunicationPreference.SetValue( person.CommunicationPreference == CommunicationType.SMS ? "2" : "1" );

            //demographics
            bpBirthday.SelectedDate = person.BirthDate;
            ddlGender.SetValue( person.Gender.ConvertToInt() );
            ddlMaritalStatus.SetValue( person.MaritalStatusValueId );
            dpAnniversaryDate.SelectedDate = person.AnniversaryDate;

            if ( !person.HasGraduated ?? false )
            {
                int gradeOffset = person.GradeOffset.Value;
                var maxGradeOffset = ddlGradePicker.MaxGradeOffset;

                // keep trying until we find a Grade that has a gradeOffset that that includes the Person's gradeOffset (for example, there might be combined grades)
                while ( !ddlGradePicker.Items.OfType<ListItem>().Any( a => a.Value.AsInteger() == gradeOffset ) && gradeOffset <= maxGradeOffset )
                {
                    gradeOffset++;
                }

                ddlGradePicker.SetValue( gradeOffset );
            }
            else
            {
                ddlGradePicker.SelectedIndex = 0;
                ypGraduation.SelectedYear = person.GraduationYear;
            }

            ScriptManager.RegisterStartupScript( ddlGradePicker, ddlGradePicker.GetType(), "grade-selection-" + BlockId.ToString(), ddlGradePicker.GetJavascriptForYearPicker( ypGraduation ), true );

            //Family Info
            var location = person.GetHomeLocation();
            acAddress.SetValues( location );
            ddlCampus.SetValue( person.GetCampus() );

        }

        private Person GetPerson()
        {
            var personId = hfPersonId.ValueAsInt();
            if ( personId == 0 )
            {
                personId = PageParameter( "PersonId" ).AsInteger();
                hfPersonId.SetValue( personId );
            }
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            return personService.Get( personId );
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var person = GetPerson();
            var personAliasEntityType = EntityTypeCache.Get( typeof( PersonAlias ) );
            var changeRequest = new ChangeRequest
            {
                EntityTypeId = personAliasEntityType.Id,
                EntityId = person.PrimaryAliasId ?? 0,
                RequestorAliasId = CurrentPersonAliasId ?? 0
            };

            EvaluatePropertyChange( changeRequest, person, "PhotoId", iuPhoto.BinaryFileId );
            EvaluatePropertyChange( changeRequest, person, "TitleValue", DefinedValueCache.Get( ddlTitle.SelectedValueAsInt() ?? 0 ) );
            EvaluatePropertyChange( changeRequest, person, "FirstName", tbFirstName.Text );
            EvaluatePropertyChange( changeRequest, person, "NickName", tbNickName.Text );
            EvaluatePropertyChange( changeRequest, person, "LastName", tbLastName.Text );


            //Evaluate PhoneNumbers
            var phoneNumberTypeIds = new List<int>();
            bool smsSelected = false;
            foreach ( RepeaterItem item in rContactInfo.Items )
            {
                HiddenField hfPhoneType = item.FindControl( "hfPhoneType" ) as HiddenField;
                PhoneNumberBox pnbPhone = item.FindControl( "pnbPhone" ) as PhoneNumberBox;
                CheckBox cbUnlisted = item.FindControl( "cbUnlisted" ) as CheckBox;
                CheckBox cbSms = item.FindControl( "cbSms" ) as CheckBox;

                if ( hfPhoneType != null &&
                    pnbPhone != null &&
                    cbSms != null &&
                    cbUnlisted != null )
                {
                    if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbPhone.Number ) ) )
                    {
                        int phoneNumberTypeId;
                        if ( int.TryParse( hfPhoneType.Value, out phoneNumberTypeId ) )
                        {
                            var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberTypeId );
                            string oldPhoneNumber = string.Empty;
                            if ( phoneNumber == null )
                            {
                                phoneNumber = new PhoneNumber
                                {
                                    PersonId = person.Id,
                                    NumberTypeValueId = phoneNumberTypeId,
                                    CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode ),
                                    IsMessagingEnabled = !smsSelected && cbSms.Checked,
                                    Number = PhoneNumber.CleanNumber( pnbPhone.Number )
                                };

                                var phoneChange = new ChangeRecord
                                {
                                    RelatedEntityTypeId = EntityTypeCache.Get( typeof( PhoneNumber ) ).Id,
                                    RelatedEntityId = 0,
                                    OldValue = "",
                                    NewValue = phoneNumber.ToJson(),
                                };
                                changeRequest.ChangeRecords.Add( phoneChange );
                            }
                            else
                            {
                                EvaluatePropertyChange( changeRequest, phoneNumber, "Number", PhoneNumber.CleanNumber( pnbPhone.Number ), true );
                                EvaluatePropertyChange( changeRequest, phoneNumber, "IsMessagingEnabled", ( !smsSelected && cbSms.Checked ), true );
                                EvaluatePropertyChange( changeRequest, phoneNumber, "IsUnlisted", cbUnlisted.Checked, true );
                            }
                        }
                    }
                }
            }

            EvaluatePropertyChange( changeRequest, person, "Email", person.Email );
            EvaluatePropertyChange( changeRequest, person, "EmailPreference",
                rblEmailPreference.SelectedValueAsEnum<EmailPreference>() );
            EvaluatePropertyChange( changeRequest, person, "CommunicationPreference",
                rblCommunicationPreference.SelectedValueAsEnum<CommunicationType>() );


            var birthday = bpBirthday.SelectedDate;
            if ( birthday.HasValue )
            {
                EvaluatePropertyChange( changeRequest, person, "BirthMonth", birthday.Value.Month );
                EvaluatePropertyChange( changeRequest, person, "BirthDay", birthday.Value.Day );
                if ( birthday.Value.Year != DateTime.MinValue.Year )
                {
                    EvaluatePropertyChange( changeRequest, person, "BirthYear", birthday.Value.Year );
                }
                else
                {
                    int? year = null;
                    EvaluatePropertyChange( changeRequest, person, "BirthYear", year );
                }
            }

            EvaluatePropertyChange( changeRequest, person, "Gender", ddlGender.SelectedValueAsEnum<Gender>() );
            EvaluatePropertyChange( changeRequest, person, "MaritalStatusValue", DefinedValueCache.Get( ddlMaritalStatus.SelectedValueAsInt() ?? 0 ) );
            EvaluatePropertyChange( changeRequest, person, "AnniversaryDate", dpAnniversaryDate.SelectedDate );
            EvaluatePropertyChange( changeRequest, person, "GraduationYear", ypGraduation.SelectedYear );

            if ( changeRequest.ChangeRecords.Any() )
            {
                ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                changeRequestService.Add( changeRequest );
                rockContext.SaveChanges();
                CompleteChanges( changeRequest, rockContext );
            }

            var groupEntity = EntityTypeCache.Get( typeof( Group ) );
            var groupLocationEntity = EntityTypeCache.Get( typeof( GroupLocation ) );
            var family = person.GetFamily();

            var familyChangeRequest = new ChangeRequest()
            {
                EntityTypeId = groupEntity.Id,
                EntityId = family.Id,
                RequestorAliasId = CurrentPersonAliasId ?? 0
            };

            EvaluatePropertyChange( familyChangeRequest, family, "Campus", CampusCache.Get( ddlCampus.SelectedValueAsInt() ?? 0 ) );

            var currentLocation = person.GetHomeLocation();
            Location location = new Location
            {
                Street1 = acAddress.Street1,
                Street2 = acAddress.Street2,
                City = acAddress.City,
                State = acAddress.State,
                PostalCode = acAddress.PostalCode
            };

            if ( currentLocation.Street1 != location.Street1 || currentLocation.PostalCode != location.PostalCode )
            {

                LocationService locationService = new LocationService( rockContext );
                locationService.Add( location );
                rockContext.SaveChanges();

                var previousLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid() );
                var homeLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );

                GroupLocation groupLocation = new GroupLocation
                {
                    CreatedByPersonAliasId = CurrentPersonAliasId,
                    ModifiedByPersonAliasId = CurrentPersonAliasId,
                    GroupId = family.Id,
                    LocationId = location.Id,
                    GroupLocationTypeValueId = homeLocationType.Id
                };

                ChangeRecord locationChangeRecord = new ChangeRecord
                {
                    RelatedEntityTypeId = EntityTypeCache.Get( typeof( GroupLocation ) ).Id,
                    RelatedEntityId = 0,
                    OldValue = "",
                    NewValue = groupLocation.ToJson(),
                    IsRejected = false,
                    WasApplied = false
                };
                familyChangeRequest.ChangeRecords.Add( locationChangeRecord );
                var homelocations = family.GroupLocations.Where( gl => gl.GroupLocationTypeValueId == homeLocationType.Id );
                foreach ( var homelocation in homelocations )
                {
                    ChangeRecord prevHome = new ChangeRecord
                    {
                        RelatedEntityTypeId = EntityTypeCache.Get( typeof( GroupLocation ) ).Id,
                        RelatedEntityId = homelocation.Id,
                        OldValue = homeLocationType.ToJson(),
                        NewValue = previousLocationType.ToJson(),
                        Property = "GroupLocationTypeValue",
                        IsRejected = false,
                        WasApplied = false
                    };
                    familyChangeRequest.ChangeRecords.Add( prevHome );
                }
            }

            if ( changeRequest.ChangeRecords.Any() )
            {
                ChangeRequestService changeRequestService = new ChangeRequestService( rockContext );
                changeRequestService.Add( familyChangeRequest );
                rockContext.SaveChanges();
                CompleteChanges( familyChangeRequest, rockContext );
            }
        }

        public void CompleteChanges( ChangeRequest changeRequest, RockContext rockContext )
        {
            using ( var dbContextTransaction = rockContext.Database.BeginTransaction() )
            {
                try
                {
                    IEntity entity = GetEntity( changeRequest.EntityTypeId, changeRequest.EntityId, rockContext );

                    foreach ( var changeRecord in changeRequest.ChangeRecords.Where( r => r.WasApplied != true && r.IsRejected == false ) )
                    {
                        var targetEntity = entity;
                        if ( changeRecord.RelatedEntityTypeId.HasValue )
                        {
                            if ( changeRecord.RelatedEntityId.HasValue && changeRecord.RelatedEntityId != 0 )
                            {
                                //existing entity
                                targetEntity = GetEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.RelatedEntityId.Value, rockContext );
                            }
                            else
                            {
                                //new entity
                                targetEntity = CreateNewEntity( changeRecord.RelatedEntityTypeId.Value, changeRecord.NewValue, rockContext );
                                changeRecord.RelatedEntityId = targetEntity.Id;
                            }
                        }
                        if ( changeRecord.Property.IsNotNullOrWhiteSpace() )
                        {
                            PropertyInfo prop = targetEntity.GetType().GetProperty( changeRecord.Property, BindingFlags.Public | BindingFlags.Instance );

                            if ( prop.PropertyType.GetInterfaces().Any( i => i.IsInterface && i.GetInterfaces().Contains( typeof( IEntity ) ) ) )
                            {
                                PropertyInfo propId = targetEntity.GetType().GetProperty( changeRecord.Property + "Id", BindingFlags.Public | BindingFlags.Instance );
                                var newObject = changeRecord.NewValue.FromJsonOrNull<BasicEntity>();
                                prop.SetValue( targetEntity, null, null );
                                if ( newObject != null )
                                {
                                    propId.SetValue( targetEntity, newObject.Id );
                                }
                                else
                                {
                                    propId.SetValue( targetEntity, null, null );
                                }
                            }
                            else
                            {
                                SetProperty( targetEntity, prop, changeRecord.NewValue );
                            }
                        }
                        changeRecord.WasApplied = true;
                    }
                    rockContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch ( Exception e )
                {
                    dbContextTransaction.Rollback();
                    throw new Exception( "Exception occured durring saving changes.", e );
                }
            }
        }

        private IEntity CreateNewEntity( int relatedEntityTypeId, string newValue, RockContext dbContext )
        {
            var entityTypeCache = EntityTypeCache.Get( relatedEntityTypeId );
            var entityType = entityTypeCache.GetEntityType();
            var dyn = newValue.FromJsonOrNull<Dictionary<string, object>>();
            var entity = ( ( IEntity ) Activator.CreateInstance( entityType ) );
            foreach ( var key in dyn.Keys )
            {
                var prop = entity.GetType().GetProperty( key );
                SetProperty( entity, prop, dyn[key].ToStringSafe() );
            }

            var entityService = Reflection.GetServiceForEntityType( entityType, dbContext );
            MethodInfo addMethodInfo = entityService.GetType().GetMethod( "Add" );
            object[] parametersArray = new object[] { entity };
            addMethodInfo.Invoke( entityService, parametersArray );
            dbContext.SaveChanges();
            return entity;
        }

        private void SetProperty( IEntity entity, PropertyInfo prop, string newValue )
        {
            if ( prop.PropertyType == typeof( string ) )
            {
                prop.SetValue( entity, newValue, null );
            }
            else if ( prop.PropertyType == typeof( int? ) )
            {
                prop.SetValue( entity, newValue.AsIntegerOrNull(), null );
            }
            else if ( ( prop.PropertyType == typeof( int ) ) )
            {
                prop.SetValue( entity, newValue.AsInteger(), null );
            }
            else if ( prop.PropertyType == typeof( DateTime? ) )
            {
                prop.SetValue( entity, newValue.AsDateTime(), null );
            }
            else if ( prop.PropertyType.IsEnum )
            {
                prop.SetValue( entity, newValue.AsInteger() );
            }
            else if ( prop.PropertyType == typeof( bool ) )
            {
                prop.SetValue( entity, newValue.AsBoolean() );
            }
        }

        private IEntity GetEntity( int entityTypeId, int entityId, RockContext dbContext )
        {
            var entityTypeCache = EntityTypeCache.Get( entityTypeId );
            var entityType = entityTypeCache.GetEntityType();
            var entityService = Reflection.GetServiceForEntityType( entityType, dbContext );
            MethodInfo queryableMethodInfo = entityService.GetType().GetMethod( "Queryable", new Type[] { } );
            IQueryable<IEntity> entityQuery = queryableMethodInfo.Invoke( entityService, null ) as IQueryable<IEntity>;
            var entity = entityQuery.Where( x => x.Id == entityId ).FirstOrDefault();

            if ( entity.TypeName == "Rock.Model.PersonAlias" )
            {
                //The entity is person alias switch to person
                entity = ( ( PersonAlias ) entity ).Person;
            }

            return entity;
        }

        public class BasicEntity : IEntity
        {
            public int Id { get; set; }


            public Guid Guid { get; set; }
            public int? ForeignId { get; set; }
            public Guid? ForeignGuid { get; set; }
            public string ForeignKey { get; set; }

            public int TypeId { get { return 0; } }

            public string TypeName { get { return "BasicEntity"; } }

            public string EncryptedKey { get { return ""; } }

            public string ContextKey { get { return ""; } }


            public List<ValidationResult> ValidationResults { get { return new List<ValidationResult>(); } }

            public bool IsValid { get { return true; } }

            public Dictionary<string, object> AdditionalLavaFields { get; set; }

            public IEntity Clone()
            {
                return this;
            }

            public Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>();
            }
        }

        private void EvaluatePropertyChange( ChangeRequest changeRequest, object item, string property, IEntityCache newValue, bool isRelated = false )
        {
            var oldValue = item.GetPropertyValue( property );
            if ( oldValue == null && newValue == null )
            {
                return;
            }

            if ( !( oldValue is IEntity ) || ( ( IEntity ) oldValue ).Id != newValue.Id )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToJson(),
                    NewValue = newValue.ToJson(),
                    IsAttribute = false,
                    IsRejected = false,
                    Property = property
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
            };
        }

        private void EvaluatePropertyChange( ChangeRequest changeRequest, object item, string property, IEntity newValue, bool isRelated = false )
        {
            var oldValue = item.GetPropertyValue( property );
            if ( oldValue == null && newValue == null )
            {
                return;
            }

            if ( !( oldValue is IEntity ) || ( ( IEntity ) oldValue ).Id != newValue.Id )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToJson(),
                    NewValue = newValue.ToJson(),
                    IsAttribute = false,
                    IsRejected = false,
                    Property = property
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
            };
        }


        private void EvaluatePropertyChange( ChangeRequest changeRequest, object item, string property, string newValue, bool isRelated = false )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue.ToStringSafe().IsNullOrWhiteSpace() && newValue.IsNullOrWhiteSpace() )
            {
                return;
            }

            if ( !( oldValue is string ) || ( string ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToStringSafe(),
                    NewValue = newValue,
                    IsAttribute = false,
                    IsRejected = false,
                    Property = property
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
            };
        }

        private void EvaluatePropertyChange( ChangeRequest changeRequest, object item, string property, int? newValue, bool isRelated = false )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue == null && newValue == null )
            {
                return;
            }

            if ( !( oldValue is int? ) || ( int? ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToStringSafe(),
                    NewValue = newValue.ToStringSafe(),
                    IsAttribute = false,
                    IsRejected = false,
                    Property = property
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
            };
        }

        private void EvaluatePropertyChange( ChangeRequest changeRequest, object item, string property, bool newValue, bool isRelated = false )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( !( oldValue is bool ) || ( bool ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = oldValue.ToStringSafe(),
                    NewValue = newValue.ToStringSafe(),
                    IsAttribute = false,
                    IsRejected = false,
                    Property = property
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
            };
        }

        private void EvaluatePropertyChange( ChangeRequest changeRequest, object item, string property, Enum newValue, bool isRelated = false )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue == null && newValue == null )
            {
                return;
            }

            if ( !( oldValue is Enum ) || !newValue.Equals( ( Enum ) oldValue ) )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = ( ( Enum ) oldValue ).ConvertToInt().ToString(),
                    NewValue = newValue.ConvertToInt().ToString(),
                    IsAttribute = false,
                    IsRejected = false,
                    Property = property
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
            };
        }

        private void EvaluatePropertyChange( ChangeRequest changeRequest, object item, string property, DateTime? newValue, bool isRelated = false )
        {
            var oldValue = item.GetPropertyValue( property );

            if ( oldValue == null && newValue == null )
            {
                return;
            }

            if ( !( oldValue is DateTime? ) || ( DateTime? ) oldValue != newValue )
            {
                var changeRecord = new ChangeRecord()
                {
                    OldValue = ( ( DateTime? ) oldValue ).ToStringSafe(),
                    NewValue = newValue.ToStringSafe(),
                    IsAttribute = false,
                    IsRejected = false,
                    Property = property
                };

                if ( isRelated && item is IEntity )
                {
                    var entity = ( IEntity ) item;

                    changeRecord.RelatedEntityId = entity.Id;
                    changeRecord.RelatedEntityTypeId = EntityTypeCache.Get( entity.GetType() ).Id;
                }

                changeRequest.ChangeRecords.Add( changeRecord );
            };
        }
    }
}