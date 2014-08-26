using System.Collections.Generic;
using IDSM.Model;
using IDSM.Model.ViewModels;
using IDSM.Repository;
using IDSM.ViewModel;

namespace IDSM.ServiceLayer
{
    public interface IService
    {
        void Save();

        IEnumerable<Game> GetAllGames();
        //ActiveTeamsViewModel GetAllGamesUserCurrentlyPlaying(int userId);

        ActiveTeamsViewModel GetAllGamesUserCurrentlyPlaying(int userId, int? userTeamId, string footballClub, string searchString);
        bool TryGetUserTeam(out UserTeam ut, int userTeamId);
        IEnumerable<string> GetAllClubs();
        OperationStatus CreateGame(int userId, string name);

        void StartGame(int gameId);
        void AddBanter(int gameId, int userTeamId, string banter);
        void ResetGame(int gameId);
        void AddUserToGame(int userId, int gameId);

        AddUserTeamViewModel GetAddUserTeamViewModelForGame(Game game);
       // ViewPlayersViewModel GetViewPlayersViewModel(int userTeamId, string footballClub, string searchString);
        OperationStatus AddUserTeamPlayer(int playerId, int userTeamId, int gameId);

        //GameBanterViewModel GetGameBanter(int gameId);
        TeamOverViewViewModel GetNextTeamOverViewViewModel(int userTeamId, int gameId);
        TeamOverViewViewModel GetTeamOverViewViewModel(int userTeamId, int? gameId, string footballClub, string searchString);

        IEnumerable<Game> GetAllGamesUserParticipatesIn(int p);
        IEnumerable<Banter> GetGameBanter(int gameId);
    }
}
