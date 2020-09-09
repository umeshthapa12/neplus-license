using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NeplusLicense.Models
{
    public class ValidationRequestModel
    {
        public IFormFile LicenseFile { get; set; }
        public string    LicenseCode { get; set; }
        public string    ModuleName  { get; set; }
        [Required(ErrorMessage = "Vendor/Customer email is required")]
        public string    Email       { get; set; }
        [Required(ErrorMessage = "Vendor/Customer client unique guid is required")]
        public Guid      ClientGuid  { get; set; }
    }
}