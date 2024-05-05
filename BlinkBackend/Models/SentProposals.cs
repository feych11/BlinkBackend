//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BlinkBackend.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SentProposals
    {
        public int SentProposal_ID { get; set; }
        public Nullable<int> Movie_ID { get; set; }
        public Nullable<int> Editor_ID { get; set; }
        public Nullable<int> Writer_ID { get; set; }
        public string Movie_Name { get; set; }
        public string Cover_Image { get; set; }
        public string Image { get; set; }
        public string Genre { get; set; }
        public string Type { get; set; }
        public Nullable<int> Episode { get; set; }
        public string Director { get; set; }
        public Nullable<int> Balance { get; set; }
        public string Status { get; set; }
        public string Sent_at { get; set; }
        public string DueDate { get; set; }
        public Nullable<bool> Writer_Notification { get; set; }
        public Nullable<bool> Editor_Notification { get; set; }
    
        public virtual Editor Editor { get; set; }
        public virtual Writer Writer { get; set; }
    }
}