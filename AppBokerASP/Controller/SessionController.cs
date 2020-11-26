using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using AppBokerASP.Database;
using Microsoft.EntityFrameworkCore;
using AppBokerASP.Database.Model;

namespace AppBokerASP.Controller
{
    [Route("session")]
    public class SessionController : BaseController
    {
        [HttpPost]
        public async Task<IActionResult> Login([FromBody]LoginArgs args)
        {
            if ((args?.Username == null && args?.EMail == null) || args?.PWHash == null)
                return new BadRequestResult();
            return Json(await UserManager.Login(args.Username?.Trim()?.ToLower(), args.EMail?.Trim()?.ToLower(), args.PWHash));
        }
    }
    public static class UserManager
    {
        public static byte[] SecretKey { get; set; }

        static UserManager()
        {
            SecretKey = File.ReadAllBytes("jwt.key");
        }

        public static async Task<LoginResult> Login(string username, string email, string passwordhash)
        {
            using var dbContext = DbProvider.BrokerDbContext;
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == username || email == x.Email);

            if (user == default)
                return new LoginResult { Success = false };


            if (!Salting(passwordhash, user.Salt).SequenceEqual(user.PasswordHash))
            {
                return new LoginResult { Success = false, Error = "password is incorrect" };
            }

            var payload = new Dictionary<string, object>()
                {
                    { "Expires", DateTime.UtcNow.AddMonths(1) },
                    { "Id", user.Id },
                    { "Created", DateTime.UtcNow }
                };

            return new LoginResult { Success = true, Error = "", Token = JsonWebToken.Encode(new Dictionary<string, object>(), payload, SecretKey, JsonWebToken.JwtHashAlgorithm.HS256), Id = user.Id, EMail = user.Email, Username = user.Username };
        }

        public static async Task<LoginResult> CreateUser(string username, string email, string pwdhash)
        {
            using var dbContext = DbProvider.BrokerDbContext;
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == username || email == x.Email);

            if (user != default)
                return new LoginResult { Success = false };

            var minedsalt = GenerateSalt();
            var saltedpw = Salting(pwdhash, minedsalt);
            var c = new UserModel(username.TrimEnd(), email.TrimEnd(), saltedpw, minedsalt);
            await dbContext.Users.AddAsync(c);
            await dbContext.SaveChangesAsync();

            return new LoginResult { Success = true, Id = c.Id, EMail = c.Email, Username = c.Username };
        }

        private static byte[] Salting(string passwordhash, byte[] salt)
        {
            var prov = System.Security.Cryptography.SHA512.Create();
            var hash = Encoding.UTF8.GetBytes(passwordhash).Concat(salt);
            return prov.ComputeHash(hash.ToArray());
        }
        private static byte[] GenerateSalt()
        {
            var num = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] saltmine = new byte[128];
            num.GetBytes(saltmine);
            return saltmine;
        }
    }
    public class LoginArgs
    {
        public string Username { get; set; }
        public string EMail { get; set; }
        public string PWHash { get; set; }
    }
    public class LoginResult
    {
        public bool Success;
        public string Error;
        public long Id { get; set; }
        public string Username { get; set; }
        public string EMail { get; set; }
        public string Token { get; set; }
    }


}
