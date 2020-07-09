using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace stl_uploader_2
{
    class Program
    {

        const string UserName = "fikexa6458@box1mls.com";
        const string Password = "c9pEKLsbgsWyKLs";

        static string Stage;

        static async Task Main(string[] args)
        {
            try
            {
                Stage = "Checking the file";               
                if (args.Length == 0)
                    throw new Exception("Please run the program with a command-line argument containing the full path to the '.stl'file");
                string UploadFileName = args[0];
                if (!File.Exists(UploadFileName))
                    throw new Exception($"File '{UploadFileName}' not found");
                if (Path.GetExtension(UploadFileName).ToLower() != ".stl")
                    throw new Exception("The file must have the '.stl'extension");
                Console.WriteLine("Please wait..\n");
                
                //HttpClient.DefaultProxy = new WebProxy("127.0.0.1:8888");

                Stage = "Initialization";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Connection.Add("Keep-Alive");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:76.0) Gecko/20100101 Firefox/76.0");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");


                Stage = "Go to the main page";
                HttpResponseMessage response = await client.GetAsync("https://service.netfabb.com/login.php");
                var parser = new HtmlParser();
                var document = parser.ParseDocument(await response.Content.ReadAsStringAsync());
                string SignLink = document.QuerySelector("a[title='Autodesk Account']").GetAttribute("href");


                Stage = "Click on the login button";
                response = await client.GetAsync(SignLink);
                parser = new HtmlParser();
                var docForm = parser.ParseDocument(await response.Content.ReadAsStringAsync());
                string FormPostLink = "https://accounts.autodesk.com" + docForm.QuerySelector("#new_user_signin_form").GetAttribute("action");
                string RequestVerificationToken = docForm.QuerySelector("input[name='__RequestVerificationToken']").GetAttribute("value");
                string QueryStrings = docForm.QuerySelector("input[id='queryStrings']").GetAttribute("value");
                string SigninThrottledMessage = docForm.QuerySelector("input[id='signinThrottledMessage']").GetAttribute("value");
                using var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", RequestVerificationToken),
                    new KeyValuePair<string, string>("queryStrings", QueryStrings),
                    new KeyValuePair<string, string>("signinThrottledMessage", SigninThrottledMessage),
                    new KeyValuePair<string, string>("UserName", UserName),
                    new KeyValuePair<string, string>("Password", Password),
                    new KeyValuePair<string, string>("RememberMe", "false")
                 });


                Stage = "Sending an authorization form";
                response = await client.PostAsync(FormPostLink, content);
                using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(UploadFileName));                
                using var formContent = new MultipartFormDataContent();
                formContent.Add(fileContent);
                formContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.ms-pki.stl");
                formContent.Headers.Add("Content-Disposition", $"attachment;filename=\"{Path.GetFileName(UploadFileName)}\"");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://service.netfabb.com/upload/upload.php");
                request.Content = formContent;


                Stage = "Sending an authorization form";
                response = await client.SendAsync(request);            
                JsonFileInfo jsonFileInfo = JsonConvert.DeserializeObject<JsonFileInfo>(await response.Content.ReadAsStringAsync());
                // проверяем, обработан ли файл
                Stage = "Check the processing status of the file";
                JsonStatusInfo jsonStatusInfo;
                do
                {
                    Thread.Sleep(500);
                    response = await client.GetAsync("https://service.netfabb.com/upload/process.php?action=status&job=" + jsonFileInfo.jobuuid.ToString());
                    jsonStatusInfo = JsonConvert.DeserializeObject<JsonStatusInfo>(response.Content.ReadAsStringAsync().Result);
                } while (jsonStatusInfo.jobstatus == "PROCESSING");
                if (jsonStatusInfo.jobstatus != "SUCCESS")
                    throw new Exception($"The file processing status was received {jsonStatusInfo.jobstatus}");

                Stage = "File download";
                response = await client.GetAsync($"https://service.netfabb.com/upload/download.php/{Path.GetFileNameWithoutExtension(UploadFileName)}_fixed.stl?job=" + jsonFileInfo.jobuuid.ToString());
                string DownloadFileName = Path.GetDirectoryName(UploadFileName) + "\\" + Path.GetFileNameWithoutExtension(UploadFileName) + "_fixed.stl";
                await File.WriteAllBytesAsync(DownloadFileName, await response.Content.ReadAsByteArrayAsync());


                Console.WriteLine($"SUCCESS! {DownloadFileName}");
                Console.WriteLine("Press any key to exit..");
                Console.ReadKey();

                // ссылка кнопки "напомнить позже" - пропустить настройку двухфакторной аутентификации
                //https://auth.autodesk.com/as/9gkNZ/resume/as/authorization.ping?opentoken=T1RLAQJbtqwxpivN0Gsl3Aga1T6wMSXSKhBOQby9urvP4fsVcvQ_kEtRAACArZ2aEDEkM1fCHRIxweaTeonVmA0I56qS9qS4NetR-7sVO_bdj-CbVeyRywio9SPvQOQNPiuScNj-HgbvlSQYdlV5OaguOGrGEY6wxt3w2wVo5TaurpLv4ibqTxE4ecoXRwYEflMpeTCsch_yimLXJIraMP4BB_rIKPWrGd79cNc*&lang=ru
            }
            catch (Exception E)
            {
                Console.WriteLine($"Stage: {Stage}");
                Console.WriteLine($"An error occurred: {E.Message}");
                Console.WriteLine("Press any key to exit..");
                Console.ReadKey();
            }
        }
    }
}
