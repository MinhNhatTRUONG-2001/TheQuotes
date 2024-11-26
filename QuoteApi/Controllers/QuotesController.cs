using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<List<QuoteDTO>>> GetTop5LatestSavedQuotes()
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            var top5Quotes = await _context.Quotes
                            .Include(q => q.User)
                            .OrderByDescending(q => q.creation_date)
                            .Take(5)
                            .ToListAsync();
            if (top5Quotes == null)
            {
                return NotFound();
            }
            List<QuoteDTO> top5QuotesDto = new List<QuoteDTO>();
            foreach (var quote in top5Quotes)
            {
                QuoteDTO quoteDto = new QuoteDTO();
                quoteDto.Id = quote.id;
                quoteDto.Quote = quote.quote_content;
                quoteDto.SaidBy = quote.who_said;
                quoteDto.When = quote.when_was_said.ToString("yyyy-MM-dd");
                quoteDto.User = new UserInfoDTO { Id = quote.User.id, Username = quote.User.username, DisplayedName = quote.User.displayed_name };
                quoteDto.CreatedOn = quote.creation_date.ToString("yyyy-MM-dd HH:mm");
                top5QuotesDto.Add(quoteDto);
            }
            return top5QuotesDto;
        }

        // GET: quotes/3
        [HttpGet("{userId}")]
        public async Task<ActionResult<List<QuoteDTO>>> GetQuotes(int userId)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            var quoteDto = await _context.Quotes
                        .Where(q => q.user_id == userId)
                        .Select(q => new QuoteDTO { 
                            Id = q.id,
                            Quote = q.quote_content,
                            SaidBy = q.who_said,
                            When = q.when_was_said.ToString("yyyy-MM-dd"),
                            User = new UserInfoDTO { Id = q.User.id, Username = q.User.username, DisplayedName = q.User.displayed_name },
                            CreatedOn = q.creation_date.ToString("yyyy-MM-dd HH:mm")
                        })
                        .ToListAsync();

            if (quoteDto == null)
            {
                return NotFound();
            }

            return quoteDto;
        }

        // GET: quotes/3/21
        [HttpGet("{userId}/{id}")]
        public async Task<ActionResult<QuoteDTO>> GetQuote(int userId, int id)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            var quote = await _context.Quotes.Include(q => q.User).FirstOrDefaultAsync(q => q.id == id && q.user_id == userId);

            if (quote == null)
            {
                return NotFound();
            }

            QuoteDTO quoteDto = new QuoteDTO();
            quoteDto.Id = quote.id;
            quoteDto.Quote = quote.quote_content;
            quoteDto.SaidBy = quote.who_said;
            quoteDto.When = quote.when_was_said.ToString("yyyy-MM-dd");
            quoteDto.User = new UserInfoDTO { Id = quote.User.id, Username = quote.User.username, DisplayedName = quote.User.displayed_name };
            quoteDto.CreatedOn = quote.creation_date.ToString("yyyy-MM-dd HH:mm");
            return quoteDto;
        }

        // GET: quotes/search
        [HttpGet("search")]
        public async Task<ActionResult<List<QuoteDTO>>> SearchQuotes(
            [FromQuery] string? content, [FromQuery] string? who_said, [FromQuery] string? start_said_date, [FromQuery] string? end_said_date,
            [FromQuery] string? username, [FromQuery] string? displayed_name, [FromQuery] string? start_creation_date, [FromQuery] string? end_creation_date)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }

            var quotes = await _context.Quotes.Include(q => q.User).ToListAsync();
            if (quotes == null)
            {
                return NotFound();
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
                quotes = quotes.Where(q => q.creation_date >= startCreationDate).ToList();
            }
            if (!string.IsNullOrWhiteSpace(end_creation_date))
            {
                DateTime endCreationDate = DateTime.Parse(end_creation_date);
                quotes = quotes.Where(q => q.creation_date <= endCreationDate).ToList();
            }
            List<QuoteDTO> quotesDto = new List<QuoteDTO>();
            foreach (var quote in quotes)
            {
                QuoteDTO quoteDto = new QuoteDTO();
                quoteDto.Id = quote.id;
                quoteDto.Quote = quote.quote_content;
                quoteDto.SaidBy = quote.who_said;
                quoteDto.When = quote.when_was_said.ToString("yyyy-MM-dd");
                quoteDto.User = new UserInfoDTO { Id = quote.User.id, Username = quote.User.username, DisplayedName = quote.User.displayed_name };
                quoteDto.CreatedOn = quote.creation_date.ToString("yyyy-MM-dd HH:mm");
                quotesDto.Add(quoteDto);
            }
            return quotesDto;
        }

        // PUT: quotes/21
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQuote(int id, QuoteDTO quoteDto, [FromHeader(Name = "Authorization")] string token = "")
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
            int userId = JwtTokenDecoder.GetUserIdFromToken(token);
            if (quote == null)
            {
                return NotFound();
            }
            if (quote.user_id != userId)
            {
                return BadRequest("You can only modify your quotes.");
            }

            quote.quote_content = quoteDto.Quote;
            quote.who_said = quoteDto.SaidBy;
            quote.when_was_said = DateOnly.Parse(quoteDto.When);

            _context.Entry(quote).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuoteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: quotes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<QuoteDTO>> PostQuote(QuoteDTO quoteDto, [FromHeader(Name = "Authorization")] string token = "")
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            if (token.Contains("Bearer "))
            {
                token = token.Split("Bearer ")[1];
            }
            int userId = JwtTokenDecoder.GetUserIdFromToken(token);
            Quote quote = new Quote();
            quote.quote_content = quoteDto.Quote;
            quote.who_said = quoteDto.SaidBy;
            quote.when_was_said = DateOnly.Parse(quoteDto.When);
            quote.user_id = userId;
            quote.creation_date = DateTime.UtcNow;
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            var savedQuote = await _context.Quotes
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.id == quote.id);
            if (savedQuote == null)
            {
                return Problem("Failed to retrieve saved quote.");
            }
            QuoteDTO savedQuoteDto = new QuoteDTO
            {
                Id = savedQuote.id,
                Quote = savedQuote.quote_content,
                SaidBy = savedQuote.who_said,
                When = savedQuote.when_was_said.ToString("yyyy-MM-dd"),
                User = new UserInfoDTO
                {
                    Id = savedQuote.User.id,
                    Username = savedQuote.User.username,
                    DisplayedName = savedQuote.User.displayed_name
                },
                CreatedOn = savedQuote.creation_date.ToString("yyyy-MM-dd HH:mm")
            };

            return CreatedAtAction(nameof(GetQuote), new { userId = savedQuote.user_id, savedQuote.id }, savedQuoteDto);
        }

        // DELETE: quotes/21
        [Authorize]
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
            int userId = JwtTokenDecoder.GetUserIdFromToken(token);

            if (quote == null)
            {
                return NotFound();
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
