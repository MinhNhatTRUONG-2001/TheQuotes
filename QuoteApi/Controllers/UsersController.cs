using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuoteApi.Controllers.Helpers;
using QuoteApi.Data;
using QuoteApi.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace QuoteApi.Controllers
{
    [Route("users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly QuoteContext _context;

        public UsersController(QuoteContext context)
        {
            _context = context;
        }

        // POST: users/register
        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(UserDTO userDTO)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (string.IsNullOrWhiteSpace(userDTO.Username) || string.IsNullOrWhiteSpace(userDTO.Email) || string.IsNullOrWhiteSpace(userDTO.DisplayedName) || string.IsNullOrWhiteSpace(userDTO.Password))
            {
                return BadRequest("Username, displayed name or password is empty.");
            }
            if (userDTO.Username.Trim().Length > 32 && userDTO.Username.Trim().All(Char.IsLetterOrDigit))
            {
                return BadRequest("Invalid username.");
            }
            if (userDTO.Email.Trim().Length > 50 || !ValidateEmailSyntax(userDTO.Email.Trim()))
            {
                return BadRequest("Invalid email.");
            }
            if (userDTO.DisplayedName.Trim().Length > 50)
            {
                return BadRequest("Invalid displayed name.");
            }
            if (!ValidatePassword(userDTO.Password))
            {
                return BadRequest("Invalid password.");
            }

            var user = await _context.Users
                .Where(u => u.username == userDTO.Username.Trim())
                .FirstOrDefaultAsync();
            var user2 = await _context.Users
                .Where(u => u.email == userDTO.Email.Trim())
                .FirstOrDefaultAsync();
            if (user != null)
            {
                return BadRequest("Username has already existed. Please use another one.");
            }
            else if (user2 != null)
            {
                return BadRequest("Email has already existed. Please use another one.");
            }
            else
            {
                var newUser = new User();
                newUser.username = userDTO.Username.Trim();
                newUser.email = userDTO.Email.Trim();
                newUser.displayed_name = userDTO.DisplayedName.Trim();
                string hashedPassword = HashPassword(userDTO.Password);
                newUser.password = hashedPassword;
                newUser.created_at = DateTime.UtcNow;
                newUser.last_login = DateTime.UtcNow;
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return GenerateJwtToken(newUser);
            }
        }

        // POST users/login
        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserDTO userDTO)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (string.IsNullOrWhiteSpace(userDTO.Username) || string.IsNullOrWhiteSpace(userDTO.Password))
            {
                return BadRequest("Email or Username or password is empty.");
            }
            var user = await _context.Users
                .Where(u => u.username == userDTO.Username.Trim() || u.email == userDTO.Username.Trim())
                .FirstOrDefaultAsync();
            if (user != null && Argon2.Verify(user.password, userDTO.Password))
            {
                user.last_login = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return GenerateJwtToken(user);
            }
            else
            {
                return BadRequest("Email or Username or Password is incorrect.");
            }
        }

        // GET users/info
        [HttpGet("info")]
        public async Task<ActionResult<UserInfoDTO>> GetUserInfo([FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            int id;
            try
            {
                id = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                return new UserInfoDTO
                {
                    Id = user.id,
                    Username = user.username,
                    DisplayedName = user.displayed_name,
                    AvatarUrl = user.avatar_url
                };
            }
        }

        // PUT users/change_info
        [HttpPut("change_info")]
        public async Task<IActionResult> ChangeUserInfo(UserInfoDTO userDTO, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            if (string.IsNullOrWhiteSpace(userDTO.DisplayedName) || userDTO.DisplayedName.Trim().Length > 50)
            {
                return BadRequest("Displayed name is empty or invalid.");
            }
            int id;
            try
            {
                id = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                user.displayed_name = userDTO.DisplayedName.Trim();
                user.last_updated = DateTime.UtcNow;
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

        // PUT users/change_password
        [HttpPut("change_password")]
        public async Task<IActionResult> ChangePassword(UserDTO userDTO, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            if (string.IsNullOrWhiteSpace(userDTO.Password) || !ValidatePassword(userDTO.Password))
            {
                return BadRequest("Password is empty or invalid.");
            }
            int id;
            try
            {
                id = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user != null && Argon2.Verify(user.password, userDTO.CurrentPassword))
            {
                if (Argon2.Verify(user.password, userDTO.CurrentPassword))
                {
                    string hashedPassword = HashPassword(userDTO.Password);
                    user.password = hashedPassword;
                    user.last_updated = DateTime.UtcNow;
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return NoContent();
                }
                else
                {
                    return BadRequest("Current password is incorrect.");
                }
            }
            else
            {
                return NotFound();
            }
        }

        // DELETE users
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount(UserDTO userDTO, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Users == null || _context.Quotes == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            int id;
            try
            {
                id = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }
            if (string.IsNullOrWhiteSpace(userDTO.Password))
            {
                return BadRequest("Password is empty.");
            }
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                if (Argon2.Verify(user.password, userDTO.Password))
                {
                    var quotes = _context.Quotes.Where(q => q.user_id == id).ToList();
                    _context.Quotes.RemoveRange(quotes);
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    return NoContent();
                }
                else
                {
                    return BadRequest("Password is incorrect.");
                }
            }
        }

        private bool ValidateEmailSyntax(string email = "")
        {
            string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
            return Regex.IsMatch(email, pattern);
        }

        private bool ValidatePassword(string password = "")
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

        private string GenerateJwtToken(User user)
        {
            string jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "";
            var claims = new List<Claim> { new Claim("userId", user.id.ToString()) };
            var jwtToken = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey.PadRight(512 / 8, '\0'))),
                    SecurityAlgorithms.HmacSha512
                )
            );
            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

        private string HashPassword(string password)
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
