using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;

namespace Ao3tracksync.Controllers
{
    public class PasswordController : Controller
    {
        // GET: Password
        public ActionResult Index()
        {
            return View();
        }

        // GET: Password/Forgot
        public ActionResult Forgot()
        {
            return View();
        }

        // POST: Password/Forgot
        [HttpPost]
        public ActionResult Forgot(FormCollection collection)
        {
            try
            {
                var model = new Models.Passwords.Forgot { Username = collection["Username"], Email = collection["Email"] };
                if (!TryValidateModel(model))
                {
                    return View(model);
                }

                using (var ctx = new Models.Ao3TrackEntities())
                {
                    var user = (from users in ctx.Users
                                where users.username == model.Username
                                select users).FirstOrDefault();

                    if (user == null)
                    {
                        ModelState.AddModelError("Username", "Unknown Username");
                        return View(model);
                    }

                    if (user.email != model.Email)
                    {
                        ModelState.AddModelError("Email", "Email address does not match database");
                        return View(model);
                    }

                    var pwrequest = new Models.PWReset { id = Guid.NewGuid(), expires = DateTime.Now.AddDays(1), user = user.id, oldhash = user.hash, complete = false };
                    ctx.PWResets.Add(pwrequest);
                    ctx.SaveChanges();

                    var uri = new Uri(Request.Url, Url.Action("Reset", new { id = pwrequest.id.ToString("N") } ));

                    MailMessage message = new MailMessage(new MailAddress("ao3track@wenchy.net", "Archive Track Reader"), new MailAddress(user.email));
                    message.Subject = "Archive Track Reader Password Reset Request";

                    var doc = new HtmlAgilityPack.HtmlDocument();
                    var html = doc.CreateElement("html");
                    doc.DocumentNode.AppendChild(html);

                    var head = doc.CreateElement("head");
                    html.AppendChild(head);

                    var title = doc.CreateElement("title");
                    title.AppendChild(doc.CreateTextNode("Archive Track Reader Password Reset Request"));
                    head.AppendChild(title);


                    var style = doc.CreateElement("style");
                    style.AppendChild(doc.CreateTextNode(@"body {
    color: #191919;
    background: #CCCCCC;
}

h1, h2, h3, h4, h5, h6, h7, h8, a {
    color: #A50000;
}

details p {
    color: #656565;
}"));
                    head.AppendChild(style);

                    var body = doc.CreateElement("body");
                    html.AppendChild(body);

                    var heading = doc.CreateElement("h1");
                    heading.AppendChild(doc.CreateTextNode("Archive Track Reader Password Reset Request"));
                    body.AppendChild(heading);

                    var para = doc.CreateElement("p");
                    para.AppendChild(doc.CreateTextNode("A Password Reset Request was made for the account: " + System.Net.WebUtility.HtmlEncode(user.username) ));
                    body.AppendChild(para);

                    para = doc.CreateElement("p");
                    para.AppendChild(doc.CreateTextNode("Follow this link "));
                    var link = doc.CreateElement("a");
                    link.Attributes.Add(doc.CreateAttribute("href", System.Net.WebUtility.HtmlEncode(uri.AbsoluteUri)));
                    link.AppendChild(doc.CreateTextNode(System.Net.WebUtility.HtmlEncode(uri.AbsoluteUri)));
                    para.AppendChild(link);
                    para.AppendChild(doc.CreateTextNode(" to change the account's password."));

                    body.AppendChild(para);
                    para.AppendChild(doc.CreateTextNode("The link will expire at " + pwrequest.expires.ToString("r") + "."));
                    para = doc.CreateElement("p");

                    body.AppendChild(para);

                    var writer = new System.IO.StringWriter();
                    doc.Save(writer);

                    message.Body = "<!DOCTYPE html>\n" + writer.ToString();
                    message.IsBodyHtml = true;

                    SmtpClient client = new SmtpClient("127.0.0.1");
                    client.Send(message);

                    return View("ForgotDone", user);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.ToString());

                return View();
            }
        }

        // GET: Password/Reset/91234561.....3452345
        public ActionResult Reset(string id)
        {
            if (!Guid.TryParseExact(id, "N", out var guid))
            {
                return View("Error");
            }

            using (var ctx = new Models.Ao3TrackEntities())
            {
                var pwreset = (from rows in ctx.PWResets
                           where rows.id == guid
                           select rows).FirstOrDefault();

                if (pwreset == null)
                {
                    return View("Error");
                }

                if (pwreset.expires <= DateTime.Now || pwreset.complete)
                {
                    return View("LinkExpired");
                }

                var user = (from users in ctx.Users
                            where users.id == pwreset.user
                            select users).Single();

                if (!user.hash.SequenceEqual(pwreset.oldhash))
                {
                    return View("LinkExpired");
                }

                return View(new Models.Passwords.Reset { Id = guid });
            }
        }

        // POST: Password/Reset/91234561.....3452345
        [HttpPost]
        public ActionResult Reset(string id, FormCollection collection)
        {
            try
            {
                if (!Guid.TryParseExact(id, "N", out var guid))
                {
                    return View("Error");
                }

                var model = new Models.Passwords.Reset { Password = collection["Password"], Check = collection["Check"] };
                if (!TryValidateModel(model))
                {
                    return View(model);
                }

                using (var ctx = new Models.Ao3TrackEntities())
                {
                    var pwreset = (from rows in ctx.PWResets
                               where rows.id == guid
                               select rows).Single();

                    if (pwreset.expires <= DateTime.Now || pwreset.complete)
                    {
                        return View("LinkExpired");
                    }

                    var user = (from users in ctx.Users
                                where users.id == pwreset.user
                                select users).Single();

                    if (!user.hash.SequenceEqual(pwreset.oldhash))
                    {
                        return View("LinkExpired");
                    }

                    user.password = model.Password;

                    pwreset.complete = true;

                    ctx.SaveChanges();

                    return View("ChangeDone", user);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.ToString());

                return View();
            }
        }
    }
}
