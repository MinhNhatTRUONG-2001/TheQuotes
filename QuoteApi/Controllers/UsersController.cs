using Microsoft.AspNetCore.Mvc;
using QuoteApi.Data;

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

        // GET: api/<UsersController>
        [HttpGet]
        public IEnumerable<string> Register()
        {
            throw new NotImplementedException();
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        public string Login(int id)
        {
            throw new NotImplementedException();
        }

        // GET api/<UsersController>/5
        [HttpGet("info/{id}")]
        public string GetUserInfo(int id)
        {
            throw new NotImplementedException();
        }

        // POST api/<UsersController>
        [HttpPost]
        public void ChangeDisplayedName([FromBody] string value)
        {
            throw new NotImplementedException();
        }

        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        public void ChangePassword(int id, [FromBody] string value)
        {
            throw new NotImplementedException();
        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        public void DeleteAccount(int id)
        {
            throw new NotImplementedException();
        }
    }
}
