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

        public ActionResult Index(int? userTeamId, string footballClub, string searchString)
        {
            // change this.
            // ViewPlayersViewModel needs to:
            // return selectlist inside the view model rather than set viewbag here - see http://stackoverflow.com/questions/6623700/how-to-bind-a-selectlist-with-viewmodel
            // then...

            if (userTeamId == null) userTeamId = 0;
            return View(_service.GetTeamOverViewViewModel((int)userTeamId, footballClub, searchString));
        }


        [HttpPost]
        public ActionResult TeamOverView(int userTeamId, string footballClub, string searchString)
        {
            var _teamOverView = _service.GetTeamOverViewViewModel((int) userTeamId, "", "");
            if (Request.IsAjaxRequest())
            {
               
                return PartialView("teamdetails", _teamOverView);
            }
            return PartialView("teamdetails", _teamOverView);
        }

        public ActionResult AddPlayer(int playerId, int userTeamId, int gameId)
        {
            ViewBag.Status = "Thre was a problem, player not added"; 
            if(_service.AddUserTeamPlayer(playerId, userTeamId, gameId).Status)
                ViewBag.Status = "Player added";

            return RedirectToAction("Index", new { userteamid = userTeamId });
        }

        public ActionResult AddBanter(int userTeamId, string banter)
        {
            _service.AddBanter(userTeamId, banter);
            return RedirectToAction("Index", new { userteamid = userTeamId });
        }
    }
}
