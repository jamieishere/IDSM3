using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(IDSM.Signlr.Startup))]
namespace IDSM.Signlr
{
    public static class Startup
    {
       // public static void ConfigureSignalR(IAppBuilder app)
        public static void Configuration(IAppBuilder app)
        {
            // For more information on how to configure your application using OWIN startup, visit http://go.microsoft.com/fwlink/?LinkID=316888

             app.MapSignalR();
        }
    }
}