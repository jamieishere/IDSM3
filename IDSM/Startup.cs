using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;

//[assembly: OwinStartup(typeof(IDSM.SignalRChat.Startup))]
//namespace IDSM.SignalRChat
[assembly: OwinStartup(typeof(IDSM.Signlr.Startup))]

namespace IDSM.Signlr
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Any connection or hub wire up and configuration should go here
            //app.MapSignalR();
            IDSM.Signlr.Startup.ConfigureSignalR(app);
            //Microsoft.AspNet.SignalR.StockTicker.Startup.ConfigureSignalR(app);
        }
    }
}