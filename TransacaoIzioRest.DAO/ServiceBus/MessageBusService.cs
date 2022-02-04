using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using System;
using System.Threading.Tasks;

namespace TransacaoIzioRest.DAO.ServiceBus
{
    public static class MessageBusService
    {
        public static async Task<QueueClient> InitializeMessageBusService(string tokenAutenticacao, string nomeCliente, string queueName)
        {
            //Consulta a string de conexão para acessar o servive Bus
            string connectionString = ConnectionStringServiceBus.getConnectionString(nomeCliente, tokenAutenticacao);

            //Cria o componente de conexão com o service Bus
            var client = new ManagementClient(connectionString);

            //Verifica se já existe a fila criada no servic Bus , senão tiver, cria ela
            if (!await client.QueueExistsAsync(queueName).ConfigureAwait(false))
            {
                await client.CreateQueueAsync(new QueueDescription(queueName)
                {
                    MaxDeliveryCount = int.MaxValue,
                    LockDuration = TimeSpan.FromMinutes(5),
                    MaxSizeInMB = 5 * 1024,
                    EnableBatchedOperations = true
                    
                }).ConfigureAwait(false);
            }

            return new QueueClient(connectionString, queueName, ReceiveMode.PeekLock);
        }
    }
}
