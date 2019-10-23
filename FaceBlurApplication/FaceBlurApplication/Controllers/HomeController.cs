using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using FaceBlurApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Json.Net;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace FaceBlurApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _he;

        // Replace <Subscription Key> with your valid subscription key.
        const string subscriptionKey = "eabdfe0844ed4e0f89570260183ab6a7";

        // replace <myresourcename> with the string found in your endpoint URL
        const string uriBase =
            "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect";

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment he)
        {
            _logger = logger;
            _he = he;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult UploadImage(IFormFile img)
        {
            if (img != null)
            {
                var fileName = Path.Combine(_he.WebRootPath, Path.GetFileName(img.FileName));

                using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                {
                    img.CopyTo(fileStream);
                }

                ViewData["fileLocation"] = "/" + Path.GetFileName(img.FileName);

                try
                {
                    MakeAnalysisRequest(fileName);
                    Console.WriteLine("\nWait a moment for the results to appear.\n");
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n" + e.Message + "\nPress Enter to exit...\n");
                }
            }

            return View();
        }

        // Gets the analysis of the specified image by using the Face REST API.
        static async void MakeAnalysisRequest(string imageFilePath)
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                BlurFaces(GetFaceRectangles(contentString), imageFilePath);
            }
        }

        // Returns the contents of the specified file as a byte array.
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        static JArray GetFaceRectangles(string contentString)
        {
            var data = JArray.Parse(contentString);
            var faceRectangles = new JArray();

            if (data.Count > 0)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    faceRectangles.Add(data[i]["faceRectangle"]);                   
                }
            }

            return faceRectangles;
        }

        static void BlurFaces(JArray facesArray, string path)
        {
            Bitmap bitmap = new Bitmap(path);

       
            for (int i = 0; i < facesArray.Count; i++)
            {
                var rectangle = new Rectangle(
                    (int)facesArray[i]["top"],
                    (int)facesArray[i]["left"],
                    (int)facesArray[i]["width"],
                    (int)facesArray[i]["height"]
                    );

                bitmap = Blur(bitmap, 10, rectangle);

            }

            bitmap.Save("test.jpg");

            Console.WriteLine("done");
        }
        private static Bitmap Blur(Bitmap image, Int32 blurSize, Rectangle rectToBlur)
        {
            return Blur(image, new Rectangle(rectToBlur.Top, rectToBlur.Left, rectToBlur.Width, rectToBlur.Height), blurSize);
        }

        private static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            Bitmap blurred = new Bitmap(image);   //image.Width, image.Height);
            using (Graphics graphics = Graphics.FromImage(blurred))
            {
                // look at every pixel in the blur rectangle
                for (Int32 xx = rectangle.Left; xx < rectangle.Right; xx += blurSize)
                {
                    for (Int32 yy = rectangle.Top; yy < rectangle.Bottom; yy += blurSize)
                    {
                        Int32 avgR = 0, avgG = 0, avgB = 0;
                        Int32 blurPixelCount = 0;
                        Rectangle currentRect = new Rectangle(xx, yy, blurSize, blurSize);

                        // average the color of the red, green and blue for each pixel in the
                        // blur size while making sure you don't go outside the image bounds
                        for (Int32 x = currentRect.Left; (x < currentRect.Right && x < image.Width); x++)
                        {
                            for (Int32 y = currentRect.Top; (y < currentRect.Bottom && y < image.Height); y++)
                            {
                                Color pixel = blurred.GetPixel(x, y);

                                avgR += pixel.R;
                                avgG += pixel.G;
                                avgB += pixel.B;

                                blurPixelCount++;
                            }
                        }

                        avgR = avgR / blurPixelCount;
                        avgG = avgG / blurPixelCount;
                        avgB = avgB / blurPixelCount;

                        // now that we know the average for the blur size, set each pixel to that color
                        graphics.FillRectangle(new SolidBrush(Color.FromArgb(avgR, avgG, avgB)), currentRect);
                    }
                }
                graphics.Flush();
            }
            return blurred;
        }


    }
}