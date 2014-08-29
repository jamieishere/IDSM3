using System.Linq;
using System.Web.UI.WebControls;
using IDSM.Model;
using System.Collections.Generic;
using System.Web.Mvc;
using IDSM.Model.ViewModels;
using IDSM.ServiceLayer;
using IDSM.Signlr;
using Microsoft.AspNet.SignalR;

namespace IDSM.Controllers
{
    [System.Web.Mvc.Authorize]
    public class ViewPlayersController : Controller
    {
        private IService _service;
        //private const int _teamSize = 2;

        public ViewPlayersController(IService service)
        {
            _service = service;
        }

        public ActionResult Index(int? userTeamId, int userId, int gameId, string footballClub, string searchString)
        {
            var _activeTeams = _service.GetAllGamesUserCurrentlyPlaying(userId, userTeamId, footballClub, searchString);
            return View(_activeTeams);
        }

        [HttpPost]
        public ActionResult TeamOverView(int userTeamId, string clubs, string searchString, int gameId)
        {
            if (clubs == null) clubs = "";
            if (searchString == null) searchString = "";

            var _teamOverView = _service.GetTeamOverViewViewModel((int)userTeamId, gameId, clubs, searchString);
            if (Request.IsAjaxRequest())
            {
               
                return PartialView("playerlist", _teamOverView);
            }
            return PartialView("playerlist", _teamOverView);
        }
    }
}
