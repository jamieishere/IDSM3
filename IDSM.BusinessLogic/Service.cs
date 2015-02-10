using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Web.Mvc;
using IDSM.Helpers;
using IDSM.Model;
using IDSM.Model.ViewModels;
using IDSM.Repository;
using IDSM.Repository.DTOs;
using AutoMapper;
using System.IO;
using CsvHelper;
using IDSM.ViewModel;
using System.Configuration;

namespace IDSM.ServiceLayer
{
    /// <summary>
    /// Service layer to wrap all the repository classes 
    /// </summary>
    public class Service: IService, IDisposable
    {
        private readonly IUnitOfWork _unitOfWork;
        private IUserRepository _users;
        private IUserTeamRepository _userTeams;
        private IPlayerRepository _players;
        private IGameRepository _games;
        private IUserTeam_PlayerRepository _userTeamPlayers;
        private IBanterRepository _banters;

        // unit of work injected by Unity
        public Service(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            InitialiseRepos();
        }

        // create instances of all repository classes
        private void InitialiseRepos(){
            _users = _users ?? new UserRepository(_unitOfWork.Context);
            _userTeams = _userTeams ?? new UserTeamRepository(_unitOfWork.Context);
            _players = _players ?? new PlayerRepository(_unitOfWork.Context);
            _games = _games ?? new GameRepository(_unitOfWork.Context);
            _userTeamPlayers = _userTeamPlayers ?? new UserTeam_PlayerRepository(_unitOfWork.Context);
            _banters= _banters ?? new BanterRepository(_unitOfWork.Context);
        }

        #region PUBLIC METHODS ----- these are used in the Controllers

        /// <summary>
        /// Creates a new game
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public OperationStatus CreateGame(int userId, string name)
        {
            OperationStatus _opStatus;
            try
            {
                _opStatus = _games.Create(new Game {CreatorId = userId, Name = name});
                if (_opStatus.Status) Save();
            }
            catch (Exception _exp)
            {
                OperationStatus.CreateFromException("Create Game failed.", _exp, true);
                throw; 
            }
            return _opStatus;
        }

        /// <summary>
        /// Gets all games
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Game> GetAllGames()
        {
            var _allgames = _games.GetAllGames();
            return _allgames;
        }

        /// <summary>
        /// Gets all player's clubs
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAllClubs()
        {
            var _clubLst = new List<string>();
            var _clubs = _players.GetList().OrderBy(p => p.Club).Select(p => p.Club);
            _clubLst.AddRange(_clubs.Distinct());

            return _clubLst;
        }

        public bool TryGetUserTeam(out UserTeam ut, int userTeamId)
        {
            return _userTeams.TryGet(out ut, x => x.Id == userTeamId);
        }

        /// <summary>
        /// Starts a game - sets initial conditions for the game, shuffles userteam order etc.
        /// </summary>
        /// <param name="gameId"></param>
        public void StartGame(int gameId)
        {
            try
            {
                List<UserTeam> _allUserTeamsForGame = _userTeams.GetList(x => x.GameId == gameId).ToList();
                if (_allUserTeamsForGame.Any())
                {
                    _allUserTeamsForGame.Shuffle();
                    foreach (UserTeam _team in _allUserTeamsForGame)
                    {
                        _team.OrderPosition = _allUserTeamsForGame.IndexOf(_team);
                    }

                    Game _game = _games.Get(x => x.Id == gameId);
                    _game.HasStarted = true;
                    MapGameToDtoAndUpdate(_game);
                }
                Save();
            }
            catch (Exception _exp)
            {
                OperationStatus.CreateFromException(String.Format("StartGame failed. gameId:{0}", gameId), _exp, true);
                throw;
            }
        }

        /// <summary>
        /// Resets a game - deletes all userteams, etc
        /// </summary>
        /// <param name="gameId"></param>
        public void ResetGame(int gameId)
        {
            try
            {
                IEnumerable<UserTeam> _allUserTeamsForGame = _userTeams.GetList(x => x.GameId == gameId);
                if (_allUserTeamsForGame != null)
                {
                    foreach (UserTeam _team in _allUserTeamsForGame)
                    {
                        //cascading delete setup for UserTeam - UserTeam_Players in IDSMContext
                        _userTeams.Delete(_team);
                    }
                }

                IEnumerable<Banter> _allBanterForGame = _banters.GetList(x => x.GameId == gameId);
                if (_allBanterForGame != null)
                {
                    foreach (Banter _banter in _allBanterForGame)
                    {
                        _banters.Delete(_banter);
                    }
                } 

                //reset all game properties to default
                Game _game = _games.Get(x => x.Id == gameId);
                _game.WinnerId = 0;
                _game.HasEnded = false;
                _game.HasStarted = false;
                _game.CurrentOrderPosition = 0;
                MapGameToDtoAndUpdate(_game);
                Save();
            }
            catch (Exception _exp)
            {
                OperationStatus.CreateFromException(String.Format("ResetGame failed. gameId:{0}", gameId), _exp, true);
                throw;
            }
        }

        /// <summary>
        /// Adds a userteam (user's team) to a game
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="gameId"></param>
        public void AddUserToGame(int userId, int gameId)
        {
            try
            {
                UserTeam ut = null;
                if (!_userTeams.TryGet(out ut, x => x.GameId == gameId && x.UserId == userId))
                {
                    _userTeams.Create(new UserTeam() { UserId = userId, GameId = gameId });   
                }
                Save();
            }
            catch (Exception _exp)
            {
                OperationStatus.CreateFromException(String.Format("AddUserToGame failed. userId:{0}, gameId:{1}", userId, gameId), _exp, true);
                throw;
            }
        }

        /// <summary>
        /// Adds a banter entry
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="userTeamId"></param>
        /// <param name="banter"></param>
        public void AddBanter(int gameId, int userTeamId, string banter)
        {
            try
            {
                _banters.Create(new Banter() {GameId = gameId, UserTeamId = userTeamId, BanterText = banter, TimeStamp = DateTime.Now});
                Save();
            }
            catch (Exception _exp)
            {
                OperationStatus.CreateFromException(String.Format("AddBanter failed. userTeamId:{0}", userTeamId), _exp, true);
                throw;
            }
        }

        /// <summary>
        /// Adds a player to the user's team for this game.
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="userTeamId"></param>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public OperationStatus AddUserTeamPlayer(int playerId, int userTeamId, int gameId)
        {
            Player _player = null;
            var _opStatus = new OperationStatus();
            // check a Player exists for this playerId
            if (!_players.TryGet(out _player, x => x.Id == playerId)) return _opStatus;

            try
            {
                _opStatus = SaveUtPlayerAndUpdateGame(userTeamId, gameId, 1, 1, playerId);
                Save();
            }
            catch (Exception _exp)
            {
                OperationStatus.CreateFromException(String.Format("AddUserTeamPlayer failed. playerId:{0}, userTeamId:{1}, gameId:{2}", playerId, userTeamId, gameId), _exp, true);
                throw;
            }
            return _opStatus;
        }

        public AddUserTeamViewModel GetAddUserTeamViewModelForGame(Game game)
        {
            try
            {
                IEnumerable<UserProfile> _allusers = _users.GetList();
                return new AddUserTeamViewModel() { Users = _allusers, Game = game };
            }
            catch (Exception _exp)
            {
                OperationStatus.CreateFromException(String.Format("GetAddUserTeamViewModelForGame failed. gameid:{0}", game.Id), _exp, true);
                throw;
            }
        }

        /// <summary>
        /// Get all active games for a user (games started)
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userTeamId"></param>
        /// <param name="footballClub"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public ActiveTeamsViewModel GetAllGamesUserCurrentlyPlaying(int userId, int? userTeamId, string footballClub, string searchString)
        {
            //var _activeGames = _games.GetAllGamesUserCurrentlyPlaying(userId);
            var _activeUserTeams = _userTeams.GetAllUserTeamsCurrentlyActive(userId);
            var _activeTeamsVM = new List<ActiveTeam>();
            var _tempTeam = new ActiveTeam();
            var _tempTeamOV = new TeamOverViewViewModel();


            foreach (UserTeam uts in _activeUserTeams)
            {
                _tempTeam.GameName = uts.Game.Name;
                _tempTeam.IsActive = (uts.OrderPosition==uts.Game.CurrentOrderPosition);
                _tempTeam.UserTeamId = uts.Id;
                _tempTeam.UserId = uts.UserId;
                _tempTeam.GameId = uts.GameId;
                _activeTeamsVM.Add(_tempTeam);
                _tempTeam = new ActiveTeam();  // item stored in list is reference, not copy... need to new up item othrewise just end up with list of same items
            }
            if (userTeamId != null)
            {
                return new ActiveTeamsViewModel() { ActiveTeams = _activeTeamsVM, TeamOverView = GetTeamOverViewViewModel((int)userTeamId,null,footballClub, searchString) };
            }
            else
            {
                return new ActiveTeamsViewModel() { ActiveTeams = _activeTeamsVM };
            }
            
        }

        public TeamOverViewViewModel GetNextTeamOverViewViewModel(int userTeamId, int gameId)
        {
            return GetTeamOverViewVm(userTeamId, gameId, "", "", true);
        }

        public TeamOverViewViewModel GetTeamOverViewViewModel(int userTeamId, int? gameId, string footballClub, string searchString)
        {
            return GetTeamOverViewVm(userTeamId, gameId, footballClub, searchString, false);
        }


        private TeamOverViewViewModel GetTeamOverViewVm(int userTeamId, int? gameId, string footballClub, string searchString, bool getNextTeam)
        {
            BantersDto _bantersDto = new BantersDto()
                {Banter = new List<Banter>(), CurrentGameId = (gameId==null) ? 0 : (int)gameId, CurrentUserTeamId = userTeamId};

            IEnumerable<SelectListItem> _clubs = _players.GetList().OrderBy(p => p.Club).Select(p => new SelectListItem(){Value=p.Club, Text =p.Club }).Distinct();



            // if userTeamId==0, just return banter & clubs.
            if (userTeamId == 0)
            {
                return new TeamOverViewViewModel()
                {
                    PlayersSearchedFor = null,
                    PlayersChosen = null,
                    OtherTeams = null,
                    GameId = 0,
                    GameName = null,
                    GameCurrentOrderPosition = 0,
                    UserTeamId = 0,
                    UserName = null,
                    UserId = 0,
                    UserTeamOrderPosition = 0,
                    AddedPlayerMessage = null,
                    HasEnded = false,
                    Banters = _bantersDto, //_banterForThisGame,
                    Clubs = _clubs
                };
            }
            // else get the userteam & other data for full TeamOverView
            else
            {
                try
                {
                    // setup message to be displayed to User once they have added their chosen player
                    string _addedPlayerMessage = "Current player is {0}.  There are {1} turns left before your go.";
                    int _tmpTurnsLeft = 0;
                    UserTeam _userTeam = null;
                    UserProfile _user = null;
                    IEnumerable<UserTeam_Player> _playersPickedForThisTeam = null;
                    IEnumerable<PlayerDto> _playersNotPickedForAnyTeam = new List<PlayerDto>();
                    Game _game = null;


                    // get this UserTeam, and from it, the User and current Game (the gameid param seems redundant)
                    if (!_userTeams.TryGet(out _userTeam, x => x.Id == userTeamId))
                        throw new ApplicationException();
                    if (!_users.TryGet(out _user, x => x.UserId == _userTeam.UserId))
                        throw new ApplicationException();
                    if (!_games.TryGet(out _game, x => x.Id == _userTeam.GameId))
                        throw new ApplicationException();

                    IEnumerable<UserTeam> _otherUserTeams = _userTeams.GetList(u => u.GameId == _userTeam.GameId);
                    List<UserTeam> _test = _otherUserTeams.ToList();

                    if (_game.HasEnded)
                    {
                        //if we want the NEXT userTeam (rather than current), we need to find out where we are in the order of play.
                        if (getNextTeam)
                        {
                            int _utCount = _game.UserTeams.Count;
                            int _orderToGet = (_userTeam.OrderPosition + 1 == _utCount) ? 0 : _userTeam.OrderPosition + 1;

                            UserTeam _nextUserTeam =
                                _userTeams.Get(u => u.OrderPosition == _orderToGet && u.GameId == _game.Id);
                            _userTeam = _nextUserTeam;

                        }

                        var _winner = _userTeams.Get(u => u.Id == _game.WinnerId);

                        _addedPlayerMessage = "Game finished!<br />Winner is "+_winner.User.UserName;
                        //_banterForThisGame = _banters.GetList(b => b.GameId == _userTeam.GameId);
                        //_banterForThisGame = GetGameBanter(_userTeam.GameId);
                        _bantersDto = new BantersDto() { Banter = GetGameBanter(_userTeam.GameId), CurrentGameId = _userTeam.GameId, CurrentUserTeamId = userTeamId };

                        _playersPickedForThisTeam = GetAllChosenUserTeamPlayersForTeam(_userTeam.Id);
                        _playersNotPickedForAnyTeam = GetPlayersNotPickedForAnyTeam(_game.Id, footballClub, searchString);
                    }
                    else
                    {
                        //if we want the NEXT userTeam (rather than current), we need to find out where we are in the order of play.
                        if(getNextTeam)
                        {
                            int _utCount = _game.UserTeams.Count;
                            int _orderToGet = (_userTeam.OrderPosition + 1 == _utCount) ? 0 : _userTeam.OrderPosition + 1;

                            UserTeam _nextUserTeam =
                                _userTeams.Get(u => u.OrderPosition == _orderToGet && u.GameId == _game.Id);
                            _userTeam = _nextUserTeam;

                        }

                        // setup the TeamOverViewViewModel properties
                        _bantersDto = new BantersDto() { Banter = GetGameBanter(_userTeam.GameId), CurrentGameId = _userTeam.GameId, CurrentUserTeamId = userTeamId }; 
                        _playersPickedForThisTeam = GetAllChosenUserTeamPlayersForTeam(_userTeam.Id);
                        _playersNotPickedForAnyTeam = GetPlayersNotPickedForAnyTeam(_game.Id, footballClub, searchString);

                        List<UserTeam> _userTeamsForGame = _userTeams.GetList(x => x.GameId == _game.Id).ToList();


                        // this next bit is to set the 'turns left' part of the addedplayer message
                        // i don't really need it....

                        // if it's NOT the user's turn
                        if (_userTeam.OrderPosition != _game.CurrentOrderPosition)
                        {
                            // get the user who's turn it actually is
                            UserTeam _activeUt =
                                _userTeams.Get(
                                    s => s.OrderPosition == _game.CurrentOrderPosition && s.GameId == _game.Id,
                                    u => u.User);

                            // then get their username
                            string _tmpActiveUserName = _users.Get(x => x.UserId == _activeUt.UserId).UserName;

                            // set a message to display the number of turns left until the user's turn
                            switch (_activeUt.OrderPosition > _userTeam.OrderPosition)
                            {
                                case true:
                                    _tmpTurnsLeft = (_userTeam.OrderPosition == _userTeamsForGame.Count)
                                        ? 1
                                        : _activeUt.OrderPosition - _userTeam.OrderPosition;
                                    break;
                                case false:
                                    _tmpTurnsLeft = (_userTeam.OrderPosition == _userTeamsForGame.Count)
                                        ? 1
                                        : _userTeam.OrderPosition - _activeUt.OrderPosition;
                                    break;
                            }
                            _addedPlayerMessage = String.Format(_addedPlayerMessage, _tmpActiveUserName, _tmpTurnsLeft);
                        }
                    }

                    return new TeamOverViewViewModel()
                    {
                        PlayersSearchedFor = _playersNotPickedForAnyTeam,
                        PlayersChosen = _playersPickedForThisTeam,
                        OtherTeams = _otherUserTeams,
                        GameId = _userTeam.GameId,
                        GameName = _game.Name,
                        GameCurrentOrderPosition = _game.CurrentOrderPosition,
                        UserTeamId = _userTeam.Id,
                        UserId = _userTeam.UserId,
                        //UserName = _user.UserName,
                        UserName = _userTeam.User.UserName,
                        UserTeamOrderPosition = _userTeam.OrderPosition,
                        AddedPlayerMessage = _addedPlayerMessage,
                        HasEnded = _game.HasEnded,
                        Banters = _bantersDto, //_banterForThisGame,
                        Clubs = _clubs
                    };
                }
                catch (Exception _exp)
                {
                    OperationStatus.CreateFromException(
                        String.Format("GetTeamOverViewViewModel failed. userteam:{0}, footballclub:{1}, searchstring:{2}",
                            userTeamId, footballClub, searchString), _exp, true);
                    throw;
                }
            }
        }

        /// <summary>
        /// Get all games a user has been added to (may not have started)
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public IEnumerable<Game> GetAllGamesUserParticipatesIn(int p)
        {
            var _allGamesForThisUser = _games.GetAllGamesUserParticipatesIn(p);
            return _allGamesForThisUser;
        }

        /// <summary>
        /// Get all banter for this game
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public IEnumerable<Banter> GetGameBanter(int gameId)
        {
            var _allBanterForThisGame = _banters.GetList(b => b.GameId==gameId).OrderByDescending(b => b.TimeStamp);
            return _allBanterForThisGame;
        }

        //public IEnumerable<Game> GetAllGamesCreatedByThisUserOrThatHaveThisUserAsAPlayer(int p)
        //{
        //    var _allGamesForThisUser = _games.GetAllGamesCreatedByThisUserOrThatHaveThisUserAsAPlayer(p);
        //    return _allGamesForThisUser;
        //}


        //public ViewPlayersViewModel GetViewPlayersViewModel(int userTeamId, string footballClub, string searchString)
        //{
        //    // so, in here... i need to.... 
        //    // get banter for this game.
        //    // banter is heriarchical.
        //    // better stored as XML?
        //    // or DB?
        //    // reddit format?
        //    // or what's app format?
        //    // do conversation style - so whats app... 
        //    // so... add banter, needs timestamp

        //    // so... get all banters.
        //    // IEnumerable<Banter> _banter = _games.GetBanter(gameId);
        //    // pass that into viewmodel.

        //    IEnumerable<Banter> _banterForThisGame = _banters.GetList();
        //   // IEnumerable<string> _clubs = GetAllClubs();
        //    //IEnumerable<SelectListItem> _clubsSelectListItems =
        //    //    new SelectList(_players.GetList().OrderBy(p => p.Club).Select(p => p.Club), "name", "name");

        //    //            var _clubLst = new List<string>();
        //    //IEnumerable<SelectListItem> _clubs =  new SelectList(_players.GetList().OrderBy(p => p.Club).Select(p => p.Club),"Club","Club");
        //    SelectList _clubs = new SelectList(_players.GetList().OrderBy(p => p.Club).Select(p => p.Club), "Club", "Club");
        //    //_clubLst.AddRange(_clubs.Distinct());

        //    //return _clubLst;
        //    if (userTeamId == 0)
        //    {
        //        return new ViewPlayersViewModel()
        //        {
        //            PlayersSearchedFor = null,
        //            PlayersChosen = null,
        //            GameId = 0,
        //            GameName = null,
        //            GameCurrentOrderPosition = 0,
        //            UserTeamId = 0,
        //            UserName = null,
        //            UserTeamOrderPosition = 0,
        //            AddedPlayerMessage = null,
        //            HasEnded = false,
        //            Banters = _banterForThisGame,
        //            Clubs = null
        //        };
        //    }
        //    else
        //    {

        //        try
        //        {
        //            // setup message to be displayed to User once they have added their chosen player
        //            string _addedPlayerMessage = "Current player is {0}.  There are {1} turns left before your go.";
        //            int _tmpTurnsLeft = 0;
        //            UserTeam _userTeam = null;
        //            UserProfile _user = null;
        //            IEnumerable<UserTeam_Player> _playersPickedForThisTeam = null;
        //            IEnumerable<PlayerDto> _playersNotPickedForAnyTeam = new List<PlayerDto>();
        //            Game _game = null;

        //            // get this UserTeam, User and Game
        //            if (!_userTeams.TryGet(out _userTeam, x => x.Id == userTeamId))
        //                throw new ApplicationException();
        //            if (!_users.TryGet(out _user, x => x.UserId == _userTeam.UserId))
        //                throw new ApplicationException();
        //            if (!_games.TryGet(out _game, x => x.Id == _userTeam.GameId))
        //                throw new ApplicationException();

        //            if (_game.HasEnded)
        //            {
        //                _addedPlayerMessage = "The game has ended.";
        //            }
        //            else
        //            {
        //                _playersPickedForThisTeam = GetAllChosenUserTeamPlayersForTeam(_userTeam.Id);
        //                _playersNotPickedForAnyTeam = GetPlayersNotPickedForAnyTeam(_game.Id, footballClub, searchString);

        //                List<UserTeam> _userTeamsForGame = _userTeams.GetList(x => x.GameId == _game.Id).ToList();

        //                if (_userTeam.OrderPosition != _game.CurrentOrderPosition)
        //                {
        //                    UserTeam _activeUt =
        //                        _userTeams.Get(
        //                            s => s.OrderPosition == _game.CurrentOrderPosition && s.GameId == _game.Id,
        //                            u => u.User);

        //                    string _tmpActiveUserName = _users.Get(x => x.UserId == _activeUt.UserId).UserName;

        //                    switch (_activeUt.OrderPosition > _userTeam.OrderPosition)
        //                    {
        //                        case true:
        //                            _tmpTurnsLeft = (_userTeam.OrderPosition == _userTeamsForGame.Count)
        //                                ? 1
        //                                : _activeUt.OrderPosition - _userTeam.OrderPosition;
        //                            break;
        //                        case false:
        //                            _tmpTurnsLeft = (_userTeam.OrderPosition == _userTeamsForGame.Count)
        //                                ? 1
        //                                : _userTeam.OrderPosition - _activeUt.OrderPosition;
        //                            break;
        //                    }
        //                    _addedPlayerMessage = String.Format(_addedPlayerMessage, _tmpActiveUserName, _tmpTurnsLeft);
        //                }
        //            }

        //            return new ViewPlayersViewModel()
        //            {
        //                PlayersSearchedFor = _playersNotPickedForAnyTeam,
        //                PlayersChosen = _playersPickedForThisTeam,
        //                GameId = _userTeam.GameId,
        //                GameName = _game.Name,
        //                GameCurrentOrderPosition = _game.CurrentOrderPosition,
        //                UserTeamId = _userTeam.Id,
        //                UserName = _user.UserName,
        //                UserTeamOrderPosition = _userTeam.OrderPosition,
        //                AddedPlayerMessage = _addedPlayerMessage,
        //                HasEnded = _game.HasEnded,
        //                Banters = _banterForThisGame,
        //                Clubs = _clubs
        //            };
        //        }
        //        catch (Exception _exp)
        //        {
        //            OperationStatus.CreateFromException(
        //                String.Format("GetUserTeamViewModel failed. userteam:{0}, footballclub:{1}, searchstring:{2}",
        //                    userTeamId, footballClub, searchString), _exp, true);
        //            throw;
        //        }
        //    }
        //}

        public void Save()
        {
            _unitOfWork.Save();
        }

        public void Dispose()
        {
            if (_unitOfWork != null)
                _unitOfWork.Dispose();
            GC.SuppressFinalize(this);
        }

        public static OperationStatus UploadPlayersCsv(string filePath)
        {
            return ProcessCsvHelper(filePath, new IDSMContext());
        }

        
        /// <summary>
        ///// Uploads players data: requires a correctly formatted csv containing Player data
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="DataContext"></param>
        /// <returns></returns>
        /// <remarks>
        ///// TODO: Currently only works for a small number of rows.  Real file size is 100,000+ rows.  This breaks the upload.
        ///// Need to ensure it works for at least the full set of Premiership clubs.
        /// </remarks>
        public static OperationStatus ProcessCsvHelper(string filePath, IDSMContext DataContext)
        {
            string Feedback = string.Empty;
            StreamReader srCSV = new StreamReader(filePath);
            CsvReader csvReader = new CsvReader(srCSV);

            // NOTE:
            //      'ID' error on CSV import is either is coming from this line, or the for each loop below.
            //       Temporarily fixed by adding an ID column to CSV
            List<Player> FootballPlayerList = new List<Player>();

            try
            {
                FootballPlayerList = new List<Player>(csvReader.GetRecords<Player>());
            }
            catch (Exception ex)
            {
                return OperationStatus.CreateFromException("Error reading from CSV.", ex);
            }

            try
            {
                foreach (Player m in FootballPlayerList)
                {
                    DataContext.Players.Add(m);
                }
                DataContext.SaveChanges();
            }
            catch (Exception ex)
            {
                return OperationStatus.CreateFromException("Error saving players to DB from CSV.", ex);
            }

            srCSV.Dispose();
            csvReader.Dispose();

            return new OperationStatus { Status = true };
        }
        #endregion

        #region PRIVATE METHODS

        private int[] GetAllChosenUserTeamPlayerIdsForGame(int gameId) 
        {
            var _chosenPlayers = _userTeamPlayers.GetList(p => p.GameId ==gameId, p => p.PlayerId).Select(x => x.PlayerId).ToArray();
            return _chosenPlayers;
        }

        private IEnumerable<UserTeam_Player> GetAllChosenUserTeamPlayersForTeam(int userTeamId)
        {
            var _chosenPlayers = _userTeamPlayers.GetList(p => p.UserTeamId == userTeamId, p => p.PlayerId).ToList();
            return _chosenPlayers;
        }

        private OperationStatus SaveUserTeamPlayer(int userteamid, int gameid, int pixelposy, int pixelposx, int playerid)
        {
            // if polayer already exists for this userteam, return true
            var _temp = _userTeamPlayers.Get(p => p.PlayerId == playerid && p.UserTeamId == userteamid);
            if (_temp != null) return new OperationStatus { Status = true };

            var _player = new UserTeam_Player() { UserTeamId = userteamid, GameId = gameid, PixelPosX = pixelposx, PixelPosY = pixelposy, PlayerId = playerid };
            return _userTeamPlayers.Create(_player);
        }

        //private OperationStatus CreateUserTeam(int userid, int gameid)
        //{
        //    //var opStatus = new OperationStatus { Status = false };
        //    var ut = new UserTeam() { UserId = userid, GameId = gameid };
        //    try
        //    {
        //        _userTeams.Create(ut);
        //    }
        //    catch (Exception ex)
        //    {
        //        return OperationStatus.CreateFromException("Error creating userteam.", ex);
        //    }

        //    return new OperationStatus { Status = true, OperationId = ut.Id };
        //}

        private IEnumerable<PlayerDto> GetPlayersNotPickedForAnyTeam(int gameId, string footballClub, string searchString)
        {
            IEnumerable<Player> _footballPlayers = null;
            int[] _chosenPlayerIds = null;

            _footballPlayers = _players.GetList().ToList(); 
            _chosenPlayerIds = GetAllChosenUserTeamPlayerIdsForGame(gameId);

            // map Player to DTO object (has extra properties)
            IEnumerable<PlayerDto> _footballPlayersDto = new List<PlayerDto>();
            Mapper.CreateMap<Player, PlayerDto>();
            _footballPlayersDto = Mapper.Map<IEnumerable<PlayerDto>>(_footballPlayers);

            //update player list, marking those already chosen
            foreach (PlayerDto p in _footballPlayersDto)
            {
                if (_chosenPlayerIds.Contains(p.Id)) { p.HasBeenChosen = true; }
            }

            //if passed, filter players by searchstring
            if (!String.IsNullOrEmpty(searchString)) 
                _footballPlayersDto = _footballPlayersDto.Where(s => s.Name.Contains(searchString));

            //if passed, filter players by club
            if (!String.IsNullOrEmpty(footballClub))
                _footballPlayersDto = _footballPlayersDto.Where(x => x.Club == footballClub);

            return _footballPlayersDto;
        }

        private OperationStatus UpdateGame(int gameId, int userTeamId, int _teamSize)
        {
            Game _game = null;

            if ((_games.TryGet(out _game, g => g.Id == gameId)))
            {
                IEnumerable<UserTeam_Player> _userTeamPlayers = GetAllChosenUserTeamPlayersForTeam(userTeamId);

                // get number of UserTeams participating in the Game
                int _utCount = _game.UserTeams.Count;

                // if this UserTeam has full allocation of Players & is the last team in the order of play, game = finished
                if ((_userTeamPlayers.Count() == _teamSize) && (_game.CurrentOrderPosition + 1 == _utCount))
                {
                    _game.HasEnded = true;
                    _game.WinnerId = CalculateWinner(gameId);
                    _game.CurrentOrderPosition = -10; //arbitrary integer to ensure it is no userteam's 'go'.
                }
                else
                {
                    int _currentOrderPosition = _game.CurrentOrderPosition;
                    _game.CurrentOrderPosition = ((_currentOrderPosition + 1) == _utCount) ? 0 : _currentOrderPosition + 1;
                }

                OperationStatus _gameSaved = MapGameToDtoAndUpdate(_game);
                if (_gameSaved.Status) return new OperationStatus { Status = true };
            }
            return new OperationStatus { Status = false };
        }

        private OperationStatus MapGameToDtoAndUpdate(Game game)
        {
            GameUpdateDto _gameDto = new GameUpdateDto();
            _gameDto = Mapper.Map(game, _gameDto);
            return _games.Update(_gameDto, g => g.Id == _gameDto.Id);
        }

        private OperationStatus SaveUtPlayerAndUpdateGame(int userTeamId, int gameId, int pixelposy, int pixelposx, int playerId)
        {
            OperationStatus _opStatus;

            try
            {
                _opStatus = SaveUserTeamPlayer(userTeamId, gameId, 1, 1, playerId);
                Save();
                
                int _teamSize;
                if (int.TryParse(ConfigurationManager.AppSettings["TeamSize"], out _teamSize))
                {
                    _opStatus = UpdateGame(gameId, userTeamId, _teamSize);
                }
                else { _opStatus = UpdateGame(gameId, userTeamId, 1); }

                Save();

            }
            catch (Exception exp)
            {
                _opStatus = OperationStatus.CreateFromException("Error saving player and updating game: " + exp.Message, exp);
            }

            return _opStatus;
        }

        private int CalculateWinner(int gameId)
        {
            List<UserTeam> _allUserTeamsForGame = _userTeams.GetList(x => x.GameId).ToList();
            var _topScore = new TopScore();

            foreach (UserTeam team in _allUserTeamsForGame)
            {
                int _tempScore = 0;

                List<UserTeam_Player> _chosenPlayersForTeam = (List<UserTeam_Player>)GetAllChosenUserTeamPlayersForTeam(team.Id);
                Player _player;

                foreach (UserTeam_Player player in _chosenPlayersForTeam)
                {
                    _player = _players.Get(x => x.Id == player.PlayerId);
                    _tempScore = _tempScore + _player.Age;
                }
                if (_tempScore > _topScore.Score)
                {
                    _topScore.UserTeamId = team.Id;
                    _topScore.Score = _tempScore;
                }
            }
            return _topScore.UserTeamId;
        }

        private struct TopScore
        {
            public int UserTeamId { get; set; }
            public int Score { get; set; }
        }

        #endregion
    }
}
