using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
                            .OrderByDescending(q => q.QuoteCreateDate)
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
                quoteDto.Id = quote.Id;
                quoteDto.Quote = quote.TheQuote;
                quoteDto.SaidBy = quote.WhoSaid;
                quoteDto.When = quote.WhenWasSaid.ToString("yyyy-MM-dd");
                top5QuotesDto.Add(quoteDto);
            }
            return top5QuotesDto;
        }

        // GET: quotes/Violet
        [HttpGet("{creator}")]
        public async Task<ActionResult<List<QuoteDTO>>> GetQuotes(string creator)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            var quoteDto = await _context.Quotes
                        .Where(q => q.QuoteCreator.ToLower() == creator.ToLower())
                        .Select(q => new QuoteDTO { Id = q.Id, Quote = q.TheQuote, SaidBy = q.WhoSaid, When = q.WhenWasSaid.ToString("yyyy-MM-dd") })
                        .ToListAsync();

            if (quoteDto == null)
            {
                return NotFound();
            }

            return quoteDto;
        }

        // GET: quotes/Violet/22
        [HttpGet("{creator}/{id}")]
        public async Task<ActionResult<QuoteDTO>> GetQuote(string creator, int id)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }
            var quote = await _context.Quotes.FindAsync(id);

            if (quote == null || quote.QuoteCreatorNormalized != creator.ToUpper())
            {
                return NotFound();
            }

            QuoteDTO quoteDto = new QuoteDTO();
            quoteDto.Id = quote.Id;
            quoteDto.Quote = quote.TheQuote;
            quoteDto.SaidBy = quote.WhoSaid;
            quoteDto.When = quote.WhenWasSaid.ToString("yyyy-MM-dd");
            return quoteDto;
        }

        // PUT: quotes/Violet/22
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{creator}/{id}")]
        public async Task<IActionResult> PutQuote(string creator, int id, QuoteDTO quoteDto)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }

            var quote = await _context.Quotes.FindAsync(id);

            if (quote == null || quote.QuoteCreatorNormalized != creator.ToUpper())
            {
                return NotFound();
            }

            quote.TheQuote = quoteDto.Quote;
            quote.WhoSaid = quoteDto.SaidBy;
            quote.WhenWasSaid = DateTime.Parse(quoteDto.When);

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
        [HttpPost("{creator}")]
        public async Task<ActionResult<QuoteDTO>> PostQuote(string creator, QuoteDTO quoteDto)
        {
            if (_context.Quotes == null)
            {
                return Problem("Entity set 'QuoteContext.Quotes'  is null.");
            }
            Quote quote = new Quote();
            quote.TheQuote = quoteDto.Quote;
            quote.WhoSaid = quoteDto.SaidBy;
            quote.WhenWasSaid = DateTime.Parse(quoteDto.When);
            quote.QuoteCreator = creator;
            quote.QuoteCreateDate = DateTime.Now;
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetQuote), new { creator = quote.QuoteCreator, id = quote.Id }, quote);
        }

        // DELETE: quotes/Violet/22
        [HttpDelete("{creator}/{id}")]
        public async Task<IActionResult> DeleteQuote(string creator, int id)
        {
            if (_context.Quotes == null)
            {
                return NotFound();
            }

            var quote = await _context.Quotes.FindAsync(id);

            if (quote == null || quote.QuoteCreatorNormalized != creator.ToUpper())
            {
                return NotFound();
            }

            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool QuoteExists(int id)
        {
            return (_context.Quotes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
