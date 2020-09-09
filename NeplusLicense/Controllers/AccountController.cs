using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NeplusLicense.Attributes;
using NeplusLicense.Entities;
using NeplusLicense.Models;
using NeplusLicense.Services;

namespace NeplusLicense.Controllers
{
    [ApiRouteTemplate, Area("Admin")]
    public class AccountController : Controller
    {
        private readonly ResponseDto          _response;
        private readonly IHttpContextAccessor _accessor;
        private readonly IDataProtector       _dataProtector;
        private readonly ITokenFactory        _tokenFactory;
        private readonly JwtConfigService     _jwtConfig;
        private readonly LicenseDbContext     _context;
        private readonly IConfiguration       _configuration;

        public AccountController(
            ResponseDto             response,
            IHttpContextAccessor    accessor,
            IDataProtectionProvider protectionProvider,
            IConfiguration          configuration,
            ITokenFactory           tokenFactory,
            JwtConfigService        jwtConfig,
            LicenseDbContext        context)
        {

            _response      = response;
            _accessor      = accessor;
            _configuration = configuration;
            _tokenFactory  = tokenFactory;
            _jwtConfig     = jwtConfig;
            _context       = context;
            _dataProtector = protectionProvider.CreateProtector(configuration["dataProtectionProposeKey"]);
        }

        /// <summary>
        /// Resets user logged in information and redirects to the login UI. Only for the cookie schemes
        /// </summary>
        [HttpPost]
        public ActionResult<ResponseDto> Logout([FromBody] TokenExchangeDto exchange)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(InvalidModelErrors);
            }

            var handler = new JwtSecurityTokenHandler {MapInboundClaims = false};
            var param   = _jwtConfig.Params;
            // when supplied false, the data are extracted from expired token.
            param.ValidateLifetime = false;
            var principal = handler.ValidateToken(exchange.AccessToken, param, out _);

            if (principal is null)
            {
                _response.MessageBody = "Invalid token";
                return Unauthorized(_response);
            }

            var username = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName);

            var user = _context.User
                .Include(u => u.RefreshTokens)
                .FirstOrDefault(u => u.Username == username);
            var remote = _accessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            if (user is null || !user.HasValidRefreshToken(exchange.RefreshToken, remote))
            {
                _response.MessageBody = "Invalid token";
                return Unauthorized(_response);
            }

            user.RemoveRefreshToken(exchange.RefreshToken);
            _context.User.Update(user);
            _context.SaveChanges();

            if (exchange.IsLogoutAll != null && (bool) exchange.IsLogoutAll)

                _response.ContentBody = new {success = true};

            return Ok(_response);
        }

        /// <summary>
        /// Validates an user input and generates JWT token.
        /// </summary>
        /// <param name="dto">Request Form data</param>
        [HttpPost]
        public ActionResult<ResponseDto> Login([FromBody] UserDto dto)
        {
            var context = _accessor.HttpContext;

            if (dto is null || string.IsNullOrWhiteSpace(dto.Username) || !(dto.PasswordArray.Length > 0))
            {
                _response.MessageBody = "Invalid form request.";

                return BadRequest(_response);
            }

            // get user by username
            var user = _context.User
                .Include(u => u.RefreshTokens)
                .FirstOrDefault(u => u.Username == dto.Username);

            if (user is null)
            {
                _response.MessageBody = "The username you've entered doesn't matched any account. Try another.";

                return NotFound(_response);
            }

            var password          = _dataProtector.Unprotect(user.Password);
            var isPasswordMatched = string.Equals(Encoding.UTF8.GetString(dto.PasswordArray), password);

            var remote = context.Connection.RemoteIpAddress.MapToIPv4().ToString();

            if (!isPasswordMatched)
            {
                _response.MessageBody = "Wrong password. Try again.";
                _context.User.Update(user);
                _context.SaveChanges();

                return BadRequest(_response);
            }

            var claims = PopulateClaims(user);

            // Use JWT token
            var tokenObj     = GetJwt(claims);
            var refreshToken = _tokenFactory.GenerateToken();

            user.AddRefreshToken(refreshToken, user.Id, remote);
            _context.User.Update(user);
            _context.SaveChanges();

            _response.MessageBody = "Login successful.";
            _response.ContentBody = new
            {
                jwt = tokenObj,
                refreshToken
            };
            return _response;
        }

        [HttpPost]
        public async Task<ActionResult<ResponseDto>> ChangePassword([FromBody] PasswordChangeDto pw)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(InvalidModelErrors);
            }

            var results = await _accessor.HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

            if (!results.Succeeded)
            {
                _response.MessageBody = "Session expired";
                return Unauthorized(_response);
            }

            var username = results.Principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName);

            var user = _context.User
                .Include(u => u.RefreshTokens)
                .FirstOrDefault(u => u.Username == username);

            if (user is null)
            {
                _response.MessageBody = "User not found";
                return BadRequest(_response);
            }

            var isEqCurrentPw = _dataProtector.Unprotect(user.Password) == pw.CurrentPassword;

            if (!isEqCurrentPw)
            {
                _response.MessageBody = "Current password does not match.";
                return BadRequest(_response);
            }

            user.Password = _dataProtector.Protect(pw.NewPassword);
            _context.User.Update(user);
            _context.SaveChanges();
            _response.MessageBody = "Password successfully changed.";

            return _response;
        }

        /// <summary>
        /// Gets user login information.
        /// </summary>
        [HttpPost]
        public ActionResult<ResponseDto> RefreshToken([FromBody] TokenExchangeDto exchange)
        {
            if (!ModelState.IsValid)
            {
                // _accessor.HttpContext.Response.Headers.Add("Token-Expired", "none");
                return BadRequest(InvalidModelErrors);
            }

            var handler = new JwtSecurityTokenHandler {MapInboundClaims = false};
            var param   = _jwtConfig.Params;
            // when supplied false, the data are extracted from expired token.
            param.ValidateLifetime = false;
            var principal = handler.ValidateToken(exchange.AccessToken, param, out _);

            if (principal is null)
            {
                _response.MessageBody = "Invalid token";
                return Unauthorized(_response);
            }

            var remote = _accessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

            var username = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName);

            var user = _context.User
                .Include(u => u.RefreshTokens)
                .FirstOrDefault(u => u.Username == username);

            if (user is null || !user.HasValidRefreshToken(exchange.RefreshToken, remote))
            {
                _response.MessageBody = "Invalid token";
                return Unauthorized(_response);
            }

            var refreshToken = _tokenFactory.GenerateToken();

            var tokenObj = GetJwt(PopulateClaims(user));

            user.RemoveRefreshToken(exchange.RefreshToken);
            user.AddRefreshToken(refreshToken, user.Id, remote);
            _context.User.Update(user);
            _context.SaveChanges();
            _response.ContentBody = new
            {
                jwt = tokenObj,
                refreshToken
            };
            return Ok(_response);
        }

        private static List<Claim> PopulateClaims(User user)
        {
            return new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, user.RegGuid.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            };
        }


        /// <summary>
        /// Checks model state and returns error states.
        /// </summary>
        private object InvalidModelErrors
        {
            get
            {
                var errors = ModelState.GroupBy(g => g.Key)

                    .Select(s => new
                    {
                        Field = s.Key,
                        Message = ModelState[s.Key]
                            .Errors
                            .Select(e => e.ErrorMessage)
                            .ToArray()

                    });
                return new {ContentBody = errors};
            }
        }

        private object GetJwt(ICollection<Claim> claims)
        {
            var now = DateTime.Now;
            // adds expiration time
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToString()));

            // Create the JWT and write it to a string

            var creds = new SigningCredentials(_jwtConfig.SignInKeys, SecurityAlgorithms.HmacSha256,
                SecurityAlgorithms.Sha512Digest);
            var jwt = new JwtSecurityToken(
                _configuration["TokenAuthentication:Issuer"],
                _configuration["TokenAuthentication:Audience"],
                claims,
                now,
                now.AddMinutes(5),
                creds
            );
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            var obj = new
            {
                Token     = encodedJwt,
                ExpiresOn = jwt.ValidTo.ToLocalTime().ToString("g"),
                Countdown = Math.Abs((jwt.ValidFrom - jwt.ValidTo).TotalSeconds)
            };
            return obj;
        }

    }
}