using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.UI;
using System.Web;
using System.Net.Mail;
using Newtonsoft.Json;

namespace Ao3tracksync.Controllers
{
    [RoutePrefix("api/ErrorReport"), Authorize(Roles = "administrators"), System.Web.Mvc.OutputCache(Location = OutputCacheLocation.None)]
    public class ErrorReportController : ApiController
    {
        public class ExceptionStub
        {
            public string ClassName { get; set; }
            public string Message { get; set; }
            public ExceptionStub InnerException {get;set;}
        }


        public class ReportMetaData
        {
            public string Version { get; set; }
            public string Platform { get; set; }
            public string Mode { get; set; }
            public string Arch { get; set; }
            public string OSName { get; set; }
            public string OSVersion { get; set; }
            public string OSArch { get; set; }
            public string HWType { get; set; }
            public string HWName { get; set; }
            public DateTime Date { get; set; }
            public ExceptionStub Exception { get; set; }
        }

        static List<(string Platform, Version Version)> ignoreErrors = new List<(string platform, Version version)> {
            ("Android", new Version(1,0,2,0)),
            ("Android", new Version(1,0,1,0)),
            ("Android", new Version(1,0,0,0)),
        };


        [AllowAnonymous]
        public void Post([FromBody]string report)
        {
            report = "[\n" + report + "\n]";
            var meta = JsonConvert.DeserializeObject<ReportMetaData[]>(report, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            });
            if (System.Version.TryParse(meta[0].Version, out var ver))
            {
                foreach (var ignore in ignoreErrors)
                {
                    if ((ignore.Platform == null || ignore.Platform == meta[0].Platform) && ver.Equals(ignore.Version))
                        return;
                }
            }

            MailMessage message = new MailMessage(new MailAddress("ao3track@wenchy.net", "Ao3Track Debug Reports"), new MailAddress("the.wench@wenchy.net"));

            message.Subject = "Ao3Track Reader Error report " + meta[0].Platform + " " + meta[0].Version;
            message.Body = JsonConvert.SerializeObject(meta, new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(report));
            message.Attachments.Add(new Attachment(stream, "ErrorReport.json", "application/json"));

            SmtpClient client = new SmtpClient("127.0.0.1");
            client.Send(message);
        }
    }
}
