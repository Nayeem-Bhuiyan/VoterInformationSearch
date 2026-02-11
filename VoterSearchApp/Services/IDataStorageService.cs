using System.Collections.Generic;
using VoterSearchApp.Models;

namespace VoterSearchApp.Services
{
    public interface IDataStorageService
    {
        void AddVoter(Voter voter);
        void AddVoters(List<Voter> voters);
        List<Voter> GetAllVoters();
        List<Voter> SearchVoters(string searchText);
        void SaveToFile();
        void LoadFromFile();
        void ClearAll();
    }
}