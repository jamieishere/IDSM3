using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using IDSM.Logging.Services.Logging;
using IDSM.Logging.Services.Logging.Log4Net;
using IDSM.Model;
using IDSM.Repository.DTOs;

namespace IDSM.Repository
{
    public interface IGameRepository : IRepositoryBase<Game>
    {
        IEnumerable<Game> GetAllGames();
        IEnumerable<Game> GetAllGamesUserParticipatesIn(int i);
    }


    /// <summary>
    /// GameRepository
    /// Contains all methods that access/manipulate Games within IDSMContext
    /// </summary>
    public class GameRepository : RepositoryBase<Game>, IGameRepository
    {
        public GameRepository(IDSMContext context) : base(context) { }

        /// <summary>
        /// GetAllGames
        /// Gets all Games, eager loads User (is this correct term?)
        /// </summary>
        /// <returns>IEnumerable<Game></returns>
        public IEnumerable<Game> GetAllGames()
        {      
            var _games = DataContext.Games
                    .Include(x => x.UserTeams)
                    .Include(x => x.UserTeams.Select(y => y.User))
                    .ToList();
            return _games;
        }

        /// <summary>
        /// Get all Games where the CreatorId==currentUserId, or one of the Game's userteam's id's==currentUserId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<Game> GetAllGamesUserParticipatesIn(int userId)
        {
            // syntax for left join from http://msdn.microsoft.com/en-us/vstudio/ee908647.aspx#leftouterjoin
            var _games = (from _g in DataContext.Games
                         join _ut in DataContext.UserTeams on _g.Id equals _ut.GameId into _uts
                         from _ut in _uts.DefaultIfEmpty()
                         where _g.CreatorId==userId || _ut.UserId==userId || userId==1 //(1=SuperAdmin, so can see all games).
                         select _g).Distinct();
            return _games;
        }
    }
}
