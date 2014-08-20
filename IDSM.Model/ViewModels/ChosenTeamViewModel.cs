
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using IDSM.Model;


namespace IDSM.Model.ViewModels
{
    /// <summary>
    /// ChosenTeamViewModel
    /// Used for the ViewPlayers/Index view
    /// Holds all players currently selected by UserTeam
    /// </summary>
    public class ChosenTeamViewModel
    {
        public IEnumerable<UserTeam_Player> PlayersChosen { get; set; }
        public int GameID { get; set; }
        public int UserTeamID { get; set; }
    }
}