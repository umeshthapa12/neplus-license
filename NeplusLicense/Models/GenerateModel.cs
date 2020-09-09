using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NeplusLicense.Models
{
    public class GenerateModel
    {
        /// <summary>
        /// A name of the vendor.
        /// </summary>
        [Required(ErrorMessage = "Vendor/Customer name is required")]
        public string VendorName { get; set; }

        /// <summary>
        /// A vendor email address
        /// </summary>
        [Required(ErrorMessage = "Vendor/Customer email is required")]
        public string Email { get; set; }

        /// <summary>
        /// Expiry date of the license supplied by product owner.
        /// </summary>
        [Required(ErrorMessage = "License expiry date is required")]
        public DateTime? ExpiresOn { get; set; }

        /// <summary>
        /// Registered customer/vendor unique Id to monitor license status.
        /// </summary>
        [Required(ErrorMessage = "Vendor/Customer unique id is required")]
        public Guid ClientGuid { get; set; }

        /// <summary>
        /// Selected packages to add in the license file. e.g. {"hrm","licensed"}, {"attendance","unlicensed"} etc.,
        /// </summary>
        [Required(ErrorMessage = "Vendor/Customer subscribed package is required")]
        public Dictionary<string, string> Package { get; set; }
    }

    public class ConfigModel
    {
        public string    PublicKey        { get; set; }
        public string    PrivateKey       { get; set; }
        public DateTime? Expires          { get; set; }
        public string    ComputedHash     { get; set; }
        public string    EncryptedLicense { get; set; }
    }

}