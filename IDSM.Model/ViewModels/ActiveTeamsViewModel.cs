using System.Collections.Generic;
using System.Web.Mvc;

namespace IDSM.Model.ViewModels
{
    public class ActiveTeamsViewModel
    {
        public IEnumerable<ActiveTeam> ActiveTeams { get; set; }
        public TeamOverViewViewModel TeamOverView { get; set; }
    }

    public class ActiveTeam
    {
        public int UserTeamId { get; set; }
        public bool IsActive { get; set; }
        public string GameName { get; set; }
    }
}