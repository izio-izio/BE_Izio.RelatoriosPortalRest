using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransacaoIzioRest.Models;

namespace TransacaoIzioRest.DAO.ServiceBus
{
    public static class EnviarMensagemFila
    {
        public static void InserirLoteFila(string NomeClienteWs, string tokenAutenticacao, List<DadosTransacaoLote> objTransacao, string ipOrigem)
        {
            
            try
            {
                //Monta configuração para serialização dos dados do objeto na fila - ServiceBus
                var settings = new JsonSerializerSettings() { ContractResolver = new SubstituteNullWithEmptyStringContractResolver() };

                string messageBody;
                //string connectionString = ConnectionStringServiceBus.getConnectionString(NomeClienteWs, tokenAutenticacao);
                //var queueClient = MessageBusService.InitializeMessageBusService(connectionString, NomeClienteWs).Result;
                
                //Cria a fila (caso não existah) e o objeto para enviar o lote de compra(s) para a fila
                var queueClient = MessageBusService.InitializeMessageBusService(tokenAutenticacao, NomeClienteWs, $"transacao-{NomeClienteWs.ToLower()}").Result;

                //Lista padrão para envio da mensagem (lote de compra(s)) para a fila
                List<Message> listMessage = new List<Message>();

                //Monta mensagem - Serializa o lote de compr(a)s com aplicando as configurações de serialização para o serviceBus
                messageBody = JsonConvert.SerializeObject(objTransacao, settings);

                //Insere a mensagem em bytes na lista para a fila serviceBus
                listMessage.Add(new Message(Encoding.UTF8.GetBytes(messageBody)));

                //Define timeout de 30 segundos de upload da mensagem na fila
                queueClient.OperationTimeout = TimeSpan.FromSeconds(30);

                //Insere a mensagem na fila serviceBus - Lista de compra(s)
                queueClient.SendAsync(listMessage).GetAwaiter().GetResult();

                //await queueClient.CloseAsync();
                //Fecha a conexão com a fila
                queueClient.CloseAsync();
            }
            catch (Exception ex)
            {
                Log.InserirLogIzio(NomeClienteWs, new DadosLog { des_erro_tecnico = "TransacaoIzioRest - Erro ao enviar mensagem a fila: " + ex.Message }, System.Reflection.MethodBase.GetCurrentMethod());
                throw;
            }

        }
    }
}
