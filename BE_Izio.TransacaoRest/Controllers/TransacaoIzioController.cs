namespace TransacaoIzioRest.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using Izio.Biblioteca;
    using System.Collections.Generic;
    using Models;
    using TransacaoRest.Models;
    using System.Web;
    using TransacaoIzioRest.DAO;

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
            ListaErrosConsultaTransacao listaErros = new ListaErrosConsultaTransacao();

            try
            {
                //Valida o token
                sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);

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
                    listaErros.errors = new List<ErrosConsultaTransacao>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new ErrosConsultaTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ex.Message });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {
                    Log.inserirLogException(sNomeCliente, new Exception("Parametro Invalido: Cod. Pessoa: " + codigoPessoa.ToString() + " | Ano Mes: " + anoMes), 0);


                    //Se exception
                    if (ex.InnerException != null)
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, new Exception(ex.Message + System.Environment.NewLine + ex.InnerException), 0);
                    }
                    else
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, ex, 0);
                    }
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
            ListaErrosConsultaTransacao listaErros = new ListaErrosConsultaTransacao();

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
                    listaErros.errors = new List<ErrosConsultaTransacao>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new ErrosConsultaTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ex.Message });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {   
                    //Se exception
                    if (ex.InnerException != null)
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, new Exception(ex.Message + System.Environment.NewLine + ex.InnerException), 0);
                    }
                    else
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, ex, 0);
                    }
                }

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

        /// <summary>
        /// Realiza a importação da transação on-line para as tabelas finais do Izio
        /// </summary>
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
                //Verifica se o token informado é válido
                sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);

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
                    //Se exception
                    if (ex.InnerException != null)
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, new Exception(ex.Message + System.Environment.NewLine + ex.InnerException), 0);
                    }
                    else
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, ex, 0);
                    }
                }

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);

            }
        }

        /// <summary>
        /// Realiza a importação das transações carregadas em lotes para tabela intermediária do Izio
        /// </summary>
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
                //Verifica se o token informado é válido
                sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);

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
                listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno no processamento do lote das transações, favor contactar o administrador." });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {
                    //Se exception
                    if (ex.InnerException != null)
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, new Exception(ex.Message + System.Environment.NewLine + ex.InnerException), 0);
                    }
                    else
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, ex, 0);
                    }
                }

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
            RetornoErroRemoverTransacao listaErros = new RetornoErroRemoverTransacao();

            try
            {
                //Verifica se o token informado é válido
                sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);

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
                    //Se exception
                    if (ex.InnerException != null)
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, new Exception(ex.Message + System.Environment.NewLine + ex.InnerException), 0);
                    }
                    else
                    {
                        //Grava o erro na tabela de log (sis_log)
                        Log.inserirLogException(sNomeCliente, ex, 0);
                    }
                }

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);

            }
        }

    }
}
