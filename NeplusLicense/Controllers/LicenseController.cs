using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NeplusLicense.Attributes;
using NeplusLicense.Entities;
using NeplusLicense.Models;
using NeplusLicense.Services;
using Standard.Licensing;

namespace NeplusLicense.Controllers
{
    [ApiRouteTemplate, Area("lic")]
    public class LicenseController : Controller
    {
        private readonly ResponseDto      _response;
        private readonly ILicenseManager  _licenseManager;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration   _configuration;
        private readonly LicenseDbContext _context;

        public LicenseController(
            ILicenseManager  licenseManager,
            IHostEnvironment environment,
            IConfiguration   configuration,
            ResponseDto      response,
            LicenseDbContext context)
        {
            _licenseManager = licenseManager;
            _environment    = environment;
            _configuration  = configuration;
            _response       = response;
            _context        = context;
        }

        /// <summary>
        /// Public api to create a license request.
        /// </summary>
        /// <param name="rm">Request model</param>
        [HttpPost]
        public ActionResult<ResponseDto> CreateRequest([FromBody] List<RequestModel> rm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(InvalidModelErrors);
            }

            // if we have invalid supplied license type
            if (typeof(LicenseType).GetProperties().Any(f=> rm.All( _ => _.LicenseType != f.Name ) ))
            {
                _response.MessageBody = "The license type value must be either 'Trail' or 'Standard'";
                return BadRequest( _response );
            }

            var rv = rm.Select( l=> new RequestedVendors
            {
                Name        = l.Name,
                ClientGuid  = l.ClientGuid,
                Email       = l.Email,
                LicenseType = l.LicenseType, // either of type `Trail`, `Standard`
                ModuleName  = l.ModuleName,
                CreatedOn   = DateTime.Now // will use this value as requested date as well.
            } );

            _context.RequestedVendors.AddRange( rv );
            _context.SaveChanges();

            _response.MessageBody = "License Request Success.";
            
            return Ok(_response);
        }

        [HttpPost, Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ActionResult<ResponseDto> Generate([FromBody] IEnumerable<GenerateModel> m)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest( InvalidModelErrors );
            }

            try
            {
                // var filePath = _licenseManager.GenerateLicense(new RequestModel
                // {
                //     Email = "asd@asd.com",
                //     Package = new Dictionary<string, string> {{"hrm", "licensed"}},
                //     VendorName = "cool"
                // });

                var items = _licenseManager
                            .GenerateLicense( m )?
                            .Select( s =>
                            {
                                var (licenseCode, filePath) = s;
                                return (licenseCode, filePath);
                            } );
               
                // var fileInfo = System.IO.File.OpenRead(filePath);
                // var file = new FormFile(fileInfo, 0, fileInfo.Length, null, fileInfo.Name)
                // {
                //     Headers = new HeaderDictionary(),
                //     ContentType = "application/octet-stream"
                // };
                //
                // var licenseInfoXmlNode = _licenseManager.ExtractLicenseInfo(file, "asd@asd.com", TODO);
                // var valid = _licenseManager.Validate(licenseInfoXmlNode, "asd@asd.com", TODO);
                
                /*--- either of license code, license file or both of them can be return as necessary ---*/
                _response.ContentBody = items?.Select( s => s.licenseCode );

                return Ok( _response );
                // return File( filePath, "application/octet-stream" );
            }
            catch (Exception e)
            {
                Console.WriteLine( e );
                return StatusCode( (int) HttpStatusCode.InternalServerError, e.Message );
            }
        }

        [HttpGet,Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public ActionResult<ResponseDto> GetVendors()
        {
            var v = _licenseManager.Vendors();
            _response.ContentBody = v;
            return Ok( _response );
        }
        
        /// <summary>
        /// Public api to validate distributed license.
        /// </summary>
        /// <param name="fd">Form data of license info</param>
        [HttpPost]
        public ActionResult Validate([FromForm] ValidationRequestModel fd)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest( InvalidModelErrors );
            }

            if (fd.LicenseFile is null && string.IsNullOrWhiteSpace( fd.LicenseCode ))
            {
                _response.MessageBody = "Either license code or a license file is missing.";
                return BadRequest( _response );
            }

            try
            {
                // extract license node for either license file or the license code.
                var licenseInfoXmlNode = string.IsNullOrWhiteSpace( fd.LicenseCode )
                    ? _licenseManager.ExtractLicenseInfo( fd.LicenseFile, fd.Email, fd.ClientGuid )
                    : _licenseManager.ExtractLicenseInfo( fd.LicenseCode, fd.Email, fd.ClientGuid );

                var validationStates = _licenseManager.Validate( licenseInfoXmlNode, fd.Email, fd.ClientGuid );

                if (validationStates.Any())
                {
                    return StatusCode( (int) HttpStatusCode.PaymentRequired, validationStates );
                }

                // indicating the valid license
                return Ok( new { ContentBody = true } );

            }
            catch (Exception e)
            {
                Console.WriteLine( e );
                return StatusCode( (int) HttpStatusCode.InternalServerError, e.Message );
            }
        }
        
        [HttpPost]
        public ActionResult ValidateModule([FromForm] ValidationRequestModel fd)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest( InvalidModelErrors );
            }

            if ( string.IsNullOrWhiteSpace( fd.ModuleName ))
            {
                _response.MessageBody = "Module name must be provided.";
                return BadRequest( _response );
            }

            try
            {
                var validationStates = _licenseManager.ValidateModule( fd.ModuleName, fd.Email, fd.ClientGuid );

                if (validationStates.Any())
                {
                    _response.ContentBody = validationStates;
                    return StatusCode( (int) HttpStatusCode.PaymentRequired, _response );
                }

                // indicating the valid license
                return Ok( new { ContentBody = true } );

            }
            catch (Exception e)
            {
                Console.WriteLine( e );
                return StatusCode( (int) HttpStatusCode.InternalServerError, e.Message );
            }
        }

        /// <summary>
        /// Checks model state and returns error states.
        /// </summary>
        private ResponseDto InvalidModelErrors
        {
            get
            {
                var errors = ModelState.GroupBy( g => g.Key )
                                       .Select( s => new
                                       {
                                           Field = s.Key,
                                           Message = ModelState[s.Key]
                                                     .Errors
                                                     .Select( e => e.ErrorMessage )
                                                     .ToArray()
                                       } );
                _response.ContentBody = errors;
                return  _response;
            }
        }
    }
}