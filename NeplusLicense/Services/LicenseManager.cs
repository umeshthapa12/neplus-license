using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NeplusLicense.Crypto;
using NeplusLicense.Entities;
using NeplusLicense.Models;
using Standard.Licensing;
using Standard.Licensing.Validation;

namespace NeplusLicense.Services
{
    public class LicenseManager : ILicenseManager
    {
        private readonly IConfiguration   _configuration;
        private readonly LicenseDbContext _context;
        private readonly IHostEnvironment _environment;

        private static SHA256 Sha256Hash => SHA256.Create();

        public LicenseManager(IConfiguration configuration, LicenseDbContext context, IHostEnvironment environment)
        {
            _configuration = configuration;
            _context       = context;
            _environment   = environment;
        }

        public IEnumerable<(string,string)> GenerateLicense(IEnumerable<GenerateModel> models)
        {
            var pass = _configuration["licensePasswordKey"];

            var keyGenerator = Standard.Licensing.Security.Cryptography.KeyGenerator.Create();
            var keyPair      = keyGenerator.GenerateKeyPair();
            var privateKey   = keyPair.ToEncryptedPrivateKeyString( pass );
            var publicKey    = keyPair.ToPublicKeyString();
            
            var licenseCodes = new List<(string,string)>();

            void CreateLicense(GenerateModel model)
            {
                /*--------------------------------------
                 more info: https://github.com/junian/Standard.Licensing
                 ---------------------------------------*/
                var license = License.New()
                    .WithUniqueIdentifier( Guid.NewGuid() )
                    .As( LicenseType.Standard )
                    .ExpiresAt( DateTime.Now.AddDays( 30 ) )
                    .WithMaximumUtilization( 1 )
                    .WithProductFeatures( model.Package )
                    .LicensedTo( model.VendorName, model.Email )
                    .CreateAndSignWithPrivateKey( privateKey, pass );

                var licenseData = license.ToString();
                var path        = $"{_environment.ContentRootPath}/App_Data/{model.ClientGuid}/";
                var dirInfo     = Directory.Exists( path );
                if (!dirInfo)
                {
                    Directory.CreateDirectory( path );
                }

                var fullPath = $"{path}/Neplus_{model.VendorName}_{DateTime.Now:yyyy-MM-dd_hh-mm-ss-fff}_License.lic";

                var encrypted = licenseData.EncryptText( _configuration["licensePasswordKey"] );

                var toBase64 =
                    Convert.ToBase64String( Encoding.UTF8.GetBytes( encrypted ), Base64FormattingOptions.None );

                File.WriteAllText( fullPath, toBase64, Encoding.UTF8 );
                var hash = CryptoService.GetHash( Sha256Hash, licenseData );

                var config = new ConfigModel
                {
                    Expires          = license.Expiration,
                    ComputedHash     = hash,
                    EncryptedLicense = encrypted,
                    PrivateKey       = privateKey,
                    PublicKey        = publicKey
                };
                AddCustomerLicenseInfo( model, config );
                
                // adding encrypted and encoded license code as a first value and the full file path as a second value
                licenseCodes.Add( (toBase64,fullPath) );
            }

            // looping thru collection of license calling local fn to generate it.
            foreach ( var m in models )
                CreateLicense( m );
            
            var data = licenseCodes.Select( s =>
            {
                var (licenseCode, filePath) = s;
                return (licenseCode, filePath);
            } );

            return data;
        }

        public IEnumerable<IValidationFailure> Validate(string xmlNodeString, string vendorEmail, Guid clientGuid)
        {
            var vendor = _context.Vendors.FirstOrDefault(v => v.Email == vendorEmail && v.ClientGuid == clientGuid);
            if (vendor is null)
            {
                return new[]
                {
                    new GeneralValidationFailure
                    {
                        Message      = $"There are no license for {vendorEmail}",
                        HowToResolve = $"Please generate/add license for {vendorEmail}"
                    }
                };
            }

            var license = License.Load(xmlNodeString);

            var validationFailures = license.Validate()
                                            .ExpirationDate()
                                            .When(lic => lic.Type == LicenseType.Standard)
                                            .And()
                                            .Signature(vendor.PublicKey)
                                            .AssertValidLicense();
            return validationFailures;
        }

        public IEnumerable<RequestedVendors> Vendors()
        {
            return _context.RequestedVendors
                           .OrderByDescending( v => v.CreatedOn )
                           .AsEnumerable();
        }

        public string ExtractLicenseInfo(IFormFile file, string vendorEmail, Guid clientGuid)
        {
            var vendor = _context.Vendors.FirstOrDefault(v => v.Email == vendorEmail && v.ClientGuid == clientGuid);
            if (vendor is null)
            {
                throw new NullReferenceException("Vendor not found.");
            }

            using var stream  = file.OpenReadStream();
            using var reader  = new StreamReader(stream);
            var       allText = "";
            while (reader.Peek() >= 0)
            {
                allText += reader.ReadToEnd();
            }

            return GetDecryptedLicenseCode( allText, vendor.ComputedHash );
        }

        public IEnumerable<IValidationFailure> ValidateModule(string moduleName, string vendorEmail, Guid clientGuid)
        {
            var m = _context.Vendors.FirstOrDefault( v => v.Email == vendorEmail &&
                                                          v.ClientGuid == clientGuid );
            var msg = new List<IValidationFailure>();
            if (m is null)
            {
                msg.Add(
                    new GeneralValidationFailure
                    {
                        Message      = $"There are no license for {vendorEmail}",
                        HowToResolve = $"Please generate/add license for {moduleName}"
                    }
                );
                return msg;
            }

            var decryptedText = m.EncryptedLicense.DecryptText(_configuration["licensePasswordKey"]);

            var isValidHash = CryptoService.VerifyHash(Sha256Hash, decryptedText, m.ComputedHash);

            if (!isValidHash)
            {
                msg.Add( new GeneralValidationFailure
                {
                    Message      = $"HASH validation fail for {vendorEmail}",
                    HowToResolve = $"Please generate/add new license for {moduleName}"
                } );
                return msg;
            }
            
            var license = License.Load(decryptedText);

            var validationFailures = license.Validate()
                                            .ExpirationDate()
                                            .When( lic => lic.Type == LicenseType.Standard
                                                          || lic.Type == LicenseType.Trial &&
                                                          lic.ProductFeatures.Contains( moduleName )
                                            )
                                            .And()
                                            .Signature( m.PublicKey )
                                            .AssertValidLicense();
            if (validationFailures.Any())
            {
                msg.Add( new GeneralValidationFailure
                    {
                        Message      = $"License as expired for {moduleName}",
                        HowToResolve = $"Please generate/add new license for {moduleName}"
                    }
                );
                return msg;
            }

            return msg;
        }
        
        public string ExtractLicenseInfo(string licenseCode, string vendorEmail, Guid clientGuid)
        {
            if (string.IsNullOrWhiteSpace( licenseCode ))
            {
                throw  new NoNullAllowedException($"{nameof(licenseCode)} must be supplied.");
            }
            
            var decoded = Encoding.UTF8.GetString( Convert.FromBase64String( licenseCode ) );
            
            var vendor = _context.Vendors.FirstOrDefault( v =>
                v.Email == vendorEmail &&
                v.ClientGuid == clientGuid &&
                v.EncryptedLicense == decoded
            );
            
            if (vendor is null)
            {
                throw new NullReferenceException("Vendor not found.");
            }

            return GetDecryptedLicenseCode( decoded, vendor.ComputedHash );
        }


        private void AddCustomerLicenseInfo(GenerateModel model, ConfigModel config)
        {
            _context.Vendors.Add(new Vendors
            {
                Email            = model.Email,
                Name             = model.VendorName,
                PrivateKey       = config.PublicKey,
                PublicKey        = config.PublicKey,
                CreatedOn        = DateTime.Now,
                ExpiresOn        = config.Expires,
                ComputedHash     = config.ComputedHash,
                EncryptedLicense = config.EncryptedLicense,
                ClientGuid       = model.ClientGuid
            });
            _context.SaveChanges();
        }

        private string GetDecryptedLicenseCode(string base64EncodedCode, string computedHash)
        {
            var encryptedText = Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedCode));

            var decryptedText = encryptedText.DecryptText(_configuration["licensePasswordKey"]);

            var isValidHash = CryptoService.VerifyHash(Sha256Hash, decryptedText, computedHash);

            if (!isValidHash)
            {
                throw new Exception("Computed hash is not valid.");
            }

            // license xml node string
            return decryptedText;
        }

        /* TODO:
         * 1) receive encoded base 64 string and decode it
         * 2) decrypt string of license file
         * 3) verify the decrypted string hash
         * 4) convert to xml node file
         * 4) verify the license file
         *
         * 
         */
    }
}