using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeplusLicense.Entities;

namespace NeplusLicense.Background
{
    public class UserBackgroundService : IHostedService, IDisposable
    {
        private readonly List<KeyValuePair<string, Timer>> _timers;
        private readonly ILogger<UserBackgroundService>    _logger;

        private readonly LicenseDbContext _context;

        public UserBackgroundService(
            ILogger<UserBackgroundService> logger,
            IServiceProvider               services
        )
        {
            // Get service provider from created scope so that we can use services
            var provider = services.CreateScope().ServiceProvider;

            // System
            _timers = new List<KeyValuePair<string, Timer>>();
            _logger = logger;

            _context = provider.GetService<LicenseDbContext>();

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation( "Timed Background Service is starting." );

            Init();

            return Task.CompletedTask;
        }

        private void Init()
        {
            _logger.LogInformation( "Timed Background Service is working." );

            var t1 = new Timer(
                               RefreshTokenCleanup,
                               null, TimeSpan.Zero,
                               TimeSpan.FromSeconds( 10 ) );
            _timers.Add( new KeyValuePair<string, Timer>( "clear_refresh_token", t1 ) );
        }

        #region background tasks

        private void RefreshTokenCleanup(object state)
        {
            _logger.LogInformation( "Refresh Token Cleanup: remove expired tokens." );
            var con = _context.Database.GetDbConnection();
            
                con.Open();
                var param = new DynamicParameters();
                param.Add( "@userId", 0, DbType.Int32 );
                param.Add( "@isClearAll", 0, DbType.Boolean );
                con.Execute( "usp_refreshTokenCleanup", param, commandType: CommandType.StoredProcedure );
            

            _logger.LogInformation( "Refresh Token Cleanup: removed." );
            con.Close();
        }

        #endregion

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation( "Timed Background Service is stopping." );

            _timers?.ForEach( t => t.Value.Change( Timeout.Infinite, 0 ) );

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timers?.ForEach( t => t.Value.Dispose() );
        }
    }
}