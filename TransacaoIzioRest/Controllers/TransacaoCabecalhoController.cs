using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TransacaoIzioRest.Models;
using TransacaoRest.DAO;

namespace TransacaoRest.Controllers
{
    /// <summary>
    /// Api das transações cabeçalhos
    /// </summary>
    public class TransacaoCabecalhoController : ApiController
    {
        /// <summary>
        /// Cadastrar uma ou mais Transações de Cabeçalho
        /// </summary>
        /// <param name="listaTransacaoCabecalhos">Lista com os dados das transações cabeçalho</param>        
        /// <remarks>Cadastrar uma ou mais Transações de Cabeçalho</remarks>
        /// <response code="400">Bad request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoCabecalho/")]
        [SwaggerResponse("200", typeof(RetornoDadosTransacaoCabecalho))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage CadastrarTransacaoCabecalho([FromBody]List<DadosTransacaoCabecalho> listaTransacaoCabecalhos)
        {
            #region Variáveis e objetos usados no processamento
            var re = Request;
            var headers = re.Headers;

            string tokenAutenticacao = "";
            string sNomeCliente = null;

            //Lista com os erros ocorridos no Metodo
            ApiErrors listaErros = new ApiErrors()
            {
                errors = new List<Erros>()
            };
            #endregion

            try
            {
                if (headers.Contains("tokenAutenticacao"))
                {
                    #region Valida o Nome do Cliente no Izio
                    try
                    {
                        tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();
                        sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);
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

                    #region Valida os Campos Obrigatórios
                    if (listaTransacaoCabecalhos == null || listaTransacaoCabecalhos.Count == 0)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "Objeto com as transações cabeçalho está vazio, impossível realizar o processamento."
                            });
                    }

                    if (listaErros.errors.Count > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }
                    #endregion

                    #region Realiza a busca no banco de dados e retorna o resultado
                    if (!string.IsNullOrEmpty(sNomeCliente))
                    {
                        TransacaoCabecalhoDAO transacaoCabecalhoDAO = new TransacaoCabecalhoDAO(sNomeCliente);
                        transacaoCabecalhoDAO.CadastrarTransacaoCabecalho(listaTransacaoCabecalhos);

                        RetornoDadosTransacaoCabecalho retornoDadosTransacaoCabecalho = new RetornoDadosTransacaoCabecalho
                        {
                            payload = listaTransacaoCabecalhos
                        };

                        return Request.CreateResponse(HttpStatusCode.OK, retornoDadosTransacaoCabecalho);
                    }
                    else
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "Não foi possível buscar o Nome do Cliente."
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
            }
            catch (Exception ex)
            {
                DadosLog dadosLog = new DadosLog
                {
                    des_erro_tecnico = ex.Message
                };

                Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                listaErros.errors.Add(
                    new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.Unauthorized).ToString(),
                        message = "Não foi possível cadastrar as transações do cabeçalho. Por favor, tente novamente ou entre em contato com o administrador."
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }
    }
}
