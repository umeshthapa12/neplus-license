using System;
using System.ComponentModel.DataAnnotations;

namespace NeplusLicense.Entities
{
    public class RefreshToken
    {
        [Key]
        public long Id { get; set; }

        public string   Token           { get; private set; }
        public DateTime Expires         { get; private set; }
        public long      UserId          { get; private set; }
        public bool     Active          { get; private set; }
        public string   RemoteIpAddress { get; private set; }
        public DateTime CreatedOn       { get; private set; }

        public RefreshToken(string token, DateTime expires, long userId, string remoteIpAddress)
        {
            var now = DateTime.Now;
            Token           = token;
            Expires         = expires;
            UserId          = userId;
            RemoteIpAddress = remoteIpAddress;
            Active          = now <= Expires;
            CreatedOn       = now;
        }
    }
}