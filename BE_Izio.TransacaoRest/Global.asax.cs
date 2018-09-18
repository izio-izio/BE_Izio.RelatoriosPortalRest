using System;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using NSwag.AspNet.Owin;

namespace TransacaoIzioRest
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

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            if (HttpContext.Current.Request.HttpMethod == "OPTIONS")
            {
                HttpContext.Current.Response.AddHeader("Cache-Control", "no-cache");
                HttpContext.Current.Response.AddHeader("Access-Control-Allow-Methods", "*");
                HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, tokenAutenticacao");
                HttpContext.Current.Response.AddHeader("Access-Control-Max-Age", "1728000");
                HttpContext.Current.Response.End();
            }

            //if (HttpContext.Current.Request.HttpMethod == "OPTIONS")
            //{
            //    HttpContext.Current.Response.AddHeader("Cache-Control", "no-cache");
            //    HttpContext.Current.Response.AddHeader("Access-Control-Allow-Methods", "*");
            //    HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "*");
            //    HttpContext.Current.Response.AddHeader("Access-Control-Max-Age", "1728000");
            //    HttpContext.Current.Response.End();

            //    HttpContext.Current.Response.Headers.Remove("Access-Control-Allow-Origin");
            //    HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", "*");
            //}

            
        }
    }
}
