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
using System.Linq;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Something that stops a cart checking out, with the lines it concerns (so the cart and
    /// checkout pages can show it on those lines, next to their Edit link). No lines means the
    /// whole cart.
    /// </summary>
    public class CheckoutProblem
    {
        public CheckoutProblem( string message, IEnumerable<int> lineItemIds = null )
        {
            Message = message;
            LineItemIds = lineItemIds?.ToList() ?? new List<int>();
        }

        public string Message { get; }

        public List<int> LineItemIds { get; }

        public override string ToString() => Message;
    }
}
