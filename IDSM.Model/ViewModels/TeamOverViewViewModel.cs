using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace IDSM.Model.ViewModels
{
    public class TeamOverViewViewModel
    {
        public IEnumerable<PlayerDto> PlayersSearchedFor { get; set; }
        public IEnumerable<UserTeam_Player> PlayersChosen { get; set; }
        //public IEnumerable<Banter> Banters { get; set; }
        public BantersDto Banters { get; set; }
        
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

    [NotMapped] // why need this attribute: http://stackoverflow.com/questions/6553935/ef-code-first-invalid-column-name-discriminator-but-no-inheritance
    public class BantersDto : Banter
    {
        public IEnumerable<Banter> Banter { get; set; }
        public int CurrentGameId { get; set; }
        public int CurrentUserTeamId { get; set; }
    }

    //[NotMapped]
    //public class BantersDto : Banter
    //{
    //    public IEnumerable<Banter> Banter { get; set; }
    //    public int CurrentGameId { get; set; }
    //    public int CurrentUserTeamId { get; set; }
    //}
}