using System;
using System.Linq;
using System.Net;
using System.Net.Http;

using Rock;
using System.Web.Http;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using System.Web;
using System.IO;
using Rock.Utility;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// REST API for Groups
    /// </summary>
    public partial class GroupExtensionsController : Rock.Rest.ApiControllerBase
    {

        /// <summary>
        /// Get a group photo from Rock
        /// </summary>
        /// <returns>A Profile object</returns>
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Groups/GetPhoto/{id}")]
        [HttpGet]
        public string GetPhoto(int id)
        {
            RockContext context = new RockContext();
            GroupService groupService = new GroupService(context);
            BinaryFileService binaryFileService = new BinaryFileService(context);

            Group group = groupService.Get(id);
            if (group != null) { 
                group.LoadAttributes();
                BinaryFile photo = binaryFileService.Get(group.GetAttributeValue("GroupPhoto").AsGuid());
                if (photo != null)
                {
                    return photo.Url;
                }
            }
            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Update a group photo from Rock
        /// </summary>
        /// <returns>A Profile object</returns>
        [Authenticate, Secured]
        [System.Web.Http.Route("api/Groups/UploadPhoto/{id}")]
        [HttpPost]
        public string UploadPhoto(int id)
        {

            RockContext context = new RockContext();

            GroupService groupService = new GroupService(context);
            BinaryFileService binaryFileService = new BinaryFileService(context);
            AttributeService attributeService = new AttributeService(context);

            Group group = groupService.Get(id);
            if (group != null)
            {
                group.LoadAttributes();
                if (group.Attributes.ContainsKey("GroupPhoto"))
                {

                    Rock.Model.Attribute attribute = attributeService.Get(group.Attributes["GroupPhoto"].Id);
                    string qualifierValue = attribute.EntityTypeQualifierValue;
                    // Make sure a file was uploaded
                    var files = HttpContext.Current.Request.Files;
                    var uploadedFile = files.AllKeys.Select(fk => files[fk]).FirstOrDefault();
                    Guid binaryFileTypeGuid = attribute.AttributeQualifiers.AsQueryable().Where(aq => aq.Key == "binaryFileType").First().Value.AsGuid();
                    var binaryFileType = new BinaryFileTypeService(context).Get(binaryFileTypeGuid);

                    if (uploadedFile == null)
                    {
                        GenerateResponse(HttpStatusCode.BadRequest, "No file was sent");
                    }

                    if (binaryFileType == null)
                    {
                        GenerateResponse(HttpStatusCode.InternalServerError, "Invalid binary file type");
                    }

                    if (!binaryFileType.IsAuthorized(Rock.Security.Authorization.EDIT, GetPerson()))
                    {
                        GenerateResponse(HttpStatusCode.Unauthorized, "Not authorized to upload this type of file");
                    }

                    BinaryFile binaryFile = null;
                    if (group.GetAttributeValue("GroupPhoto").AsGuidOrNull().HasValue)
                    {
                        binaryFile = binaryFileService.Get(group.GetAttributeValue("GroupPhoto").AsGuid());
                    } else
                    {
                        binaryFile = new BinaryFile();
                        binaryFileService.Add(binaryFile);
                    }

                    binaryFile.IsTemporary = false;
                    binaryFile.BinaryFileTypeId = binaryFileType.Id;
                    binaryFile.MimeType = uploadedFile.ContentType;
                    binaryFile.FileName = Path.GetFileName(uploadedFile.FileName);
                    binaryFile.ContentStream = FileUtilities.GetFileContentStream(uploadedFile);

                    context.SaveChanges();

                    return binaryFile.Url;
                }

            }
            throw new HttpResponseException(HttpStatusCode.NotFound);
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
    }
}