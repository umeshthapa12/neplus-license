using System;
using System.ComponentModel.DataAnnotations;

namespace NeplusLicense.Models
{
    /// <summary>
    /// A license request model from the client hosted separately.
    /// </summary>
    public class RequestModel
    {
        [Required(ErrorMessage = "Customer/Vendor unique Id is required.")]
        public Guid?  ClientGuid  { get; set; }
        [Required(ErrorMessage = "Customer/vendor name is required.")]
        public string Name        { get; set; }
        [Required(ErrorMessage = "Customer/vendor email address is required.")]
        public string Email       { get; set; }
        [Required(ErrorMessage = "Module name is required.")]
        public string ModuleName  { get; set; }
        [Required(ErrorMessage = "License type is required. Allowed types are 'Trail', 'Standard'")]
        public string LicenseType { get; set; }
    }
}