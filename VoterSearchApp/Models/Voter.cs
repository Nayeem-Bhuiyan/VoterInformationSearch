using System;
using System.ComponentModel.DataAnnotations;

namespace VoterSearchApp.Models
{
    public class Voter
    {
        public string SerialNumber { get; set; }
        public string VoterNumber { get; set; }
        public string Name { get; set; }
        public string FatherName { get; set; }
        public string MotherName { get; set; }
        public string Profession { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PdfFileName { get; set; }
        public string Address { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }

        // Helper method for search
        public bool ContainsText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var searchText = text.ToLower();

            return (!string.IsNullOrEmpty(VoterNumber) && VoterNumber.ToLower().Contains(searchText)) ||
                   (!string.IsNullOrEmpty(Name) && Name.ToLower().Contains(searchText)) ||
                   (!string.IsNullOrEmpty(FatherName) && FatherName.ToLower().Contains(searchText)) ||
                   (!string.IsNullOrEmpty(MotherName) && MotherName.ToLower().Contains(searchText));
        }

        public bool EndsWithDigits(string digits)
        {
            if (string.IsNullOrEmpty(VoterNumber) || string.IsNullOrEmpty(digits))
                return false;

            return VoterNumber.EndsWith(digits);
        }
    }

    public class VoterSearchModel
    {
        public string SearchText { get; set; }
    }
}