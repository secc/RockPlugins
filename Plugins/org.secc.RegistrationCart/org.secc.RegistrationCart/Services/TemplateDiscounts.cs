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
using System.Data.Entity;
using System.Linq;

using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Model;

using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// A cart line's discount code, from its Rock registration template's discounts, checked the
    /// way SECC's RegistrationEntry checked it (<see cref="DiscountRules"/>). Each line is one
    /// registration: one code, its own. A shared code (a staff code) may be on several lines;
    /// every use counts toward the code's Maximum Usage, which Rock counts per instance.
    /// </summary>
    public class TemplateDiscounts
    {
        private readonly RockContext _rockContext;

        public TemplateDiscounts( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>A code that can go on a line, with the value it takes off.</summary>
        public class UsableCode
        {
            public string Code { get; set; }

            /// <summary>Flat amount, 0 when the code doesn't apply to the registrant (Maximum Registrants 0).</summary>
            public decimal Amount { get; set; }

            /// <summary>As a fraction (0.25 = 25%), as Rock stores it; 0 when the code doesn't apply to the registrant.</summary>
            public decimal Percentage { get; set; }
        }

        /// <summary>The template's term for a discount code ("Discount Code" when it has none).</summary>
        public static string Term( RegistrationTemplate template ) =>
            template?.DiscountCodeTerm.IsNotNullOrWhiteSpace() == true ? template.DiscountCodeTerm : "Discount Code";

        public static TemplateDiscountTerms ToTerms( RegistrationTemplateDiscount d ) =>
            d == null ? null : new TemplateDiscountTerms( d.Code, d.DiscountPercentage, d.DiscountAmount, d.MaxUsage, d.MinRegistrants, d.MaxRegistrants, d.StartDate, d.EndDate, d.AutoApplyDiscount );

        /// <summary>
        /// Checks a code the family typed for a line. Returns null and the code (with the template's
        /// spelling) when it can be used; otherwise the message for the family.
        /// </summary>
        /// <param name="otherLines">The cart's other lines that will be registered with this one
        /// (their uses of the code count toward its Maximum Usage).</param>
        public string Check( RegistrationTemplate template, int registrationInstanceId, string code, IEnumerable<CartLineItem> otherLines, out UsableCode usable )
        {
            usable = null;
            var typed = ( code ?? "" ).Trim();
            var discount = Find( template.Id, typed );
            var problem = DiscountRules.Check( ToTerms( discount ), RockDateTime.Today, discount == null ? 0 : TimesUsed( registrationInstanceId, discount.Code, otherLines ) );
            if ( problem != DiscountProblem.None )
            {
                return DiscountRules.Message( problem, typed, Term( template ), discount?.MinRegistrants );
            }

            usable = ToUsable( discount );
            return null;
        }

        /// <summary>
        /// The template's first automatic discount (in order) that can be used on the line, or
        /// null. As Rock's RegistrationEntry, only when the line has no code of its own; the
        /// discount code workflow runs only when none applies.
        /// </summary>
        public UsableCode FirstAutoApply( RegistrationTemplate template, int registrationInstanceId, IEnumerable<CartLineItem> otherLines )
        {
            var others = otherLines?.ToList() ?? new List<CartLineItem>();
            var discount = Discounts( template.Id )
                .Where( d => d.AutoApplyDiscount )
                .OrderBy( d => d.Order )
                .ToList()
                .FirstOrDefault( d => DiscountRules.Check( ToTerms( d ), RockDateTime.Today, TimesUsed( registrationInstanceId, d.Code, others ) ) == DiscountProblem.None );
            return discount != null ? ToUsable( discount ) : null;
        }

        /// <summary>
        /// The registrations in the instance that used the code, plus the given cart lines for the
        /// instance with it (Rock counts the instance's registrations; the cart's lines become
        /// registrations together).
        /// </summary>
        public int TimesUsed( int registrationInstanceId, string code, IEnumerable<CartLineItem> otherLines )
        {
            // As Rock: an equality test in SQL, which ignores case.
            var registered = new RegistrationService( _rockContext ).Queryable().AsNoTracking()
                .Count( r => r.RegistrationInstanceId == registrationInstanceId && r.DiscountCode == code );
            var inCart = ( otherLines ?? Enumerable.Empty<CartLineItem>() )
                .Count( l => l.RegistrationInstanceId == registrationInstanceId && DiscountRules.SameCode( l.TemplateDiscountCode, code ) );
            return registered + inCart;
        }

        /// <summary>The template's discount with that code (ignoring case), or null.</summary>
        public RegistrationTemplateDiscount Find( int registrationTemplateId, string code )
        {
            if ( code.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var trimmed = code.Trim();
            return Discounts( registrationTemplateId )
                .ToList()
                .FirstOrDefault( d => DiscountRules.SameCode( d.Code, trimmed ) );
        }

        /// <summary>
        /// Sets the value of the template's discount with that code (a family's own FA- code, to
        /// the amount their latest answers ask for) and returns it as usable; null when it is gone.
        /// Saved with the context.
        /// </summary>
        public UsableCode SetValue( int registrationTemplateId, string code, decimal amount, decimal percentage )
        {
            var discount = new RegistrationTemplateDiscountService( _rockContext ).Queryable()
                .FirstOrDefault( d => d.RegistrationTemplateId == registrationTemplateId && d.Code == code );
            if ( discount == null )
            {
                return null;
            }

            discount.DiscountAmount = amount;
            discount.DiscountPercentage = percentage;
            return ToUsable( discount );
        }

        /// <summary>
        /// Deletes the template's discount with that code when no registration and no cart line
        /// uses it: a code the discount code workflow just made that the line won't use.
        /// </summary>
        public void RemoveUnused( int registrationTemplateId, string code )
        {
            var service = new RegistrationTemplateDiscountService( _rockContext );
            var discount = service.Queryable().FirstOrDefault( d => d.RegistrationTemplateId == registrationTemplateId && d.Code == code );
            if ( discount == null )
            {
                return;
            }

            var templateId = registrationTemplateId;
            var used = new RegistrationService( _rockContext ).Queryable().AsNoTracking()
                    .Any( r => r.RegistrationInstance.RegistrationTemplateId == templateId && r.DiscountCode == code )
                || new CartLineItemService( _rockContext ).Queryable().AsNoTracking()
                    .Any( l => l.RegistrationTemplateId == templateId && l.TemplateDiscountCode == code );
            if ( !used )
            {
                service.Delete( discount );
            }
        }

        private IQueryable<RegistrationTemplateDiscount> Discounts( int registrationTemplateId ) =>
            new RegistrationTemplateDiscountService( _rockContext ).Queryable().AsNoTracking()
                .Where( d => d.RegistrationTemplateId == registrationTemplateId );

        private static UsableCode ToUsable( RegistrationTemplateDiscount d )
        {
            var applies = DiscountRules.AppliesToRegistrant( ToTerms( d ) );
            return new UsableCode
            {
                Code = d.Code,
                Amount = applies ? d.DiscountAmount : 0m,
                Percentage = applies ? d.DiscountPercentage : 0m
            };
        }
    }
}
