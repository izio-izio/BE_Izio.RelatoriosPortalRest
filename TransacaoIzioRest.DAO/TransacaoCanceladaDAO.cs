using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using EmailRest.Models;
using Izio.Biblioteca;
using Izio.Biblioteca.DAO;
using Izio.Biblioteca.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Web;
using TransacaoIzioRest.Models;

namespace TransacaoIzioRest.DAO
{
    /// <summary>
    /// Classe para remover os registros de uma venda cancelada
    ///  - Remove o registra da venda na Viewizio_3
    ///  - Remove o registra da venda na tab_transacao/tab_transacao_itens
    ///  - Remove o registra da venda na tab_transacao_cpf/tab_transacao_itens_cpf
    /// </summary>
    public class TransacaoCanceladaDAO
    {
        #region Constantes Remover Venda Cancelada

        private string DadosNaoEncontrados = "Não foram encontrados registros";

        #endregion

        SqlServer sqlServer;
        string NomeClienteWs;
        string tokenAutenticacao;
        public TransacaoCanceladaDAO(string sNomeCliente, string _tokenAutenticacao)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
            tokenAutenticacao = _tokenAutenticacao;
        }

        /// <summary>
        /// Apaga os registros da venda cancela no Izio:
        ///  - Remove o registra da venda na Viewizio_3
        ///  - Remove o registra da venda na tab_transacao/tab_transacao_itens
        ///  - Remove o registra da venda na tab_transacao_cpf/tab_transacao_itens_cpf
        /// </summary>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <returns></returns>
        #region Apaga os registros da venda cancelada na base do Izio
        public string ExcluirRegistrosCompraCancelada(DadosTransacaoCancelada objTransacao, string IpOrigem, ApiErrors retorno)
        {
            //Total de registros deletados
            Int32 totalRegistrosExcluidos = 0;
            var mensagemRetorno = "";

            try
            {
                //Envia para a fila o evento de cancelamento(exclusão) de compra
                #region Envia para a fila o evento de cancelamento(exclusão) de compra
                if (!InserirFilaTransacaoCancelada(objTransacao))
                {
                    enviarEmail("Erro no processamento cancelamento de compra: </br> </br> " + JsonConvert.SerializeObject(objTransacao), $"{NomeClienteWs} - Erro ao inserir cancelamento de compra na fila");
                }
                #endregion

                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();

                //Executa a execução dos registros da venda cancelada
                #region Executa a execução dos registros da venda cancelada

                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                //Monta os parametros
                #region Parametros
                //Data da compra
                sqlServer.Command.Parameters.AddWithValue("@datacompra", objTransacao.dat_compra);
                sqlServer.Command.Parameters.AddWithValue("@valorcompra", objTransacao.vlr_compra);
                sqlServer.Command.Parameters.AddWithValue("@cupom", objTransacao.cupom);
                sqlServer.Command.Parameters.AddWithValue("@cod_loja", objTransacao.cod_loja);

                // **********************************************************************************
                // **********************************************************************************
                #endregion

                //Exclui os registros da compra ainda não processados na viewizio_3
                sqlServer.Command.CommandText = @"delete 
                                                  from 
                                                     viewizio_3 
                                                  where
                                                     datacompra = @datacompra and
                                                     valorcompra = @valorcompra and
                                                     cupom = @cupom and
                                                     cod_loja = @cod_loja ";

                //executa o delete e retorna o total de linhas afetatas
                totalRegistrosExcluidos += sqlServer.Command.ExecuteNonQuery();

                //Se não excluiu nenhum registros passa para as tabelas finais (tab_transacao/tab_transacao_itens e tab_transacao_cpf/tab_transacao_itens_cpf)
                if (totalRegistrosExcluidos == 0)
                {

                    //Exclui os itens da compra da tabela de itens de compra identificada
                    sqlServer.Command.CommandText = @"
declare @cod_transacao bigint
declare @total int = 0
select @cod_transacao = cod_transacao from tab_transacao with(nolock) where dat_compra = @datacompra and vlr_compra = @valorcompra and cupom = @cupom and cod_loja = @cod_loja

set @cod_transacao =  coalesce(@cod_transacao,0)

if (@cod_transacao > 0)
begin
    
    delete tri
    from 
       tab_transacao_itens tri with(nolock)
    where
       tri.cod_transacao = @cod_transacao
    set @total = @total  + @@rowcount

    delete tri
    from 
       tab_transacao tri with(nolock)
    where
       tri.cod_transacao = @cod_transacao
    set @total = @total  + @@rowcount
end

select @total
";

                    //executa o delete e retorna o total de linhas afetatas
                    var result = sqlServer.Command.ExecuteScalar();
                    if (result != null)
                        totalRegistrosExcluidos += Convert.ToInt32(result);
                }

                if (totalRegistrosExcluidos == 0)
                {
                    //Exclui os registros da compra não identificada
                    sqlServer.Command.CommandText = @"
declare @cod_Transacao bigint
declare @total int = 0
select @cod_Transacao = cod_tab_transacao_cpf from tab_transacao_cpf with(nolock) where dat_compra = @datacompra and vlr_compra = @valorcompra and cupom = @cupom and cod_loja = @cod_loja

set @cod_Transacao =  coalesce(@cod_Transacao,0)

if (@cod_transacao > 0)
begin
    
    delete tri
    from 
       tab_transacao_itens_cpf tri with(nolock)
    where
       tri.cod_tab_transacao_cpf = @cod_transacao
    set @total = @total  + @@rowcount
    delete tri
    from 
       tab_transacao_cpf tri with(nolock)
    where
       tri.cod_tab_transacao_cpf = @cod_transacao
    set @total = @total  + @@rowcount
end

select @total ";

                    //executa o delete e retorna o total de linhas afetatas
                    var result = sqlServer.Command.ExecuteScalar();
                    if (result != null)
                        totalRegistrosExcluidos += Convert.ToInt32(result);

                }
                #endregion

                sqlServer.Commit();

                retorno.errors = new List<Erros>();

                //Se total de linhas afetadas for igual a zero, indica que não foi excluido nenhum registros
                if (totalRegistrosExcluidos == 0)
                {
                    //Seta a lista de erros com o erro
                    retorno.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.NotFound).ToString(), message = DadosNaoEncontrados + ", para exclusão da venda Cancelada." });
                }
            }
            catch (Exception ex)
            {
                sqlServer.Rollback();
                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();
                }
            }

            return mensagemRetorno;

        }

        private Boolean InserirFilaTransacaoCancelada(DadosTransacaoCancelada objTransacao)
        {
            string sEtapa = "";
            List<DadosTransacaoCancelada> listaCompras = new List<DadosTransacaoCancelada>();
            List<DadosTransacaoCancelada> listaFila = new List<DadosTransacaoCancelada>();

            try
            {

                //Seta o protocolo de ssl de envio da mensagem para a fila
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //A seta a lista que será inserida na fila
                listaCompras.Add(objTransacao);

                //Coloca em cache os dados de utilizacao da fila
                sEtapa = "Coloca em cache os dados de utilizacao da fila";
                Dictionary<string, string> listParam = new Dictionary<string, string>();
                listParam = consultaParametroFila();
                string connectionString = listParam.ContainsKey("AzureBusConStr") ? listParam["AzureBusConStr"] : "Endpoint=sb://izioservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Dpnh2JPn9tgXGqRK9afC99bQI5qEDQfS3u55sU6F/oM=";

                //Cria componente de verificação se a fila já está criada
                sEtapa = "Cria componente de verificação se a fila já está criada";
                ServiceBusAdministrationClient queue = new ServiceBusAdministrationClient(connectionString);
                var existQueue = queue.QueueExistsAsync($"cancelamento-transacao-{NomeClienteWs.ToLower()}").GetAwaiter().GetResult();

                //Se a fila não existir
                sEtapa = "Se a fila não existir";
                if (!existQueue)
                {
                    //Cria a nova fila
                    sEtapa = "Cria a nova fila";
                    var options = new CreateQueueOptions($"cancelamento-transacao-{NomeClienteWs.ToLower()}");
                    options.MaxDeliveryCount = int.MaxValue;
                    options.LockDuration = TimeSpan.FromMinutes(5);
                    options.MaxSizeInMegabytes = 5 * 1024;
                    options.EnableBatchedOperations = true;
                    queue.CreateQueueAsync(options).GetAwaiter().GetResult();
                }

                //Inicia o componente para conexao com a fila
                sEtapa = "Inicia o componente para conexao com a fila";
                ServiceBusClient _client = new ServiceBusClient(connectionString);

                //Cria o componente para envio da mensagem para fila
                sEtapa = "Cria o componente para envio da mensagem para fila";
                ServiceBusSender _clientSender = _client.CreateSender($"cancelamento-transacao-{NomeClienteWs.ToLower()}");

                while (listaCompras.Count() > 0)
                {
                    //Se o lote de compras tiver mais de 200 regitros, ele é dividido e inserido na fila por lote
                    listaFila = listaCompras.Take(200).ToList();

                    //Converte em json o objeto postado na api
                    sEtapa = "Converte em json o objeto postado na api";
                    string resultado = JsonConvert.SerializeObject(listaFila, Formatting.None);

                    //Cria uma nova mensagem deixando json em bytes
                    sEtapa = "Cria uma nova mensagem deixanto json em bytes";
                    ServiceBusMessage message = new ServiceBusMessage(Encoding.UTF8.GetBytes(resultado));

                    //Envia a mensagem para a fila
                    sEtapa = "Envia a mensagem para a fila";
                    _clientSender.SendMessageAsync(message).GetAwaiter().GetResult();

                    //Remove da fila de processamento, os registros inseridos na fila
                    if (listaFila.Count > listaCompras.Count) listaCompras.RemoveRange(0, listaFila.Count);
                    else listaCompras.RemoveRange(0, listaFila.Count);
                }

                //Fecha os componentes de conexão com a fila
                _clientSender.CloseAsync();
                _client.DisposeAsync();

                return true;
            }
            catch (Exception ex)
            {
                DadosLog dadosLog = new DadosLog { des_erro_tecnico = $"Cancelamento Compra - {sEtapa} - Erro ao enviar mensagem para fila. {ex.Message.ToString()}" };
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                return false;
            }
        }

        public string ExcluirRegistroFilaCompraCancelada(DadosTransacaoCancelada objTransacao, ApiErrors retorno)
        {
            //Total de registros deletados
            int totalRegistrosExcluidos = 0;
            var mensagemRetorno = "";

            try
            {
                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();

                //Executa a execução dos registros da venda cancelada
                #region Executa a execução dos registros da venda cancelada

                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                //Monta os parametros
                #region Parametros
                //Data da compra
                IDbDataParameter pdat_compra = sqlServer.Command.CreateParameter();
                pdat_compra.ParameterName = "@datacompra";
                pdat_compra.Value = objTransacao.dat_compra;
                sqlServer.Command.Parameters.Add(pdat_compra);

                IDbDataParameter pvalorcompra = sqlServer.Command.CreateParameter();
                pvalorcompra.ParameterName = "@valorcompra";
                pvalorcompra.Value = objTransacao.vlr_compra;
                sqlServer.Command.Parameters.Add(pvalorcompra);

                IDbDataParameter pcupom = sqlServer.Command.CreateParameter();
                pcupom.ParameterName = "@cupom";
                pcupom.Value = objTransacao.cupom;
                sqlServer.Command.Parameters.Add(pcupom);

                IDbDataParameter pcod_loja = sqlServer.Command.CreateParameter();
                pcod_loja.ParameterName = "@cod_loja";
                pcod_loja.Value = objTransacao.cod_loja;
                sqlServer.Command.Parameters.Add(pcod_loja);

                // **********************************************************************************
                // **********************************************************************************
                #endregion

                //Exclui os registros da compra ainda não processados na viewizio_3
                sqlServer.Command.CommandText = @"delete 
                                                  from 
                                                     viewizio_3
                                                  where
                                                     datacompra = @datacompra and
                                                     valorcompra = @valorcompra and
                                                     cupom = @cupom and
                                                     cod_loja = @cod_loja ";

                //executa o delete e retorna o total de linhas afetatas
                totalRegistrosExcluidos += sqlServer.Command.ExecuteNonQuery();

                //Se não excluiu nenhum registros passa para as tabelas finais (tab_transacao/tab_transacao_itens e tab_transacao_cpf/tab_transacao_itens_cpf)
                if (totalRegistrosExcluidos == 0)
                {

                    //Exclui os itens da compra da tabela de itens de compra identificada
                    sqlServer.Command.CommandText = @"
declare @cod_transacao bigint
declare @total int = 0
select @cod_transacao = cod_transacao from tab_transacao with(nolock) where dat_compra = @datacompra and vlr_compra = @valorcompra and cupom = @cupom and cod_loja = @cod_loja

set @cod_transacao =  coalesce(@cod_transacao,0)

if (@cod_transacao > 0)
begin
    
    delete tri
    from 
       tab_transacao_itens tri with(nolock)
    where
       tri.cod_transacao = @cod_transacao
    set @total = @total  + @@rowcount

    delete tri
    from 
       tab_transacao tri with(nolock)
    where
       tri.cod_transacao = @cod_transacao
    set @total = @total  + @@rowcount
end

select @total
";

                    //executa o delete e retorna o total de linhas afetatas
                    var result = sqlServer.Command.ExecuteScalar();
                    if (result != null)
                        totalRegistrosExcluidos += Convert.ToInt32(result);
                }
                
                if(totalRegistrosExcluidos == 0)
                {
                    //Exclui os registros da compra não identificada
                    sqlServer.Command.CommandText = @"
declare @cod_Transacao bigint
declare @total int = 0
select @cod_Transacao = cod_tab_transacao_cpf from tab_transacao_cpf with(nolock) where dat_compra = @datacompra and vlr_compra = @valorcompra and cupom = @cupom and cod_loja = @cod_loja

set @cod_Transacao =  coalesce(@cod_Transacao,0)

if (@cod_transacao > 0)
begin
    
    delete tri
    from 
       tab_transacao_itens_cpf tri with(nolock)
    where
       tri.cod_tab_transacao_cpf = @cod_transacao
    set @total = @total  + @@rowcount
    delete tri
    from 
       tab_transacao_cpf tri with(nolock)
    where
       tri.cod_tab_transacao_cpf = @cod_transacao
    set @total = @total  + @@rowcount
end

select @total ";

                    //executa o delete e retorna o total de linhas afetatas
                    //totalRegistrosExcluidos += sqlServer.Command.ExecuteNonQuery();

                    var result = sqlServer.Command.ExecuteScalar();
                    if (result != null)
                        totalRegistrosExcluidos += Convert.ToInt32(result);

                }

                //if (totalRegistrosExcluidos > 0)
                //{
                //    if (!ExcluiCreditoCashback(sqlServer))
                //    {
                //        mensagemRetorno = " Não foi possível remover crétidos concedidos";
                //    }
                //}

                #endregion

                sqlServer.Commit();

                retorno.errors = new List<Erros>();

                //Se total de linhas afetadas for igual a zero, indica que não foi excluido nenhum registros
                if (totalRegistrosExcluidos == 0)
                {
                    //Seta a lista de erros com o erro
                    retorno.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.NotFound).ToString(), message = DadosNaoEncontrados + ", para exclusão da venda Cancelada." });
                }
            }
            catch (Exception ex)
            {
                sqlServer.Rollback();
                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();
                }
            }

            return mensagemRetorno;

        }

        public Dictionary<string, string> consultaParametroFila()
        {
            Dictionary<string, string> listParam = new Dictionary<string, string>();
            int i = 0;

            while (i < 3)
            {
                try
                {
                    listParam = GetFromCache<Dictionary<string, string>>($"{NomeClienteWs}_servicebus_parmetroDAO", 1, () =>
                    {
                        ParametroDAO parametrosDAO = new ParametroDAO(NomeClienteWs, tokenAutenticacao);
                        return parametrosDAO.ListarParametros("queue_azure,logarRequest,AzureBusConStr");
                    });
                    i = 4;
                }
                catch (Exception ex)
                {
                    i++;

                    if (!ex.Message.ToLower().Contains("timeout") || i >= 3)
                    {
                        throw;
                    }

                    //Dorme 3 segundos
                    System.Threading.Thread.Sleep(3000);
                }

            }

            return listParam;
        }

        public int ValidaCompraCancelada(DadosTransacaoCancelada dados)
        {
            int retorno = 0;
            try
            {
                sqlServer.StartConnection();
                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.Parameters.AddWithValue("@dat_compra",dados.dat_compra);
                sqlServer.Command.Parameters.AddWithValue("@cupom", dados.cupom);
                sqlServer.Command.Parameters.AddWithValue("@vlr_compra", dados.vlr_compra);
                sqlServer.Command.Parameters.AddWithValue("@cod_loja", dados.cod_loja);

                sqlServer.Command.CommandText = @"
if not exists(select 1 from tab_lancamento_credito_campanha with(nolock) where dat_compra = @dat_compra and cupom = @cupom and vlr_compra = @vlr_compra and cod_loja = @cod_loja)
begin
  if not exists(select 1 from tab_lancamento_credito_campanha_numero_sorte with(nolock) where dat_compra = @dat_compra and cupom = @cupom and vlr_compra = @vlr_compra)
  begin 
     if not exists(select 1 from tab_lancamento_credito_campanha_selo with(nolock) where dat_compra = @dat_compra and cupom = @cupom and vlr_compra = @vlr_compra and cod_loja = @cod_loja)
	    select 0
     else 
	    select 3
  end
  else
     select 2
end
else
  select 1
";

                var result = sqlServer.Command.ExecuteScalar();
                if (result != null) retorno = (int)result;

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();
                }
            }
            return retorno;
        }
        #endregion


        /// <summary>
        ///  - Remove o registra da venda na Viewizio_3
        /// </summary>
        /// <param name="dataProcessamento"></param>
        /// <returns></returns>
        #region Apaga os registros da viewizio3 para reprocessamento.
        public string ExcluirRegistrosIntermediarios(string dataProcessamento)
        {
            string retorno = "";
            try
            {
                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();

                #region Limpa ou Trunca a viewizio3 de acordo com o parâmetro enviado

                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                if (string.IsNullOrEmpty(dataProcessamento))
                {
                    //Trunca a table viewizio_3
                    sqlServer.Command.CommandText = @"truncate table
                                                     viewizio_3";
                }
                else
                {
                    //Exclui os registros na viewizio_3 com base na data enviada
                    sqlServer.Command.CommandText = @"delete 
                                                      from 
                                                         viewizio_3 
                                                      where
                                                         datacompra between '" + dataProcessamento + "' and '" + dataProcessamento + " 23:59:59' " +
                                                     "select @@rowcount";

                }

                //executa o delete e retorna o total de linhas afetatas
                retorno = "Total de linhas Excluídas: " + (int)sqlServer.Command.ExecuteScalar();

                #endregion

                sqlServer.Commit();

            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();


                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return retorno;
        }
        #endregion


        /// <summary>
        /// Exclui o credito gerado caso nao tenha sido integrado  (retona 
        /// </summary>
        /// <param name="sqlServer"></param>
        #region Exclui o credito gerado na market pay, por que a compra foi cancelada


        private bool ExcluiCreditoCashback(SqlServer sqlServer, bool fl_loyalty = true, long cod_empresa = 0)
        {
            try
            {
                var sCodEmpresa = fl_loyalty ? "" : ", cod_empresa";
                var sWhere = fl_loyalty ? "" : " AND cod_empresa = @cod_empresa_izpay ";
                var lancamentoIntegrado = false;

                sqlServer.Command.Parameters.AddWithValue("@cod_empresa_izpay", cod_empresa);
                sqlServer.Command.CommandText = $@"select dat_cadastro,des_nsu_origem, cod_cnpj_estabelecimento, id_cartao, vlr_credito,cod_lancamento_credito_campanha,cod_lancamento_credito_etapa
                                                   from 
                                                      tab_lancamento_credito_campanha with(nolock)
                                                   where
                                                      dat_compra = CAST(@datacompra AS DATETIME2(0)) and
                                                      vlr_compra = @valorcompra and
                                                      cupom = @cupom and cod_lancamento_credito_etapa > 1 {sWhere}";

                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                if (sqlServer.Reader.HasRows && sqlServer.Reader.Read())
                {
                    lancamentoIntegrado = true;
                }
                sqlServer.Reader.Close();
                sqlServer.Command.CommandText = $@"SELECT * INTO #tmp FROM dbo.tab_lancamento_credito_campanha where dat_compra = CAST(@datacompra AS DATETIME2(0)) and
                                                              vlr_compra = @valorcompra and
                                                             cupom = @cupom and cod_lancamento_credito_etapa = 1 {sWhere}

INSERT INTO dbo.tab_lancamento_credito_campanha_exclusao
(
    cod_lancamento_credito_campanha,
    cod_gestao_campanha,
    dat_cadastro,
    vlr_credito,
    dat_validade,
    cod_cpf,
    cupom,
    cod_transacao,
    dat_compra,
    vlr_compra{sCodEmpresa}
)
SELECT cod_lancamento_credito_campanha,
       cod_gestao_campanha,
       (CONVERT(DATETIMEOFFSET, GETDATE()) AT TIME ZONE 'E. South America Standard Time'),
       vlr_credito,
       dat_validade,
       cod_cpf,
       cupom,
       cod_transacao,
       dat_compra,
       vlr_compra{sCodEmpresa}
FROM #tmp WITH (NOLOCK)

DELETE FROM dbo.tab_lancamento_credito_campanha
WHERE cod_lancamento_credito_campanha IN (
                                             SELECT cod_lancamento_credito_campanha FROM #tmp WITH (NOLOCK)
                                         )";
                sqlServer.Command.ExecuteNonQuery();

                return !lancamentoIntegrado;

            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                {
                    sqlServer.Reader.Close();
                    sqlServer.Reader.Dispose();
                }

            }
        }


        #endregion

        public static TEntity GetFromCache<TEntity>(string key, int numHorasCache, Func<TEntity> valueFactory) where TEntity : class
        {
            ObjectCache cache = MemoryCache.Default;
            // the lazy class provides lazy initializtion which will eavaluate the valueFactory expression only if the item does not exist in cache
            var newValue = new Lazy<TEntity>(valueFactory);
            CacheItemPolicy policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(numHorasCache) };
            //The line below returns existing item or adds the new value if it doesn't exist
            var value = cache.AddOrGetExisting(key, newValue, policy) as Lazy<TEntity>;
            return (value ?? newValue).Value; // Lazy<T> handles the locking itself
        }
        private void enviarEmail(string desTexto, string desTitulo)
        {
            try
            {


                List<Header> lstHeader = new List<Header>();
                lstHeader.Add(new Header
                {
                    name = "tokenAutenticacao",
                    value = tokenAutenticacao
                });

                var acesso = Utilidades.ConsultarConfiguracoesCliente(NomeClienteWs);


                int i = 0;

                EmailTemplateEnvio email = new EmailTemplateEnvio
                {
                    des_email = "monitoramento@izio.com.br",
                    des_cod_campanha = 0,
                    cod_tipo_email_template = (int)TipoTemplate.TEMPLATE_CONTEUDO_GENERICO,
                    des_complemneto = desTexto,
                    des_titulo_email = desTitulo

                };

                var result = Utilidades.ChamadaApiExternaStatusCode(
                                                 tipoRequisicao: "POST",
                                                 metodo: "EmailRest/api/Email/EnvioEmailTemplate/",
                                                 body: JsonConvert.SerializeObject(email),
                                                 url: "https://api.izio.com.br/",
                                                 Headers: lstHeader);

                if (result != null)
                {
                    result = result;
                }
            }
            catch (Exception ex)
            {
                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = "Erro ao enviar email processamento fila: " + ex.Message;

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
        }
    }
}