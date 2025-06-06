﻿using System;
using System.Security.Permissions;

namespace WPFGrowerApp.Models
{
    public class GrowerSearchResult
    {
        public decimal GrowerNumber { get; set; }
        public string GrowerName { get; set; }
        public string ChequeName { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public decimal Acres { get; set; }
        public string Notes { get; set; }
        public string PayGroup { get; set; }
        public string Phone2 { get; set; }
        public bool IsOnHold { get; set; }
    }
}
