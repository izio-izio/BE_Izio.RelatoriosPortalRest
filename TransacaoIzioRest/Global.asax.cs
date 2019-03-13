using NSwag;
using NSwag.AspNet.Owin;
using NSwag.SwaggerGeneration.Processors.Security;
using System.Web;
using System;
using System.Web.Http;
using System.Web.Routing;

namespace TransacaoIzioRest
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
                        document.Info.Title = HttpContext.Current.ApplicationInstance.GetType().BaseType.Assembly.GetName().Name.ToString();
                    };
                });
            });

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
        }
    }
}
