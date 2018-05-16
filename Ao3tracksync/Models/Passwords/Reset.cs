using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Ao3tracksync.Models.Passwords
{
    public class Reset : IValidatableObject
    {
        public Guid Id { get; set; }

        [DisplayName("New Password"), PasswordPropertyText, MinLength(Controllers.UserController.MIN_PW_SIZE, ErrorMessage = Controllers.UserController.PW_SIZE_MSG)]
        public string Password { get; set; }

        [DisplayName("New Password Again")]
        public string Check { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Password != Check)
            {
                yield return new ValidationResult("Passwords did not match", new List<string> { "Password", "Check" });
            }
        }
    }
}
