using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ao3tracksync.Models.Passwords
{
    public class Forgot
    {
        public Guid Id { get; set; }

        [DisplayName("Archive Track Reader Username"),Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [DisplayName("Email Address"), RegularExpression(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", ErrorMessage = "You must enter a valid Email Address")]
        public string Email { get; set; }
    }
}
