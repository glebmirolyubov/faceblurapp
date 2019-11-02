﻿using System;
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
using HtmlAgilityPack;
using System.Net;
using System.Web;

namespace FaceBlurApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _he;
        private string _contentString;

        const string subscriptionKey = "3d8c0a687bb64a27903b162a2badaf89";

        const string uriBase =
            "https://vova.cognitiveservices.azure.com/face/v1.0/detect";

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

        async Task LoadWebpage(string url)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(url);
            var pageContents = await response.Content.ReadAsStringAsync();

            //Console.WriteLine(pageContents);

            //ViewData["LoadedWebpage"] = pageContents;

            GetAllImages(pageContents, url);
        }

        public void GetAllImages(string pageContent, string url)
        {
            var document = new HtmlDocument();
            document.LoadHtml(pageContent);

            var urls = document.DocumentNode.Descendants("img")
                                .Select(e => e.GetAttributeValue("src", null))
                                .Where(s => !String.IsNullOrEmpty(s));

            var uri = new Uri(url);
            var baseUri = uri.GetLeftPart(System.UriPartial.Authority);
            int count = 0;

            /*
            int count = 0;


            foreach (var item in urls)
            {
                if (CheckURLCorrect(item) == true)
                {
                    System.Drawing.Image image = DownloadImageFromUrl(item);

                    string rootPath = _he.WebRootPath;
                    string fileName = System.IO.Path.Combine(rootPath, count+".jpg");
                    image.Save(fileName);
                    count++;

                    //Task t = MakeAnalysisRequest(fileName);
                    //t.Wait();

                    //BlurFaces(GetFaceRectangles(_contentString), fileName);
                } else
                {
                    string absoluteURL = baseUri + item;

                    if (CheckURLCorrect(absoluteURL) == true)
                    
                        Console.WriteLine("CORRECT!: "+absoluteURL);                  
                }
            }
            */


            foreach (HtmlNode node in document.DocumentNode.SelectNodes("//img[@src]"))
            {
                var src = node.Attributes["src"].Value.Split('?');

                if (CheckURLCorrect(src[0]) == true)
                {
                    try
                    {
                        System.Drawing.Image image = DownloadImageFromUrl(src[0]);

                        string rootPath = _he.WebRootPath;
                        string fileName = System.IO.Path.Combine(rootPath, count + ".jpg");
                        image.Save(fileName);
                        count++;

                        //Task t = MakeAnalysisRequest(fileName);
                        //t.Wait();

                        //BlurFaces(GetFaceRectangles(_contentString), fileName);
                    }
                    catch (Exception e)
                    {

                    }
                } 
                else
                {
                    var baseUrl = new Uri(baseUri);
                    var uriTest = new Uri(baseUrl, src[0]);

                    try
                    {
                        System.Drawing.Image image = DownloadImageFromUrl(uriTest.AbsoluteUri);

                        string rootPath = _he.WebRootPath;
                        string fileName = System.IO.Path.Combine(rootPath, count + ".jpg");
                        image.Save(fileName);
                        count++;
                    } 
                    catch( Exception e)
                    {
                        Console.WriteLine(e.Message);
                    } 
                }

                //node.SetAttributeValue("src", "https://sun9-63.userapi.com/c637928/v637928334/1a2f7/6Yqr9p7PyXE.jpg");

                //https://www.goethe.de/en/uun/org/pra.html USE THIS URL AS TEST
            }
            

            ViewData["LoadedWebpage"] = document.DocumentNode.OuterHtml;
        }

        public bool CheckURLCorrect(string url)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            return result;
        }

        public System.Drawing.Image DownloadImageFromUrl(string imageUrl)
        {
            System.Drawing.Image image = null;

            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;

                System.Net.WebResponse webResponse = webRequest.GetResponse();

                System.IO.Stream stream = webResponse.GetResponseStream();

                image = System.Drawing.Image.FromStream(stream);

                webResponse.Close();
            }
            catch (Exception ex)
            {
                return null;
            }

            return image;
        }

        public IActionResult WebpageWithBlurredFaces (string url)
        {
            Task t = LoadWebpage(url);
            t.Wait();

            return View();
        }

        public void UploadImage(IFormFile img)
        {
            if (img != null)
            {
                var fileName = Path.Combine(_he.WebRootPath, Path.GetFileName(img.FileName));

                try
                {
                    using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                    {
                        img.CopyTo(fileStream);
                    }
                }
                catch (Exception e)
                {
                    ViewData["FaceCount"] = "NoFaces";
                }

                ViewData["initialImage"] = "/" + Path.GetFileName(img.FileName);
                ViewData["Arrow"] = "/arrow.png";

                try
                {
                    Task t = MakeAnalysisRequest(fileName);
                    t.Wait();

                    BlurFaces(GetFaceRectangles(_contentString), fileName);

                    Console.WriteLine("\nWait a moment for the results to appear.\n");
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n" + e.Message + "\nPress Enter to exit...\n");
                }
            }
        }

        public async Task MakeAnalysisRequest(string imageFilePath)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                response = await client.PostAsync(uri, content);

                string contentString = await response.Content.ReadAsStringAsync();
                _contentString = contentString;
            }
        }


        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        public JArray GetFaceRectangles(string contentString)
        {
            ViewData["FaceCount"] = "NoFaces";
            var data = JArray.Parse(contentString);
            var faceRectangles = new JArray();

            if (data.Count > 0)
            {
                ViewData["FaceCount"] = "Faces";
                for (int i = 0; i < data.Count; i++)
                {
                    faceRectangles.Add(data[i]["faceRectangle"]);                   
                }
            }
            
            return faceRectangles;
        }

        public void BlurFaces(JArray facesArray, string path)
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

                bitmap = Blur(bitmap, 20, rectangle);
            }

            string filePath = Path.Combine(_he.WebRootPath, "blurred.jpg");

            bitmap.Save(filePath);
   
            ViewData["blurredImage"] = "/blurred.jpg";
        }
        private static Bitmap Blur(Bitmap image, Int32 blurSize, Rectangle rectToBlur)
        {
            return Blur(image, new Rectangle(rectToBlur.Top, rectToBlur.Left, rectToBlur.Width, rectToBlur.Height), blurSize);
        }

        private static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            Bitmap blurred = new Bitmap(image); 
            using (Graphics graphics = Graphics.FromImage(blurred))
            {
                for (Int32 xx = rectangle.Left; xx < rectangle.Right; xx += blurSize)
                {
                    for (Int32 yy = rectangle.Top; yy < rectangle.Bottom; yy += blurSize)
                    {
                        Int32 avgR = 0, avgG = 0, avgB = 0;
                        Int32 blurPixelCount = 0;
                        Rectangle currentRect = new Rectangle(xx, yy, blurSize, blurSize);

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

                        graphics.FillRectangle(new SolidBrush(Color.FromArgb(avgR, avgG, avgB)), currentRect);
                    }
                }
                graphics.Flush();
            }
            return blurred;
        }
    }
}