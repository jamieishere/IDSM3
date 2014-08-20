using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using IDSM.Model;

namespace IDSM.Signlr
{
    [HubName("banter")]
    public class BanterHub : Hub
    {
        public void Send(string message)
        {
            // Call the addNewMessageToPage method to update clients.
            Clients.All.addNewBanterToPage(message);
        }
    }
}