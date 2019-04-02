using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AdventureLog.Startup))]
namespace AdventureLog
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
