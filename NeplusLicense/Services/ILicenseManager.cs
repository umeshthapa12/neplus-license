using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using NeplusLicense.Entities;
using NeplusLicense.Models;
using Standard.Licensing.Validation;

namespace NeplusLicense.Services
{
    public interface ILicenseManager
    {
        /// <summary>
        /// Generates a license file and returns a file path.
        /// </summary>
        /// <param name="model">License request model containing customer data</param>
        /// <returns>Generated license file path</returns>
        IEnumerable<(string, string)> GenerateLicense(IEnumerable<GenerateModel> model);

        /// <summary>
        /// Extracts license xml string from encrypted string in the file.
        /// </summary>
        /// <param name="file">Encrypted file from the client.</param>
        /// <param name="vendorEmail">Vendor/Customer email</param>
        /// <param name="clientGuid">Vendor/Customer unique id</param>
        /// <returns>Decrypted license xml string</returns>
        string ExtractLicenseInfo(IFormFile file, string vendorEmail, Guid clientGuid);

        /// <summary>
        ///  Extracts license xml string from encrypted string.
        /// </summary>
        /// <param name="licenseCode">Base64 encoded license code</param>
        /// <param name="vendorEmail">Vendor/Customer email</param>
        /// <param name="clientGuid">Vendor/Customer unique id</param>
        /// <returns>Decrypted license xml string</returns>
        public string ExtractLicenseInfo(string licenseCode, string vendorEmail, Guid clientGuid);

        /// <summary>
        /// Validate input license node and returns if it fails to validate ro any other error state omitted by license validation.
        /// </summary>
        /// <param name="xmlNodeString">License xml string</param>
        /// <param name="vendorEmail">Vendor/customer email</param>
        /// <param name="clientGuid">Vendor/Customer unique Id</param>
        /// <returns>collection of error state of type IValidationFailure</returns>
        IEnumerable<IValidationFailure> Validate(string xmlNodeString, string vendorEmail, Guid clientGuid);
        
        
        IEnumerable<IValidationFailure> ValidateModule(string moduleName, string vendorEmail, Guid clientGuid);

        /// <summary>
        /// Get all vendors
        /// </summary>
        /// <returns>collection of requested vendors</returns>
        IEnumerable<RequestedVendors> Vendors();
    }
}