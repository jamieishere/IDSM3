using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using IDSM.Controllers;
using IDSM.Model.ViewModels;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using IDSM.Model;
using IDSM.Signlr;
using IDSM.ServiceLayer;
using System.Web.Mvc;

namespace IDSM.Signlr
{
    [System.Web.Mvc.Authorize]
    [HubName("banter")]
    public class BanterHub : Hub
    {
        IService _service;
        private readonly static ConnectionMapping<string> _connections = new ConnectionMapping<string>();

        public BanterHub()
        {
            _service =  DependencyResolver.Current.GetService<IService>(); 
        }


        public void Send(string message, int gameId, int userTeamId, string who)
        {
            // Call the addNewMessageToPage method to update clients.
            // this uses a jquery method on the page to append to a div
           

            string name = Context.User.Identity.Name;
            //Clients.All.testAdd("name: " + name);

            //ensures goes to all devices user is using.
            foreach (var connectionId in _connections.GetConnections(who))
            {
                Clients.Client(connectionId).testAdd(name + ": " + message, gameId);
            }

            //next problem is - this could get pushed to whichever game they are currently viewing.






            try
            {
            _service.AddBanter(gameId, userTeamId, message);
            var _updatedBanter = _service.GetGameBanter(1);
            BantersDto _bantersDto = new BantersDto()
                {Banter = _updatedBanter, CurrentGameId = gameId, CurrentUserTeamId = userTeamId};

            // this method i create a dummy controller that can return a partialview to the page - thus i can actually get ALL the banter and refresh the entire view instead of appending bits...
            //http://stackoverflow.com/questions/22098233/partialview-to-html-string
            var bogusController = ViewRenderer.CreateController<HomeController>();
            
                // var partialViewGuts = ViewRenderer.RenderPartialViewToString(bogusController.ControllerContext, "view", model);
                var partialViewGuts = ViewRenderer.RenderPartialView("~/Views/ViewPlayers/banterlist.cshtml", _bantersDto, bogusController.ControllerContext);
                Clients.All.addNewBanterToPage(partialViewGuts);
            }
            catch (Exception ex)
            {

                Clients.All.addNewBanterToPage("There has been a problem getting the banter");
            }

        }
        // if you want to use multiple event handerls for a client method that the server calls you cannot use the generated proxy
        //http://www.asp.net/signalr/overview/signalr-20/hubs-api/hubs-api-guide-javascript-client
        // might be usefl



        //public void AddPlayer(int playerId, int userTeamId, int gameId, int userId, string who)
        public void AddPlayer(int playerId, int userTeamId, int gameId, int userId)
        {
            try
            {

                //string name = Context.User.Identity.Name;
                //Clients.All.testAdd("name: " + name);

                //ensures goes to all devices user is using.
               

            _service.AddUserTeamPlayer(playerId, userTeamId, gameId);

            var _teamOverView = _service.GetTeamOverViewViewModel(userTeamId, gameId, "", "");
            var _teamOverView2 = _service.GetNextTeamOverViewViewModel(userTeamId, gameId);

            var bogusController = ViewRenderer.CreateController<HomeController>();

            var partialViewPlayerList = ViewRenderer.RenderPartialView("~/Views/ViewPlayers/playerlist.cshtml", _teamOverView, bogusController.ControllerContext);
            var searchForm = ViewRenderer.RenderPartialView("~/Views/ViewPlayers/searchform.cshtml", _teamOverView, bogusController.ControllerContext);

            // update this client's view - remove the form
            Clients.Caller.addNewPlayerListToPage(partialViewPlayerList, gameId);
            Clients.Caller.addNewSearchFormToPage(searchForm, gameId);

            var partialViewPlayerList2 = ViewRenderer.RenderPartialView("~/Views/ViewPlayers/playerlist.cshtml", _teamOverView2, bogusController.ControllerContext);
            var searchForm2 = ViewRenderer.RenderPartialView("~/Views/ViewPlayers/searchform.cshtml", _teamOverView2, bogusController.ControllerContext);

            // only send the new content to the NEXT userteam player, not all the others (it's only the next userteam player who should see the addplayerform)
            // hopefully username matches Context.User.Identity.Name;
            foreach (var connectionId in _connections.GetConnections(_teamOverView2.UserName))
            {
                Clients.Client(connectionId).addNewPlayerListToPage(partialViewPlayerList2, gameId); 
                Clients.Client(connectionId).addNewSearchFormToPage(searchForm2, gameId);
            }
            //Clients.Others.addNewPlayerListToPage(partialViewPlayerList2); 
           // Clients.Others.addNewSearchFormToPage(searchForm2);
            }
            catch (Exception ex)
            {

                Clients.All.addNewBanterToPage("There has been a problem adding the player");
            }

        }

        public override Task OnConnected()
        {
            string name = Context.User.Identity.Name;

            _connections.Add(name, Context.ConnectionId);

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string name = Context.User.Identity.Name;

            _connections.Remove(name, Context.ConnectionId);

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            string name = Context.User.Identity.Name;

            if (!_connections.GetConnections(name).Contains(Context.ConnectionId))
            {
                _connections.Add(name, Context.ConnectionId);
            }

            return base.OnReconnected();
        }
    }
}