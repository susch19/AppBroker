using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AppBokerASP.Core.Model
{
    public class UserModel
    {

        [Key]
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] Salt { get; set; }

        public UserModel()
        {

        }
        public UserModel(string username, string email, byte[] passwordHash, byte[] salt)
        {
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            Salt = salt;
        }

    }
}
