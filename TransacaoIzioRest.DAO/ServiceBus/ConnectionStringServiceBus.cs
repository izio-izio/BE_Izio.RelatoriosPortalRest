using Izio.Biblioteca.DAO;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace TransacaoIzioRest.DAO.ServiceBus
{
    public static class ConnectionStringServiceBus
    {
        public static string getConnectionString(string sNomeCliente, string tokenAutenticacao)
        {
            //Cache dos parametros - Para evitar a ida no banco a cada callback recebido
            Dictionary<string, string> listParam = new Dictionary<string, string>();
            listParam = GetFromCache<Dictionary<string, string>>($"{sNomeCliente}_sb_parametroDAO", 6, () =>
            {
                var args = new Dictionary<string, string>();
                ParametroDAO param = new ParametroDAO(sNomeCliente, tokenAutenticacao);
                args = param.ListarParametros("AzureBusConStr");
                return args;
            });

            return listParam.Count() == 0 ?
                "Endpoint=sb://izioservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Dpnh2JPn9tgXGqRK9afC99bQI5qEDQfS3u55sU6F/oM=" :
                listParam["AzureBusConStr"];
        }

        public static TEntity GetFromCache<TEntity>(string key, int horaCache, Func<TEntity> valueFactory) where TEntity : class
        {
            ObjectCache cache = MemoryCache.Default;
            // the lazy class provides lazy initializtion which will eavaluate the valueFactory expression only if the item does not exist in cache
            var newValue = new Lazy<TEntity>(valueFactory);
            CacheItemPolicy policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(horaCache) };
            //The line below returns existing item or adds the new value if it doesn't exist
            var value = cache.AddOrGetExisting(key, newValue, policy) as Lazy<TEntity>;
            return (value ?? newValue).Value; // Lazy<T> handles the locking itself
        }
    }
}
