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
using System.ComponentModel;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Services;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.RegistrationCart.Blocks
{
    [DisplayName( "Registration Cart Catalog" )]
    [Category( "Registration Cart" )]
    [Description( "Lists Rock registration templates that have an open registration instance, as a shoppable catalog." )]
    [IconCssClass( "fa fa-shopping-cart" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [TextField( "Heading", Key = AttributeKey.Heading, IsRequired = false, DefaultValue = "Registration", Order = 0 )]
    [TextField( "Intro", Key = AttributeKey.Intro, IsRequired = false, DefaultValue = "Add your whole family to one cart and check out once.", Order = 1 )]
    [LinkedPage( "Offering Detail Page", Key = NavAttributeKey.OfferingDetailPage, IsRequired = false, Order = 2 )]
    [LinkedPage( "Cart Page", Key = NavAttributeKey.CartPage, IsRequired = false, Order = 3 )]
    [BooleanField( "Allow Adding Family Members", Key = AttributeKey.AllowAddFamilyMember, DefaultBooleanValue = true,
        Description = "Let the signed-in person add a new child to their family while choosing who is going.", Order = 4 )]
    [IntegerField( "Maximum Family Members Added Per Day", Key = AttributeKey.MaxFamilyMembersPerDay, IsRequired = false, DefaultIntegerValue = 5,
        Description = "How many new people a family can gain in a day before this page stops adding more (they are told to contact the office). 0 for no limit.", Order = 5 )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.BLOCK_CATALOG )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.CATALOG )]
    public class Catalog : RegistrationCartBlockBase
    {
        private static class AttributeKey
        {
            public const string Heading = "Heading";
            public const string Intro = "Intro";
            public const string AllowAddFamilyMember = "AllowAddFamilyMember";
            public const string MaxFamilyMembersPerDay = "MaxFamilyMembersPerDay";
        }

        public override string ObsidianFileUrl => ObsidianBlockUrl( "catalog" );

        public override object GetObsidianBlockInitialization()
        {
            var manager = GetCartManager();
            var cart = manager?.GetOpenCart();
            var box = new CatalogInitializationBox
            {
                Navigation = BuildNavigation( cart ),
                Heading = GetAttributeValue( AttributeKey.Heading ),
                Intro = GetAttributeValue( AttributeKey.Intro ),
                CanAddFamilyMembers = GetAttributeValue( AttributeKey.AllowAddFamilyMember ).AsBoolean( true ),
                // Rock's school grades (value = grade offset), youngest first, for a new child.
                GradeOptions = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.SCHOOL_GRADES.AsGuid() ).DefinedValues
                    .OrderByDescending( v => v.Value.AsInteger() )
                    .Select( v => new Rock.ViewModels.Utility.ListItemBag { Value = v.Value, Text = v.Description } )
                    .ToList()
            };

            var catalog = new CatalogService( RockContext ) { ViewerCart = cart }.GetCatalog();
            foreach ( var t in catalog )
            {
                var starts = t.Instances.Where( i => i.EventStart.HasValue ).Select( i => i.EventStart.Value ).ToList();
                var ends = t.Instances.Select( i => i.EventEnd ?? i.EventStart ).Where( d => d.HasValue ).Select( d => d.Value ).ToList();
                box.Offerings.Add( new OfferingSummaryBag
                {
                    IdKey = t.Template.IdKey,
                    DetailUrl = this.GetLinkedPageUrl( NavAttributeKey.OfferingDetailPage, OfferingKeyParameter, t.Template.IdKey ),
                    OfferingTypeText = t.Template.Category?.Name,
                    Title = t.Template.Name,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    DateText = starts.Any() ? DateRangeText( starts.Min(), ends.Any() ? ends.Max() : starts.Max() ) : "",
                    MinPrice = t.Instances.Min( i => i.Price ),
                    OptionCount = t.Instances.Count,
                    CampusNames = t.Instances.Where( i => i.CampusName != null ).Select( i => i.CampusName ).Distinct().OrderBy( n => n ).ToList()
                } );
            }

            AddCamps( box, catalog, manager, cart );
            return box;
        }

        /// <summary>
        /// Adds a child to the signed-in person's family from a camp's "who's going" step, so a
        /// family whose child isn't in Rock yet can still pick them. The camp being looked at
        /// decides the new person's connection status, as on the offering page.
        /// </summary>
        [BlockAction]
        public BlockActionResult AddFamilyMember( AddFamilyMemberRequestBag request, int? registrationInstanceId )
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

            CatalogService.CatalogTemplate template = null;
            if ( registrationInstanceId.HasValue )
            {
                new CatalogService( RockContext ).GetOpenInstance( registrationInstanceId.Value, out template );
            }

            var person = manager.AddChildToFamily( request?.FirstName, request?.LastName, request?.BirthDate.AsDateTime(), request?.GradeOffset, template?.Template,
                GetAttributeValue( AttributeKey.MaxFamilyMembersPerDay ).AsIntegerOrNull() ?? 5, out var error );
            if ( person == null )
            {
                return ActionBadRequest( error ?? "The person could not be added." );
            }

            var box = ( CatalogInitializationBox ) GetObsidianBlockInitialization();
            return ActionOk( new { People = box.People, NewPersonAliasId = person.PrimaryAliasId } );
        }

        /// <summary>
        /// The catalog as families see it: one card per camp, read from the registration names
        /// (<see cref="CampNames"/>), each with its options by campus. A template that is not
        /// named like a camp is a card by itself. The family's people come along so the page can
        /// offer each one only what they may register for.
        /// </summary>
        private void AddCamps( CatalogInitializationBox box, List<CatalogService.CatalogTemplate> catalog, CartManager manager, Model.Cart cart )
        {
            var campuses = CampusCache.All().Select( c => new CampusName( c.Id, c.Name, c.ShortCode ) ).ToList();
            var camps = new Dictionary<string, CampBag>();

            foreach ( var t in catalog )
            {
                var detailUrl = this.GetLinkedPageUrl( NavAttributeKey.OfferingDetailPage, OfferingKeyParameter, t.Template.IdKey );
                foreach ( var i in t.Instances )
                {
                    var name = CampNames.Parse( t.Template.Name, i.Instance.Name, campuses );
                    var key = name?.CampKey ?? "template|" + t.Template.IdKey;
                    if ( !camps.TryGetValue( key, out var camp ) )
                    {
                        camp = new CampBag
                        {
                            Key = key,
                            Title = name?.Title ?? t.Template.Name,
                            Year = name?.Year ?? "",
                            TypeText = name == null ? t.Template.Category?.Name : null,
                            IsServing = name?.IsServing ?? false,
                            Description = name == null ? t.Description : null,
                            ImageUrl = t.ImageUrl
                        };
                        camps[key] = camp;
                    }

                    camp.ImageUrl = camp.ImageUrl ?? t.ImageUrl;
                    // A campus the linked calendar event names wins over the one in the name.
                    var campusId = i.CampusId ?? name?.CampusId;
                    camp.Options.Add( new CampOptionBag
                    {
                        InstanceId = i.Instance.Id,
                        DetailUrl = detailUrl,
                        Name = i.Instance.Name,
                        RoleText = name?.RoleText ?? "",
                        CampusId = campusId,
                        CampusName = campusId.HasValue ? CampusCache.Get( campusId.Value )?.Name : null,
                        Price = i.Price,
                        SessionText = DateRangeText( i.EventStart, i.EventEnd ),
                        EligibleGradeBands = ( int ) i.Bands,
                        MinAge = name?.MinAge,
                        MaxAge = name?.MaxAge,
                        SpotsRemaining = i.SpotsRemaining,
                        WaitListEnabled = i.WaitListEnabled
                    } );
                }
            }

            foreach ( var camp in camps.Values )
            {
                camp.MinPrice = camp.Options.Min( o => o.Price );
                camp.Campuses = camp.Options.Where( o => o.CampusId.HasValue )
                    .GroupBy( o => o.CampusId.Value )
                    .Select( g => new CampCampusBag { Id = g.Key, Name = g.First().CampusName } )
                    .OrderBy( c => c.Name )
                    .ToList();
            }

            box.Camps = camps.Values.OrderBy( c => c.IsServing ).ThenBy( c => c.Year == "" ).ThenBy( c => c.Title ).ToList();

            if ( manager != null )
            {
                var self = GetCurrentPerson();
                box.FamilyCampusId = self?.PrimaryCampusId;
                box.PreventDuplicateRegistrations = CartSettings.PreventDuplicateRegistrations;
                var family = manager.GetRegisterablePeople().Where( p => p.PrimaryAliasId.HasValue ).ToList();
                var registered = FamilyRegistrations( family.Select( p => p.Id ).ToList(), catalog, campuses );
                var inCart = cart?.LineItems.Where( l => l.CartRegistrant != null )
                    .Select( l => new { l.CartRegistrant.PersonAliasId, l.RegistrationInstanceId } ).ToList();
                box.People = family
                    .Select( p =>
                    {
                        var band = GradeBands.ForPerson( p );
                        return new CampPersonBag
                        {
                            PersonAliasId = p.PrimaryAliasId.Value,
                            FullName = p.FullName,
                            GradeBand = ( int ) band,
                            GradeText = band == GradeBand.Adults || band == GradeBand.None
                                ? GradeBands.Friendly( band )
                                : ( p.GradeFormatted.IsNotNullOrWhiteSpace() ? p.GradeFormatted : GradeBands.Friendly( band ) ),
                            Age = p.Age,
                            IsSelf = p.Id == self?.Id,
                            Registrations = registered.TryGetValue( p.Id, out var list ) ? list : new List<CampRegistrationBag>(),
                            InCartInstanceIds = inCart?.Where( l => l.PersonAliasId == p.PrimaryAliasId.Value ).Select( l => l.RegistrationInstanceId ).Distinct().ToList() ?? new List<int>()
                        };
                    } )
                    .ToList();
            }
        }

        /// <summary>
        /// What the family is already registered for (wait list included) among the catalog's
        /// camps, by person id, so the page can say so before anyone fills in a form. Every
        /// instance of the catalog's templates counts, closed ones too: a camper registered at
        /// another campus or in a window that has since closed is still going to that camp.
        /// </summary>
        private Dictionary<int, List<CampRegistrationBag>> FamilyRegistrations( List<int> personIds, List<CatalogService.CatalogTemplate> catalog, List<CampusName> campuses )
        {
            var templateIds = catalog.Select( t => t.Template.Id ).ToList();
            if ( !personIds.Any() || !templateIds.Any() )
            {
                return new Dictionary<int, List<CampRegistrationBag>>();
            }

            var rows = new RegistrationRegistrantService( RockContext ).Queryable().AsNoTracking()
                .Where( r => !r.Registration.IsTemporary
                    && r.PersonAliasId.HasValue
                    && personIds.Contains( r.PersonAlias.PersonId )
                    && templateIds.Contains( r.Registration.RegistrationInstance.RegistrationTemplateId ) )
                .Select( r => new
                {
                    r.PersonAlias.PersonId,
                    InstanceId = r.Registration.RegistrationInstanceId,
                    InstanceName = r.Registration.RegistrationInstance.Name,
                    TemplateId = r.Registration.RegistrationInstance.RegistrationTemplateId,
                    TemplateName = r.Registration.RegistrationInstance.RegistrationTemplate.Name,
                    r.OnWaitList
                } )
                .ToList();

            var idKeys = catalog.ToDictionary( t => t.Template.Id, t => t.Template.IdKey );
            return rows
                .GroupBy( r => r.PersonId )
                .ToDictionary( g => g.Key, g => g
                    .GroupBy( r => r.InstanceId )
                    .Select( i =>
                    {
                        var row = i.First();
                        var name = CampNames.Parse( row.TemplateName, row.InstanceName, campuses );
                        return new CampRegistrationBag
                        {
                            CampKey = name?.CampKey ?? "template|" + idKeys[row.TemplateId],
                            InstanceId = row.InstanceId,
                            CampusName = name?.CampusId != null ? CampusCache.Get( name.CampusId.Value )?.Name : null,
                            IsWaitList = i.All( r => r.OnWaitList )
                        };
                    } )
                    .ToList() );
        }
    }
}
