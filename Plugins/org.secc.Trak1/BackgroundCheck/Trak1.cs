// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using org.secc.Trak1.Helpers;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.Trak1.BackgroundCheck
{
    /// <summary>
    /// Trak-1 Background Check
    /// Note: This component requires 
    /// </summary>
    [Description( "Trak-1 Background Check" )]
    [Export( typeof( BackgroundCheckComponent ) )]
    [ExportMetadata( "ComponentName", "Trak-1" )]

    [TextField( "User Name", "Trak-1 User Name", true, "", "", 0 )]
    [EncryptedTextField( "Subscriber Code", "Subscriber code provided by Trak-1", true, "", "", 1, null, true )]
    [EncryptedTextField( "Company Code", "Company code provided by Trak-1", true, "", "", 2, null, true )]
    [UrlLinkField( "Package URL", "The Trak-1 URL to request package information.", true, "https://stgapi.trak-1.com/Trak1.WebServices/Integration/GetAvailablePackages", "", 3 )]
    [UrlLinkField( "Request URL", "The Trak-1 URL to send requests.", true, "https://stgapi.trak-1.com/Trak1.WebServices/Integration/RunPackage", "", 4 )]
    [UrlLinkField( "Status URL", "URL to get status of requests.", true, "https://stgapi.trak-1.com/Trak1.WebServices/Integration/GetReportStatus", "", 5 )]
    [UrlLinkField( "Report URL", "URL to get report of requests.", true, "https://stgapi.trak-1.com/Trak1.WebServices/Integration/GetReportUrl", "", 6 )]
    public class Trak1 : BackgroundCheckComponent
    {

        private Person GetCurrentPerson()
        {
            using ( var rockContext = new RockContext() )
            {
                var currentUser = new UserLoginService( rockContext ).GetByUserName( UserLogin.GetCurrentUserName() );
                return currentUser != null ? currentUser.Person : null;
            }
        }

        /// <summary>
        /// Sends a background request to Trak-1
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="workflow">The Workflow initiating the request.</param>
        /// <param name="personAttribute">The person attribute.</param>
        /// <param name="ssnAttribute">The SSN attribute.</param>
        /// <param name="requestTypeAttribute">The request type attribute.</param>
        /// <param name="billingCodeAttribute">The billing code attribute.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns>
        /// True/False value of whether the request was successfully sent or not
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <remarks>
        /// Note: If the associated workflow type does not have attributes with the following keys, they
        /// will automatically be added to the workflow type configuration in order to store the results
        /// of the background check request
        ///     RequestStatus:          The request status returned by request
        ///     RequestMessage:         Any error messages returned by request
        ///     ReportStatus:           The report status returned
        ///     ReportLink:             The location of the background report on server
        ///     ReportRecommendation:   Recomendataion
        ///     Report (BinaryFile):    The downloaded background report
        /// </remarks>
        public override bool SendRequest( RockContext rockContext, Rock.Model.Workflow workflow,
            AttributeCache personAttribute, AttributeCache ssnAttribute, AttributeCache requestTypeAttribute,
            AttributeCache billingCodeAttribute, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            try
            {
                // Check to make sure workflow is not null
                if ( workflow == null )
                {
                    errorMessages.Add( "Trak-1 background check provider requires a valid workflow." );
                    return false;
                }

                // Get the person that the request is for
                Person person = null;
                if ( personAttribute != null )
                {
                    Guid? personAliasGuid = workflow.GetAttributeValue( personAttribute.Key ).AsGuidOrNull();
                    if ( personAliasGuid.HasValue )
                    {
                        person = new PersonAliasService( rockContext ).Queryable()
                            .Where( p => p.Guid.Equals( personAliasGuid.Value ) )
                            .Select( p => p.Person )
                            .FirstOrDefault();
                        person.LoadAttributes( rockContext );
                    }
                }

                if ( person == null )
                {
                    errorMessages.Add( "Trak-1 background check provider requires the workflow to have a 'Person' attribute that contains the person who the background check is for." );
                    return false;
                }

                //Get required fields from workflow
                var packageList = GetPackageList();
                var packageName = workflow.GetAttributeValue( requestTypeAttribute.Key );
                var package = packageList.Where( p => p.PackageName == packageName ).FirstOrDefault();
                if ( package == null )
                {
                    errorMessages.Add( "Package name not valid" );
                    return false;
                }

                var requiredFields = package.Components.SelectMany( c => c.RequiredFields ).ToList();
                var requiredFieldsDict = new Dictionary<string, string>();
                foreach ( var field in requiredFields )
                {
                    if ( !workflow.Attributes.ContainsKey( field.Name ) )
                    {
                        errorMessages.Add( "Workflow does not contain attribute for required field " + field.Name );
                        return false;
                    }
                    requiredFieldsDict[field.Name] = workflow.GetAttributeValue( field.Name );
                }


                //Generate Request
                var authentication = new Trak1Authentication
                {
                    UserName = GetAttributeValue( "UserName" ),
                    SubscriberCode = Encryption.DecryptString( GetAttributeValue( "SubscriberCode" ) ),
                    CompanyCode = Encryption.DecryptString( GetAttributeValue( "CompanyCode" ) ),
                    BranchName = "Main"
                };

                var ssn = "";
                if ( ssnAttribute != null )
                {
                    ssn = Rock.Field.Types.SSNFieldType.UnencryptAndClean( workflow.GetAttributeValue( ssnAttribute.Key ) );
                    if ( !string.IsNullOrWhiteSpace( ssn ) && ssn.Length == 9 )
                    {
                        ssn = ssn.Insert( 5, "-" ).Insert( 3, "-" );
                    }
                }

                var homeLocation = person.GetHomeLocation();

                var applicant = new Trak1Applicant
                {
                    SSN = ssn,
                    FirstName = person.FirstName,
                    MiddleName = person.MiddleName,
                    LastName = person.LastName,
                    Email = person.Email,
                    DateOfBirth = ( person.BirthDate ?? new DateTime() ).ToString( "yyyy-MM-dd" ),
                    Address1 = homeLocation.Street1,
                    Address2 = homeLocation.Street2,
                    City = homeLocation.City,
                    State = homeLocation.State,
                    Zip = homeLocation.PostalCode,
                    RequiredFields = requiredFieldsDict
                };

                var request = new Trak1Request
                {
                    Authentication = authentication,
                    Applicant = applicant,
                    PackageName = packageName
                };


                var content = JsonConvert.SerializeObject( request );

                Trak1Response response = null;

                using ( var client = new HttpClient( new HttpClientHandler() ) )
                {
                    client.BaseAddress = new Uri( GetAttributeValue( "RequestURL" ) );
                    var clientResponse = client.PostAsync( "", new StringContent( content, Encoding.UTF8, "application/json" ) ).Result.Content.ReadAsStringAsync().Result;
                    response = JsonConvert.DeserializeObject<Trak1Response>( clientResponse );
                }

                if ( !string.IsNullOrWhiteSpace( response.Error?.Message ) )
                {
                    errorMessages.Add( response.Error.Message );
                    return false;
                }

                var transactionId = response.TransactionId;


                int? personAliasId = person.PrimaryAliasId;

                if ( personAliasId.HasValue )
                {
                    // Create a background check file
                    using ( var newRockContext = new RockContext() )
                    {
                        var backgroundCheckService = new BackgroundCheckService( newRockContext );
                        var backgroundCheck = backgroundCheckService.Queryable()
                            .Where( c =>
                                c.WorkflowId.HasValue &&
                                c.WorkflowId.Value == workflow.Id )
                            .FirstOrDefault();

                        if ( backgroundCheck == null )
                        {
                            backgroundCheck = new Rock.Model.BackgroundCheck();
                            backgroundCheck.PersonAliasId = personAliasId.Value;
                            backgroundCheck.WorkflowId = workflow.Id;
                            backgroundCheckService.Add( backgroundCheck );
                        }

                        backgroundCheck.RequestDate = RockDateTime.Now;
                        backgroundCheck.RequestId = transactionId.ToString();
                        backgroundCheck.PackageName = request.PackageName;
                        newRockContext.SaveChanges();
                    }
                }
                return true;
            }

            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, null );
                errorMessages.Add( ex.Message );
                return false;
            }
        }

        public List<Trak1Package> GetPackageList()
        {
            var packageUrl = GetAttributeValue( "PackageURL" );
            var subscriberCode = Encryption.DecryptString( GetAttributeValue( "SubscriberCode" ) );
            var companyCode = Encryption.DecryptString( GetAttributeValue( "CompanyCode" ) );

            List<Trak1Package> packages = null;

            using ( var client = new HttpClient( new HttpClientHandler() ) )
            {
                var url = string.Format( "{0}/{1}/{2}", packageUrl, subscriberCode, companyCode );
                client.BaseAddress = new Uri( url );
                var clientResponse = client.GetAsync( "" ).Result.Content.ReadAsStringAsync().Result;
                packages = JsonConvert.DeserializeObject<List<Trak1Package>>( clientResponse );
            }
            return packages;
        }

        public override string GetReportUrl( string reportKey )
        {
            var isAuthorized = this.IsAuthorized( Rock.Security.Authorization.VIEW, this.GetCurrentPerson() );

            if ( isAuthorized )
            {
                var filePath = System.Web.VirtualPathUtility.ToAbsolute( "~/GetFile.ashx" );
                return string.Format( "{0}?guid={1}", filePath, reportKey );
            }
            else
            {
                return "Unauthorized";
            }
        }
    }
}