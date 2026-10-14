// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.ViewModels.Blocks.Event.RegistrationEntry;
using Rock.ViewModels.Controls;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// The registration template's forms, the way Rock's own Obsidian RegistrationEntry block
    /// handles them: the form bags the browser renders (Rock's own bag types, so its field
    /// controls and visibility rules work unchanged), a registrant's starting values, and
    /// writing the answers back to the person, the registrant and the group member at
    /// checkout. Answers are keyed by RegistrationTemplateFormField Guid, as in Rock.
    /// Registrants in the cart are always existing family members, so unlike Rock this never
    /// creates or matches a person.
    /// </summary>
    public class RegistrationForms
    {
        private readonly RockContext _rockContext;

        public RegistrationForms( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        #region Forms

        /// <summary>The template's forms as Rock's RegistrationEntry sends them to the browser.</summary>
        public List<RegistrationEntryFormBag> GetForms( RegistrationTemplate template )
        {
            var forms = template.Forms.OrderBy( f => f.Order ).ToList();
            var allAttributeFields = forms
                .SelectMany( f => f.Fields.Where( ff => !ff.IsInternal && ff.Attribute?.IsActive == true ) )
                .ToList();

            return forms.Select( form => new RegistrationEntryFormBag
            {
                Fields = VisibleFields( form )
                    .Select( field => ToBag( field, allAttributeFields ) )
                    .ToList()
            } ).ToList();
        }

        /// <summary>
        /// Drop-down choices for the person fields the forms use, built as Rock's RegistrationEntry
        /// builds them (values are Guids, which is what the answers carry).
        /// </summary>
        public static ViewModels.FormListsBag GetFormLists( RegistrationTemplate template )
        {
            var types = new HashSet<RegistrationPersonFieldType>( template.Forms
                .SelectMany( VisibleFields )
                .Where( f => f.FieldSource == RegistrationFieldSource.PersonField )
                .Select( f => f.PersonFieldType ) );
            var lists = new ViewModels.FormListsBag();

            if ( types.Contains( RegistrationPersonFieldType.Campus ) )
            {
                lists.Campuses = CampusCache.All()
                    .Where( c => c.IsActive ?? true )
                    .OrderBy( c => c.Order )
                    .Select( c => new ListItemBag { Value = c.Guid.ToString(), Text = c.Name } )
                    .ToList();
            }

            if ( types.Contains( RegistrationPersonFieldType.Grade ) )
            {
                lists.Grades = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SCHOOL_GRADES.AsGuid() ).DefinedValues
                    .OrderByDescending( v => v.Value.AsInteger() )
                    .Select( v => new ListItemBag { Value = v.Guid.ToString(), Text = v.Description } )
                    .ToList();
            }

            lists.MaritalStatuses = types.Contains( RegistrationPersonFieldType.MaritalStatus ) ? DefinedValues( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS ) : lists.MaritalStatuses;
            lists.ConnectionStatuses = types.Contains( RegistrationPersonFieldType.ConnectionStatus ) ? DefinedValues( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS ) : lists.ConnectionStatuses;
            lists.Races = types.Contains( RegistrationPersonFieldType.Race ) ? DefinedValues( Rock.SystemGuid.DefinedType.PERSON_RACE ) : lists.Races;
            lists.Ethnicities = types.Contains( RegistrationPersonFieldType.Ethnicity ) ? DefinedValues( Rock.SystemGuid.DefinedType.PERSON_ETHNICITY ) : lists.Ethnicities;
            return lists;
        }

        private static List<ListItemBag> DefinedValues( string definedTypeGuid ) =>
            DefinedTypeCache.Get( definedTypeGuid.AsGuid() )?.DefinedValues
                .Where( v => v.IsActive )
                .Select( v => new ListItemBag { Value = v.Guid.ToString(), Text = v.Value } )
                .ToList() ?? new List<ListItemBag>();

        /// <summary>Whether the template asks anything at all (so the cart can skip the step).</summary>
        public static bool HasFields( RegistrationTemplate template ) =>
            template.Forms.Any( f => VisibleFields( f ).Any() );

        private static IEnumerable<RegistrationTemplateFormField> VisibleFields( RegistrationTemplateForm form ) =>
            form.Fields
                .Where( f => !f.IsInternal && ( f.Attribute == null || f.Attribute.IsActive ) )
                .OrderBy( f => f.Order );

        private static RegistrationEntryFormFieldBag ToBag( RegistrationTemplateFormField field, List<RegistrationTemplateFormField> allAttributeFields )
        {
            var attribute = field.AttributeId.HasValue ? AttributeCache.Get( field.AttributeId.Value ) : null;

            return new RegistrationEntryFormFieldBag
            {
                Guid = field.Guid,
                Attribute = attribute != null ? PublicAttributeForEdit( attribute ) : null,
                FieldSource = field.FieldSource,
                PersonFieldType = field.PersonFieldType,
                IsRequired = field.IsRequired,
                IsSharedValue = field.IsSharedValue,
                VisibilityRuleType = field.FieldVisibilityRules.FilterExpressionType,
                PreHtml = field.PreText,
                PostHtml = field.PostText,
                ShowOnWaitList = field.ShowOnWaitlist,
                IsLockedIfValuesExist = field.IsLockedIfValuesExist,
                VisibilityRules = field.FieldVisibilityRules.RuleList
                    .Select( rule => ToVisibilityBag( rule, allAttributeFields ) )
                    .Where( rule => rule != null )
                    .ToList()
            };
        }

        /// <summary>Same translation Rock's RegistrationEntry does, so the browser can evaluate the rule.</summary>
        private static RegistrationEntryVisibilityBag ToVisibilityBag( Rock.Field.FieldVisibilityRule rule, List<RegistrationTemplateFormField> allAttributeFields )
        {
            if ( !rule.ComparedToFormFieldGuid.HasValue )
            {
                return null;
            }

            var comparedToField = allAttributeFields.SingleOrDefault( f => f.Guid == rule.ComparedToFormFieldGuid );
            var attribute = comparedToField?.AttributeId != null ? AttributeCache.Get( comparedToField.AttributeId.Value ) : null;
            var fieldType = attribute?.FieldType?.Field;
            if ( fieldType == null )
            {
                return null;
            }

            var filterValues = new List<string>();
            var comparisonType = rule.ComparisonType.ConvertToString( false );
            if ( comparisonType != null )
            {
                filterValues.Add( comparisonType );
            }

            filterValues.Add( rule.ComparedToValue );
            var comparison = fieldType.GetPublicFilterValue( filterValues.ToJson(), attribute.ConfigurationValues );

            return new RegistrationEntryVisibilityBag
            {
                ComparedToRegistrationTemplateFormFieldGuid = rule.ComparedToFormFieldGuid.Value,
                ComparisonValue = new PublicComparisonValueBag
                {
                    ComparisonType = ( int? ) comparison.ComparisonType,
                    Value = comparison.Value
                }
            };
        }

        #endregion

        #region Starting values

        /// <summary>
        /// A registrant's starting answers, as Rock's RegistrationEntry fills them for a family
        /// member: the person's current values for fields that show them (and always first and
        /// last name), registrant attribute defaults, and group member attributes from the
        /// person's membership in <paramref name="groupId"/> when they already have one.
        /// </summary>
        public Dictionary<Guid, object> GetStartingValues( RegistrationTemplate template, Person person, int? groupId )
        {
            var values = new Dictionary<Guid, object>();
            GroupMember groupMember = null;
            if ( groupId.HasValue && person != null )
            {
                groupMember = new GroupMemberService( _rockContext ).GetByGroupIdAndPersonId( groupId.Value, person.Id ).FirstOrDefault();
            }

            foreach ( var field in template.Forms.SelectMany( VisibleFields ) )
            {
                object value = null;
                switch ( field.FieldSource )
                {
                    case RegistrationFieldSource.PersonField:
                        var isName = field.PersonFieldType == RegistrationPersonFieldType.FirstName || field.PersonFieldType == RegistrationPersonFieldType.LastName;
                        if ( isName || field.ShowCurrentValue || field.IsLockedIfValuesExist )
                        {
                            value = PersonFieldValue( person, field.PersonFieldType );
                        }

                        break;

                    case RegistrationFieldSource.PersonAttribute:
                        if ( field.ShowCurrentValue || field.IsLockedIfValuesExist )
                        {
                            value = AttributeEditValue( person, field );
                        }

                        break;

                    case RegistrationFieldSource.RegistrantAttribute:
                        value = AttributeEditValue( null, field ); // a new registrant: the attribute's default
                        break;

                    case RegistrationFieldSource.GroupMemberAttribute:
                        value = groupMember != null ? AttributeEditValue( groupMember, field ) : AttributeEditValue( null, field );
                        break;
                }

                if ( value != null )
                {
                    values[field.Guid] = value;
                }
            }

            return values;
        }

        private object PersonFieldValue( Person person, RegistrationPersonFieldType type )
        {
            if ( person == null )
            {
                return null;
            }

            switch ( type )
            {
                case RegistrationPersonFieldType.FirstName:
                    return person.NickName.IsNullOrWhiteSpace() ? person.FirstName : person.NickName;
                case RegistrationPersonFieldType.LastName:
                    return person.LastName;
                case RegistrationPersonFieldType.MiddleName:
                    return person.MiddleName;
                case RegistrationPersonFieldType.Email:
                    return person.Email;
                case RegistrationPersonFieldType.Campus:
                    return person.GetFamily( _rockContext )?.Campus?.Guid;
                case RegistrationPersonFieldType.Gender:
                    return person.Gender.ConvertToInt().ToString();
                case RegistrationPersonFieldType.Birthdate:
                    return new BirthdayPickerBag { Year = person.BirthYear ?? 0, Month = person.BirthMonth ?? 0, Day = person.BirthDay ?? 0 };
                case RegistrationPersonFieldType.AnniversaryDate:
                    return new BirthdayPickerBag { Year = person.AnniversaryDate?.Year ?? 0, Month = person.AnniversaryDate?.Month ?? 0, Day = person.AnniversaryDate?.Day ?? 0 };
                case RegistrationPersonFieldType.Address:
                    var location = person.GetHomeLocation( _rockContext );
                    return new AddressControlBag
                    {
                        Street1 = location?.Street1 ?? string.Empty,
                        Street2 = location?.Street2 ?? string.Empty,
                        City = location?.City ?? string.Empty,
                        State = location?.State ?? string.Empty,
                        PostalCode = location?.PostalCode ?? string.Empty,
                        Country = location?.Country ?? string.Empty
                    };
                case RegistrationPersonFieldType.MaritalStatus:
                    return DefinedValueGuid( person.MaritalStatusValueId );
                case RegistrationPersonFieldType.ConnectionStatus:
                    return DefinedValueGuid( person.ConnectionStatusValueId );
                case RegistrationPersonFieldType.Grade:
                    var gradeOffset = ( person.GradeOffset ?? -1 ).ToString();
                    return DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SCHOOL_GRADES ).DefinedValues
                        .FirstOrDefault( v => v.Value == gradeOffset )?.Guid.ToString() ?? string.Empty;
                case RegistrationPersonFieldType.HomePhone:
                    return person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() )?.Number;
                case RegistrationPersonFieldType.WorkPhone:
                    return person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() )?.Number;
                case RegistrationPersonFieldType.MobilePhone:
                    return person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() )?.Number;
                case RegistrationPersonFieldType.Race:
                    return DefinedValueGuid( person.RaceValueId );
                case RegistrationPersonFieldType.Ethnicity:
                    return DefinedValueGuid( person.EthnicityValueId );
            }

            return null;
        }

        private static string DefinedValueGuid( int? definedValueId ) =>
            definedValueId.HasValue ? DefinedValueCache.Get( definedValueId.Value )?.Guid.ToString() ?? string.Empty : string.Empty;

        private string AttributeEditValue( IHasAttributes entity, RegistrationTemplateFormField field )
        {
            var attribute = AttributeCache.Get( field.AttributeId ?? 0 );
            if ( attribute == null )
            {
                return null;
            }

            if ( entity == null )
            {
                return PublicEditValue( attribute, attribute.DefaultValue );
            }

            entity.LoadAttributes( _rockContext );
            return PublicEditValue( attribute, entity.GetAttributeValue( attribute.Key ) );
        }

        #endregion

        #region Storing answers

        /// <summary>Answers as stored on the cart line.</summary>
        public static string ToJson( Dictionary<Guid, object> values ) => ( values ?? new Dictionary<Guid, object>() ).ToJson();

        /// <summary>
        /// Answers read back from the cart line. Text values come back as strings and objects
        /// (address, birthday, phone) as their JSON, which is what Rock's own save logic expects.
        /// </summary>
        public static Dictionary<Guid, object> FromJson( string json )
        {
            var values = new Dictionary<Guid, object>();
            if ( json.IsNullOrWhiteSpace() )
            {
                return values;
            }

            foreach ( var property in JObject.Parse( json ).Properties() )
            {
                var guid = property.Name.AsGuidOrNull();
                if ( !guid.HasValue || property.Value.Type == JTokenType.Null )
                {
                    continue;
                }

                values[guid.Value] = property.Value is JValue scalar ? scalar.Value?.ToString() : property.Value.ToString( Newtonsoft.Json.Formatting.None );
            }

            return values;
        }

        #endregion

        #region Public attribute values (Rock's PublicAttributeHelper is internal)

        private static Rock.Field.IFieldType FieldTypeOf( AttributeCache attribute ) =>
            attribute.FieldType?.Field ?? new Rock.Field.Types.TextFieldType();

        /// <summary>What Rock's internal PublicAttributeHelper.GetPublicAttributeForEdit builds.</summary>
        private static PublicAttributeBag PublicAttributeForEdit( AttributeCache attribute ) => new PublicAttributeBag
        {
            FieldTypeGuid = ControlFieldTypeGuid( attribute.FieldType ),
            AttributeGuid = attribute.Guid,
            Name = attribute.Name,
            Categories = attribute.Categories.OrderBy( c => c.Order ).Select( c => new PublicAttributeCategoryBag
            {
                Guid = c.Guid,
                Name = c.Name,
                Order = c.Order
            } ).ToList(),
            Order = attribute.Order,
            Key = attribute.Key,
            IsRequired = attribute.IsRequired,
            Description = attribute.Description,
            ConfigurationValues = FieldTypeOf( attribute ).GetPublicConfigurationValues( attribute.ConfigurationValues, Rock.Field.ConfigurationValueUsage.Edit, null ),
            PreHtml = attribute.PreHtml,
            PostHtml = attribute.PostHtml
        };

        private static readonly PropertyInfo ControlFieldTypeGuidProperty =
            typeof( FieldTypeCache ).GetProperty( "ControlFieldTypeGuid", BindingFlags.Instance | BindingFlags.NonPublic );

        /// <summary>
        /// The field type the browser renders: Rock's internal FieldTypeCache.ControlFieldTypeGuid
        /// (a "universal" field type's control), read by reflection; else the field type itself.
        /// </summary>
        private static Guid ControlFieldTypeGuid( FieldTypeCache fieldType ) =>
            ControlFieldTypeGuidProperty?.GetValue( fieldType ) as Guid? ?? fieldType.Guid;

        private static string PublicEditValue( AttributeCache attribute, string privateValue ) =>
            FieldTypeOf( attribute ).GetPublicEditValue( privateValue, attribute.ConfigurationValues );

        private static string PrivateValue( AttributeCache attribute, string publicValue ) =>
            FieldTypeOf( attribute ).GetPrivateEditValue( publicValue, attribute.ConfigurationValues );

        #endregion

        #region Writing answers at checkout

        /// <summary>
        /// Writes the person field and person attribute answers to the registrant's person (and
        /// the family's campus and home address), and the registrant attribute answers to the
        /// registrant, honouring "lock if values exist". Mirrors Rock's RegistrationEntry.
        /// The registrant must already be saved.
        /// </summary>
        public void ApplyToPersonAndRegistrant( RegistrationTemplate template, Person person, RegistrationRegistrant registrant, Dictionary<Guid, object> values )
        {
            var fields = template.Forms.SelectMany( f => f.Fields ).ToList();
            var personChanges = new History.HistoryChangeList();
            var (campusId, address) = ApplyPersonFields( fields, person, values, personChanges );
            SaveFamilyCampusAndAddress( person, campusId, address );
            _rockContext.SaveChanges();

            if ( ApplyPersonAttributes( template, person, values ) )
            {
                person.SaveAttributeValues( _rockContext );
            }

            registrant.LoadAttributes( _rockContext );
            if ( ApplyAttributes( fields, RegistrationFieldSource.RegistrantAttribute, registrant, values, writeBlanks: true ) )
            {
                registrant.SaveAttributeValues( _rockContext );
            }

            if ( personChanges.Any() )
            {
                HistoryService.SaveChanges( _rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(), person.Id, personChanges, true );
            }
        }

        /// <summary>Writes the group member attribute answers once the registrant is in the group.</summary>
        public void ApplyToGroupMember( RegistrationTemplate template, GroupMember groupMember, Dictionary<Guid, object> values )
        {
            if ( ApplyGroupMemberAttributes( template, groupMember, values ) )
            {
                groupMember.SaveAttributeValues( _rockContext );
            }
        }

        /// <summary>
        /// Sets the person field answers on <paramref name="person"/> in memory (nothing saved) and
        /// returns the campus and home address the answers ask for, which belong to the family.
        /// Used to render the signature document as the person will be once registered.
        /// </summary>
        public (int? CampusId, Location Address) ApplyPersonFields( RegistrationTemplate template, Person person, Dictionary<Guid, object> values ) =>
            ApplyPersonFields( template.Forms.SelectMany( f => f.Fields ).ToList(), person, values, new History.HistoryChangeList() );

        /// <summary>Sets the person attribute answers on <paramref name="person"/> in memory. True when one changed.</summary>
        public bool ApplyPersonAttributes( RegistrationTemplate template, Person person, Dictionary<Guid, object> values )
        {
            person.LoadAttributes( _rockContext );
            return ApplyAttributes( template.Forms.SelectMany( f => f.Fields ).ToList(), RegistrationFieldSource.PersonAttribute, person, values );
        }

        /// <summary>Sets the group member attribute answers on <paramref name="groupMember"/> in memory. True when one changed.</summary>
        public bool ApplyGroupMemberAttributes( RegistrationTemplate template, GroupMember groupMember, Dictionary<Guid, object> values )
        {
            groupMember.LoadAttributes( _rockContext );
            return ApplyAttributes( template.Forms.SelectMany( f => f.Fields ).ToList(), RegistrationFieldSource.GroupMemberAttribute, groupMember, values );
        }

        /// <summary>
        /// The registrant attribute answers as attribute caches and values, for Lava (the signature
        /// document's Registrant), the way Rock's RegistrationEntry builds them.
        /// </summary>
        public static (Dictionary<string, AttributeCache> Attributes, Dictionary<string, AttributeValueCache> Values) RegistrantAttributes( RegistrationTemplate template, Dictionary<Guid, object> values )
        {
            var attributes = new Dictionary<string, AttributeCache>();
            var attributeValues = new Dictionary<string, AttributeValueCache>();
            foreach ( var field in template.Forms.SelectMany( f => f.Fields ).Where( f => f.AttributeId.HasValue && f.FieldSource == RegistrationFieldSource.RegistrantAttribute ) )
            {
                var attribute = AttributeCache.Get( field.AttributeId.Value );
                if ( attribute == null || attributes.ContainsKey( attribute.Key ) )
                {
                    continue;
                }

                attributes.Add( attribute.Key, attribute );
                attributeValues.Add( attribute.Key, new AttributeValueCache( attribute.Id, null, PrivateValue( attribute, values.GetValueOrNull( field.Guid ).ToStringSafe() ) ) );
            }

            return (attributes, attributeValues);
        }

        /// <summary>
        /// Sets each answered attribute field of <paramref name="source"/> on the entity. Rock
        /// writes registrant attributes even when unanswered (blank), the others only when answered.
        /// </summary>
        private static bool ApplyAttributes( List<RegistrationTemplateFormField> fields, RegistrationFieldSource source, IHasAttributes entity, Dictionary<Guid, object> values, bool writeBlanks = false )
        {
            var isChanged = false;
            foreach ( var field in fields.Where( f => f.FieldSource == source && f.AttributeId.HasValue ) )
            {
                var attribute = AttributeCache.Get( field.AttributeId.Value );
                var answer = values.GetValueOrNull( field.Guid );
                if ( attribute == null || ( answer == null && !writeBlanks ) )
                {
                    continue;
                }

                var original = entity.GetAttributeValue( attribute.Key );
                if ( field.IsLockedIfValuesExist && original.IsNotNullOrWhiteSpace() )
                {
                    continue;
                }

                var updated = PrivateValue( attribute, answer.ToStringSafe() );
                entity.SetAttributeValue( attribute.Key, updated );
                isChanged |= ( original ?? string.Empty ).Trim() != ( updated ?? string.Empty ).Trim();
            }

            return isChanged;
        }

        private (int? CampusId, Location Address) ApplyPersonFields( List<RegistrationTemplateFormField> fields, Person person, Dictionary<Guid, object> values, History.HistoryChangeList changes )
        {
            int? campusId = null;
            Location address = null;

            foreach ( var field in fields.Where( f => f.FieldSource == RegistrationFieldSource.PersonField ) )
            {
                var answer = values.GetValueOrNull( field.Guid );
                if ( answer == null )
                {
                    continue;
                }

                var text = answer.ToStringSafe();
                switch ( field.PersonFieldType )
                {
                    // First and last name identify the family member being registered; like Rock,
                    // an existing person's name isn't changed from a registration form.
                    case RegistrationPersonFieldType.Email:
                        if ( Unlocked( field, person.Email ) )
                        {
                            History.EvaluateChange( changes, "Email", person.Email, text.Trim() );
                            person.Email = text.Trim();
                        }

                        break;

                    case RegistrationPersonFieldType.MiddleName:
                        if ( Unlocked( field, person.MiddleName ) )
                        {
                            History.EvaluateChange( changes, "Middle Name", person.MiddleName, text.Trim() );
                            person.MiddleName = text.Trim();
                        }

                        break;

                    case RegistrationPersonFieldType.Campus:
                        if ( !field.IsLockedIfValuesExist || !person.PrimaryCampusId.HasValue )
                        {
                            var campusGuid = text.AsGuidOrNull();
                            campusId = campusGuid.HasValue ? CampusCache.Get( campusGuid.Value )?.Id : null;
                        }

                        break;

                    case RegistrationPersonFieldType.Address:
                        if ( !field.IsLockedIfValuesExist || person.GetHomeLocation( _rockContext ) == null )
                        {
                            var bag = text.FromJsonOrNull<AddressControlBag>();
                            if ( bag != null )
                            {
                                address = new Location
                                {
                                    Street1 = bag.Street1,
                                    Street2 = bag.Street2,
                                    City = bag.City,
                                    State = bag.State,
                                    PostalCode = bag.PostalCode,
                                    Country = bag.Country ?? GlobalAttributesCache.Get().OrganizationCountry
                                };
                            }
                        }

                        break;

                    case RegistrationPersonFieldType.Birthdate:
                        if ( !field.IsLockedIfValuesExist || !person.BirthDate.HasValue )
                        {
                            var birthDate = text.FromJsonOrNull<BirthdayPickerBag>()?.ToDateTime();
                            if ( birthDate.HasValue )
                            {
                                History.EvaluateChange( changes, "Birth Date", person.BirthDate, birthDate );
                                person.SetBirthDate( birthDate );
                            }
                        }

                        break;

                    case RegistrationPersonFieldType.AnniversaryDate:
                        if ( !field.IsLockedIfValuesExist || !person.AnniversaryDate.HasValue )
                        {
                            var anniversary = text.FromJsonOrNull<BirthdayPickerBag>()?.ToDateTime();
                            History.EvaluateChange( changes, "Anniversary Date", person.AnniversaryDate, anniversary );
                            person.AnniversaryDate = anniversary;
                        }

                        break;

                    case RegistrationPersonFieldType.Gender:
                        if ( !field.IsLockedIfValuesExist || person.Gender == Gender.Unknown )
                        {
                            var gender = text.ConvertToEnumOrNull<Gender>() ?? Gender.Unknown;
                            History.EvaluateChange( changes, "Gender", person.Gender, gender );
                            person.Gender = gender;
                        }

                        break;

                    case RegistrationPersonFieldType.Grade:
                        if ( !field.IsLockedIfValuesExist || !person.GradeOffset.HasValue )
                        {
                            var gradeGuid = text.AsGuidOrNull();
                            var gradeOffset = gradeGuid.HasValue ? DefinedValueCache.Get( gradeGuid.Value )?.Value.AsIntegerOrNull() : null;
                            var graduationYear = Person.GraduationYearFromGradeOffset( gradeOffset );

                            // Don't wipe out a past graduation year when a blank grade is picked.
                            if ( graduationYear.HasValue || ( person.GradeOffset.HasValue && person.GradeOffset >= 0 ) )
                            {
                                History.EvaluateChange( changes, "Graduation Year", person.GraduationYear, graduationYear );
                                person.GraduationYear = graduationYear;
                            }
                        }

                        break;

                    case RegistrationPersonFieldType.MaritalStatus:
                        if ( !field.IsLockedIfValuesExist || !person.MaritalStatusValueId.HasValue )
                        {
                            person.MaritalStatusValueId = DefinedValueId( text );
                        }

                        break;

                    case RegistrationPersonFieldType.ConnectionStatus:
                        if ( !field.IsLockedIfValuesExist || !person.ConnectionStatusValueId.HasValue )
                        {
                            person.ConnectionStatusValueId = DefinedValueId( text );
                        }

                        break;

                    case RegistrationPersonFieldType.Race:
                        if ( !field.IsLockedIfValuesExist || !person.RaceValueId.HasValue )
                        {
                            person.RaceValueId = DefinedValueId( text );
                        }

                        break;

                    case RegistrationPersonFieldType.Ethnicity:
                        if ( !field.IsLockedIfValuesExist || !person.EthnicityValueId.HasValue )
                        {
                            person.EthnicityValueId = DefinedValueId( text );
                        }

                        break;

                    case RegistrationPersonFieldType.MobilePhone:
                        SavePhone( field, person, answer, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE, changes );
                        break;

                    case RegistrationPersonFieldType.HomePhone:
                        SavePhone( field, person, answer, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME, changes );
                        break;

                    case RegistrationPersonFieldType.WorkPhone:
                        SavePhone( field, person, answer, Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK, changes );
                        break;
                }
            }

            return (campusId, address);
        }

        private static bool Unlocked( RegistrationTemplateFormField field, string current ) =>
            !field.IsLockedIfValuesExist || current.IsNullOrWhiteSpace();

        private static int? DefinedValueId( string guidText )
        {
            var guid = guidText.AsGuidOrNull();
            return guid.HasValue ? DefinedValueCache.Get( guid.Value )?.Id : null;
        }

        private void SavePhone( RegistrationTemplateFormField field, Person person, object answer, string phoneTypeGuid, History.HistoryChangeList changes )
        {
            var numberType = DefinedValueCache.Get( phoneTypeGuid.AsGuid() );
            if ( numberType == null )
            {
                return;
            }

            var phone = person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberType.Id );
            if ( field.IsLockedIfValuesExist && phone?.Number.IsNotNullOrWhiteSpace() == true )
            {
                return;
            }

            // A plain number, or the SMS control's { number, isMessagingEnabled }.
            var text = answer.ToStringSafe();
            var withSms = text.FromJsonOrNull<PhoneNumberBoxWithSmsControlBag>();
            var number = PhoneNumber.CleanNumber( withSms?.Number ?? text );
            if ( number.IsNullOrWhiteSpace() )
            {
                return;
            }

            if ( phone == null )
            {
                phone = new PhoneNumber { NumberTypeValueId = numberType.Id };
                person.PhoneNumbers.Add( phone );
            }

            History.EvaluateChange( changes, $"{numberType.Value} Phone", phone.Number, number );
            phone.Number = number;
            if ( withSms != null )
            {
                phone.IsMessagingEnabled = withSms.IsMessagingEnabled;
            }
        }

        /// <summary>The family's campus (when it has none, or the form set one) and home address, as Rock's RegistrationEntry saves them.</summary>
        private void SaveFamilyCampusAndAddress( Person person, int? campusId, Location address )
        {
            var family = person.GetFamily( _rockContext );
            if ( family == null )
            {
                return;
            }

            if ( campusId.HasValue && family.CampusId != campusId )
            {
                family.CampusId = campusId;
            }

            if ( address != null && address.IsMinimumViableAddress() )
            {
                var existing = new LocationService( _rockContext ).Get( address.Street1, address.Street2, address.City, address.State, address.PostalCode, address.Country, family, true, false );
                if ( existing != null )
                {
                    GroupService.AddNewGroupAddress( _rockContext, family, Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME, existing );
                }
                else
                {
                    GroupService.AddNewGroupAddress( _rockContext, family, Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME,
                        address.Street1, address.Street2, address.City, address.State, address.PostalCode, address.Country, true );
                }
            }
        }

        #endregion
    }
}
