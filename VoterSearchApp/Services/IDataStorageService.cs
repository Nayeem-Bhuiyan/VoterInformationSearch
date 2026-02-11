using VoterSearchApp.Models;

namespace VoterSearchApp.Services
{
    public interface IDataStorageService
    {
        void AddVoter(Voter voter);
        void AddVoters(List<Voter> voters);
        List<Voter> GetAllVoters();
        PagedResult<Voter> GetPagedVoters(int page, int pageSize, string searchText = null);
        List<Voter> SearchVoters(string searchText);
        void SaveToFile();
        void LoadFromFile();
        void ClearAll();
    }
}