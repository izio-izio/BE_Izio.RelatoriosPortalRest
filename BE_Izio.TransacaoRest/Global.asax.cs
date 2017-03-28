using System.Web.Http;
using System.Web.Routing;
using NSwag.AspNet.Owin;

namespace TransacaoRest
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RouteTable.Routes.MapOwinPath("swagger", app =>
            {
                app.UseSwaggerUi(typeof(WebApiApplication).Assembly, new SwaggerUiOwinSettings
                {
                    MiddlewareBasePath = "/swagger"
                });
            });

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
