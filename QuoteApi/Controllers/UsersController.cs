using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
            if (userDTO.Username == null || userDTO.DisplayedName == null || userDTO.Password == null
                || userDTO.Username.Trim() == "" || userDTO.DisplayedName.Trim() == "" || userDTO.Password.Trim() == "")
            {
                return BadRequest("Username, displayed name or password is empty.");
            }
            if (userDTO.Username.Length > 32 && userDTO.Username.All(Char.IsLetterOrDigit))
            {
                return BadRequest("Invalid username.");
            }
            if (userDTO.DisplayedName.Length > 50)
            {
                return BadRequest("Invalid displayed name.");
            }
            if (!ValidatePassword(userDTO.Password))
            {
                return BadRequest("Invalid password.");
            }

            var user = await _context.Users.Where(u => u.username == userDTO.Username).FirstOrDefaultAsync();
            if (user == null)
            {
                var newUser = new User();
                newUser.username = userDTO.Username;
                newUser.displayed_name = userDTO.DisplayedName;
                string hashedPassword = HashPassword(userDTO.Password);
                newUser.password = hashedPassword;
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return GenerateJwtToken(newUser);
            }
            else
            {
                return BadRequest("User has already existed.");
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
            if (userDTO.Username == null || userDTO.Password == null
                || userDTO.Username.Trim() == "" || userDTO.Password.Trim() == "")
            {
                return BadRequest("Username or password is empty.");
            }
            var user = await _context.Users.Where(u => u.username == userDTO.Username).FirstOrDefaultAsync();
            if (user != null && Argon2.Verify(user.password, userDTO.Password))
            {
                return GenerateJwtToken(user);
            }
            else
            {
                return BadRequest("Username or password is incorrect.");
            }
        }

        // GET users/info
        [Authorize]
        [HttpGet("info")]
        public async Task<ActionResult<UserDTO>> GetUserInfo([FromHeader] string token = "")
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            int id = GetUserIdFromToken(token);
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                return new UserDTO { Username = user.username, DisplayedName = user.displayed_name };
            }
        }

        // PUT users/change_info
        [Authorize]
        [HttpPut("change_info")]
        public async Task<IActionResult> ChangeUserInfo(UserDTO userDTO, [FromHeader] string token = "")
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (userDTO.DisplayedName == null || userDTO.DisplayedName.Trim() == "" || userDTO.DisplayedName.Length > 50)
            {
                return BadRequest("Displayed name is empty or invalid.");
            }
            int id = GetUserIdFromToken(token);
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                user.displayed_name = userDTO.DisplayedName;
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

        // PUT users/change_password
        [Authorize]
        [HttpPut("change_password")]
        public async Task<IActionResult> ChangePassword(UserDTO userDTO, [FromHeader] string token = "")
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (userDTO.Password == null || userDTO.Password.Trim() == "" || !ValidatePassword(userDTO.Password))
            {
                return BadRequest("Password is empty or invalid.");
            }
            int id = GetUserIdFromToken(token);
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user != null && Argon2.Verify(user.password, userDTO.CurrentPassword))
            {
                if (Argon2.Verify(user.password, userDTO.CurrentPassword))
                {
                    string hashedPassword = HashPassword(userDTO.Password);
                    user.password = hashedPassword;
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
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount([FromHeader] string token = "")
        {
            if (_context.Users == null || _context.Quotes == null)
            {
                return NotFound();
            }
            int id = GetUserIdFromToken(token);
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }
            else
            {
                var quotes = _context.Quotes.Where(q => q.user_id == id).ToList();
                _context.Quotes.RemoveRange(quotes);
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return NoContent();
            }
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

        private int GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var decodedToken = handler.ReadJwtToken(token);
            return int.Parse(decodedToken.Claims.First(c => c.Type == "userId").Value);
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
