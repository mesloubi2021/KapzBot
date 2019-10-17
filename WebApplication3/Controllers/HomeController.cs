﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration configuration;
        private static readonly string CommandUrl = "https://api.telegram.org/bot{0}/{1}";
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly HttpClient imageHttpClient = new HttpClient();

        public HomeController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var token = configuration.GetValue<string>("TelegramToken");

            var url = string.Format(CommandUrl, token, "getWebhookInfo");
            var content = await httpClient.GetStringAsync(url);

            ViewData["hookinfo"] = content;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendPhoto()
        {
            var photo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Screenshot.jpg");

            var subscriptionKey = configuration.GetValue<string>("COMPUTER_VISION_SUBSCRIPTION_KEY");
            var endpoint = configuration.GetValue<string>("COMPUTER_VISION_ENDPOINT");
            var uriBase = endpoint + "vision/v2.1/ocr";


            imageHttpClient.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            HttpResponseMessage response;

            // Two REST API methods are required to extract text.
            // One method to submit the image for processing, the other method
            // to retrieve the text found in the image.

            // operationLocation stores the URI of the second REST API method,
            // returned by the first REST API method.
            string operationLocation;

            // Reads the contents of the specified local image
            // into a byte array.
            byte[] byteData = GetImageAsByteArray(photo);

            // Adds the byte array as an octet stream to the request body.
            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses the "application/octet-stream" content type.
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                // The first REST API method, Batch Read, starts
                // the async process to analyze the written text in the image.
                response = await imageHttpClient.PostAsync(uriBase, content);
            }

            // Asynchronously get the JSON response.
            string contentString = await response.Content.ReadAsStringAsync();

            ViewData["result"] = contentString;
            return View();

        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            // Open a read-only file stream for the specified file.
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                // Read the file's contents into a byte array.
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
