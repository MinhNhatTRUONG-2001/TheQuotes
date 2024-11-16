using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuoteApi.Data;

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
                quoteDto.Quote = quote.the_quote;
                quoteDto.SaidBy = quote.who_said;
                quoteDto.When = quote.when_was_said.ToString("yyyy-MM-dd");
                top5QuotesDto.Add(quoteDto);
            }
            return top5QuotesDto;
        }

        // GET: quotes/Violet
        [HttpGet("{creator_id}")]
        public async Task<ActionResult<List<QuoteDTO>>> GetQuotes(int creator_id)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            var quoteDto = await _context.Quotes
                        .Where(q => q.user_id == creator_id)
                        .Select(q => new QuoteDTO { Id = q.id, Quote = q.the_quote, SaidBy = q.who_said, When = q.when_was_said.ToString("yyyy-MM-dd") })
                        .ToListAsync();

            if (quoteDto == null)
            {
                return NotFound();
            }

            return quoteDto;
        }

        // GET: quotes/Violet/22
        [HttpGet("{creator_id}/{id}")]
        public async Task<ActionResult<QuoteDTO>> GetQuote(int creator_id, int id)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            var quote = await _context.Quotes.FindAsync(id);

            if (quote == null || quote.user_id != creator_id)
            {
                return NotFound();
            }

            QuoteDTO quoteDto = new QuoteDTO();
            quoteDto.Id = quote.id;
            quoteDto.Quote = quote.the_quote;
            quoteDto.SaidBy = quote.who_said;
            quoteDto.When = quote.when_was_said.ToString("yyyy-MM-dd");
            return quoteDto;
        }

        // PUT: quotes/Violet/22
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{creator_id}/{id}")]
        public async Task<IActionResult> PutQuote(int creator_id, int id, QuoteDTO quoteDto)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }

            var quote = await _context.Quotes.FindAsync(id);

            if (quote == null || quote.user_id != creator_id)
            {
                return NotFound();
            }

            quote.the_quote = quoteDto.Quote;
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

        // POST: quotes/Violet
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{creator_id}")]
        public async Task<ActionResult<QuoteDTO>> PostQuote(int creator_id, QuoteDTO quoteDto)
        {
            if (_context.Quotes == null)
            {
                return Problem("Entity set 'QuoteContext.Quotes'  is null.");
            }
            Quote quote = new Quote();
            quote.the_quote = quoteDto.Quote;
            quote.who_said = quoteDto.SaidBy;
            quote.when_was_said = DateOnly.Parse(quoteDto.When);
            quote.user_id = creator_id;
            quote.creation_date = DateTime.Now;
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuote), new { creator_id = quote.user_id, quote.id }, quote);
        }

        // DELETE: quotes/Violet/22
        [HttpDelete("{creator}/{id}")]
        public async Task<IActionResult> DeleteQuote(int creator_id, int id)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }

            var quote = await _context.Quotes.FindAsync(id);

            if (quote == null || quote.user_id != creator_id)
            {
                return NotFound();
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
