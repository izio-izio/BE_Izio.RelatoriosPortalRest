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
        public static void InserirLoteFila(string NomeClienteWs, string tokenAutenticacao, List<DadosTransacaoLote> listaCompras, string ipOrigem)
        {
            List<DadosTransacaoLote> listaFila = new List<DadosTransacaoLote>();
            try
            {
                //Monta configuração para serialização dos dados do objeto na fila - ServiceBus
                var settings = new JsonSerializerSettings() { ContractResolver = new SubstituteNullWithEmptyStringContractResolver() };

                string messageBody;
                //string connectionString = ConnectionStringServiceBus.getConnectionString(NomeClienteWs, tokenAutenticacao);
                //var queueClient = MessageBusService.InitializeMessageBusService(connectionString, NomeClienteWs).Result;
                
                //Cria a fila (caso não existah) e o objeto para enviar o lote de compra(s) para a fila
                var queueClient = MessageBusService.InitializeMessageBusService(tokenAutenticacao, NomeClienteWs, $"transacao-{NomeClienteWs.ToLower()}").Result;

                //Define timeout de 30 segundos de upload da mensagem na fila
                queueClient.OperationTimeout = TimeSpan.FromSeconds(30);

                //Lista padrão para envio da mensagem (lote de compra(s)) para a fila
                List<Message> listMessage = new List<Message>();

                while (listaCompras.Count() > 0)
                {
                    //Se o lote de compras tiver mais de 200 regitros, ele é dividido e inserido na fila por lote
                    listaFila = listaCompras.Take(200).ToList();

                    //Monta mensagem - Serializa o lote de compr(a)s com aplicando as configurações de serialização para o serviceBus
                    messageBody = JsonConvert.SerializeObject(listaFila, settings);

                    //Insere a mensagem em bytes na lista para a fila serviceBus
                    listMessage.Add(new Message(Encoding.UTF8.GetBytes(messageBody)));

                    //Insere a mensagem na fila serviceBus - Lista de compra(s)
                    queueClient.SendAsync(listMessage).GetAwaiter().GetResult();

                    //Remove da fila de processamento, os registros inseridos na fila
                    if (listaFila.Count > listaCompras.Count) listaCompras.RemoveRange(0, listaFila.Count);
                    else listaCompras.RemoveRange(0, listaFila.Count);

                    //Limpa a fila de mensagens
                    listMessage.Clear();
                }
                //await queueClient.CloseAsync();
                //Fecha a conexão com a fila
                queueClient.CloseAsync();
            }
            catch (Exception ex)
            {
                Log.InserirLogIzio(NomeClienteWs, new DadosLog { des_erro_tecnico = "TransacaoIzioRest - Erro ao enviar mensagem para fila: " + ex.Message }, System.Reflection.MethodBase.GetCurrentMethod());
                throw;
            }

        }
    }
}
