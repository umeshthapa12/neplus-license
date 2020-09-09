using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NeplusLicense.Entities
{
    public class User
    {
        [Key]
        public long Id { get; set; }
        public Guid RegGuid { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }

        public bool HasValidRefreshToken(string refreshToken, string remoteIp)
        {
            return RefreshTokens.Any(r => r.Token == refreshToken &&
                                          r.Active &&
                                          r.Expires >= DateTime.Now &&
                                          r.RemoteIpAddress == remoteIp);
        }

        public void AddRefreshToken(string token, long userId, string remoteIpAddress, double daysToExpire = 1)
        {
            RefreshTokens.Add(new RefreshToken(token, DateTime.Now.AddDays(daysToExpire), userId, remoteIpAddress));
        }

        public void RemoveRefreshToken(string refreshToken)
        {
            RefreshTokens.Remove(RefreshTokens.First(t => t.Token == refreshToken));
        }
    }
}