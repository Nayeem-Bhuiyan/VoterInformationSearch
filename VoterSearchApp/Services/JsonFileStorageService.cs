using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VoterSearchApp.Models;

namespace VoterSearchApp.Services
{
    public class JsonFileStorageService : IDataStorageService
    {
        private readonly string _dataFilePath;
        private List<Voter> _voters;
        private readonly IWebHostEnvironment _environment;

        public JsonFileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _dataFilePath = Path.Combine(environment.WebRootPath, "data", "voters.json");
            _voters = new List<Voter>();
            EnsureDataDirectoryExists();
            LoadFromFile();
        }

        private void EnsureDataDirectoryExists()
        {
            var dataDir = Path.GetDirectoryName(_dataFilePath);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
        }

        public void AddVoter(Voter voter)
        {
            _voters.Add(voter);
            SaveToFile();
        }

        public void AddVoters(List<Voter> voters)
        {
            _voters.AddRange(voters);
            SaveToFile();
        }

        public List<Voter> GetAllVoters()
        {
            return _voters.OrderBy(v => v.SerialNumber).ToList();
        }

        /// <summary>
        /// পৃষ্ঠা অনুযায়ী ভোটার তথ্য রিটার্ন করে (Pagination)
        /// </summary>
        public PagedResult<Voter> GetPagedVoters(int page, int pageSize, string searchText = null)
        {
            var query = _voters.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchText))
            {
                var searchLower = searchText.ToLower().Trim();

                // Check if search text is digits (last 4-5 digits search)
                if (searchLower.All(c => char.IsDigit(c)))
                {
                    if (searchLower.Length >= 4 && searchLower.Length <= 5)
                    {
                        // Search by last 4-5 digits
                        query = query.Where(v => !string.IsNullOrEmpty(v.VoterNumber) &&
                               v.VoterNumber.EndsWith(searchLower));
                    }
                    else
                    {
                        // Search full voter number
                        query = query.Where(v => !string.IsNullOrEmpty(v.VoterNumber) &&
                               v.VoterNumber.Contains(searchLower));
                    }
                }
                else
                {
                    // Text search in name fields (case-insensitive)
                    query = query.Where(v =>
                        (!string.IsNullOrEmpty(v.Name) && v.Name.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(v.FatherName) && v.FatherName.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(v.MotherName) && v.MotherName.ToLower().Contains(searchLower)));
                }
            }

            // Get total count before pagination
            var totalCount = query.Count();

            // Apply pagination
            var items = query
                .OrderBy(v => v.SerialNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Return paged result
            return new PagedResult<Voter>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };
        }

        public List<Voter> SearchVoters(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return GetAllVoters();

            var searchLower = searchText.ToLower().Trim();

            // Check if search text is digits (last 4-5 digits search)
            if (searchLower.All(c => char.IsDigit(c)))
            {
                if (searchLower.Length >= 4 && searchLower.Length <= 5)
                {
                    // Search by last 4-5 digits
                    return _voters
                        .Where(v => !string.IsNullOrEmpty(v.VoterNumber) &&
                               v.VoterNumber.EndsWith(searchLower))
                        .OrderBy(v => v.SerialNumber)
                        .ToList();
                }
                else
                {
                    // Search full voter number
                    return _voters
                        .Where(v => !string.IsNullOrEmpty(v.VoterNumber) &&
                               v.VoterNumber.Contains(searchLower))
                        .OrderBy(v => v.SerialNumber)
                        .ToList();
                }
            }
            else
            {
                // Text search in name fields (case-insensitive)
                return _voters
                    .Where(v => (!string.IsNullOrEmpty(v.Name) && v.Name.ToLower().Contains(searchLower)) ||
                           (!string.IsNullOrEmpty(v.FatherName) && v.FatherName.ToLower().Contains(searchLower)) ||
                           (!string.IsNullOrEmpty(v.MotherName) && v.MotherName.ToLower().Contains(searchLower)))
                    .OrderBy(v => v.SerialNumber)
                    .ToList();
            }
        }

        public void SaveToFile()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_voters, options);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }

        public void LoadFromFile()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    _voters = JsonSerializer.Deserialize<List<Voter>>(json) ?? new List<Voter>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
                _voters = new List<Voter>();
            }
        }

        public void ClearAll()
        {
            _voters.Clear();
            SaveToFile();
        }
    }
}

