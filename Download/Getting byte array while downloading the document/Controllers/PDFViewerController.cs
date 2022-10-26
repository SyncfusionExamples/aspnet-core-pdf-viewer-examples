using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Syncfusion.EJ2.PdfViewer;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Drawing;
using Microsoft.JSInterop;

namespace Getbytearraywhiledownload1.Controllers.PdfViewer
{
    public partial class PdfViewerController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        //Initialize the memory cache object
        public IMemoryCache _cache;
        [Microsoft.AspNetCore.Components.Inject]
        IJSRuntime JSRuntime { get; set; }
        public PdfViewerController(IHostingEnvironment hostingEnvironment, IMemoryCache cache)
        {
            _hostingEnvironment = hostingEnvironment;
            _cache = cache;
        }
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }
        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/Load")]
        //Post action for Loading the PDF documents
        public IActionResult Load([FromBody] Dictionary<string, string> jsonObject)
        {
            Console.WriteLine("Load called");
            //Initialize the PDF viewer object with memory cache object
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            MemoryStream stream = new MemoryStream();
            object jsonResult = new object();
            if (jsonObject != null && jsonObject.ContainsKey("document"))
            {
                if (bool.Parse(jsonObject["isFileName"]))
                {
                    string documentPath = GetDocumentPath(jsonObject["document"]);
                    if (!string.IsNullOrEmpty(documentPath))
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(documentPath);
                        stream = new MemoryStream(bytes);
                    }
                    else
                    {
                        string fileName = jsonObject["document"].Split(new string[] { "://" }, StringSplitOptions.None)[0];

                        if (fileName == "http" || fileName == "https")
                        {
                            WebClient WebClient = new WebClient();

                            byte[] pdfDoc = WebClient.DownloadData(jsonObject["document"]);

                            stream = new MemoryStream(pdfDoc);
                        }
                        else
                        {
                            return this.Content(jsonObject["document"] + " is not found");
                        }
                    }
                }
                else
                {
                    byte[] bytes = Convert.FromBase64String(jsonObject["document"]);
                    stream = new MemoryStream(bytes);
                }
            }
            jsonResult = pdfviewer.Load(stream, jsonObject);
            return Content(JsonConvert.SerializeObject(jsonResult));
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/RenderPdfPages")]
        public IActionResult RenderPdfPages([FromBody] Dictionary<string, string> jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            object jsonResult = pdfviewer.GetPage(jsonObject);
            return Content(JsonConvert.SerializeObject(jsonResult));
        }
       
        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/RenderAnnotationComments")]
        public IActionResult RenderAnnotationComments([FromBody] Dictionary<string, string> jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            object jsonResult = pdfviewer.GetAnnotationComments(jsonObject);
            return Content(JsonConvert.SerializeObject(jsonResult));
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/Unload")]
        //Post action for unloading and disposing the PDF document resources 
        public IActionResult Unload([FromBody] Dictionary<string, string> jsonObject)
        {
            //Initialize the PDF Viewer object with memory cache object
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            pdfviewer.ClearCache(jsonObject);
            return this.Content("Document cache is cleared");
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/RenderThumbnailImages")]
        public IActionResult RenderThumbnailImages([FromBody] Dictionary<string, string> jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            object result = pdfviewer.GetThumbnailImages(jsonObject);
            return Content(JsonConvert.SerializeObject(result));
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/Bookmarks")]
        //Post action for processing the bookmarks from the PDF documents
        public IActionResult Bookmarks([FromBody] Dictionary<string, string> jsonObject)
        {
            //Initialize the PDF Viewer object with memory cache object
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            object jsonResult = pdfviewer.GetBookmarks(jsonObject);
            return Content(JsonConvert.SerializeObject(jsonResult));
        }
        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/ExportAnnotations")]
       
        //Post action to export annotations
        public IActionResult ExportAnnotations([FromBody] Dictionary<string, string> jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            string jsonResult = pdfviewer.ExportAnnotation(jsonObject);
            return Content(jsonResult);
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/ImportAnnotations")]
        //Post action to import annotations
        public IActionResult ImportAnnotations([FromBody] Dictionary<string, string> jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            string jsonResult = string.Empty;
            object JsonResult;
            if (jsonObject != null && jsonObject.ContainsKey("fileName"))
            {
                
                 string basePath = _hostingEnvironment.WebRootPath;
                string dataPath = string.Empty;
                dataPath = basePath + @"/Data/HTTP Succinctly.json";
                if (!string.IsNullOrEmpty(dataPath))
                {
                    jsonResult = System.IO.File.ReadAllText(dataPath);
                }
                else
                {
                    return this.Content(jsonObject["document"] + " is not found");
                }
            }
            else
            {
                string extension = Path.GetExtension(jsonObject["importedData"]);
                if (extension != ".xfdf")
                {
                    JsonResult = pdfviewer.ImportAnnotation(jsonObject);
                    return Content(JsonConvert.SerializeObject(JsonResult));
                }
                else
                {
                    string documentPath = GetDocumentPath(jsonObject["importedData"]);
                    if (!string.IsNullOrEmpty(documentPath))
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(documentPath);
                        jsonObject["importedData"] = Convert.ToBase64String(bytes);
                        JsonResult = pdfviewer.ImportAnnotation(jsonObject);
                        return Content(JsonConvert.SerializeObject(JsonResult));
                    }
                    else
                    {
                        return this.Content(jsonObject["document"] + " is not found");
                    }
                }
            }
            return Content(jsonResult);
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/ImportFormFields")]
        public IActionResult ImportFormFields([FromBody] Dictionary<string, string> jsonObject)

        {

            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            object jsonResult = pdfviewer.ImportFormFields(jsonObject);
            return Content(JsonConvert.SerializeObject(jsonResult));
        }
        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/ExportFormFields")]
        public IActionResult ExportFormFields([FromBody] Dictionary<string, string> jsonObject)

        {

            PdfRenderer pdfviewer = new PdfRenderer(_cache);

            string jsonResult = pdfviewer.ExportFormFields(jsonObject);

            return Content(jsonResult);

        }
        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/Download")]
        //Post action for downloading the PDF documents
        public async void Download([FromBody] Dictionary<string, string> jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            //Gets the PDF document as base64 string.
            string documentBase = pdfviewer.GetDocumentAsBase64(jsonObject);
            //Converts the specified string, which encodes binary data as base-64 digits, to an equivalent 8-bit unsigned integer array.
            byte[] bytes = Convert.FromBase64String(documentBase.Split("base64,")[1]);
            //Prints the base64 string in the output window.
            Console.WriteLine(documentBase.ToString());
            Content(documentBase);
        }

        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/PrintImages")]
        //Post action for printing the PDF documents
        public IActionResult PrintImages([FromBody] Dictionary<string, string> jsonObject)
        {
            //Initialize the PDF Viewer object with memory cache object
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            object pageImage = pdfviewer.GetPrintImage(jsonObject);
            return Content(JsonConvert.SerializeObject(pageImage));
        }
        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/RenderPdfTexts")]
        public IActionResult RenderPdfTexts([FromBody] Dictionary<string, string> jsonObject)
        {
            PdfRenderer pdfviewer = new PdfRenderer(_cache);
            object result = pdfviewer.GetDocumentText(jsonObject);
            return Content(JsonConvert.SerializeObject(result));
        }
        //Gets the path of the PDF document
        private string GetDocumentPath(string document)
        {
            string documentPath = string.Empty;
            if (!System.IO.File.Exists(document))
            {
                string basePath = _hostingEnvironment.WebRootPath;
                string dataPath = string.Empty;
                dataPath = basePath + @"/Data/";
                if (System.IO.File.Exists(dataPath + document))
                    documentPath = dataPath + document;
            }
            else
            {
                documentPath = document;
            }
            return documentPath;
        }

    }
}
