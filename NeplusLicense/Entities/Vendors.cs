using System;
using System.ComponentModel.DataAnnotations;

namespace NeplusLicense.Entities
{
    public class Vendors
    {
        [Key] public long      Id               { get; set; }
        public       string    Name             { get; set; }
        public       string    Email            { get; set; }
        public       string    PublicKey        { get; set; }
        public       string    PrivateKey       { get; set; }
        public       string    ComputedHash     { get; set; }
        public       string    EncryptedLicense { get; set; }
        public       DateTime? CreatedOn        { get; set; }
        public       DateTime? ExpiresOn        { get; set; }

        public Guid ClientGuid { get; set; }
    }
}