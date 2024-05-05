using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlinkBackend.Classes
{
    public class SentProposal
    {
        public string Movie_ID { get; set; }
        public string Writer_ID { get; set; }
        public string Editor_ID { get; set; }
        public string Movie_Name { get; set; }
        public string[] Genre { get; set; }
        public string Type { get; set; }
        public string Director { get; set; }
        public DateTime DueDate { get; set; }
        public string Cover_Image { get; set; } // Can be a file path or URL
        public string Image { get; set; }       // Can be a file path or URL
        public IFormFile Cover_Image_File { get; set; } // Actual file data (if applicable)
        public IFormFile Image_File { get; set; }
    }
}