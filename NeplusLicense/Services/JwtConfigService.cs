using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace NeplusLicense.Services
{
    public class JwtConfigService
    {
        private readonly IConfiguration _configuration;

        public JwtConfigService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SymmetricSecurityKey SignInKeys =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["TokenAuthentication:SecretKey"]));

        public TokenValidationParameters Params => new TokenValidationParameters
        {
            // The signing key must match!
            ValidateIssuerSigningKey = true,

            //to be used for signature validation.
            IssuerSigningKey = SignInKeys,

            // Validate the JWT Issuer (iss) claim
            ValidateIssuer = true,

            //represents a valid issuer that will be used to check against the token's issuer.
            ValidIssuer   = _configuration["TokenAuthentication:Issuer"],
            ValidAudience = _configuration["TokenAuthentication:Audience"],

            // Validate the JWT Audience (aud) claim
            ValidateAudience = true,

            // Validate the token expiry
            ValidateLifetime = true,

            // indicating whether tokens must have an 'expiration' value.
            RequireExpirationTime = true,

            // If you want to allow a certain amount of clock drift, set that here:
            ClockSkew       = TimeSpan.Zero,
            SaveSigninToken = true

        };
    }
}