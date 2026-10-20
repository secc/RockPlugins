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
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.ElectronicSignature;
using Rock.Model;
using Rock.Pdf;
using Rock.Security;
using Rock.ViewModels.Blocks.Event.RegistrationEntry;
using Rock.ViewModels.Controls;
using Rock.Web.Cache;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// The template's inline signature document (Rock's "embed" signature action), handled the
    /// way Rock's Obsidian RegistrationEntry handles it: the document is rendered from the
    /// registrant's answers, signed in the browser with Rock's electronic signature control, and
    /// the signed data comes back encrypted, bound by a hash to the camper, the answers and the
    /// HTML that was signed. At checkout the document is rendered and checked before the card is
    /// charged (<see cref="Prepare"/>), saved with the registration in the same transaction
    /// (<see cref="Attach"/>), and its PDF made once the registrations commit
    /// (<see cref="Complete"/>), so a PDF failure never undoes a registration someone paid for
    /// and every paid registration has its signed document. The maintenance job retries PDFs
    /// that failed.
    /// </summary>
    public class RegistrationSignatures
    {
        /// <summary>CartLineItem.SignatureData prefix for a still-valid document the person signed before.</summary>
        private const string ExistingPrefix = "existing:";

        private readonly RockContext _rockContext;

        public RegistrationSignatures( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>The template's signature document when it must be signed inline, else null.</summary>
        public SignatureDocumentTemplate RequiredDocument( RegistrationTemplate template )
        {
            if ( !template.RequiredSignatureDocumentTemplateId.HasValue || template.SignatureDocumentAction != SignatureDocumentAction.Embed )
            {
                return null;
            }

            var document = new SignatureDocumentTemplateService( _rockContext ).Get( template.RequiredSignatureDocumentTemplateId.Value );
            return document != null && document.IsActive ? document : null;
        }

        /// <summary>
        /// The document to sign for <paramref name="person"/> with these answers, or the guid of a
        /// document they signed recently enough that it is still valid (then nothing is shown).
        /// </summary>
        public RegistrationEntrySignatureDocumentBag GetDocument( RegistrationInstance instance, Person registrar, Person person, Dictionary<Guid, object> answers )
        {
            var template = RequiredDocument( instance.RegistrationTemplate );
            if ( template == null )
            {
                return null;
            }

            var existing = ExistingValidDocumentGuid( template, person );
            if ( existing.HasValue )
            {
                return new RegistrationEntrySignatureDocumentBag { ExistingSignatureDocumentGuid = existing };
            }

            var html = RenderDocument( instance, template, person, answers );
            var token = new[] { RockDateTime.Now.ToString( "o" ), Hash( SignedFor( person, instance, answers ) + html ) }.ToJson();

            return new RegistrationEntrySignatureDocumentBag
            {
                DocumentHtml = html,
                SecurityToken = Encryption.EncryptString( token )
            };
        }

        /// <summary>
        /// Checks the signed HTML is the one rendered for this camper, option and answers and
        /// returns the signed data, encrypted, to keep on the cart line. Null when the token
        /// doesn't match.
        /// </summary>
        public string Sign( Person person, RegistrationInstance instance, Dictionary<Guid, object> answers, string documentHtml, string securityToken, ElectronicSignatureValueViewModel signature, string ipAddress, string userAgent )
        {
            var token = Encryption.DecryptString( securityToken ).FromJsonOrNull<List<string>>();
            if ( person == null || instance == null || token == null || token.Count != 2 || token[1] != Hash( SignedFor( person, instance, answers ) + documentHtml ) || signature == null )
            {
                return null;
            }

            return Encryption.EncryptString( new SignedData
            {
                SignedFor = Hash( SignedFor( person, instance, answers ) ),
                DocumentHtml = documentHtml,
                SignatureData = signature.SignatureData,
                SignedByName = signature.SignedByName,
                SignedByEmail = signature.SignedByEmail,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                SignedDateTime = RockDateTime.Now
            }.ToJson() );
        }

        /// <summary>The value to keep on the cart line for a still-valid earlier document.</summary>
        public static string ForExistingDocument( Guid signatureDocumentGuid ) => ExistingPrefix + signatureDocumentGuid;

        /// <summary>
        /// Whether the cart line's signature value is usable: it names a still-valid earlier
        /// document, or it was signed for exactly this camper, option and answers (so changed
        /// answers need a new signature).
        /// </summary>
        public bool IsUsable( RegistrationTemplate template, Person person, string signatureData, int registrationInstanceId, Dictionary<Guid, object> answers )
        {
            if ( signatureData.IsNullOrWhiteSpace() )
            {
                return false;
            }

            if ( signatureData.StartsWith( ExistingPrefix ) )
            {
                return ExistingValidDocumentGuid( RequiredDocument( template ), person ) == signatureData.Substring( ExistingPrefix.Length ).AsGuidOrNull();
            }

            var signed = Encryption.DecryptString( signatureData ).FromJsonOrNull<SignedData>();
            return signed != null && signed.SignedFor == Hash( SignedFor( person.Id, registrationInstanceId, template, answers ) );
        }

        /// <summary>A cart line's signed document, ready to save with its registration.</summary>
        public class PreparedDocument
        {
            /// <summary>A new document (not saved, no PDF yet).</summary>
            public SignatureDocument Document { get; set; }

            /// <summary>A still-valid document the person signed before, to link instead.</summary>
            public int? ExistingDocumentId { get; set; }

            /// <summary>Why the document can't be saved; checkout stops before the charge.</summary>
            public string Error { get; set; }
        }

        /// <summary>
        /// Before the charge: the line's signed document, rendered again from the saved answers
        /// (rather than trusting what the browser sent, as Rock does) and checked against Rock's
        /// rules, so saving it with the paid registration can't fail. Null when the template has
        /// no inline document.
        /// </summary>
        public PreparedDocument Prepare( RegistrationInstance instance, Person registrar, Person person, string signatureData, Dictionary<Guid, object> answers )
        {
            var template = RequiredDocument( instance.RegistrationTemplate );
            if ( template == null )
            {
                return null;
            }

            if ( signatureData.IsNullOrWhiteSpace() )
            {
                return new PreparedDocument { Error = "The document hasn't been signed." };
            }

            if ( signatureData.StartsWith( ExistingPrefix ) )
            {
                var existingId = new SignatureDocumentService( _rockContext ).GetId( signatureData.Substring( ExistingPrefix.Length ).AsGuid() );
                return existingId.HasValue
                    ? new PreparedDocument { ExistingDocumentId = existingId }
                    : new PreparedDocument { Error = "The document signed earlier can't be found. Please sign it again." };
            }

            var signed = Encryption.DecryptString( signatureData ).FromJsonOrNull<SignedData>();
            if ( signed == null )
            {
                return new PreparedDocument { Error = "The signature can't be read. Please sign the document again." };
            }

            var document = CreateDocument( template, signed, instance, registrar, person, answers );
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            if ( !System.ComponentModel.DataAnnotations.Validator.TryValidateObject( document, new System.ComponentModel.DataAnnotations.ValidationContext( document ), results, true ) )
            {
                return new PreparedDocument { Error = "The signed document can't be saved: " + results.Select( r => r.ErrorMessage ).JoinStrings( " " ) };
            }

            return new PreparedDocument { Document = document };
        }

        /// <summary>Links the prepared document to the registrant (adding a new one); saved with the registration.</summary>
        public void Attach( RegistrationRegistrant registrant, PreparedDocument prepared )
        {
            if ( prepared?.ExistingDocumentId != null )
            {
                registrant.SignatureDocumentId = prepared.ExistingDocumentId;
            }
            else if ( prepared?.Document != null )
            {
                new SignatureDocumentService( _rockContext ).Add( prepared.Document );
                registrant.SignatureDocument = prepared.Document;
            }
        }

        /// <summary>
        /// Makes a saved document's PDF (from its stored signed text and signature, as Rock's
        /// RegistrationEntry does when it saves one) and then sends the template's completion
        /// communication, which carries the PDF. Does nothing for a document that has its PDF.
        /// True when it made one.
        /// </summary>
        public bool Complete( int signatureDocumentId )
        {
            var document = new SignatureDocumentService( _rockContext ).Queryable()
                .Include( d => d.SignatureDocumentTemplate )
                .Include( d => d.SignedByPersonAlias.Person )
                .FirstOrDefault( d => d.Id == signatureDocumentId );
            if ( document == null || document.BinaryFileId.HasValue || document.SignedDocumentText.IsNullOrWhiteSpace() )
            {
                return false;
            }

            var template = document.SignatureDocumentTemplate;
            var signatureInformationHtml = ElectronicSignatureHelper.GetSignatureInformationHtml( new GetSignatureInformationHtmlOptions
            {
                SignatureType = template.SignatureType,
                SignedName = document.SignedName,
                DrawnSignatureDataUrl = document.SignatureData,
                SignedByPerson = document.SignedByPersonAlias?.Person,
                SignedDateTime = document.SignedDateTime,
                SignedClientIp = document.SignedClientIp,
                SignatureVerificationHash = document.SignatureVerificationHash
            } );
            var signedHtml = ElectronicSignatureHelper.GetSignedDocumentHtml( document.SignedDocumentText, signatureInformationHtml );

            using ( var pdfGenerator = new PdfGenerator() )
            {
                var binaryFileTypeId = template.BinaryFileTypeId ?? BinaryFileTypeCache.GetId( Rock.SystemGuid.BinaryFiletype.DIGITALLY_SIGNED_DOCUMENTS.AsGuid() );
                document.BinaryFile = pdfGenerator.GetAsBinaryFileFromHtml( binaryFileTypeId ?? 0, document.Name, signedHtml );
                document.BinaryFile.ParentEntityId = template.Id;
                document.BinaryFile.ParentEntityTypeId = EntityTypeCache.Get<SignatureDocumentTemplate>().Id;
                document.BinaryFile.IsTemporary = false;
            }

            _rockContext.SaveChanges();

            if ( template.CompletionSystemCommunicationId.HasValue )
            {
                ElectronicSignatureHelper.SendSignatureCompletionCommunication( document.Id, out _ );
            }

            return true;
        }

        #region Rendering

        private Guid? ExistingValidDocumentGuid( SignatureDocumentTemplate template, Person person )
        {
            if ( template == null || !template.IsValidInFuture || !template.ValidityDurationInDays.HasValue )
            {
                return null;
            }

            // Signed today and valid for 1 day is still valid today, so add a day before comparing (as Rock does).
            var earliest = RockDateTime.Today.AddDays( -template.ValidityDurationInDays.Value + 1 );
            return new RegistrationRegistrantService( _rockContext ).Queryable()
                .Where( r => r.PersonAlias.PersonId == person.Id
                    && r.SignatureDocument.SignatureDocumentTemplateId == template.Id
                    && r.SignatureDocument.SignedDateTime >= earliest )
                .OrderByDescending( r => r.SignatureDocument.SignedDateTime )
                .Select( r => ( Guid? ) r.SignatureDocument.Guid )
                .FirstOrDefault();
        }

        /// <summary>
        /// The document's Lava rendered with Rock's merge fields (Registration, Registrant), with
        /// the answers applied to the person in memory on a throwaway context (nothing saved).
        /// </summary>
        private string RenderDocument( RegistrationInstance instance, SignatureDocumentTemplate template, Person person, Dictionary<Guid, object> answers )
        {
            using ( var preview = new RockContext() )
            {
                var registrationTemplate = new RegistrationTemplateService( preview ).Get( instance.RegistrationTemplateId );
                var previewPerson = new PersonService( preview ).Get( person.Id );
                var forms = new RegistrationForms( preview );
                var (campusId, address) = forms.ApplyPersonFields( registrationTemplate, previewPerson, answers );
                forms.ApplyPersonAttributes( registrationTemplate, previewPerson, answers );

                GroupMember groupMember = null;
                var groupId = new CatalogService( preview ).GetLinkedGroupId( instance.Id );
                var group = groupId.HasValue ? new GroupService( preview ).Get( groupId.Value ) : null;
                if ( group != null )
                {
                    groupMember = new GroupMember
                    {
                        GroupId = group.Id,
                        Group = group,
                        PersonId = previewPerson.Id,
                        Person = previewPerson,
                        GroupMemberStatus = registrationTemplate.GroupMemberStatus,
                        GroupRoleId = registrationTemplate.GroupTypeId == group.GroupTypeId && registrationTemplate.GroupMemberRoleId.HasValue
                            ? registrationTemplate.GroupMemberRoleId.Value
                            : group.GroupType.DefaultGroupRoleId ?? group.GroupType.Roles.Select( r => r.Id ).FirstOrDefault()
                    };
                    forms.ApplyGroupMemberAttributes( registrationTemplate, groupMember, answers );
                }

                var campus = campusId.HasValue ? CampusCache.Get( campusId.Value ) : previewPerson.GetFamily( preview )?.CampusId is int familyCampusId ? CampusCache.Get( familyCampusId ) : null;
                var mergeFields = new Dictionary<string, object>
                {
                    { "Registration", new LavaSignatureRegistration( instance, registrationTemplate, groupId, 1 ) },
                    { "Registrant", new LavaSignatureRegistrant( previewPerson, address ?? previewPerson.GetHomeLocation( preview ), campus, groupMember, registrationTemplate, answers ) }
                };

                return ElectronicSignatureHelper.GetSignatureDocumentHtml( template.LavaTemplate, mergeFields );
            }
        }

        /// <summary>What Rock's RegistrationEntry builds, without the PDF (<see cref="Complete"/> makes it once the registration is saved).</summary>
        private SignatureDocument CreateDocument( SignatureDocumentTemplate template, SignedData signed, RegistrationInstance instance, Person registrar, Person person, Dictionary<Guid, object> answers )
        {
            // Encode what the browser sent, and don't trust its HTML: render it again from the saved answers.
            var documentHtml = RenderDocument( instance, template, person, answers );
            var document = new SignatureDocument
            {
                SignatureDocumentTemplateId = template.Id,
                Status = SignatureDocumentStatus.Signed,
                Name = $"{person.FullName} ({instance.Name})",
                SignedByPersonAliasId = registrar.PrimaryAliasId,
                AssignedToPersonAliasId = registrar.PrimaryAliasId,
                AppliesToPersonAliasId = person.PrimaryAliasId,
                SignedDocumentText = documentHtml,
                LastStatusDate = signed.SignedDateTime,
                SignedDateTime = signed.SignedDateTime,
                SignatureData = signed.SignatureData.EncodeHtml(),
                SignedName = signed.SignedByName.EncodeHtml(),
                SignedByEmail = signed.SignedByEmail.EncodeHtml(),
                SignedClientIp = signed.IpAddress.EncodeHtml(),
                SignedClientUserAgent = signed.UserAgent.EncodeHtml()
            };

            document.SignatureVerificationHash = SignatureDocumentService.CalculateSignatureVerificationHash( document );
            return document;
        }

        /// <summary>
        /// The camper, the option and the template's answers (in a fixed order) a document is
        /// rendered and signed for, so the signature stays bound to them.
        /// </summary>
        private static string SignedFor( Person person, RegistrationInstance instance, Dictionary<Guid, object> answers ) =>
            SignedFor( person.Id, instance.Id, instance.RegistrationTemplate, answers );

        private static string SignedFor( int personId, int registrationInstanceId, RegistrationTemplate template, Dictionary<Guid, object> answers ) =>
            personId + "|" + registrationInstanceId + "|" + RegistrationForms.AnswersKey( template, answers );

        private static string Hash( string source )
        {
            using ( var sha256 = SHA256.Create() )
            {
                return Convert.ToBase64String( sha256.ComputeHash( Encoding.Unicode.GetBytes( source ) ) );
            }
        }

        #endregion

        #region Types (ports of Rock RegistrationEntry's private classes, same member names for the Lava)

        private class SignedData
        {
            /// <summary>Hash of the option and answers the document was rendered for.</summary>
            public string SignedFor { get; set; }

            public string DocumentHtml { get; set; }
            public string SignatureData { get; set; }
            public string SignedByName { get; set; }
            public string SignedByEmail { get; set; }
            public string IpAddress { get; set; }
            public string UserAgent { get; set; }
            public DateTime SignedDateTime { get; set; }
        }

        private class LavaSignatureRegistration : Rock.Lava.LavaDataObject
        {
            public int InstanceId { get; }
            public string InstanceName { get; }
            public int TemplateId { get; }
            public string TemplateName { get; }
            public string RegistrationTerm { get; }
            public string RegistrantTerm { get; }
            public int RegistrantCount { get; }
            public int? GroupId { get; }

            public LavaSignatureRegistration( RegistrationInstance instance, RegistrationTemplate template, int? groupId, int registrantCount )
            {
                InstanceId = instance.Id;
                InstanceName = instance.Name;
                TemplateId = template.Id;
                TemplateName = template.Name;
                RegistrationTerm = template.RegistrationTerm;
                RegistrantTerm = template.RegistrantTerm;
                RegistrantCount = registrantCount;
                GroupId = groupId;
            }
        }

        private class LavaSignatureRegistrant : LavaHasAttributes
        {
            public Location Address { get; }
            public CampusCache Campus { get; }
            public DefinedValueCache ConnectionStatus { get; }
            public DateTime? AnniversaryDate { get; }
            public DateTime? BirthDate { get; }
            public string Email { get; }
            public string FirstName { get; }
            public string MiddleName { get; }
            public string LastName { get; }
            public Gender Gender { get; }
            public string GradeFormatted { get; }
            public int? GradeOffset { get; }
            public int? GraduationYear { get; }
            public DefinedValueCache MaritalStatus { get; }
            public string HomePhone { get; }
            public string MobilePhone { get; }
            public string WorkPhone { get; }
            public Person Person { get; }
            public GroupMember GroupMember { get; }

            public LavaSignatureRegistrant( Person person, Location homeLocation, CampusCache campus, GroupMember groupMember, RegistrationTemplate template, Dictionary<Guid, object> answers )
            {
                var (attributes, attributeValues) = RegistrationForms.RegistrantAttributes( template, answers );
                Attributes = attributes;
                AttributeValues = attributeValues;

                Address = homeLocation;
                Campus = campus;
                AnniversaryDate = person.AnniversaryDate;
                BirthDate = person.BirthDate;
                ConnectionStatus = person.ConnectionStatusValueId.HasValue ? DefinedValueCache.Get( person.ConnectionStatusValueId.Value ) : null;
                Email = person.Email;
                FirstName = person.FirstName;
                MiddleName = person.MiddleName;
                LastName = person.LastName;
                Gender = person.Gender;
                GradeFormatted = person.GradeFormatted;
                GradeOffset = person.GradeOffset;
                GraduationYear = person.GraduationYear;
                HomePhone = PhoneNumber.FormattedNumber( "", person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() )?.Number, false );
                MaritalStatus = person.MaritalStatusValueId.HasValue ? DefinedValueCache.Get( person.MaritalStatusValueId.Value ) : null;
                MobilePhone = PhoneNumber.FormattedNumber( "", person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() )?.Number, false );
                WorkPhone = PhoneNumber.FormattedNumber( "", person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() )?.Number, false );
                Person = person;
                GroupMember = groupMember;
            }
        }

        private class LavaHasAttributes : Rock.Lava.LavaDataObject, IHasAttributes
        {
            int IHasAttributes.Id => 0;

            public Dictionary<string, AttributeCache> Attributes { get; set; }

            public Dictionary<string, AttributeValueCache> AttributeValues { get; set; }

            public Dictionary<string, string> AttributeValueDefaults => null;

            public string GetAttributeValue( string key )
            {
                if ( AttributeValues != null && AttributeValues.ContainsKey( key ) )
                {
                    return AttributeValues[key].Value;
                }

                return Attributes != null && Attributes.ContainsKey( key ) ? Attributes[key].DefaultValue : null;
            }

            public List<string> GetAttributeValues( string key )
            {
                var value = GetAttributeValue( key );
                return value.IsNotNullOrWhiteSpace() ? value.SplitDelimitedValues().ToList() : new List<string>();
            }

            public void SetAttributeValue( string key, string value )
            {
                throw new NotSupportedException();
            }
        }

        #endregion
    }
}
