using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlinkBackend.Models
{
    public class User
    {
        public Nullable<int> Editor_ID { get; set; }
        public Nullable<int> Reader_ID { get; set; }
        public Nullable<int> Writer_ID { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Image { get; set; }
        public string Role { get; set; }
        public Nullable<int> Balance { get; set; }
        public Nullable<double> Rating { get; set; }
    }
}