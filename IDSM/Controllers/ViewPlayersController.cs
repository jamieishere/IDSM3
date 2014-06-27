using System.Web.UI.WebControls;
using IDSM.Model;
using System.Collections.Generic;
using System.Web.Mvc;
using IDSM.Model.ViewModels;
using IDSM.ServiceLayer;

namespace IDSM.Controllers
{
    public class ViewPlayersController : Controller
    {
        private IService _service;
        private const int _teamSize = 1;

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
        public ActionResult TeamOverView(int userTeamId, string clubs, string searchString)
        {
            if (clubs == null) clubs = "";
            if (searchString == null) searchString = "";

            var _teamOverView = _service.GetTeamOverViewViewModel((int)userTeamId, clubs, searchString);
            if (Request.IsAjaxRequest())
            {
               
                return PartialView("playerlist", _teamOverView);
            }
            return PartialView("playerlist", _teamOverView);
        }

        public ActionResult AddPlayer(int playerId, int userTeamId, int gameId, int userId)
        {
            ViewBag.Status = "Thre was a problem, player not added"; 
            if(_service.AddUserTeamPlayer(playerId, userTeamId, gameId).Status)
                ViewBag.Status = "Player added";

            return RedirectToAction("Index", new { userTeamId = userTeamId, gameId = gameId, userId = userId });
        }

        public ActionResult AddBanter(int userTeamId, string banter)
        {
            _service.AddBanter(userTeamId, banter);
            return RedirectToAction("Index", new { userteamid = userTeamId });
        }
    }
}
