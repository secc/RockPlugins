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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using org.secc.RegistrationCart.Services;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Model;
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Blocks
{
    [DisplayName( "Registration Cart Offering" )]
    [Category( "Registration Cart" )]
    [Description( "Choose a family member, a registration instance and fees for one Rock registration template and add them to the cart." )]
    [IconCssClass( "fa fa-shopping-cart" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [LinkedPage( "Cart Page", Key = NavAttributeKey.CartPage, IsRequired = false, Order = 0 )]
    [LinkedPage( "Catalog Page", Key = NavAttributeKey.CatalogPage, IsRequired = false, Order = 1 )]
    [BooleanField( "Allow Adding Family Members", Key = AttributeKey.AllowAddFamilyMember, DefaultBooleanValue = true,
        Description = "Let the signed-in person add a new child to their family from this page.", Order = 2 )]
    [WorkflowTypeField( "Discount Code Workflow", Key = AttributeKey.DiscountCodeWorkflow, AllowMultiple = false, IsRequired = false,
        Description = "Runs when a camper is added to the cart to apply a registration template discount code, e.g. financial assistance. Passes an entity which is a Dictionary containing \"RegistrationInstance\" and \"RegistrationInfo\" keys, as SECC's RegistrationEntry did. The code applies to that camper alone.", Order = 3 )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.BLOCK_OFFERING_DETAIL )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.OFFERING_DETAIL )]
    public class OfferingDetail : RegistrationCartBlockBase
    {
        private static class AttributeKey
        {
            public const string AllowAddFamilyMember = "AllowAddFamilyMember";
            public const string DiscountCodeWorkflow = "DiscountCodeWorkflow";
        }

        public override string ObsidianFileUrl => ObsidianBlockUrl( "offeringDetail" );

        public override object GetObsidianBlockInitialization()
        {
            var manager = GetCartManager();
            var cart = manager?.GetOpenCart();
            var box = new OfferingDetailInitializationBox
            {
                Navigation = BuildNavigation( cart ),
                CanAddFamilyMembers = GetAttributeValue( AttributeKey.AllowAddFamilyMember ).AsBoolean( true )
            };

            var key = PageParameter( OfferingKeyParameter );
            var templateId = key.IsNotNullOrWhiteSpace() ? new RegistrationTemplateService( RockContext ).Get( key, AllowIntegerIds )?.Id : null;
            var template = templateId.HasValue ? new CatalogService( RockContext ).GetTemplate( templateId.Value ) : null;
            if ( template == null )
            {
                box.ErrorMessage = "That offering is not open for registration.";
                return box;
            }

            box.Offering = ToBag( template );
            box.Offering.Forms = new RegistrationForms( RockContext ).GetForms( template.Template );
            box.Offering.FormLists = RegistrationForms.GetFormLists( template.Template );
            var signatureDocument = new RegistrationSignatures( RockContext ).RequiredDocument( template.Template );
            box.Offering.RequiresSignature = signatureDocument != null;
            box.Offering.IsSignatureDrawn = signatureDocument?.SignatureType == Rock.Model.SignatureType.Drawn;
            box.Offering.SignatureDocumentTerm = signatureDocument?.DocumentTerm.IfEmpty( "Release" ) ?? "Release";
            if ( manager != null )
            {
                box.People = BuildPeople( manager, cart );
            }

            return box;
        }

        [BlockAction]
        public BlockActionResult AddToCart( AddToCartRequestBag request )
        {
            var manager = GetCartManager();
            if ( manager == null )
            {
                return ActionUnauthorized( "Please sign in to register." );
            }

            if ( request == null )
            {
                return ActionBadRequest( "Nothing to add." );
            }

            var signatureData = request.ExistingSignatureDocumentGuid.HasValue
                ? RegistrationSignatures.ForExistingDocument( request.ExistingSignatureDocumentGuid.Value )
                : request.SignatureData;
            var line = manager.AddLineItem( request.PersonAliasId, request.RegistrationInstanceId, request.FeeItemIds, request.FieldValues, signatureData, GetAttributeValue( AttributeKey.DiscountCodeWorkflow ).AsGuidOrNull(), out var error );
            if ( line == null )
            {
                return ActionBadRequest( error ?? "That item could not be added." );
            }

            return ActionOk( BuildNavigation( manager.GetOpenCart() ) );
        }

        /// <summary>
        /// A family member's starting answers to the template's forms for one instance: their
        /// current values where the form shows them, defaults otherwise.
        /// </summary>
        [BlockAction]
        public BlockActionResult GetStartingValues( int personAliasId, int registrationInstanceId )
        {
            var manager = GetCartManager();
            if ( manager == null )
            {
                return ActionUnauthorized( "Please sign in." );
            }

            var person = manager.ResolveRegisterablePerson( personAliasId );
            var instance = new RegistrationInstanceService( RockContext ).Get( registrationInstanceId );
            if ( person == null || instance == null )
            {
                return ActionBadRequest( "That person or option was not found." );
            }

            var groupId = new CatalogService( RockContext ).GetLinkedGroupId( instance.Id );
            return ActionOk( new RegistrationForms( RockContext ).GetStartingValues( instance.RegistrationTemplate, person, groupId ) );
        }

        /// <summary>
        /// The template's signature document rendered for this family member with their answers,
        /// or the guid of one they signed before that is still valid.
        /// </summary>
        [BlockAction]
        public BlockActionResult GetSignatureDocument( SignatureRequestBag request )
        {
            var (person, instance, error) = ResolveSignatureRequest( request );
            if ( error != null )
            {
                return error;
            }

            var document = new RegistrationSignatures( RockContext ).GetDocument( instance, GetCurrentPerson(), person, request.FieldValues );
            return document != null ? ActionOk( document ) : ActionBadRequest( "This registration has no document to sign." );
        }

        /// <summary>Records the signature on the document returned by GetSignatureDocument.</summary>
        [BlockAction]
        public BlockActionResult SignDocument( SignatureRequestBag request )
        {
            var (_, _, error) = ResolveSignatureRequest( request );
            if ( error != null )
            {
                return error;
            }

            var signed = new RegistrationSignatures( RockContext ).Sign( request.FieldValues, request.DocumentHtml, request.SecurityToken, request.Signature,
                RequestContext.ClientInformation.IpAddress, RequestContext.ClientInformation.UserAgent );
            return signed != null ? ActionOk( signed ) : ActionBadRequest( "The document changed. Please review it and sign again." );
        }

        private (Rock.Model.Person, RegistrationInstance, BlockActionResult) ResolveSignatureRequest( SignatureRequestBag request )
        {
            var manager = GetCartManager();
            if ( manager == null )
            {
                return (null, null, ActionUnauthorized( "Please sign in." ));
            }

            var person = request != null ? manager.ResolveRegisterablePerson( request.PersonAliasId ) : null;
            var instance = request != null ? new RegistrationInstanceService( RockContext ).Get( request.RegistrationInstanceId ) : null;
            if ( person == null || instance == null )
            {
                return (null, null, ActionBadRequest( "That person or option was not found." ));
            }

            return (person, instance, null);
        }

        [BlockAction]
        public BlockActionResult AddFamilyMember( AddFamilyMemberRequestBag request )
        {
            var manager = GetCartManager();
            if ( manager == null )
            {
                return ActionUnauthorized( "Please sign in first." );
            }

            if ( !GetAttributeValue( AttributeKey.AllowAddFamilyMember ).AsBoolean( true ) )
            {
                return ActionForbidden( "Adding family members is turned off." );
            }

            var person = manager.AddChildToFamily( request?.FirstName, request?.LastName, ( GradeBand ) ( request?.GradeBand ?? 0 ), "", out var error );
            if ( person == null )
            {
                return ActionBadRequest( error ?? "The person could not be added." );
            }

            var people = BuildPeople( manager, manager.GetOpenCart() );
            return ActionOk( new { People = people, NewPersonAliasId = person.PrimaryAliasId } );
        }

        #region Helpers

        private static OfferingBag ToBag( CatalogService.CatalogTemplate t )
        {
            var starts = t.Instances.Where( i => i.EventStart.HasValue ).Select( i => i.EventStart.Value ).ToList();
            var ends = t.Instances.Select( i => i.EventEnd ?? i.EventStart ).Where( d => d.HasValue ).Select( d => d.Value ).ToList();
            var bag = new OfferingBag
            {
                IdKey = t.Template.IdKey,
                OfferingTypeText = t.Template.Category?.Name,
                RegistrantTerm = t.Template.RegistrantTerm.IsNotNullOrWhiteSpace() ? t.Template.RegistrantTerm : "Registrant",
                Title = t.Template.Name,
                Description = t.Description,
                ImageUrl = t.ImageUrl,
                DateText = starts.Any() ? DateRangeText( starts.Min(), ends.Any() ? ends.Max() : starts.Max() ) : ""
            };

            foreach ( var i in t.Instances )
            {
                bag.Variants.Add( new VariantBag
                {
                    Id = i.Instance.Id,
                    Name = i.Instance.Name,
                    BasePrice = i.Price,
                    CampusId = i.CampusId,
                    CampusName = i.CampusName,
                    SessionText = DateRangeText( i.EventStart, i.EventEnd ),
                    DeadlineText = i.RegistrationDeadline.HasValue ? "Register by " + i.RegistrationDeadline.Value.ToString( "MMM d" ) : "",
                    EligibleGradeBands = ( int ) i.Bands,
                    SpotsRemaining = i.SpotsRemaining,
                    WaitListEnabled = i.WaitListEnabled
                } );
            }

            foreach ( var f in t.Fees )
            {
                bag.AddOnGroups.Add( new AddOnGroupBag
                {
                    Id = f.Fee.Id,
                    Name = f.Fee.Name,
                    IsRequired = f.IsRequired,
                    IsLocked = f.IsLocked,
                    SelectionMode = ( int ) f.Mode,
                    Items = f.Items.Select( i => new AddOnItemBag
                    {
                        Id = i.Item.Id,
                        Name = i.Item.Name,
                        Price = i.Item.Cost,
                        EligibleGradeBands = ( int ) i.Bands
                    } ).ToList()
                } );
            }

            return bag;
        }

        private List<RegistrantOptionBag> BuildPeople( CartManager manager, Model.Cart cart )
        {
            var self = GetCurrentPerson();
            var inCart = cart?.Registrants.Select( r => r.PersonAliasId ).ToList() ?? new List<int>();

            return manager.GetRegisterablePeople()
                .Where( p => p.PrimaryAliasId.HasValue )
                .Select( p =>
                {
                    var band = GradeBands.ForPerson( p );
                    return new RegistrantOptionBag
                    {
                        PersonAliasId = p.PrimaryAliasId.Value,
                        FullName = p.FullName,
                        GradeBand = ( int ) band,
                        GradeText = band == GradeBand.Adults || band == GradeBand.None
                            ? GradeBands.Friendly( band )
                            : ( p.GradeFormatted.IsNotNullOrWhiteSpace() ? p.GradeFormatted : GradeBands.Friendly( band ) ),
                        IsSelf = p.Id == self?.Id,
                        IsInCart = inCart.Contains( p.PrimaryAliasId.Value )
                    };
                } )
                .ToList();
        }

        #endregion
    }
}
