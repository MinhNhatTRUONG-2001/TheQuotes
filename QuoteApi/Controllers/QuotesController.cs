using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuoteApi.Controllers.Helpers;
using QuoteApi.Data;
using QuoteApi.DTOs;

namespace QuoteApi.Controllers
{
    [Route("quotes")]
    [ApiController]
    public class QuotesController : ControllerBase
    {
        private readonly QuoteContext _context;

        public QuotesController(QuoteContext context)
        {
            _context = context;
        }

        // GET: quotes
        [HttpGet]
        public async Task<ActionResult<List<QuoteDTO>>> GetTop5LatestSavedQuotes([FromHeader(Name = "Authorization")] string? token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            int userId = -1;
            if (token.StartsWith("Bearer"))
            {
                token = token.Substring("Bearer".Length).Trim();
            }
            if (token != "")
            {
                try
                {
                    userId = JwtTokenDecoder.GetUserIdFromToken(token);
                }
                catch
                {
                    return BadRequest("Invalid token.");
                }
            }
            var top5Quotes = await _context.Quotes
                            .Include(q => q.User)
                            .Include(q => q.id_favourite_quotes_quote_id)
                            .OrderByDescending(q => q.created_at)
                            .Take(5)
                            .ToListAsync();
            if (top5Quotes == null)
            {
                return NotFound("No quote found.");
            }
            List<QuoteDTO> top5QuotesDto = new List<QuoteDTO>();
            foreach (var quote in top5Quotes)
            {
                QuoteDTO quoteDto = new QuoteDTO
                {
                    Id = quote.id,
                    Quote = quote.quote_content,
                    SaidBy = quote.who_said,
                    When = quote.when_was_said?.ToString("yyyy-MM-dd"),
                    Source = quote.source,
                    User = new UserInfoDTO
                    {
                        Id = quote.User.id,
                        Username = quote.User.username,
                        DisplayedName = quote.User.displayed_name,
                        AvatarUrl = quote.User.avatar_url
                    },
                    CreatedAt = quote.created_at.ToString("yyyy-MM-dd HH:mm")
                };
                if (token != "")
                {
                    var favouriteQuote = quote.id_favourite_quotes_quote_id.FirstOrDefault(q => q.user_id == userId);
                    if (favouriteQuote != null)
                    {
                        quoteDto.Favourite = new FavouriteQuoteDTO
                        {
                            Id = favouriteQuote.id.ToString(),
                            UserId = favouriteQuote.user_id,
                            QuoteId = favouriteQuote.quote_id,
                            SavedAt = favouriteQuote.saved_at.ToString("yyyy-MM-dd HH:mm")
                        };
                    }
                }
                top5QuotesDto.Add(quoteDto);
            }
            return top5QuotesDto;
        }

        // GET: quotes/user/3
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<QuoteDTO>>> GetQuotesByUserId(int userId, [FromHeader(Name = "Authorization")] string? token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            int tokenUserId = -1;
            if (token.StartsWith("Bearer"))
            {
                token = token.Substring("Bearer".Length).Trim();
            }
            if (token != "")
            {
                try
                {
                    tokenUserId = JwtTokenDecoder.GetUserIdFromToken(token);
                }
                catch
                {
                    return BadRequest("Invalid token.");
                }
            }

            var quotes = await _context.Quotes
                        .Include(q => q.User)
                        .Include(q => q.id_favourite_quotes_quote_id)
                        .Where(q => q.user_id == userId)
                        .OrderByDescending(q => q.created_at)
                        .ToListAsync();
            if (quotes == null)
            {
                return NotFound("Quote not found.");
            }

            List<QuoteDTO> quotesDto = new List<QuoteDTO>();
            foreach (var quote in quotes)
            {
                QuoteDTO quoteDto = new QuoteDTO
                {
                    Id = quote.id,
                    Quote = quote.quote_content,
                    SaidBy = quote.who_said,
                    When = quote.when_was_said?.ToString("yyyy-MM-dd"),
                    Source = quote.source,
                    User = new UserInfoDTO
                    {
                        Id = quote.User.id,
                        Username = quote.User.username,
                        DisplayedName = quote.User.displayed_name,
                        AvatarUrl = quote.User.avatar_url
                    },
                    CreatedAt = quote.created_at.ToString("yyyy-MM-dd HH:mm")
                };
                if (token != "")
                {
                    var favouriteQuote = quote.id_favourite_quotes_quote_id.FirstOrDefault(q => q.user_id == tokenUserId);
                    if (favouriteQuote != null)
                    {
                        quoteDto.Favourite = new FavouriteQuoteDTO
                        {
                            Id = favouriteQuote.id.ToString(),
                            UserId = favouriteQuote.user_id,
                            QuoteId = favouriteQuote.quote_id,
                            SavedAt = favouriteQuote.saved_at.ToString("yyyy-MM-dd HH:mm")
                        };
                    }
                }
                quotesDto.Add(quoteDto);
            }
            return quotesDto;
        }

        // GET: quotes/21
        [HttpGet("{id}")]
        public async Task<ActionResult<QuoteDTO>> GetQuote(int id, [FromHeader(Name = "Authorization")] string? token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            int userId = -1;
            if (token.StartsWith("Bearer"))
            {
                token = token.Substring("Bearer".Length).Trim();
            }
            if (token != "")
            {
                try
                {
                    userId = JwtTokenDecoder.GetUserIdFromToken(token);
                }
                catch
                {
                    return BadRequest("Invalid token.");
                }
            }

            var quote = await _context.Quotes
                .Include(q => q.User)
                .Include(q => q.id_favourite_quotes_quote_id)
                .FirstOrDefaultAsync(q => q.id == id);

            if (quote == null)
            {
                return NotFound("Quote not found.");
            }

            QuoteDTO quoteDto = new QuoteDTO
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
                CreatedAt = quote.created_at.ToString("yyyy-MM-dd HH:mm")
            };
            if (token != "")
            {
                var favouriteQuote = quote.id_favourite_quotes_quote_id.FirstOrDefault(q => q.user_id == userId);
                if (favouriteQuote != null)
                {
                    quoteDto.Favourite = new FavouriteQuoteDTO
                    {
                        Id = favouriteQuote.id.ToString(),
                        UserId = favouriteQuote.user_id,
                        QuoteId = favouriteQuote.quote_id,
                        SavedAt = favouriteQuote.saved_at.ToString("yyyy-MM-dd HH:mm")
                    };
                }
            }
            return quoteDto;
        }

        // GET: quotes/search
        [HttpGet("search")]
        public async Task<ActionResult<List<QuoteDTO>>> SearchQuotes(
            [FromQuery] string? content, [FromQuery] string? who_said, [FromQuery] string? start_said_date, [FromQuery] string? end_said_date,
            [FromQuery] string? username, [FromQuery] string? displayed_name, [FromQuery] string? start_creation_date, [FromQuery] string? end_creation_date,
            [FromHeader(Name = "Authorization")] string? token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            int userId = -1;
            if (token.StartsWith("Bearer"))
            {
                token = token.Substring("Bearer".Length).Trim();
            }
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    userId = JwtTokenDecoder.GetUserIdFromToken(token);
                }
                catch
                {
                    return BadRequest("Invalid token.");
                }
            }

            var quotes = await _context.Quotes
                .Include(q => q.User)
                .Include(q => q.id_favourite_quotes_quote_id)
                .ToListAsync();
            if (quotes == null)
            {
                return NotFound("No quotes found.");
            }
            if (!string.IsNullOrWhiteSpace(content))
            {
                quotes = quotes.Where(q => q.quote_content.ToLower().Contains(content.ToLower())).ToList();
            }
            if (!string.IsNullOrWhiteSpace(who_said))
            {
                quotes = quotes.Where(q => q.who_said.ToLower().Contains(who_said.ToLower())).ToList();
            }
            if (!string.IsNullOrWhiteSpace(start_said_date))
            {
                DateOnly startSaidDate = DateOnly.Parse(start_said_date);
                quotes = quotes.Where(q => q.when_was_said >= startSaidDate).ToList();
            }
            if (!string.IsNullOrWhiteSpace(end_said_date))
            {
                DateOnly endSaidDate = DateOnly.Parse(end_said_date);
                quotes = quotes.Where(q => q.when_was_said <= endSaidDate).ToList();
            }
            if (!string.IsNullOrWhiteSpace(username))
            {
                quotes = quotes.Where(q => q.User.username.ToLower().Contains(username.ToLower())).ToList();
            }
            if (!string.IsNullOrWhiteSpace(displayed_name))
            {
                quotes = quotes.Where(q => q.User.displayed_name.ToLower().Contains(displayed_name.ToLower())).ToList();
            }
            if (!string.IsNullOrWhiteSpace(start_creation_date))
            {
                DateTime startCreationDate = DateTime.Parse(start_creation_date);
                quotes = quotes.Where(q => q.created_at >= startCreationDate).ToList();
            }
            if (!string.IsNullOrWhiteSpace(end_creation_date))
            {
                DateTime endCreationDate = DateTime.Parse(end_creation_date);
                quotes = quotes.Where(q => q.created_at <= endCreationDate).ToList();
            }
            quotes = quotes.OrderByDescending(q => q.created_at).ToList();
            List<QuoteDTO> quotesDto = new List<QuoteDTO>();
            foreach (var quote in quotes)
            {
                QuoteDTO quoteDto = new QuoteDTO
                {
                    Id = quote.id,
                    Quote = quote.quote_content,
                    SaidBy = quote.who_said,
                    When = quote.when_was_said?.ToString("yyyy-MM-dd"),
                    Source = quote.source,
                    User = new UserInfoDTO
                    {
                        Id = quote.User.id,
                        Username = quote.User.username,
                        DisplayedName = quote.User.displayed_name,
                        AvatarUrl = quote.User.avatar_url
                    },
                    CreatedAt = quote.created_at.ToString("yyyy-MM-dd HH:mm")
                };
                if (token != "")
                {
                    var favouriteQuote = quote.id_favourite_quotes_quote_id.FirstOrDefault(q => q.user_id == userId);
                    if (favouriteQuote != null)
                    {
                        quoteDto.Favourite = new FavouriteQuoteDTO
                        {
                            Id = favouriteQuote.id.ToString(),
                            UserId = favouriteQuote.user_id,
                            QuoteId = favouriteQuote.quote_id,
                            SavedAt = favouriteQuote.saved_at.ToString("yyyy-MM-dd HH:mm")
                        };
                    }
                }
                quotesDto.Add(quoteDto);
            }
            return quotesDto;
        }

        // PUT: quotes/21
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQuote(int id, QuoteDTO quoteDto, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            if (string.IsNullOrWhiteSpace(quoteDto.Quote) || string.IsNullOrWhiteSpace(quoteDto.SaidBy))
            {
                return BadRequest("Please fill in required field.");
            }
            if (quoteDto.Quote.Trim().Length > 500)
            {
                return BadRequest("Invalid 'Quote Content'.");
            }
            if (quoteDto.SaidBy.Trim().Length > 50)
            {
                return BadRequest("Invalid 'Who Said The Quote'.");
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }

            var quote = await _context.Quotes.FindAsync(id);
            int userId;
            try
            {
                userId = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }
            if (quote == null)
            {
                return NotFound("Quote not found.");
            }
            if (quote.user_id != userId)
            {
                return BadRequest("You can only modify your quotes.");
            }

            quote.quote_content = quoteDto.Quote;
            quote.who_said = quoteDto.SaidBy;
            if (quoteDto.When != null)
            {
                quote.when_was_said = DateOnly.Parse(quoteDto.When);
            }
            else
            {
                quote.when_was_said = null;
            }
            quote.source = quoteDto.Source;
            quote.last_updated = DateTime.UtcNow;

            _context.Entry(quote).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuoteExists(id))
                {
                    return NotFound("Quote not found.");
                }
                else
                {
                    throw new Exception("Error while updating your quote.");
                }
            }

            return NoContent();
        }

        // POST: quotes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<QuoteDTO>> PostQuote(QuoteDTO quoteDto, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            if (string.IsNullOrWhiteSpace(quoteDto.Quote) || string.IsNullOrWhiteSpace(quoteDto.SaidBy))
            {
                return BadRequest("Please fill in required field.");
            }
            if (quoteDto.Quote.Trim().Length > 500)
            {
                return BadRequest("Invalid 'Quote Content'.");
            }
            if (quoteDto.SaidBy.Trim().Length > 50)
            {
                return BadRequest("Invalid 'Who Said The Quote'.");
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
            Quote quote = new Quote();
            try
            {
                quote.quote_content = quoteDto.Quote;
                quote.who_said = quoteDto.SaidBy;
                if (quoteDto.When != null)
                {
                    quote.when_was_said = DateOnly.Parse(quoteDto.When);
                }
                if (string.IsNullOrWhiteSpace(quoteDto.Source))
                {
                    quote.source = quoteDto.Source;
                }
                quote.user_id = userId;
                quote.created_at = DateTime.UtcNow;
                _context.Quotes.Add(quote);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return BadRequest("Error while saving your quote");
            }
            var savedQuote = await _context.Quotes
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.id == quote.id);
            if (savedQuote == null)
            {
                return BadRequest("Failed to retrieve saved quote.");
            }
            QuoteDTO savedQuoteDto = new QuoteDTO
            {
                Id = savedQuote.id,
                Quote = savedQuote.quote_content,
                SaidBy = savedQuote.who_said,
                When = savedQuote.when_was_said?.ToString("yyyy-MM-dd"),
                User = new UserInfoDTO
                {
                    Id = savedQuote.User.id,
                    Username = savedQuote.User.username,
                    DisplayedName = savedQuote.User.displayed_name
                },
                CreatedAt = savedQuote.created_at.ToString("yyyy-MM-dd HH:mm")
            };

            return CreatedAtAction(nameof(GetQuote), new { userId = savedQuote.user_id, savedQuote.id }, savedQuoteDto);
        }

        // DELETE: quotes/21
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuote(int id, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }

            var quote = await _context.Quotes.FindAsync(id);
            int userId;
            try
            {
                userId = JwtTokenDecoder.GetUserIdFromToken(token);
            }
            catch
            {
                return BadRequest("Invalid token.");
            }

            if (quote == null)
            {
                return NotFound("Quote not found.");
            }
            if (quote.user_id != userId)
            {
                return BadRequest("You can only delete your quotes.");
            }

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool QuoteExists(int id)
        {
            return (_context.Quotes?.Any(q => q.id == id)).GetValueOrDefault();
        }
    }
}
