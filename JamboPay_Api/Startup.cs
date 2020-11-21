using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(JamboPay_Api.Startup))]

namespace JamboPay_Api
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
