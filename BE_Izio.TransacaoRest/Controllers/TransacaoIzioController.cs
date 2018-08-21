namespace TransacaoIzioRest.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using System.Collections.Generic;
    using TransacaoRest.Models;
    using System.Web;
    using TransacaoIzioRest.DAO;
    using System.Web.Http.Description;
    using Izio.Biblioteca;
    using Izio.Biblioteca.Model;
    using TransacaoIzioRest.Models;

    public class TransacaoIzioController : ApiController
    {
        /// <summary>
        /// Metodo para retonar as compras dos ultimos 6 meses do cliente
        /// </summary>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da API</param>
        /// <param name="codigoPessoa">Codigo da pessoa para a consulta das compras</param>
        /// <param name="anoMes">Ano e mês (yyyyMM) para consulta das compras neste periodo</param>
        /// <returns></returns>
        // POST: api/Pessoa/Autenticar
        [HttpGet]
        [Route("api/TransacaoIzio/ConsultaUltimasCompras/{tokenAutenticacao}/{codigoPessoa}/{anoMes}")]
        public HttpResponseMessage ConsultaUltimasCompras([FromUri]string tokenAutenticacao,[FromUri] string codigoPessoa, [FromUri] string anoMes)
        {
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoConsultaTransacao retornoConsulta = new RetornoConsultaTransacao();

            //Objeto para processamento local da API
            DadosConsultaTransacao dadosConsulta = new DadosConsultaTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                try
                {
                    //Verifica se o token informado é válido
                    sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);
                }
                catch (Exception)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(), message = "Token informado não é valido." });
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, listaErros);
                }
                #endregion

                //Executa metodo para consulta das transacações
                DAO.TransacaoDAO consulta = new DAO.TransacaoDAO(sNomeCliente);
                dadosConsulta = consulta.ConsultaUltimasTransacao(Convert.ToInt64(codigoPessoa), anoMes);

                if (dadosConsulta.payload != null && (dadosConsulta.payload.listaTransacao != null && dadosConsulta.payload.listaTransacao.Count > 0))
                {
                    retornoConsulta.payload = dadosConsulta.payload;
                    return Request.CreateResponse(HttpStatusCode.OK, retornoConsulta);
                }
                else
                {
                    listaErros.errors = dadosConsulta.errors;
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
            }
            catch (Exception ex)
            {
                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<Erros>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Parametro Invalido: Cod. Pessoa: " + codigoPessoa.ToString() + " | Ano Mes: " + anoMes });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {
                    DadosLog dadosLog = new DadosLog();
                    dadosLog.des_erro_tecnico = "Parametro Invalido: Cod. Pessoa: " + codigoPessoa.ToString() + " | Ano Mes: " + anoMes;
                    Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                    dadosLog.des_erro_tecnico = ex.ToString();
                    //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                    Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
                }

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

        /// <summary>
        /// Metodo para retonar os itens de uma compra
        /// </summary>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da API</param>
        /// <param name="codigoTransacao">Codigo da transacao para a consulta dos itens da compra</param>
        /// <returns></returns>
        // POST: api/Pessoa/Autenticar
        [HttpGet]
        [Route("api/TransacaoIzio/ConsultaItensCompra/{tokenAutenticacao}/{codigoTransacao}")]
        public HttpResponseMessage ConsultaItensCompra([FromUri]string tokenAutenticacao, [FromUri] string codigoTransacao)
        {
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoDadosItensTransacao retornoConsulta = new RetornoDadosItensTransacao();

            //Objeto para processamento local da API
            DadosConsultaItensTransacao dadosConsulta = new DadosConsultaItensTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();

            try
            {
                //Valida o token
                sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);

                //Executa metodo para consulta das transacações
                DAO.TransacaoDAO consulta = new DAO.TransacaoDAO(sNomeCliente);
                dadosConsulta = consulta.ConsultaItensTransacao(Convert.ToInt64(codigoTransacao));

                if (dadosConsulta.payload != null && (dadosConsulta.payload.listaItensTransacao != null && dadosConsulta.payload.listaItensTransacao.Count > 0))
                {
                    retornoConsulta.payload = dadosConsulta.payload;
                    return Request.CreateResponse(HttpStatusCode.OK, retornoConsulta);
                }
                else
                {
                    listaErros.errors = dadosConsulta.errors;
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
            }
            catch (Exception ex)
            {
                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<Erros>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ex.Message });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {
                    DadosLog dadosLog = new DadosLog();
                    dadosLog.des_erro_tecnico = ex.ToString();

                    //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                    Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
                }

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

        /// <summary>
        /// Realiza a importação da transação on-line para as tabelas finais do Izio
        /// </summary>
        /// <remarks>
        /// Importa as vendas para as tabelas finais do Izio
        /// Processamento On-line das Vendas.
        /// </remarks>
        /// <param name="objTransacao">Json com os dados de 1 venda (capa e os itens)</param>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da api</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/TransacaoIzio/ImportaTransacao/{tokenAutenticacao}")]
        public HttpResponseMessage ImportaTransacao([FromBody] DadosTransacaoOnline objTransacao, [FromUri] string tokenAutenticacao)
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoPayloadTransacao retProcessamento = new RetornoPayloadTransacao();

            //Objeto para processamento local da API
            RetornoDadosProcTransacao retorno = new RetornoDadosProcTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ListaErrosTransacao listaErros = new ListaErrosTransacao();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                try
                {
                    //Verifica se o token informado é válido
                    sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);
                }
                catch (Exception)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(), message = "Token informado não é valido." });
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, listaErros);
                }
                #endregion

                //Cria objeto para processamento das transacoes
                DAO.ImportaTransacaoDAO impTransacao = new DAO.ImportaTransacaoDAO(sNomeCliente);
                retorno = impTransacao.ImportaTransacao(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);

                if (retorno.errors != null && retorno.errors.Count > 0)
                {
                    listaErros.errors = retorno.errors;
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
                else
                {
                    //retorno.payload.code = Convert.ToInt32(HttpStatusCode.OK).ToString();
                    //retorno.payload.message = "Processamento das transações realizado com sucesso.";
                    retProcessamento.payload = retorno.payload;
                    return Request.CreateResponse(HttpStatusCode.Created, retProcessamento);
                }
            }
            catch (System.Exception ex)
            {

                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<ErrosTransacao>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno no processamento on-line das transações, favor contactar o administrador." });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {
                    DadosLog dadosLog = new DadosLog();
                    dadosLog.des_erro_tecnico = ex.ToString();

                    //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                    Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
                }

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);

            }
        }

        /// <summary>
        /// Realiza a importação em lote das transações carregadas em lotes para tabela intermediária do Izio
        /// </summary>
        /// <remarks>
        /// Método para importar as vendas em lote de 1000 em 1000 registros para a tabela intermediária do Izio
        /// 
        /// As vendas importadas por este método serão processadas para as tabelas finais do Izio em processamento back.
        /// </remarks>
        /// <param name="objTransacao">Lote em Json com as vendas</param>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da api</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/TransacaoIzio/ImportaLoteTransacoes/{tokenAutenticacao}")]
        public HttpResponseMessage ImportaLoteTransacoes([FromBody] List<DadosTransacaoLote> objTransacao, [FromUri] string tokenAutenticacao)
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoPayloadTransacao retProcessamento = new RetornoPayloadTransacao();

            //Objeto para processamento local da API
            RetornoDadosProcTransacao retorno = new RetornoDadosProcTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ListaErrosTransacao listaErros = new ListaErrosTransacao();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                try
                {
                    //Verifica se o token informado é válido
                    sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);
                }
                catch (Exception)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(), message = "Token informado não é valido." });
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, listaErros);
                }
                #endregion

                //Cria objeto para processamento das transacoes
                DAO.ImportaTransacaoDAO impTransacao = new DAO.ImportaTransacaoDAO(sNomeCliente);
                if (sNomeCliente.ToUpper() == "CONDOR")
                {
                    retorno = impTransacao.ImportaLoteTransacaoSemTransacao(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                }
                else
                {
                    retorno = impTransacao.ImportaLoteTransacao(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                }

                if (retorno.errors != null && retorno.errors.Count > 0)
                {
                    listaErros.errors = retorno.errors;
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
                else
                {
                    retProcessamento.payload = retorno.payload;
                    return Request.CreateResponse(HttpStatusCode.Created, retProcessamento);
                }
            }
            catch (System.Exception ex)
            {
                Log.inserirLogException(sNomeCliente, ex, 0);
                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<ErrosTransacao>();
                }

                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno no processamento do lote das transações, favor contactar o administrador." });

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio("lab", dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);

            }
        }

        /// <summary>
        /// Realiza a exclusão dos registros de uma compra cancelada
        /// </summary>
        /// <param name="objTransacao">Dados da compra cancelada</param>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da api</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/TransacaoIzio/ExcluirRegistrosCompraCancelada/{tokenAutenticacao}")]
        public HttpResponseMessage ExcluirRegistrosCompraCancelada([FromBody] DadosTransacaoCancelada objTransacao, [FromUri] string tokenAutenticacao)
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoSucessoRemoverTransacao retProcessamento = new RetornoSucessoRemoverTransacao();

            //Objeto para processamento local da API
            RetornoRemoveTransacao retorno = new RetornoRemoveTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                try
                {
                    //Verifica se o token informado é válido
                    sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);
                }
                catch (Exception)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(), message = "Token informado não é valido." });
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, listaErros);
                }
                #endregion

                //Cria objeto para processamento das transacoes
                TransacaoCanceladaDAO excluiTransacao = new TransacaoCanceladaDAO(sNomeCliente);
                retorno = excluiTransacao.ExcluirRegistrosCompraCancelada(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);

                if (retorno.errors != null && retorno.errors.Count > 0)
                {
                    listaErros.errors = retorno.errors;
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
                else
                {
                    retProcessamento.payload = retorno.payload;
                    return Request.CreateResponse(HttpStatusCode.OK, retProcessamento);
                }
            }
            catch (System.Exception ex)
            {

                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<Erros>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new Erros{ code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno no processamento do lote das transações, favor contactar o administrador." });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {
                    DadosLog dadosLog = new DadosLog();
                    dadosLog.des_erro_tecnico = ex.ToString();

                    //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                    Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
                }

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);

            }
        }

        /// <summary>
        /// Realiza a exclusão dos registros intermediários para reprocessamento.
        /// </summary>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da api</param>
        /// <param name="dataProcessamento">Data (yyyy-MM-dd) para exclusão processamento (deixar em branco para deletar tudo)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/TransacaoIzio/ExcluirRegistrosIntermediarios/{tokenAutenticacao}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ResponseType(typeof(ApiSuccess))]
        public HttpResponseMessage ExcluirRegistrosIntermediarios([FromUri] string tokenAutenticacao, [FromUri] string dataProcessamento = "")
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                try
                {
                    //Verifica se o token informado é válido
                    sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);
                }
                catch (Exception)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(), message = "Token informado não é valido." });
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, listaErros);
                }
                #endregion

                //Cria objeto para processamento das transacoes
                TransacaoCanceladaDAO excluiTransacao = new TransacaoCanceladaDAO(sNomeCliente);
                excluiTransacao.ExcluirRegistrosIntermediarios(dataProcessamento);

                ApiSuccess success = new ApiSuccess();

                success.payload = new Sucesso();
                success.payload.code = "200";
                success.payload.message = "Dados Excluídos com Sucesso.";
                return Request.CreateResponse(HttpStatusCode.OK, success);

            }
            catch (System.Exception ex)
            {

                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<Erros>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno no processamento do lote das transações, favor contactar o administrador." });
                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();
                
                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);

            }
        }


        /// <summary>
        /// Método para consultar os registros que foram enviados para Izio para a tabela intermédiária.
        /// </summary>
        /// <remarks>
        /// Consultar o total das vendas importadas em lote no Izio. As Vendas que foram carregadas na tabela intermediária
        /// </remarks>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da api</param>
        /// <param name="dataImportacao">Data (yyyyMMdd) para consulta das transações (vendas) importadas em lote</param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/TransacaoIzio/ConsultarTransacoesCarregadaEmLote/{tokenAutenticacao}/{dataImportacao}")]
        [ResponseType(typeof(RetornoDadosTermino))]
        public HttpResponseMessage ConsultarTransacoesCarregadaEmLote([FromUri] string tokenAutenticacao, [FromUri] string dataImportacao)
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            //Objeto de retorno com o movimento das lojas
            RetornoDadosTermino retorno = new RetornoDadosTermino();

            try
            {
                #region Validação dos campos 
                if (string.IsNullOrEmpty(dataImportacao))
                {
                    //Seta o Objeto com o Erro ocorrido
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "O campo data importação é obrigatório." });
                }
                else if (dataImportacao.Trim().Length < 8 || dataImportacao.Trim().Length > 8)
                {
                    //Seta o Objeto com o Erro ocorrido
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Data importação não foi informada corretamente (yyyyMMdd)." });
                }
                else
                {
                    DateTime data;
                    if (!DateTime.TryParseExact(dataImportacao, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out data))
                    {
                        //Seta o Objeto com o Erro ocorrido
                        listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "A data importação não foi informada corretamente (yyyyMMdd)." });
                    }
                }

                if (listaErros.errors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
                #endregion

                //Valida Token no Izio
                #region Valida Token no Izio
                try
                {
                    //Verifica se o token informado é válido
                    sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);
                }
                catch (Exception)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(), message = "Token informado não é valido." });
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, listaErros);
                }
                #endregion

                //Cria objeto para processamento das transacoes
                TransacaoDAO dao = new TransacaoDAO(sNomeCliente);

                retorno = dao.ConsultarTransacoesCarregadaEmLote(DateTime.ParseExact(dataImportacao, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));

                return Request.CreateResponse(HttpStatusCode.OK, retorno);

            }
            catch (System.Exception ex)
            {
                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno no processamento do lote das transações, favor contactar o administrador." });
                
                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);

            }
        }

    }
}
