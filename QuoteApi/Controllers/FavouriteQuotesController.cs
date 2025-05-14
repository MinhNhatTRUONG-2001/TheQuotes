using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuoteApi.Controllers.Helpers;
using QuoteApi.Data;
using QuoteApi.DTOs;

namespace QuoteApi.Controllers
{
    [Route("favourite_quotes")]
    [ApiController]
    public class FavouriteQuotesController : ControllerBase
    {
        private readonly QuoteContext _context;

        public FavouriteQuotesController(QuoteContext context)
        {
            _context = context;
        }

        // GET: favourite_quotes
        [HttpGet]
        public async Task<ActionResult<List<QuoteDTO>>> GetUserFavouriteQuotes([FromHeader(Name = "Authorization")] string token = "") {
            if (_context.FavouriteQuotes == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            int userId;
            try
            {
                userId = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }

            var favouriteQuotes = await _context.FavouriteQuotes
                .Where(q => q.user_id == userId)
                .OrderByDescending(q => q.saved_at)
                .ToListAsync();

            List<QuoteDTO> favouriteQuotesResponse = new List<QuoteDTO>();
            foreach (var favouriteQuote in favouriteQuotes)
            {
                var quote = await _context.Quotes
                    .Include(q => q.User)
                    .Where(q => q.id == favouriteQuote.quote_id)
                    .FirstOrDefaultAsync();
                if (quote != null)
                {
                    QuoteDTO quoteResponse = new QuoteDTO
                    {
                        Id = quote.id,
                        Quote = quote.quote_content,
                        SaidBy = quote.who_said,
                        When = quote.when_was_said?.ToString("yyyy-MM-dd"),
                        User = new UserInfoDTO
                        {
                            Id = quote.User.id,
                            Username = quote.User.username,
                            DisplayedName = quote.User.displayed_name,
                            AvatarUrl = quote.User.avatar_url
                        },
                        Source = quote.source,
                        CreatedAt = quote.created_at.ToString("yyyy-MM-dd HH:mm"),
                        Favourite = new FavouriteQuoteDTO
                        {
                            Id = favouriteQuote.id.ToString(),
                            UserId = favouriteQuote.user_id,
                            QuoteId = favouriteQuote.quote_id,
                            SavedAt = favouriteQuote.saved_at.ToString("yyyy-MM-dd HH:mm")
                        }
                    };
                    favouriteQuotesResponse.Add(quoteResponse);
                }
            }

            return Ok(favouriteQuotesResponse);
        }

        // POST: favourite_quotes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<FavouriteQuoteDTO>> AddFavouriteQuote(FavouriteQuoteDTO favouriteQuoteDto, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.FavouriteQuotes == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            int userId;
            try
            {
                userId = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }

            FavouriteQuote favouriteQuote = new FavouriteQuote();
            try {
                favouriteQuote.user_id = userId;
                favouriteQuote.quote_id = favouriteQuoteDto.QuoteId;
                favouriteQuote.saved_at = DateTime.UtcNow;
                _context.FavouriteQuotes.Add(favouriteQuote);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest("Error while saving your favourite quote.");
            }
            FavouriteQuoteDTO favouriteQuoteResponse = new FavouriteQuoteDTO {
                Id = favouriteQuote.id.ToString(),
                UserId = favouriteQuote.user_id,
                QuoteId = favouriteQuote.quote_id,
                SavedAt = favouriteQuote.saved_at.ToString("yyyy-MM-dd HH:mm")
            };
            return Ok(favouriteQuoteResponse);
        }

        // DELETE: favourite_quotes/e963411f-0f3f-4906-bbbd-9e9a712acfc9
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFavouriteQuote(string id, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.FavouriteQuotes == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }

            int userId;
            try
            {
                userId = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }
            
            var favouriteQuote = await _context.FavouriteQuotes
                .Where(q => q.id.ToString() == id)
                .FirstOrDefaultAsync();
            if (favouriteQuote == null)
            {
                return NotFound("Favourite quote not found.");
            }
            if (favouriteQuote.user_id != userId)
            {
                return BadRequest("You can only delete your favourite quotes.");
            }

            _context.FavouriteQuotes.Remove(favouriteQuote);
            await _context.SaveChangesAsync();

            return Ok("Favourite quote unsaved!");
        }
    }
}
