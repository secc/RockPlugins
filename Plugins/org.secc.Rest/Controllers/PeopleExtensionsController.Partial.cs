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
//
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

using Rock;
using System.Web.Http;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using org.secc.PersonMatch;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// Extensions for the REST API for People
    /// </summary>
    public partial class PeopleExtensionsController : Rock.Rest.ApiControllerBase
    {

        /// <summary>
        /// Try to find a matching person or add the person to rock
        /// </summary>
        /// <returns>The id of the matching or new person</returns>
        [Authenticate, Secured]
        [System.Web.Http.Route("api/People/MatchOrCreatePerson")]
        [HttpPost]
        public int MatchOrCreatePerson(PersonMatch personMatch)
        {
            try {
                Person person = personMatch.person;
                Location location = personMatch.location;
                RockContext context = new RockContext();
                PersonService personService = new PersonService(context);
                var matchPerson = personService.GetByMatch(person.FirstName, person.LastName, person.BirthDate, person.Email, person.PhoneNumbers.Select(pn => pn.Number).FirstOrDefault(), location.Street1, location.PostalCode);
                if (matchPerson != null && matchPerson.Count() > 0)
                {
                    return matchPerson.FirstOrDefault().Id;
                }
                else
                { 
                    personService.Add(person);
                    context.SaveChanges();
                    return person.Id;
                }
            } catch (Exception e)
            {
                GenerateResponse(HttpStatusCode.InternalServerError, e.Message);
                throw e;
            }
        }
        
        /// <summary>
        /// Generates the response.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        private void GenerateResponse(HttpStatusCode code, string message = null)
        {
            var response = new HttpResponseMessage(code);

            if (!string.IsNullOrWhiteSpace(message))
            {
                response.Content = new StringContent(message);
            }

            throw new HttpResponseException(response);
        }

        public class PersonMatch
        {
            public Person person;

            public Location location;
        }
    }
}