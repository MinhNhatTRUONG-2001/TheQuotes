using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace QuoteApi.Controllers.Helpers
{
    public class JwtTokenDecoder
    {
        public static int GetUserIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                // Note: manually padding to 512 bits if it is a short key, as the SymmetricSignatureProvider does not do the HMACSHA512 RFC2104 padding for you.
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET_KEY").PadRight(512 / 8, '\0'))),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha512]
            };
            handler.ValidateToken(token, validationParameters, out var validatedToken);
            var decodedToken = handler.ReadJwtToken(token);
            return int.Parse(decodedToken.Claims.First(c => c.Type == "userId").Value);
        }
    }
}
