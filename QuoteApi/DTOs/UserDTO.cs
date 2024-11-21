﻿namespace QuoteApi.DTOs
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? DisplayedName { get; set; }
        public string? Password { get; set; }
        public string? CurrentPassword { get; set; } // for changing password only
    }
}
