﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neodynamic.SDK.Web;
using Microsoft.AspNetCore.Hosting;

namespace WCPAspNetCoreCS.Controllers
{
    public class DemoPrintFileWithPwdProtectionController : Controller
    {
        private readonly IHostingEnvironment _hostEnvironment;


        public DemoPrintFileWithPwdProtectionController(IHostingEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;

        }

        public IActionResult Index()
        {
            ViewData["WCPScript"] = Neodynamic.SDK.Web.WebClientPrint.CreateScript(Url.Action("ProcessRequest", "WebClientPrintAPI", null, Url.ActionContext.HttpContext.Request.Scheme), Url.Action("PrintFile", "DemoPrintFileWithPwdProtection", null, Url.ActionContext.HttpContext.Request.Scheme), Url.ActionContext.HttpContext.Session.Id);

            return View();
        }

        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult PrintFile(string useDefaultPrinter, string printerName, string fileType, string wcp_pub_key_base64, string wcp_pub_key_signature_base64)
        {
            string fileName = Guid.NewGuid().ToString("N") + "." + fileType;
            string filePath = null;
            switch (fileType)
            {
                case "PDF":
                    filePath = "/files/LoremIpsum-PasswordProtected.pdf";
                    break;
                case "DOC":
                    filePath = "/files/LoremIpsum-PasswordProtected.doc";
                    break;
                case "XLS":
                    filePath = "/files/SampleSheet-PasswordProtected.xls";
                    break;
            }

            if (filePath != null && string.IsNullOrEmpty(wcp_pub_key_base64) == false)
            {
                //ALL the test files are protected with the same password for demo purposes 
                //This password will be encrypted and stored in file metadata
                string plainTextPassword = "ABC123";


                PrintFile file = null;
                if (fileType == "PDF")
                {
                    file = new PrintFilePDF(_hostEnvironment.ContentRootPath + filePath, fileName);
                    ((PrintFilePDF)file).Password = plainTextPassword;
                    //((PrintFilePDF)file).PrintRotation = PrintRotation.None;
                    //((PrintFilePDF)file).PagesRange = "1,2,3,10-15";
                    //((PrintFilePDF)file).PrintAnnotations = true;
                    //((PrintFilePDF)file).PrintAsGrayscale = true;
                    //((PrintFilePDF)file).PrintInReverseOrder = true;

                }
                else if (fileType == "DOC")
                {
                    file = new PrintFileDOC(_hostEnvironment.ContentRootPath + filePath, fileName);
                    ((PrintFileDOC)file).Password = plainTextPassword;
                    //((PrintFileDOC)file).PagesRange = "1,2,3,10-15";
                    //((PrintFileDOC)file).PrintInReverseOrder = true;
                }
                else if (fileType == "XLS")
                {
                    file = new PrintFileXLS(_hostEnvironment.ContentRootPath + filePath, fileName);
                    ((PrintFileXLS)file).Password = plainTextPassword;
                    //((PrintFileXLS)file).PagesFrom = 1;
                    //((PrintFileXLS)file).PagesTo = 3;
                }



                //create an encryption metadata to set to the PrintFile
                EncryptMetadata encMetadata = new EncryptMetadata(wcp_pub_key_base64, wcp_pub_key_signature_base64);

                //set encyption metadata to PrintFile to ENCRYPT the Password to unlock the file
                file.EncryptMetadata = encMetadata;

                //create ClientPrintJob for printing encrypted file
                ClientPrintJob cpj = new ClientPrintJob();
                cpj.PrintFile = file;

                if (useDefaultPrinter == "checked" || printerName == "null")
                    cpj.ClientPrinter = new DefaultPrinter();
                else
                    cpj.ClientPrinter = new InstalledPrinter(System.Net.WebUtility.UrlDecode(printerName));


                return File(cpj.GetContent(), "application/octet-stream");
            }
            else
            {
                return BadRequest("File not found!");
            }
        }

    }
}