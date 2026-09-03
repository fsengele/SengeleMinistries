using System;

namespace SengeleMinistries.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Speaker { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
    }
}
