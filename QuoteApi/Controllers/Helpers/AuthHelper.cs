using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using Microsoft.IdentityModel.Tokens;
using QuoteApi.Data;

namespace server.Controllers.Helpers
{
    public class AuthHelpers
    {
        public static bool ValidateEmailSyntax(string email = "")
        {
            string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
            return Regex.IsMatch(email, pattern);
        }

        public static bool ValidatePassword(string password = "")
        {
            string digitPattern = @"[0-9]";
            string lowercasePattern = @"[a-z]";
            string uppercasePattern = @"[A-Z]";
            string specialCharacterPattern = @"\W|_";

            if (password.Length < 8 || password.Length > 64 ||
                !Regex.IsMatch(password, digitPattern) ||
                !Regex.IsMatch(password, lowercasePattern) ||
                !Regex.IsMatch(password, uppercasePattern) ||
                !Regex.IsMatch(password, specialCharacterPattern))
            {
                return false;
            }

            return true;
        }

        public static string GenerateJwtToken(User user, double expirationMinutes)
        {
            string jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "";
            var claims = new List<Claim> { new Claim("userId", user.id.ToString()) };
            var jwtToken = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey.PadRight(512 / 8, '\0'))),
                    SecurityAlgorithms.HmacSha512
                )
            );
            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            new Random().NextBytes(salt);
            var argon2Config = new Argon2Config { Password = Encoding.UTF8.GetBytes(password), Salt = salt };
            var argon2 = new Argon2(argon2Config);
            using (SecureArray<byte> hash = argon2.Hash())
            {
                return argon2Config.EncodeString(hash.Buffer);
            }
        }
    }
}