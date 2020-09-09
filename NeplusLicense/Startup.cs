using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.WebEncoders.Testing;
using Microsoft.IdentityModel.Tokens;
using NeplusLicense.Background;
using NeplusLicense.Entities;
using NeplusLicense.Models;
using NeplusLicense.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace NeplusLicense
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        private IConfiguration Configuration { get; }

        private IWebHostEnvironment Environment { get;  }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RouteOptions>( options => options.LowercaseUrls = true );

            services.AddScoped<ResponseDto>();
            services.AddScoped<JwtConfigService>();
            services.AddScoped<ITokenFactory,TokenFactory>();
            
            services.AddHttpContextAccessor();
            services.AddSingleton( Configuration );
            services.AddSingleton( Environment.ContentRootFileProvider );
            
            var certificatesDirectory = new DirectoryInfo( "Cert" );
            services.AddDataProtection( o => { o.ApplicationDiscriminator = "neplus"; } )
                .UseCryptographicAlgorithms( new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                } )
                .SetApplicationName( "neplus" )
                //.ProtectKeysWithCertificate(new X509Certificate2(@"Cert\app-key.pfx", "jobharu"))
                .PersistKeysToFileSystem( certificatesDirectory );

            services.AddControllers().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.WriteIndented = true;
                o.JsonSerializerOptions.IgnoreNullValues = true;
                o.JsonSerializerOptions.PropertyNamingPolicy = null;
                o.JsonSerializerOptions.DictionaryKeyPolicy = null;
                o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            }).AddNewtonsoftJson(o =>
                {
                    o.SerializerSettings.Converters.Add(new StringEnumConverter
                        {NamingStrategy = new CamelCaseNamingStrategy()});
                    // Ignores null property from the object. E.G: someProp: null will be ignored
                    o.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    o.SerializerSettings.Formatting = Formatting.Indented;
                    o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                }
            );
            
            services.AddCors();
            
            if (!Environment.IsDevelopment())
            {
                // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-3.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts
                services.AddHsts(options =>
                {
                    options.Preload           = true;
                    options.IncludeSubDomains = true;
                    options.MaxAge            = TimeSpan.FromDays(30);
                    options.ExcludedHosts.Add("example.com");
                    options.ExcludedHosts.Add("www.example.com");
                });

                // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-3.1
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                    options.HttpsPort          = 443;
                });
            }

            services.AddScoped<ILicenseManager, LicenseManager>();
           
            services.AddApiVersioning( config =>
            {
                config.DefaultApiVersion                   = new ApiVersion( 2, 0 );
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions                   = true;
            } );

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(
                        JwtBearerDefaults.AuthenticationScheme,
                        o =>
                        {
                            if (o.SecurityTokenValidators.FirstOrDefault() is JwtSecurityTokenHandler
                                jwtSecurityTokenHandler)
                                jwtSecurityTokenHandler.MapInboundClaims = false;

                            o.TokenValidationParameters = Params;
                            o.SaveToken                 = true;
                            o.RequireHttpsMetadata      = Environment.IsProduction();
                        });

            services.AddAuthorization(o =>
            {
                o.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .Build();
            });
            
            services.AddScoped<LicenseDbContext>()
                    .AddDbContext<LicenseDbContext>(o =>
                    {
                        o.UseSqlServer(Configuration["NeplusLicenseConnection"], u =>
                        {
                            // UseRowNumberForPaging for the backward compability for sql server 2005 - 2008 etc. 2012 won't need to be call this method.
                            //u.UseRowNumberForPaging();

                            // by default == null will get translated to Is NULL to get results as C# would expect.
                            // EF Core compensate for difference between database/in memory null handling. 
                            // If you want to remove this processing done by EF and convert == null to database as = NULL then you can configure it.
                            // On your DbContextOptionsBuilder(the options on which you call UseSqlServer) call UseRelationalNulls()
                            u.UseRelationalNulls();

                        });
                    });

            services.AddResponseCaching();
            services.AddResponseCompression();

            services.AddHostedService<UserBackgroundService>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            app.UseResponseCaching();

            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                
                app.UseCors( builder =>
                {
                    var domains = new[]
                    {
                        "http://lcoalhost:4200",
                        "http://192.168.1.69:4200",
                        "http://0.0.0.0:4200",
                    };

                    var customClientHeaders = new[]
                    {
                        "Token-Expired"
                    };

                    builder.WithOrigins( origins: domains )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders( customClientHeaders )
                        .AllowCredentials();

                    
                } );
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseAuthentication();

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });

        }
        
        private SymmetricSecurityKey SignInKeys =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["TokenAuthentication:SecretKey"]));

        private TokenValidationParameters Params => new TokenValidationParameters
        {
            // The signing key must match!
            ValidateIssuerSigningKey = true,

            //to be used for signature validation.
            IssuerSigningKey = SignInKeys,

            // Validate the JWT Issuer (iss) claim
            ValidateIssuer = true,

            //represents a valid issuer that will be used to check against the token's issuer.
            ValidIssuer   = Configuration["TokenAuthentication:Issuer"],
            ValidAudience = Configuration["TokenAuthentication:Audience"],

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