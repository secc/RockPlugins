// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the Southeast Christian Church License (the "License");
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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using org.secc.Mapping.Model;
using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.Mapping
{
    public static class AzureDistanceMatrix
    {
        private static readonly HttpClient client = new HttpClient();
        
        public static async Task<List<Destination>> OrderDestinations(string origin, List<Destination> destinations)
        {
            var destinationAddresses = new List<string>();

            // Get stored distances from database
            RockContext rockContext = new RockContext();
            LocationDistanceStoreService locationDistanceStoreService = new LocationDistanceStoreService(rockContext);
            locationDistanceStoreService.LoadDurations(origin, destinations);

            // Get new distances from Azure Maps
            foreach (var destination in destinations.Where(d => !d.IsCalculated && d.Address.IsNotNullOrWhiteSpace()))
            {
                destinationAddresses.Add(destination.Address);
            }
            
            if (destinationAddresses.Any())
            {
                try
                {
                    // Get the Azure Maps key from Rock
                    string azureMapsKey = GlobalAttributesCache.Get().GetValue("AzureMapsKey");
                    
                    // Prepare the request payload for Azure Maps Route Matrix API
                    var payload = new
                    {
                        origins = new[] { new { 
                            address = origin
                        }},
                        destinations = destinationAddresses.Select(address => new { 
                            address = address 
                        }).ToArray(),
                        travelMode = "car",
                        routeType = "fastest"
                    };

                    string requestUrl = $"https://atlas.microsoft.com/route/matrix/json?api-version=1.0&subscription-key={azureMapsKey}";
                    
                    // Serialize payload to JSON
                    string jsonPayload = JsonConvert.SerializeObject(payload);
                    
                    // Create HTTP request
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(requestUrl, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var result = JObject.Parse(jsonResponse);
                        
                        // Process the response
                        var matrix = result["matrix"];
                        if (matrix != null && matrix.Count() > 0)
                        {
                            var routes = matrix[0];
                            
                            for (int i = 0; i < destinationAddresses.Count; i++)
                            {
                                if (routes[i] != null)
                                {
                                    double travelDistance = routes[i]["response"]["routeSummary"]["lengthInMeters"].Value<double>() / 1609.344; // Convert meters to miles
                                    double travelDuration = routes[i]["response"]["routeSummary"]["travelTimeInSeconds"].Value<double>() / 60; // Convert seconds to minutes
                                    
                                    var destinationsForAddress = destinations.Where(d => d.Address == destinationAddresses[i]).ToList();
                                    foreach (var destination in destinationsForAddress)
                                    {
                                        destination.TravelDistance = travelDistance;
                                        destination.TravelDuration = travelDuration;
                                        destination.IsCalculated = true;
                                        locationDistanceStoreService.AddOrUpdate(origin, destination, "Azure");
                                    }
                                }
                            }
                            
                            rockContext.SaveChanges();
                        }
                    }
                    else
                    {
                        // Log error
                        Rock.Model.ExceptionLogService.LogException(new Exception($"Azure Maps API error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}"));
                    }
                }
                catch (Exception ex)
                {
                    // Log exception
                    Rock.Model.ExceptionLogService.LogException(ex);
                }
            }

            return destinations
                .Where(d => d.IsCalculated == true)
                .OrderBy(d => d.TravelDuration)
                .ToList();
        }
    }
}