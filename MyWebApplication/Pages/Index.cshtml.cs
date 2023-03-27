using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyWebApplication.Pages
{
    public class IndexModel : PageModel
    {
        protected ILogger Logger { get; }
        protected IConfiguration Configuration { get; }
        protected IWebHostEnvironment WebHostEnvironment { get; }

        protected HttpClient PasHttpClient { get; }

        public IndexModel(IConfiguration configuration, ILogger<IndexModel> logger, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            Logger = logger;
            WebHostEnvironment = webHostEnvironment;
            PasHttpClient = PasUtil.CreatePasHttpClient(Configuration);
        }

        public string DocumentFilename { get; } = "example.pdf";
        public string ViewingSessionId { get; set; }

        public async Task OnGetAsync()
        {
            HttpResponseMessage response;
            string json;

            // 1. Create a new viewing session
            response = await PasHttpClient.PostAsync("ViewingSession", new StringContent(JsonConvert.SerializeObject(new
            {
                source = new
                {
                    type = "upload",
                    displayName = DocumentFilename
                }
            })));
            json = await response.Content.ReadAsStringAsync();

            // 2. Send the viewingSessionId and viewer assets to the browser right away so the viewer UI can start loading.
            ViewingSessionId = (string)JObject.Parse(json)["viewingSessionId"];

            // 3. Upload the source document to PrizmDoc so that it can start being converted to SVG.
            //    The viewer will request this content and receive it automatically once it is ready.
            //    We do this part on a background thread so that we don't block the HTML from being
            //    sent to the browser.
#pragma warning disable CS4014
            Task.Run(async () =>
            {
                try {
                    var filepath = Path.Combine(WebHostEnvironment.ContentRootPath, "Documents", DocumentFilename);
                    using var stream = System.IO.File.OpenRead(filepath);
                    var route = $"ViewingSession/u{ViewingSessionId}/SourceFile";
                    var content = new StreamContent(stream);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response = await PasHttpClient.PutAsync(route, content);
                    response.EnsureSuccessStatusCode();
                } catch (Exception e) {
                    Logger.LogError(e.ToString());
                }
            });
#pragma warning restore CS4014
        }
    }
}
