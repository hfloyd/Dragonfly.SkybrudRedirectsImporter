namespace Dragonfly.SkybrudRedirectsImporter.Controllers.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.UI.WebControls;
    using Dragonfly.NetModels;
    using Dragonfly.SkybrudRedirectsImporter.Models;
    using Dragonfly.SkybrudRedirectsImporter.Utilities;
    using Dragonfly.UmbracoHelpers;
    using Skybrud.Umbraco.Redirects.Exceptions;
    using Skybrud.Umbraco.Redirects.Models;
    using Skybrud.Umbraco.Redirects.Models.Options;
    using Umbraco.Core.Models;
    using Umbraco.Web.WebApi;
    using Constants = Dragonfly.SkybrudRedirectsImporter.Constants;


    // [IsBackOffice]
    // GET: /Umbraco/backoffice/Api/ImportApi <-- UmbracoAuthorizedApiController

    [IsBackOffice]
    public partial class ImportApiController : UmbracoAuthorizedApiController
    {
        private readonly IRedirectsService _redirectsService;

        public ImportApiController(IRedirectsService SkybrudRedirectsService)
        {
            _redirectsService = SkybrudRedirectsService;
        }

        /// /Umbraco/backoffice/Api/ImportApi/Start
        [System.Web.Http.AcceptVerbs("GET")]
        public HttpResponseMessage Start()
        {
            var returnSB = new StringBuilder();
            var returnStatusMsg = new StatusMessage(true); //assume success
            var pvPath = Constants.RazorViewsPath + "Start.cshtml";
            var specialMessage = "";
            var specialMessageClass = "bg-info";

            //Setup
            //var testerService = SetupServices();
            //testerService.TestAllPublishedNodes();
            //var results = testerService.GetResultSet();


            //GET DATA TO DISPLAY
            IEnumerable<FileInfo> exampleFiles;
            var examplesFolder = Constants.AppPluginsPath + "Examples";
            var examplesMsg = FilesIO.GetListOfFiles(examplesFolder, out exampleFiles);

            IEnumerable<FileInfo> filesList;
            var filesMsg = FilesIO.GetListOfFiles("", out filesList);

            var formInputs = new FormInputsImport();
            formInputs.Type = Constants.RedirectType.Permanent;
            formInputs.ForwardQueryString = false;
            formInputs.AvailableRootNodes = ImportHelper.GetRootNodes(_redirectsService, Services.ContentService);

            //UPDATE STATUS MSG
            //returnStatusMsg.InnerStatuses.Add(filesMsg);
            //returnStatusMsg.Success = filesMsg.Success;
            //if (!filesList.Any())
            //{
            //    returnStatusMsg.Success = false;
            //    returnStatusMsg.Message = "There are no Test Result Sets available.";
            //    returnStatusMsg.Code = "NoResultSetFiles";
            //}

            //VIEW DATA 
            var viewData = new ViewDataDictionary();
            viewData.Model = returnStatusMsg;
            // viewData.Add("RazorViewsPath", Constants.RazorViewsPath);
            viewData.Add("SpecialMessage", specialMessage);
            viewData.Add("SpecialMessageClass", specialMessageClass);
            viewData.Add("ExampleFiles", exampleFiles);
            viewData.Add("FilesList", filesList);
            viewData.Add("FormInputs", formInputs);


            //RENDER
            var controllerContext = this.ControllerContext;
            var displayHtml =
                ApiControllerHtmlHelper.GetPartialViewHtml(controllerContext, pvPath, viewData, HttpContext.Current);
            returnSB.AppendLine(displayHtml);

            //RETURN AS HTML
            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    returnSB.ToString(),
                    Encoding.UTF8,
                    "text/html"
                )
            };
        }

        /// /Umbraco/backoffice/Api/ImportApi/ImportRedirects
        [System.Web.Http.HttpPost]
        public HttpResponseMessage ImportRedirects(FormInputsImport FormInputs)
        {
            var returnSB = new StringBuilder();
            var returnStatusMsg = new StatusMessage(true); //assume success
            var pvPath = $"{Constants.RazorViewsPath}ImportResults.cshtml";

            //Setup 
            var resultsSet = new ImportResultSet();
            resultsSet.FormInputs = FormInputs;
            var errorItems = new List<ImportErrorItem>();
            var newRedirects = new List<RedirectItem>();
            var options = new AddRedirectOptions();

            //GET DATA TO DISPLAY
            var specialMessage = "";
            var specialMsgClass = "bg-info text-white";

            if (FormInputs == null)
            {
                returnStatusMsg.Success = false;
                returnStatusMsg.Message = $"Form Inputs data was missing.";
            }
            else
            {
                options.ForwardQueryString = FormInputs.ForwardQueryString;
                options.IsPermanent = FormInputs.Type == Constants.RedirectType.Permanent;
                options.RootNodeId = FormInputs.SiteRootNode;

                //Get list from file
                var importsDataModel = new ImportData();

                //Convert CSV to Model
                var statusConversion = CsvConverter.ConvertImportDataCsvToModel(FormInputs.Filename, out importsDataModel);
                returnStatusMsg.InnerStatuses.Add(statusConversion);
                if (!statusConversion.Success)
                {
                    returnStatusMsg.Success = false;
                    returnStatusMsg.Message = statusConversion.Message;
                }

                if (importsDataModel.Items.Any())
                {
                    //Setup
                    var siteRoot = FormInputs.SiteRootNode;
                    var allContentNodes = siteRoot > 0 ? NodesHelper.AllContentNodes(Umbraco, siteRoot).ToList() : NodesHelper.AllContentNodes(Umbraco).ToList();
                    var allMediaNodes = NodesHelper.AllMediaNodes(Umbraco).ToList();

                    foreach (var import in importsDataModel.Items)
                    {
                        bool isError = false;

                        if (string.IsNullOrWhiteSpace(import.OldUrl))
                        {
                            isError = true;
                            var errItem = new ImportErrorItem(import, "No Old Url provided.");
                            errorItems.Add(errItem);
                        }

                        if (string.IsNullOrWhiteSpace(import.NewUrl))
                        {
                            isError = true;
                            var errItem = new ImportErrorItem(import, "No New Url provided.");
                            errorItems.Add(errItem);
                        }

                        if (!isError)
                        {
                            // Split the Old URL and query string
                            string[] oldUrlParts = import.OldUrl.Split('?');
                            string oldUrl = oldUrlParts[0].TrimEnd('/');
                            string oldQuery = oldUrlParts.Length == 2 ? oldUrlParts[1] : string.Empty;

                            string[] newUrlParts = import.NewUrl.Split('?');
                            string newQuery = newUrlParts.Length == 2 ? newUrlParts[1] : string.Empty;
                            string newUrl = newUrlParts[0].TrimEnd('/');
                            string newAnchor = "";
                            if (newUrl.Contains("#"))
                            {
                                var x = newUrl.Split('#');
                                newUrl = x[0].TrimEnd('/');
                                newAnchor = "#" + x[1];
                            }
                           

                            //Update Options
                            options.OriginalUrl = import.OldUrl;

                            //Try to determine type
                            if (newUrl.Contains("://"))
                            {
                                //full url, assume external
                                RedirectDestination destination = new RedirectDestination(0, Guid.Empty, import.NewUrl, RedirectDestinationType.Url);
                                options.Destination = destination;

                                // Add the External redirect
                                try
                                {
                                    RedirectItem redirect = _redirectsService.AddRedirect(options);
                                    newRedirects.Add(redirect);
                                }
                                catch (Exception e)
                                {
                                    isError = true;
                                    var errItem = new ImportErrorItem(import, e.Message);
                                    errorItems.Add(errItem);
                                }
                            }
                            else
                            {
                                //Try to lookup node
                                RedirectDestination destinationNode = null;

                                var matchingContent = allContentNodes.Where(n => n.Url.TrimEnd('/') == newUrl);
                                if (matchingContent.Any())
                                {
                                    // Content Link
                                    var node = matchingContent.First();
                                    destinationNode = new RedirectDestination(node.Id, node.Key, node.Url, import.NewUrl, RedirectDestinationType.Content);
                                }
                                else
                                {
                                    var xPeriod = newUrl.Split('.');
                                    var importExt = xPeriod[xPeriod.Length - 1];
                                    var matchingExts = allMediaNodes.Where(n => n.Url.EndsWith(importExt));

                                    var xSlash = newUrl.Split('/');
                                    var importFilename = xSlash[xSlash.Length - 1];
                                    var matchingFilename = allMediaNodes.Where(n => n.Url.EndsWith(importFilename));

                                    var matchingMedia = allMediaNodes.Where(n => n.Url == newUrl);
                                    if (matchingMedia.Any())
                                    {
                                        // Media Link
                                        var node = matchingMedia.First();
                                        destinationNode = new RedirectDestination(node.Id, node.Key, node.Url, RedirectDestinationType.Media);
                                    }
                                    else
                                    {
                                        isError = true;
                                        var errItem = new ImportErrorItem(import,
                                            "Unable to locate a node matching the New Url");
                                        errorItems.Add(errItem);
                                    }
                                }

                                if (!isError)
                                {
                                    // Add the redirect
                                    try
                                    {
                                        options.Destination = destinationNode;
                                        RedirectItem redirect = _redirectsService.AddRedirect(options);
                                        newRedirects.Add(redirect);
                                    }
                                    catch (Exception e)
                                    {
                                        isError = true;
                                        var errItem = new ImportErrorItem(import, e.Message);
                                        errorItems.Add(errItem);
                                    }
                                }
                            }
                        }
                    }

                    //All imports handled, update return data
                    resultsSet.ErrorItems = errorItems;
                    resultsSet.SuccessItems = newRedirects;
                    if (errorItems.Any())
                    {
                        resultsSet.HasError = true;
                        resultsSet.ErrorMessage = "Some Imports failed.";
                    }
                }
            }

            if (resultsSet.HasError)
            {
                specialMessage = resultsSet.ErrorMessage;
                specialMsgClass = "bg-danger text-white";
            }


            //VIEW DATA 
            var viewData = new ViewDataDictionary();
            viewData.Model = returnStatusMsg;
            viewData.Add("SpecialMessage", specialMessage);
            viewData.Add("SpecialMessageClass", specialMsgClass);
            viewData.Add("FormInputs", FormInputs);
            viewData.Add("Results", resultsSet);


            //RENDER
            var controllerContext = this.ControllerContext;
            var displayHtml = ApiControllerHtmlHelper.GetPartialViewHtml(controllerContext, pvPath, viewData, HttpContext.Current);
            returnSB.AppendLine(displayHtml);

            //RETURN AS HTML
            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    returnSB.ToString(),
                    Encoding.UTF8,
                    "text/html"
                )
            };
        }




        //[HttpPost]
        //public async Task<HttpResponseMessage> Import()
        //{
        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        throw new HttpResponseException(new HttpResponseMessage
        //        {
        //            StatusCode = HttpStatusCode.UnsupportedMediaType,
        //            Content = new StringContent("File must be a valid CSV or Excel file")
        //        });
        //    }

        //    var uploadFolder = HttpContext.Current.Server.MapPath(FileUploadPath);
        //    Directory.CreateDirectory(uploadFolder);
        //    var provider = new CustomMultipartFormDataStreamProvider(uploadFolder);

        //    var result = await Request.Content.ReadAsMultipartAsync(provider);

        //    var file = result.FileData[0];
        //    var path = file.LocalFileName;
        //    var ext = path.Substring(path.LastIndexOf('.')).ToLower();

        //    if (ext != ".csv" && ext != ".xlsx")
        //    {
        //        throw new HttpResponseException(new HttpResponseMessage
        //        {
        //            StatusCode = HttpStatusCode.UnsupportedMediaType,
        //            Content = new StringContent("File must be a valid CSV or Excel file")
        //        });
        //    }

        //    var fileNameAndPath = HttpContext.Current.Server.MapPath(FileUploadPath + string.Format(FileName, DateTime.Now.Ticks));

        //    File.Copy(file.LocalFileName, fileNameAndPath, true);

        //    var importer = new RedirectsImporterService();

        //    IRedirectsFile redirectsFile;

        //    switch (ext)
        //    {
        //        default:
        //            var csvFile = new CsvRedirectsFile(new RedirectPublishedContentFinder(UmbracoContext.ContentCache))
        //            {
        //                FileName = fileNameAndPath,
        //                Seperator = CsvSeparator.Comma
        //            };

        //            redirectsFile = csvFile;

        //            break;
        //    }

        //    var response = importer.Import(redirectsFile);

        //    using (var ms = new MemoryStream())
        //    {
        //        using (var outputFile = new FileStream(response.File.FileName, FileMode.Open, FileAccess.Read))
        //        {
        //            byte[] bytes = new byte[outputFile.Length];
        //            outputFile.Read(bytes, 0, (int)outputFile.Length);
        //            ms.Write(bytes, 0, (int)outputFile.Length);

        //            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
        //            httpResponseMessage.Content = new ByteArrayContent(bytes.ToArray());
        //            httpResponseMessage.Content.Headers.Add("x-filename", "redirects.csv");
        //            httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        //            httpResponseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
        //            httpResponseMessage.Content.Headers.ContentDisposition.FileName = "redirects.csv";
        //            httpResponseMessage.StatusCode = HttpStatusCode.OK;

        //            return httpResponseMessage;
        //        }
        //    }
        //}

        //public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
        //{
        //    public CustomMultipartFormDataStreamProvider(string path) : base(path) { }

        //    public override string GetLocalFileName(HttpContentHeaders headers)
        //    {
        //        return headers.ContentDisposition.FileName.Replace("\"", string.Empty);
        //    }
        //}
    }
}
