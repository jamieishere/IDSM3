﻿using System;
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
        IEnumerable<Game> GetAllGamesUserCurrentlyPlaying(int userId);
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
        /// GetAllGamesUserCurrentlyPlaying
        /// Gets all Games, eager loads User (is this correct term?)
        /// </summary>
        /// <returns>IEnumerable<Game></returns>
        public IEnumerable<Game> GetAllGamesUserCurrentlyPlaying(int userId)
        {
            var _games = DataContext.Games
                    //.Include(x => x.UserTeams.Where(u => u.UserId == userId).SingleOrDefault())
                    .Include(x => x.UserTeams.SingleOrDefault(u => u.UserId == userId))
                    .Where(x => x.HasEnded!=true)
                    .ToList();
            return _games;
        }

    }
}
