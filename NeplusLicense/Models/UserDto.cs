using System;

namespace NeplusLicense.Models
{
    public class UserDto
    {
        public string Username { get; set; }
        public byte[] PasswordArray { get; set; }
    }
}