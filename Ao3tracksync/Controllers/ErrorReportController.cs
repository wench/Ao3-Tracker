using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.UI;
using System.Web;
using System.Net.Mail;

namespace Ao3tracksync.Controllers
{
    [RoutePrefix("api/ErrorReport"), Authorize(Roles = "administrators"), System.Web.Mvc.OutputCache(Location = OutputCacheLocation.None)]
    public class ErrorReportController : ApiController
    {
        [AllowAnonymous]
        public void Post([FromBody]string report)
        {
            MailMessage message = new MailMessage(new MailAddress("ao3track@wenchy.net", "Ao3Track Debug Reports"), new MailAddress("the.wench@wenchy.net"));

            message.Subject = "Ao3Track Reader Error report";
            message.Body = "See attachment";

            var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(report));
            message.Attachments.Add(new Attachment(stream, "ErrorReport.json", "application/json"));

            SmtpClient client = new SmtpClient("127.0.0.1");
            client.Send(message);
        }
    }
}
