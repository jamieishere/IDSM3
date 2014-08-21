using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IDSM.Controllers;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using IDSM.Model;
using IDSM.Signlr;
using IDSM.ServiceLayer;

namespace IDSM.Signlr
{
    [HubName("banter")]
    public class BanterHub : Hub
    {
        IService _service;

        public BanterHub()
        {
            _service =  DependencyResolver.Current.GetService<IService>(); 
        }


        public void Send(string message)
        {
            // Call the addNewMessageToPage method to update clients.
            // this uses a jquery method on the page to append to a div
           // Clients.All.addNewBanterToPage(message);

            _service.AddBanter(1, 1, message);
            var _updatedBanter = _service.GetGameBanter(1);

            // this method i create a dummy controller that can return a partialview to the page - thus i can actually get ALL the banter and refresh the entire view instead of appending bits...
            //http://stackoverflow.com/questions/22098233/partialview-to-html-string
            var bogusController = ViewRenderer.CreateController<HomeController>();
            try
            {
                // var partialViewGuts = ViewRenderer.RenderPartialViewToString(bogusController.ControllerContext, "view", model);
                var partialViewGuts = ViewRenderer.RenderPartialView("~/Views/ViewPlayers/banterlist.cshtml", _updatedBanter, bogusController.ControllerContext);
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



        //public void Send(string message)
        //{
        //    // Call the addNewMessageToPage method to update clients.
        //    // this uses a jquery method on the page to append to a div
        //    // Clients.All.addNewBanterToPage(message);

        //    _service.AddBanter(1, 1, message);
        //    var _updatedBanter = _service.GetGameBanter(1);

        //    // this method i create a dummy controller that can return a partialview to the page - thus i can actually get ALL the banter and refresh the entire view instead of appending bits...
        //    //http://stackoverflow.com/questions/22098233/partialview-to-html-string
        //    var bogusController = ViewRenderer.CreateController<HomeController>();
        //    try
        //    {
        //        // var partialViewGuts = ViewRenderer.RenderPartialViewToString(bogusController.ControllerContext, "view", model);
        //        var partialViewGuts = ViewRenderer.RenderPartialView("~/Views/ViewPlayers/banterlist.cshtml", _updatedBanter, bogusController.ControllerContext);
        //        Clients.All.addNewBanterToPage(partialViewGuts);
        //    }
        //    catch (Exception ex)
        //    {

        //        Clients.All.addNewBanterToPage("There has been a problem getting the banter");
        //    }

        //}
    }
}