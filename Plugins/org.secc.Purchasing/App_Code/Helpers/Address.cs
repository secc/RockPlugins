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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.secc.Purchasing.Helpers
{
    [Serializable]
    public class Address
    {
        const string ArenaAddressSeparator = "^";
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }


        public Address() { }
        public Address(string arenaAddress)
        {
            ParseArenaAddress(arenaAddress);
        }

        public Address(string street, string city, string state, string postal)
        {
            StreetAddress = street.Trim();
            City = city.Trim();
            State = state.Trim();
            PostalCode = postal.Trim();
        }

        public bool IsValid()
        {
            return (Validate().Count == 0);
        }

        public string ToArenaFormat()
        {
            Dictionary<string, string> ValErrors = Validate();
            if (ValErrors.Count > 0)
                throw new RequisitionNotValidException("Address is not valid.", ValErrors);

            StringBuilder addressBulder = new StringBuilder();
            addressBulder.Append(StreetAddress.Trim());
            addressBulder.Append(ArenaAddressSeparator);
            addressBulder.Append(City.Trim());
            addressBulder.Append(ArenaAddressSeparator);
            addressBulder.Append(State.Trim());
            addressBulder.Append(ArenaAddressSeparator);
            addressBulder.Append(PostalCode.Trim());

            return addressBulder.ToString();
        }

        private void ParseArenaAddress(string arenaAddress)
        {
            if (String.IsNullOrEmpty(arenaAddress))
                throw new ArgumentNullException("ArenaAddress", "Arena formatted address is required.");
            string[] AddressParts = arenaAddress.Split(ArenaAddressSeparator.ToCharArray());

            if (AddressParts.Length != 4)
                throw new ArgumentException("Arena Address", "Arena Address is not properly formatted.");

            StreetAddress = AddressParts[0].ToString();
            City = AddressParts[1].ToString();
            State = AddressParts[2].ToString();
            PostalCode = AddressParts[3].ToString();
        }

        public Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string,string>();
            if (String.IsNullOrEmpty(StreetAddress.Trim()))
                ValErrors.Add("Street Address", "Street Address is required.");
            if(String.IsNullOrEmpty(City.Trim()))
                ValErrors.Add("City", "City is required.");
            if(String.IsNullOrEmpty(State.Trim()))
                ValErrors.Add("State", "State is required.");
            if(String.IsNullOrEmpty(PostalCode.Trim()))
                ValErrors.Add("Postal Code", "Postal Code is required.");

            return ValErrors;

        }


    }
}
