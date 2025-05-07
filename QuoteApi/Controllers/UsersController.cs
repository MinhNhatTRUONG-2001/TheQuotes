using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuoteApi.Controllers.Helpers;
using QuoteApi.Data;
using QuoteApi.DTOs;
using QuoteApi.Services.Email;
using server.Controllers.Helpers;
using SixLabors.ImageSharp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace QuoteApi.Controllers
{
    [Route("users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly QuoteContext _context;
        private readonly IEmailService _emailService;
        private readonly long _imageFileSizeLimit = 3 * 1024 * 1024; // 3 MB

        public UsersController(QuoteContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            if (userDTO.Email.Trim().Length > 50 || !AuthHelpers.ValidateEmailSyntax(userDTO.Email.Trim()))
            {
                return BadRequest("Invalid email.");
            }
            if (userDTO.DisplayedName.Trim().Length > 50)
            {
                return BadRequest("Invalid displayed name.");
            }
            if (!AuthHelpers.ValidatePassword(userDTO.Password))
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
                string hashedPassword = AuthHelpers.HashPassword(userDTO.Password);
                newUser.password = hashedPassword;
                newUser.created_at = DateTime.UtcNow;
                newUser.last_login = DateTime.UtcNow;
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return AuthHelpers.GenerateJwtToken(newUser, 60 * 24); // 1 day
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
                return AuthHelpers.GenerateJwtToken(user, 60 * 24); // 1 day
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
                return NotFound("User not found.");
            }
            else
            {
                return new UserInfoDTO
                {
                    Id = user.id,
                    Username = user.username,
                    DisplayedName = user.displayed_name,
                    AvatarUrl = user.avatar_url,
                    AboutMe = user.about_me,
                };
            }
        }

        // GET users/info/3
        [HttpGet("info/{id}")]
        public async Task<ActionResult<UserInfoDTO>> GetUserInfoById(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound("User not found.");
            }
            else
            {
                return new UserInfoDTO
                {
                    Id = user.id,
                    Username = user.username,
                    DisplayedName = user.displayed_name,
                    AvatarUrl = user.avatar_url,
                    AboutMe = user.about_me,
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
            if (!string.IsNullOrWhiteSpace(userDTO.DisplayedName) && userDTO.DisplayedName.Trim().Length > 50)
            {
                return BadRequest("Displayed name is too long.");
            }
            if (!string.IsNullOrWhiteSpace(userDTO.AboutMe) && userDTO.AboutMe.Trim().Length > 2000)
            {
                return BadRequest("About me is too long.");
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
                return NotFound("User not found.");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(userDTO.DisplayedName))
                {
                    user.displayed_name = userDTO.DisplayedName.Trim();
                }
                if (!string.IsNullOrWhiteSpace(userDTO.AboutMe))
                {
                    user.about_me = userDTO.AboutMe.Trim();
                }
                user.last_updated = DateTime.UtcNow;
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok("User info is changed successfully!");
            }
        }

        // PUT users/change_avatar
        [HttpPut("change_avatar")]
        public async Task<IActionResult> ChangeUserAvatar([FromForm] IFormFile? avatarFile, [FromHeader(Name = "Authorization")] string token = "")
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

            if (avatarFile != null && avatarFile.Length > 0)
            {
                if (avatarFile.Length > _imageFileSizeLimit)
                {
                    return BadRequest($"File size exceeds the limit of {_imageFileSizeLimit / (1024 * 1024)} MB!");
                }
                if (!avatarFile.ContentType.StartsWith("image/"))
                {
                    return BadRequest("File type is not allowed!");
                }
                using var stream = avatarFile.OpenReadStream();
                using var image = await Image.LoadAsync(stream);

                int width = image.Width;
                int height = image.Height;

                if (width < 200 || height < 200)
                {
                    return BadRequest("Image must be at least 200x200 pixels.");
                }
                if (width > 5000 || height > 5000) // Imagekit does not support displaying images larger than 5000x5000 pixels (maybe just on Free plan)
                {
                    return BadRequest("Image dimension is too large. Maximum is 5000x5000 pixels.");
                }
            }
            
            var user = await _context.Users.Where(u => u.id == id).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound("User not found.");
            }
            else
            {
                List<string> responseMessage = await FileUploadHelper.UpdateUserAvatarToImageKit(_context, user, avatarFile, null);
                if (responseMessage[0] == "BadRequest")
                {
                    return BadRequest(responseMessage[1]);
                }
                else
                {
                    return Ok(responseMessage[1]);
                }
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
            if (string.IsNullOrWhiteSpace(userDTO.Password) || !AuthHelpers.ValidatePassword(userDTO.Password))
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
            if (user != null)
            {
                if (Argon2.Verify(user.password, userDTO.CurrentPassword))
                {
                    string hashedPassword = AuthHelpers.HashPassword(userDTO.Password);
                    user.password = hashedPassword;
                    user.last_updated = DateTime.UtcNow;
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    return Ok("Password is changed successfully!");
                }
                else
                {
                    return BadRequest("Current password is incorrect.");
                }
            }
            else
            {
                return NotFound("User not found.");
            }
        }

        [HttpPost("password_reset_request")]
        public async Task<IActionResult> PasswordResetRequest(EmailDTO emailDTO)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(emailDTO.Email))
            {
                return BadRequest("Email is empty.");
            }
            if (!AuthHelpers.ValidateEmailSyntax(emailDTO.Email))
            {
                return BadRequest("Invalid email syntax.");
            }

            var user = await _context.Users.Where(u => u.email == emailDTO.Email).FirstOrDefaultAsync();

            string token, receiverEmail;
            double expMinutes = 10;
            if (user != null)
            {
                token = AuthHelpers.GenerateJwtToken(user, expMinutes);
                receiverEmail = user.email;
            }
            else
            {
                return NotFound("Cannot find user with this email.");
            }

            var clientUrl = Environment.GetEnvironmentVariable("CLIENT_URL");
            string subject = "The Quotes - Password Reset Request";
            string body = $"<p>Please use the link below to reset your password:</p>" +
                $"<a href=\"{clientUrl}/password-reset/{token}/\" target=\"_blank\">Reset password</a>" +
                $"<p>The link is valid in <strong>{expMinutes} minutes</strong>.</p>";
            await _emailService.SendEmailAsync(receiverEmail, subject, body);
            return Ok("Password reset request is sent. Please check your email inbox!");
        }

        [HttpPut("password_reset/{token}")]
        public async Task<IActionResult> PasswordReset([FromBody] UserDTO userDTO, string token = "")
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            if (string.IsNullOrWhiteSpace(userDTO.Password) || !AuthHelpers.ValidatePassword(userDTO.Password))
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
            if (user == null)
            {
                return NotFound("User not found.");
            }
            else
            {
                string hashedPassword = AuthHelpers.HashPassword(userDTO.Password);
                user.password = hashedPassword;
                user.last_updated = DateTime.UtcNow;
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok("Password is reset successfully! You can go to the login page now.");
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
                return NotFound("User not found.");
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
    }
}
