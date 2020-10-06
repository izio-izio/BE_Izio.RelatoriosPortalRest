using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using TransacaoIzioRest.DAO;
using TransacaoIzioRest.Models;

namespace TransacaoIzioRest.Controllers
{
    public class TransacaoIzioRestController : ApiController
    {
        //Mensagens Retorno 
        #region Mensagens Retorno 
        private string ObjetoTransacaoVazio = "Objeto com os dados das vendas está vazio, impossível realizar o processamento.";
        private string ObjetoItensTransacaoVazio = "Objeto com os itens das vendas está vazio, impossível realizar o processamento.";
        private string ObjetoTransacaoCanceladaVazio = "Objeto com os dados da venda cancelada está vazio, impossível realizar o processamento.";
        private string SucessoExclusao = "Compra cancelada excluída com sucesso.";
        private string DadosNaoEncontradosItens = "Não foi possível realizar consulta dos itens da venda.";
        private string DadosNaoEncontrados = "Não foram encontrados registros.";
        private string NaoExisteCodPessoa = "Codigo da pessoa informada, não existe na base do Izio";
        #endregion

        /// <summary>
        /// Metodo para retonar as compras dos ultimos 6 meses do cliente
        /// </summary>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da API</param>
        /// <param name="codigoPessoa">Codigo da pessoa para a consulta das compras</param>
        /// <param name="anoMes">Ano e mês (yyyyMM) para consulta das compras neste periodo</param>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ConsultaUltimasCompras/{tokenAutenticacao}/{codigoPessoa}/{anoMes}")]
        [SwaggerResponse("200", typeof(RetornoConsultaTransacao))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        public HttpResponseMessage ConsultaUltimasCompras([FromUri]string tokenAutenticacao, [FromUri] string codigoPessoa, [FromUri] string anoMes)
        {
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoConsultaTransacao retornoConsulta = new RetornoConsultaTransacao();

            //Objeto para processamento local da API
            DadosConsultaTransacao dadosConsulta = new DadosConsultaTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio

                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

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
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = DadosNaoEncontrados + "." });
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
        /// Metodo para consultar os itens de uma compra
        /// </summary>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da API</param>
        /// <param name="codigoTransacao">Codigo da transacao para a consulta dos itens da compra</param>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ConsultaItensCompra/{tokenAutenticacao}/{codigoTransacao}")]
        [SwaggerResponse("200", typeof(RetornoDadosItensTransacao))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        public HttpResponseMessage ConsultaItensCompra([FromUri]string tokenAutenticacao, [FromUri] string codigoTransacao)
        {
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoDadosItensTransacao retornoConsulta = new RetornoDadosItensTransacao();

            //Objeto para processamento local da API
            RetornoDadosItensTransacao dadosConsulta = new RetornoDadosItensTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            try
            {
                //Valida o token
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

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
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = DadosNaoEncontradosItens + "." });
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
        [HttpPost, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ImportaTransacao/{tokenAutenticacao}")]
        [SwaggerResponse("201", typeof(ApiSuccess))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        public HttpResponseMessage ImportaTransacao([FromBody] DadosTransacaoOnline objTransacao, [FromUri] string tokenAutenticacao)
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto para processamento local da API
            ApiSuccess retorno = new ApiSuccess();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();
                #endregion

                //Cria objeto para processamento das transacoes
                DAO.ImportaTransacaoDAO impTransacao = new DAO.ImportaTransacaoDAO(sNomeCliente);

                //Valida os campos obrigatório
                #region Valida os campos obrigatório

                // Valida se o objeto com as transações foi preenchido
                if (objTransacao == null)
                {
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoTransacaoVazio });
                }
                else
                {
                    objTransacao.cod_cpf = Regex.Replace(objTransacao.cod_cpf, "[^0-9,]", ""); 

                    if (objTransacao.ListaItens == null)
                    {
                        listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoItensTransacaoVazio });
                    }
                    else if (objTransacao.ListaItens != null && objTransacao.ListaItens.Count == 0)
                    {
                        listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoItensTransacaoVazio });
                    }
                    else if (objTransacao.cod_pessoa > 0 && !impTransacao.VerificaCodPessoaExiste(objTransacao.cod_pessoa.Value, objTransacao.cupom, objTransacao.dat_compra, objTransacao.cod_loja))
                    {
                        listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = NaoExisteCodPessoa });
                    }
                }
                //Se a lista estiver preenchida, é por que foi encontrado erros na validação
                if (listaErros.errors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }

                #endregion

                //Importa a venda na base do Izio (tab_transacao/tab_transacao_itens ou tab_transacao_cpf/tab_transacao_itens_cpf)
                listaErros = impTransacao.ImportaTransacao(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);

                //Verifica se houve erro na importação das compras e caso tenha acontecido, retorna o erro.
                if (listaErros.errors != null && listaErros.errors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
                else
                {
                    //Importação das vendas realizada com sucesso
                    retorno.payload = new Sucesso();
                    retorno.payload.code = Convert.ToInt32(HttpStatusCode.Accepted).ToString();
                    retorno.payload.message = "Transação Importada com sucesso.";

                    return Request.CreateResponse(HttpStatusCode.Created, retorno);
                }
            }
            catch (System.Exception ex)
            {

                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<Erros>();
                }
                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno no processamento on-line das transações, favor contactar o administrador." });

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
        [HttpPost, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ImportaLoteTransacoes/{tokenAutenticacao}")]
        [SwaggerResponse("201", typeof(ApiSuccess))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        public HttpResponseMessage ImportaLoteTransacoes([FromBody] List<DadosTransacaoLote> objTransacao, [FromUri] string tokenAutenticacao)
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto para processamento local da API
            ApiSuccess retorno = new ApiSuccess();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();
                #endregion

                //Valida os campos obrigatórios
                #region Valida os campos obrigatórios

                //Valida se o objeto com as transações foi preenchido
                if (objTransacao == null)
                {
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoTransacaoVazio });
                }
                else if (objTransacao.Count == 0)
                {
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoItensTransacaoVazio });
                }

                //Se a lista estiver preenchida, é por que foi encontrado erros na validação
                if (listaErros.errors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }

                #endregion

                //Cria objeto para processamento das transacoes
                DAO.ImportaTransacaoDAO impTransacao = new DAO.ImportaTransacaoDAO(sNomeCliente);

                if (sNomeCliente.ToUpper() == "CONDOR")
                {
                    listaErros = impTransacao.ImportaLoteTransacaoSemTransacao(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                }
                else
                {
                    listaErros = impTransacao.ImportaLoteTransacao(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
                }

                if (listaErros.errors != null && listaErros.errors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
                else
                {
                    retorno.payload = new Sucesso();
                    retorno.payload.code = Convert.ToInt32(HttpStatusCode.Accepted).ToString();
                    retorno.payload.message = "Lote de Transações Importado com sucesso.";
                    return Request.CreateResponse(HttpStatusCode.Created, retorno);
                }
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
        /// Realiza a exclusão dos registros de uma compra cancelada
        /// </summary>
        /// <param name="objTransacao">Dados da compra cancelada</param>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da api</param>
        /// <returns></returns>
        [HttpPost, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ExcluirRegistrosCompraCancelada/{tokenAutenticacao}")]
        [SwaggerResponse("200", typeof(ApiSuccess))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        public HttpResponseMessage ExcluirRegistrosCompraCancelada([FromBody] DadosTransacaoCancelada objTransacao, [FromUri] string tokenAutenticacao)
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            ApiSuccess retornoSucesso = new ApiSuccess();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();
                #endregion

                //Valida campos obrigatorio
                #region Valida campos obrigatorio

                //Valida se o objeto com as transações foi preenchido
                if (objTransacao == null)
                {
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoTransacaoCanceladaVazio });
                }

                //Se a lista estiver preenchida, é por que foi encontrado erros na validação
                if (listaErros.errors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }

                #endregion

                //Cria objeto para processamento das transacoes
                TransacaoCanceladaDAO excluiTransacao = new TransacaoCanceladaDAO(sNomeCliente,tokenAutenticacao);
                listaErros = excluiTransacao.ExcluirRegistrosCompraCancelada(objTransacao, HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);

                if (listaErros.errors != null && listaErros.errors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }
                else
                {
                    retornoSucesso.payload = new Sucesso();
                    retornoSucesso.payload.code = Convert.ToInt32(HttpStatusCode.OK).ToString();
                    retornoSucesso.payload.message = SucessoExclusao;

                    return Request.CreateResponse(HttpStatusCode.OK, retornoSucesso);
                }
            }
            catch (System.Exception ex)
            {

                if (listaErros.errors == null)
                {
                    listaErros.errors = new List<Erros>();
                }

                //Seta o Objeto com o Erro ocorrido
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno na exclusão da compra, favor contactar o administrador." });

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
        [HttpPost, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ExcluirRegistrosIntermediarios/{tokenAutenticacao}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [SwaggerResponse("200", typeof(ApiSuccess))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        public HttpResponseMessage ExcluirRegistrosIntermediarios([FromUri] string tokenAutenticacao, [FromUri] string dataProcessamento = "")
        {
            //Nome do cliente que esta executando a API, gerado após validação do Token
            string sNomeCliente = "";

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();
                #endregion

                //Cria objeto para processamento das transacoes
                TransacaoCanceladaDAO excluiTransacao = new TransacaoCanceladaDAO(sNomeCliente, tokenAutenticacao);
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
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Erro interno a exclusão das vendas em lote, favor contactar o administrador." });

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
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ConsultarTransacoesCarregadaEmLote/{tokenAutenticacao}/{dataImportacao}")]
        [SwaggerResponse("200", typeof(RetornoDadosTermino))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
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
                //Valida Token no Izio
                #region Valida Token no Izio
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();
                #endregion

                //Validação dos campos
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

        /// <summary>
        /// Metodo para retonar as compras dos ultimos 6 meses do cliente
        /// </summary>
        /// <param name="codigoPessoa">Codigo da pessoa para a consulta das compras</param>
        /// <param name="qtdMes"> consulta das compras neste periodo</param>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoIzio/ConsultaTotalDesconto/{codigoPessoa}")]
        [SwaggerResponse("200", typeof(RetornoDadosTotalDesconto))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        public HttpResponseMessage ConsultaTotalDesconto([FromUri] long codigoPessoa, int qtdMes = 3 )
        {
            var re = Request;
            var headers = re.Headers;

            string tokenAutenticacao = "";
            string sNomeCliente = null;

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoDadosTotalDesconto retornoConsulta = new RetornoDadosTotalDesconto();

            //Objeto para processamento local da API
            DadosConsultaDesconto dadosConsulta = new DadosConsultaDesconto();

            //Objeto de retorno contendo os erros da execução da API
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            try
            {
                //Valida Token no Izio
                #region Valida Token no Izio

                if (headers.Contains("tokenAutenticacao"))
                {
                    #region Valida o Nome do Cliente no Izio
                    try
                    {
                        tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();
                        sNomeCliente = Request.Headers.GetValues("sNomeCliente").First();
                    }
                    catch (Exception)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(),
                                message = "Erro na captura do 'sNomeCliente' na Izio."
                            });

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }
                    #endregion
                }
                else
                {
                    listaErros.errors.Add(
                        new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(),
                            message = "Request Não autorizado. Token Inválido ou Nulo."
                        });

                    return Request.CreateResponse(HttpStatusCode.Unauthorized, listaErros);
                }

                #endregion

                if (qtdMes < 1 || qtdMes > 6)
                {
                    listaErros.errors.Add(
                       new Erros
                       {
                           code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                           message = "Permitido apenas entre 1 e 6 meses."
                       });

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }

                //Executa metodo para consulta das transacações
                DAO.TransacaoDAO consulta = new DAO.TransacaoDAO(sNomeCliente);
                dadosConsulta = consulta.ConsultaTotalDesconto(codigoPessoa, qtdMes);

                if (dadosConsulta.payload != null && (dadosConsulta.payload != null && dadosConsulta.payload.Count > 0))
                {
                    retornoConsulta.payload = dadosConsulta.payload;
                    return Request.CreateResponse(HttpStatusCode.OK, retornoConsulta);
                }
                else
                {
                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = DadosNaoEncontrados + "." });
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
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "Parametro Invalido: Cod. Pessoa: " + codigoPessoa.ToString()  });

                if (!ex.Message.ToUpper().Contains("TOKEN"))
                {
                    DadosLog dadosLog = new DadosLog();
                    dadosLog.des_erro_tecnico = "Parametro Invalido: Cod. Pessoa: " + codigoPessoa.ToString() ;
                    Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                    dadosLog.des_erro_tecnico = ex.ToString();
                    //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                    Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
                }

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }
    }
}
