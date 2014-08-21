﻿using System.Collections.Generic;
using System.Web.Mvc;

namespace IDSM.Model.ViewModels
{
    public class TeamOverViewViewModel
    {
        public IEnumerable<PlayerDto> PlayersSearchedFor { get; set; }
        public IEnumerable<UserTeam_Player> PlayersChosen { get; set; }
        //public IEnumerable<Banter> Banters { get; set; }
        public IEnumerable<Banter> Banters { get; set; }
        
        public IEnumerable<SelectListItem> Clubs { get; set; }
        public int GameId { get; set; }
        public string GameName { get; set; }
        public int GameCurrentOrderPosition { get; set; }
        public int UserTeamId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int UserTeamOrderPosition { get; set; }
        public string AddedPlayerMessage { get; set; }
        public bool HasEnded { get; set; }
    }

    public class PlayerDto : Player
    {
        public bool HasBeenChosen { get; set; }
    }

    public class Banters
    {
        //public IEnumerable<Banter> Banters { get; set; }
        public int GameId { get; set; }
        public int UserTeamId { get; set; }
    }
}