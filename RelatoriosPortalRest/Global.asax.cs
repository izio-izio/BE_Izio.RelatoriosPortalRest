using NSwag;
using NSwag.AspNet.Owin;
using NSwag.SwaggerGeneration.Processors.Security;
using RelatoriosPortalRest;
using System;
using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace RelatoriosPortalRest
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RouteTable.Routes.MapOwinPath("swagger", app =>
            {
                app.UseSwaggerUi3(typeof(WebApiApplication).Assembly, s =>
                {
                    s.MiddlewareBasePath = "/swagger";
                    s.GeneratorSettings.DocumentProcessors.Add(new SecurityDefinitionAppender("tokenAutenticacao", new SwaggerSecurityScheme
                    {
                        Type = SwaggerSecuritySchemeType.ApiKey,
                        Name = "tokenAutenticacao",
                        In = SwaggerSecurityApiKeyLocation.Header,
                        Description = "tokenAutenticacao"
                    }));
                    s.GeneratorSettings.OperationProcessors.Add(new OperationSecurityScopeProcessor("tokenAutenticacao"));
                    s.PostProcess = document =>
                    {
                        document.Info.Title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                        document.Info.Version = ConfigurationManager.AppSettings.Get("apiVersion") ?? "1.0.0";

                    };
                });
            });

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
