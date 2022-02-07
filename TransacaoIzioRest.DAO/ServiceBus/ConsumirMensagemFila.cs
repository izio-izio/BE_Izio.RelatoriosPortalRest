using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TransacaoIzioRest.Models;

namespace TransacaoIzioRest.DAO.ServiceBus
{
    public class ConsumirMensagemFila
    {
        string stringConexao;
        static string _sNomeCliente;
        static QueueClient queueClient;
        public static List<DadosTransacaoLote> listaNota { get; set; }

        static IMessageReceiver messageReceiver;

        public ConsumirMensagemFila(string sNomeCliente, string tokenAutenticacao)
        {
            _sNomeCliente = sNomeCliente;

            string connectionString = ConnectionStringServiceBus.getConnectionString(_sNomeCliente, tokenAutenticacao);
            stringConexao = connectionString;
            queueClient = new QueueClient(connectionString, _sNomeCliente, ReceiveMode.PeekLock);

            listaNota = new List<DadosTransacaoLote>();
        }

        public static async Task<int> ReceberAsync(string sNomeCliente, string tokenAutenticacao, ImportaTransacaoDAO dao)
        {
            var listaLocal = new List<DadosTransacaoLote>();
            List<string> listaExcluirFila = new List<string>();
            int i = 0;
            try
            {
                //Pega a string de conexao
                string strConexao = ConnectionStringServiceBus.getConnectionString(sNomeCliente, tokenAutenticacao);

                //Cria componente para leitura de registros da fila
                messageReceiver = new MessageReceiver(strConexao, $"transacao-{sNomeCliente.ToLower()}", ReceiveMode.PeekLock);
                Boolean temMsg = true;

                DateTime datInicio = DateTime.Now;

                //Enquanto existir mensagem na fila fica no processamento
                while (temMsg)
                {
                    // Receive the message - Pega 10 mensagens da fila
                    //Message message = await messageReceiver.ReceiveAsync();
                    var messages = await messageReceiver.ReceiveAsync(10,TimeSpan.FromMinutes(3));

                    if (messages != null)
                    {
                        foreach (Message message in messages)
                        {
                            //Converte a mensagem de Byte para texto (volta o json)
                            var body = Encoding.UTF8.GetString(message.Body);

                            //var body = Encoding.ASCII.GetString(message.Body);
                            List<DadosTransacaoLote> nota;

                            try
                            {
                                //Serializa a mensagem na classse de notas Izio - IzCoder
                                if (body.StartsWith("{") && body.EndsWith("}"))
                                {
                                    var t = JsonConvert.DeserializeObject<List<DadosTransacaoLote>>(body);
                                    nota = t;
                                }
                                else
                                {
                                    nota = JsonConvert.DeserializeObject<List<DadosTransacaoLote>>(body);
                                }

                                //Adiciona as mensagens na lista de processamento, está lista será inserida na viewizio_3
                                listaLocal.AddRange(nota);
                                listaExcluirFila.Add(message.SystemProperties.LockToken);
                            }
                            catch (Exception ex)
                            {
                                DadosLog dadosLog = new DadosLog();
                                dadosLog.des_erro_tecnico = $"ServiceBus - ImportaTransacaoDAO - Erro: {ex.Message} - fila: {body}";
                                Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
                            }

                            //Apaga uma mensagem da fila
                            //await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
                            i++;

                            //Se atingiu 1000 registros lidos da fila, finaliza o processamento
                            //temMsg = i >= 1000 ? false : true;

                            //Verifica se a data e hora da mensagem é menor que a data do inicio do processamento.
                            //O processamento só ocorre para mensagens recebidas antes da data e hora do inicio do processamento
                            if (temMsg)
                                temMsg = TimeZoneInfo.ConvertTime(message.SystemProperties.EnqueuedTimeUtc, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")) < datInicio ? true : false;

                            if (DateTime.Now > datInicio.AddMinutes(15))
                                temMsg = false;
                        }
                    }
                    else
                    {
                        temMsg = false;
                    }

                    //Verifica se não existe mais mensagem na fila e a lista local está preenchida
                    //ou
                    //Verifica se a lista local tem 100 registros
                    //Sendo afirmativo um dos dois casos, persiste no banco de dados os callbacks de retorno do sefaz
                    //e
                    //apaga da fila as notas persistidas
                    if ((!temMsg && listaLocal.Count() > 0) || listaLocal.Count() >= 1000)
                    {
                        //Persiste no banco de dados as notas
                        dao.ImportaLoteTransacaoFila(listaLocal);
                        listaLocal.Clear();

                        //Exclui da fila as notas persistidas
                        await messageReceiver.CompleteAsync(listaExcluirFila);
                        listaExcluirFila.Clear();
                    }

                }

                await messageReceiver.CloseAsync();
            }
            catch (Exception ex)
            {

                throw;
            }

            // return listaLocal;

            Thread.Sleep(1000 * 10);
            return i;
        }

        static async Task ReceiveMessagesAsync(int numberOfMessagesToReceive)
        {
            Boolean temMsg = true;
            while (temMsg)
            {
                // Receive the message
                Message message = await messageReceiver.ReceiveAsync();

                if (message == null)
                { temMsg = false; }
                else
                {
                    var body = Encoding.ASCII.GetString(message.Body);
                    DadosTransacaoLote nota;
                    if (body.StartsWith("{") && body.EndsWith("}"))
                    {
                        var t = JsonConvert.DeserializeObject<DadosTransacaoLote>(body);
                        nota = t;
                    }
                    else
                    {
                        nota = JsonConvert.DeserializeObject<DadosTransacaoLote>(body);
                    }

                    listaNota.Add(nota);

                    // Process the message
                    // Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}");

                    // Complete the message so that it is not received again.
                    // This can be done only if the MessageReceiver is created in ReceiveMode.PeekLock mode (which is default).
                    await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
                }
            }
        }

        public async Task<List<DadosTransacaoLote>> ProcessMessageHandler()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 10,
                AutoComplete = false
            };

            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);

            return listaNota;
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            var body = Encoding.ASCII.GetString(message.Body);
            DadosTransacaoLote nota;

            if (body.StartsWith("{") && body.EndsWith("}"))
            {
                var t = JsonConvert.DeserializeObject<DadosTransacaoLote>(body);
                nota = t;
            }
            else
            {
                nota = JsonConvert.DeserializeObject<DadosTransacaoLote>(body);
            }

            //ImportaTransacaoDAO.ProcessarLoteTransacaoFromServiceBus(transacao, _sNomeCliente);

            listaNota.Add(nota);

            await queueClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            DadosLog dadosLog = new DadosLog();
            dadosLog.des_erro_tecnico = exceptionReceivedEventArgs.Exception.ToString();

            Log.InserirLogIzio(_sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

            return Task.CompletedTask;
        }

        public static async Task<int> ConsumirFilaNotasSefazAsync(string nomeCliente, string tokenAutenticacao, int totalNotas)
        {
            int i = 0;
            try
            {
                ImportaTransacaoDAO dao = new ImportaTransacaoDAO(nomeCliente, tokenAutenticacao);

                //Limpa tabela temporaria para receber os novos retornos de callback
                //dao.LimparTabelaCallback();

                //Cria task para execução sincrona e executa o metodo para consumir a fila de callbacks recebidos
                var result = Task.Run(async () => await ConsumirMensagemFila.ReceberAsync(nomeCliente, tokenAutenticacao,dao)).ConfigureAwait(false);

                //Pega o numero de mensagem processadas da fila
                i = result.GetAwaiter().GetResult();

                //Executa o consumo da fila e armazena a quantidade de notas processadas
                //string retProc = dao.ProcessarIzCoderNotasSefaz();

                //Limpa a tabela temporaria apos processamento
                //dao.LimparTabelaCallback();

                ////Se o processamento ocorreu com erro (retProc <> "") , gera um exception com a mensagem de erro retornado do metodo
                //if (!string.IsNullOrEmpty(retProc))
                //{
                //    throw new Exception(retProc);
                //}
            }
            catch (Exception ex)
            {

                throw;
            }

            return i;
        }
    }
}
