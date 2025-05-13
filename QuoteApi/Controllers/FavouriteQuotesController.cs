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
            return favouriteQuoteResponse;
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
