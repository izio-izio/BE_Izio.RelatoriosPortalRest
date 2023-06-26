using Izio.Biblioteca;
using Izio.Biblioteca.DAO;
using Izio.Biblioteca.Model;
using NSwag.Annotations;
using ProgramaBeneficioController.Models;
using RelatoriosPortalRest.DAO;
using RelatoriosPortalRest.Models;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;


namespace RelatoriosPortalRest.Controllers
{
    public class ProgramaBeneficioController : ApiController
    {
        /// <summary>
        ///     Consulta a relação de usuários cadastrados no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint progbeneficios/pessoa-distintas-vendas.
        ///     
        ///     <para>
        ///         Fluxo
        ///             codLoja = opcional;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYYMMDD
        ///     </para>
        ///     
        ///     ### Filtros QueryParam ###
        ///     <para>
        ///         "codLoja" string - Separadas por vírgula;
        ///         "primeiraData" string: Data inicial (YYYYMMDD);
        ///         "ultimaData" string: Data final (YYYYMMDD)
        ///     </para>
        /// 
        ///     ### Status de retorno da API ###
        ///     <para>
        ///         Status Code 200 = Sucesso na requisição;
        ///         Status Code 400 = Bad Request (Dados inseridos para utilização da API incorretos);
        ///         Status Code 500 = Internal Server Error (Ocorreu um erro no lado do servidor para buscar os dados);
        ///     </para>
        /// </remarks>
        /// <param name="codLoja">Código das lojas separados por vírgula</param>
        /// <param name="primeiraData">Data inicial (YYYYMMDD)</param>
        /// <param name="ultimaData">Data final (YYYYMMDD)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/Relatorios/Progbeneficio/TotalDeClientes")]
        [SwaggerResponse("200", typeof(PessoasDistintas))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage TotalDeClientes(string primeiraData, string ultimaData, string codLoja = "")
        {
            #region variáveis e objetos usados no processamento           
            string sNomeCliente = null;
            string tokenAutenticacao = null;

            ApiErrors listaErros = new ApiErrors()
            {
                errors = new List<Erros>()
            };
            #endregion

            #region Validação campos de entrada

            if (!string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "Ultima data deve ser informada."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "Primeira data deve ser informada."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário inserir as datas para a busca."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                DateTime primeira = DateTime.ParseExact(primeiraData, "yyyyMMdd", CultureInfo.InvariantCulture);
                DateTime ultima = DateTime.ParseExact(ultimaData, "yyyyMMdd", CultureInfo.InvariantCulture);

                if (ultima < primeira)
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Primeira data deve ser menor que a última data para análise."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("pben_api_xkey");
                string xApiKey = listParam["pben_api_xkey"];

                PessoasDistintas data = new PessoasDistintas();

                ProgramaBeneficioDAO dao = new ProgramaBeneficioDAO(sNomeCliente, tokenAutenticacao);
                PessoasDistintas retorno = dao.BuscarTotalDeClientes(xApiKey, primeiraData, ultimaData, codLoja);

                data = retorno;

                if (retorno != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, retorno);
                }

                else
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                        message = "Ocorreu um erro ao buscar os dados."
                    });
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }

            }
            catch (Exception ex)
            {
                DadosLog dadosLog = new DadosLog
                {
                    des_erro_tecnico = ex.ToString()
                };

                Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                listaErros.errors.Add(
                    new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                        message = ex.Message
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }


        /// <summary>
        ///     Consulta a relação de vendas e ticket médio no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint progbeneficios/loja-receita.
        ///     
        ///     <para>
        ///         Fluxo
        ///             codLoja = opcional;
        ///             codCampanha = opcional;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYYMMDD
        ///     </para>
        ///     
        ///     ### Filtros QueryParam ###
        ///     <para>
        ///         "codLoja" string - Separadas por vírgula;
        ///         "codCampanha" string - Separadas por vírgula;
        ///         "primeiraData" string: Data inicial (YYYYMMDD);
        ///         "ultimaData" string: Data final (YYYYMMDD)
        ///     </para>
        /// 
        ///     ### Status de retorno da API ###
        ///     <para>
        ///         Status Code 200 = Sucesso na requisição;
        ///         Status Code 400 = Bad Request (Dados inseridos para utilização da API incorretos);
        ///         Status Code 500 = Internal Server Error (Ocorreu um erro no lado do servidor para buscar os dados);
        ///     </para>
        /// </remarks>
        /// <param name="codLoja">Código das lojas separados por vírgula</param>
        /// <param name="codCampanha">Código das campanhas separados por vírgula</param>
        /// <param name="primeiraData">Data inicial (YYYYMMDD)</param>
        /// <param name="ultimaData">Data final (YYYYMMDD)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/Relatorios/Progbeneficio/VendasTicketMedio")]
        [SwaggerResponse("200", typeof(VendasTicketMedio))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage VendasTicketMedio(string primeiraData, string ultimaData, string codLoja = "", string codCampanha = "")
        {
            #region variáveis e objetos usados no processamento           
            string sNomeCliente = null;
            string tokenAutenticacao = null;

            ApiErrors listaErros = new ApiErrors()
            {
                errors = new List<Erros>()
            };
            #endregion

            #region Validação campos de entrada

            if (!string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "Ultima data deve ser informada."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "Primeira data deve ser informada."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário inserir as datas para a busca."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                DateTime primeira = DateTime.ParseExact(primeiraData, "yyyyMMdd", CultureInfo.InvariantCulture);
                DateTime ultima = DateTime.ParseExact(ultimaData, "yyyyMMdd", CultureInfo.InvariantCulture);

                if (ultima < primeira)
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Primeira data deve ser menor que a última data para análise."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("pben_api_xkey");
                string xApiKey = listParam["pben_api_xkey"];

                VendasTicketMedio data = new VendasTicketMedio();

                ProgramaBeneficioDAO dao = new ProgramaBeneficioDAO(sNomeCliente, tokenAutenticacao);
                VendasTicketMedio retorno = dao.VendasTicketMedio(xApiKey, primeiraData, ultimaData, codLoja, codCampanha);

                data = retorno;

                if (retorno != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, retorno);
                }

                else
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                        message = "Ocorreu um erro ao buscar os dados."
                    });
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                }

            }
            catch (Exception ex)
            {
                DadosLog dadosLog = new DadosLog
                {
                    des_erro_tecnico = ex.ToString()
                };

                Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                listaErros.errors.Add(
                    new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                        message = ex.Message
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }
    }
}

