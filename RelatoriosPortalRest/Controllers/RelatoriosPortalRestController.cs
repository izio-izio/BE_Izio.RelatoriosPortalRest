using Izio.Biblioteca;
using Izio.Biblioteca.DAO;
using Izio.Biblioteca.Model;
using NSwag.Annotations;
using RelatoriosPortalRest.DAO;
using RelatoriosPortalRest.Models;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;


namespace RelatoriosPortalRest.Controllers
{
    public class RelatoriosController : ApiController
    {
        /// <summary>
        ///     Consulta a relação de usuários cadastrados no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint usuarios-cadastrados.
        ///     
        ///     <para>
        ///         Para cada tipo de filtro de entrada é retornado um objeto.
        ///         Fluxo 1
        ///             período > 0: Retorna as informações de usuários com as informações consolidadas por período;
        ///             codLoja: É possível combinar com o filtro de lojas;
        ///             primeiraData e ultimaData: devem ser vazias;
        ///         Fluxo 2
        ///             periodo = 0 obrigatóriamente;
        ///             codLoja = null obrigatóriamente;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYY-MM-DD
        ///     </para>
        ///     
        ///     ### Filtros QueryParam ###
        ///     <para>
        ///         "periodo" (int) - Default = 1: retorna os períodos agregados;
        ///         "codLoja" string - Separadas por vírgula;
        ///         "primeiraData" string: Data inicial (YYYY-MM-DD);
        ///         "ultimaData" string: Data final (YYYY-MM-DD)
        ///     </para>
        /// 
        ///     ### Status de retorno da API ###
        ///     <para>
        ///         Status Code 200 = Sucesso na requisição;
        ///         Status Code 400 = Bad Request (Dados inseridos para utilização da API incorretos);
        ///         Status Code 500 = Internal Server Error (Ocorreu um erro no lado do servidor para buscar os dados);
        ///     </para>
        /// </remarks>
        /// <param name="periodo">Período (DEFAULT = 1)</param>
        /// <param name="codLoja">Código das lojas separados por vírgula</param>
        /// <param name="primeiraData">Data inicial (YYYY-MM-DD)</param>
        /// <param name="ultimaData">Data final (YYYY-MM-DD)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/Relatorios/UsuariosCadastrados")]
        [SwaggerResponse("200", typeof(UsuariosCadastrados))]
        [SwaggerResponse("200", typeof(UsuariosCadastradosData))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage UsuariosCadastrados(int periodo = 1, string codLoja = "", string primeiraData = "", string ultimaData = "")
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

            if(!string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData)){
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

            if (string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData) && periodo <= 0)
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário inserir ao menos 1 filtro para a busca."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                if(periodo != null && periodo > 0)
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelo período com as datas informadas."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }

                if (!string.IsNullOrEmpty(codLoja))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "O filtro de loja só é possível com o período > 0."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }
            
            if (periodo != null && periodo > 0)
            {
                if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelas datas com o período informado."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (periodo == 0 && !string.IsNullOrEmpty(codLoja))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário informar o período para filtro de lojas."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("yhub-xapikey");
                string xApiKey = listParam["yhub-xapikey"];

                UsuariosCadastradosData dataAgregado = new UsuariosCadastradosData();
                UsuariosCadastrados data = new UsuariosCadastrados();

                RelatoriosPortalRestDAO dao = new RelatoriosPortalRestDAO(sNomeCliente, tokenAutenticacao);
                Tuple<UsuariosCadastradosData, UsuariosCadastrados> retorno = dao.BuscarUsuariosCadastrados(periodo, xApiKey, codLoja, primeiraData, ultimaData);

                dataAgregado = retorno.Item1;
                data = retorno.Item2;

                if (retorno != null)
                {
                    if (retorno.Item1.statusCode == 200 || retorno.Item2.statusCode == 200)
                    {

                        if (periodo == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, dataAgregado);
                        }

                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, data);
                        }
                    }

                    else
                    {
                        DadosLog dadosLog = new DadosLog
                        {
                            des_erro_tecnico = "Ocorreu um erro ao buscar as informações da YHUB no endpoint: UsuariosCadastrados" + retorno.ToJson()
                        };

                        Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "Ocorreu um erro ao buscar os dados."
                        });
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }
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
        ///     Consulta a relação de receitas, transações e ticket médio no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint ReceitasTransacoesTicketMedio.
        ///     
        ///     <para>
        ///         Para cada tipo de filtro de entrada é retornado um objeto.
        ///         Fluxo 1
        ///             período > 0: Retorna as informações de usuários com as informações consolidadas por período;
        ///             codLoja: É possível combinar com o filtro de lojas;
        ///             primeiraData e ultimaData: devem ser vazias;
        ///         Fluxo 2
        ///             periodo = 0 obrigatóriamente;
        ///             codLoja = null obrigatóriamente;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYY-MM-DD
        ///     </para>
        ///     
        ///     ### Filtros QueryParam ###
        ///     <para>
        ///         "periodo" (int) - Default = 1: retorna os períodos agregados;
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
        /// <param name="periodo">Período (DEFAULT = 1)</param>
        /// <param name="codLoja">Código das lojas separados por vírgula</param>
        /// <param name="primeiraData">Data inicial (YYYYMMDD)</param>
        /// <param name="ultimaData">Data final (YYYYMMDD)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/Relatorios/ReceitasTransacoesTicketMedio")]
        [SwaggerResponse("200", typeof(VendasIdentificadas))]
        [SwaggerResponse("200", typeof(VendasIdentificadasData))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage ReceitasTransacoesTicketMedio(int periodo = 1, string codLoja = "", string primeiraData = "", string ultimaData = "")
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

            if (string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData) && periodo <= 0)
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário inserir ao menos 1 filtro para a busca."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                if (periodo != null && periodo > 0)
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelo período com as datas informadas."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }

                if (!string.IsNullOrEmpty(codLoja))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "O filtro de loja só é possível com o período > 0."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (periodo != null && periodo > 0)
            {
                if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelas datas com o período informado."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (periodo == 0 && !string.IsNullOrEmpty(codLoja))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário informar o período para filtro de lojas."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("yhub-xapikey");
                string xApiKey = listParam["yhub-xapikey"];

                VendasIdentificadasData dataAgregado = new VendasIdentificadasData();
                VendasIdentificadas data = new VendasIdentificadas();

                RelatoriosPortalRestDAO dao = new RelatoriosPortalRestDAO(sNomeCliente, tokenAutenticacao);
                Tuple<VendasIdentificadasData, VendasIdentificadas> retorno = dao.ReceitasTransacoesTicketMedio(periodo, xApiKey, codLoja, primeiraData, ultimaData);

                dataAgregado = retorno.Item1;
                data = retorno.Item2;

                if (retorno != null)
                {
                    if (retorno.Item1.statusCode == 200 || retorno.Item2.statusCode == 200)
                    {

                        if (periodo == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, dataAgregado);
                        }

                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, data);
                        }
                    }

                    else
                    {
                        DadosLog dadosLog = new DadosLog
                        {
                            des_erro_tecnico = "Ocorreu um erro ao buscar as informações da YHUB no endpoint: ReceitasTransacoes" + retorno.ToJson()
                        };

                        Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "Ocorreu um erro ao buscar os dados."
                        });
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }
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
        ///     Consulta a relação de pessoas ativas no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint pessoa-distintas-vendas.
        ///     
        ///     <para>
        ///         Para cada tipo de filtro de entrada é retornado um objeto.
        ///         Fluxo 1
        ///             período > 0: Retorna as informações de usuários com as informações consolidadas por período;
        ///             codLoja: É possível combinar com o filtro de lojas;
        ///             primeiraData e ultimaData: devem ser vazias;
        ///         Fluxo 2
        ///             periodo = 0 obrigatóriamente;
        ///             codLoja = null obrigatóriamente;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYY-MM-DD
        ///     </para>
        ///     
        ///     ### Filtros QueryParam ###
        ///     <para>
        ///         "periodo" (int) - Default = 1: retorna os períodos agregados;
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
        /// <param name="periodo">Período (DEFAULT = 1)</param>
        /// <param name="codLoja">Código das lojas separados por vírgula</param>
        /// <param name="primeiraData">Data inicial (YYYYMMDD)</param>
        /// <param name="ultimaData">Data final (YYYYMMDD)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/Relatorios/PessoasAtivas")]
        [SwaggerResponse("200", typeof(PessoasAtivas))]
        [SwaggerResponse("200", typeof(PessoasAtivasData))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage PessoasAtivas(int periodo = 1, string codLoja = "", string primeiraData = "", string ultimaData = "")
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

            if (string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData) && periodo <= 0)
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário inserir ao menos 1 filtro para a busca."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                if (periodo != null && periodo > 0)
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelo período com as datas informadas."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }

                if (!string.IsNullOrEmpty(codLoja))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "O filtro de loja só é possível com o período > 0."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (periodo != null && periodo > 0)
            {
                if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelas datas com o período informado."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (periodo == 0 && !string.IsNullOrEmpty(codLoja))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário informar o período para filtro de lojas."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("yhub-xapikey");
                string xApiKey = listParam["yhub-xapikey"];

                PessoasAtivasData dataAgregado = new PessoasAtivasData();
                PessoasAtivas data = new PessoasAtivas();

                RelatoriosPortalRestDAO dao = new RelatoriosPortalRestDAO(sNomeCliente, tokenAutenticacao);
                Tuple<PessoasAtivasData, PessoasAtivas> retorno = dao.PessoasAtivas(periodo, xApiKey, codLoja, primeiraData, ultimaData);

                dataAgregado = retorno.Item1;
                data = retorno.Item2;

                if (retorno != null)
                {
                    if (retorno.Item1.statusCode == 200 || retorno.Item2.statusCode == 200)
                    {

                        if (periodo == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, dataAgregado);
                        }

                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, data);
                        }
                    }

                    else
                    {
                        DadosLog dadosLog = new DadosLog
                        {
                            des_erro_tecnico = "Ocorreu um erro ao buscar as informações da YHUB no endpoint: PessoasAtivas" + retorno.ToJson()
                        };

                        Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "Ocorreu um erro ao buscar os dados."
                        });
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }
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
        ///     Consulta a relação de frequencia de compra no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint ReceitasTransacoesTicketMedio.
        ///     
        ///     <para>
        ///         Para cada tipo de filtro de entrada é retornado um objeto.
        ///         Fluxo 1
        ///             período > 0: Retorna as informações de usuários com as informações consolidadas por período;
        ///             codLoja: É possível combinar com o filtro de lojas;
        ///             primeiraData e ultimaData: devem ser vazias;
        ///         Fluxo 2
        ///             periodo = 0 obrigatóriamente;
        ///             codLoja = null obrigatóriamente;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYYMMDD
        ///     </para>
        ///     
        ///     ### Filtros QueryParam ###
        ///     <para>
        ///         "periodo" (int) - Default = 1: retorna os períodos agregados;
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
        /// <param name="periodo">Período (DEFAULT = 1)</param>
        /// <param name="codLoja">Código das lojas separados por vírgula</param>
        /// <param name="primeiraData">Data inicial (YYYYMMDD)</param>
        /// <param name="ultimaData">Data final (YYYYMMDD)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/Relatorios/FrequenciaDeCompra")]
        [SwaggerResponse("200", typeof(FrequenciaGastoMedio))]
        [SwaggerResponse("200", typeof(FrequenciaGastoMedioData))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage FrequenciaDeCompra(int periodo = 1, string codLoja = "", string primeiraData = "", string ultimaData = "")
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

            if (string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData) && periodo <= 0)
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário inserir ao menos 1 filtro para a busca."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                if (periodo != null && periodo > 0)
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelo período com as datas informadas."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }

                if (!string.IsNullOrEmpty(codLoja))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "O filtro de loja só é possível com o período > 0."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (periodo != null && periodo > 0)
            {
                if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pelas datas com o período informado."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (periodo == 0 && !string.IsNullOrEmpty(codLoja))
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário informar o período para filtro de lojas."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("yhub-xapikey");
                string xApiKey = listParam["yhub-xapikey"];

                FrequenciaGastoMedioData dataAgregado = new FrequenciaGastoMedioData();
                FrequenciaGastoMedio data = new FrequenciaGastoMedio();

                RelatoriosPortalRestDAO dao = new RelatoriosPortalRestDAO(sNomeCliente, tokenAutenticacao);
                Tuple<FrequenciaGastoMedioData, FrequenciaGastoMedio> retorno = dao.FrequenciaDeCompra(periodo, xApiKey, codLoja, primeiraData, ultimaData);

                dataAgregado = retorno.Item1;
                data = retorno.Item2;

                if (retorno != null)
                {

                    if (retorno.Item1.statusCode == 200 || retorno.Item2.statusCode == 200)
                    {

                        if (periodo == 0)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, dataAgregado);
                        }

                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, data);
                        }
                    }

                    else
                    {
                        DadosLog dadosLog = new DadosLog
                        {
                            des_erro_tecnico = "Ocorreu um erro ao buscar as informações da YHUB no endpoint: FrequenciaGastoMedio" + retorno.ToJson()
                        };

                        Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "Ocorreu um erro ao buscar os dados."
                        });
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }

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
        ///     Consulta a relação de segmentos ou produtos mais vendidos no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint produtos-receita.
        ///     
        ///     <para>
        ///         Para cada tipo de filtro de entrada é retornado um objeto.
        ///         Fluxo 1
        ///             arvore = 1: Retorna as informações de segmentos mais vendidos por período;
        ///             codLoja: É possível combinar com o filtro de lojas;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYYMMDD;
        ///         Fluxo 2
        ///             arvore = 2: Retorna as informações de produtos mais vendidos por período;
        ///             codLoja = null obrigatóriamente;
        ///             primeiraData e ultimaData: devem ser informadas as 2 datas para análise no formato YYYYMMDD;
        ///     </para>
        ///     
        ///     ### Filtros QueryParam ###
        ///     <para>
        ///         "arvore" (int) - 1 ou 2 - Retorna ou produtos ou segmentos mais vendidos;
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
        /// <param name="arvore">1 - Segmento / 2 - Produto</param>
        /// <param name="codLoja">Código das lojas separados por vírgula</param>
        /// <param name="primeiraData">Data inicial (YYYYMMDD)</param>
        /// <param name="ultimaData">Data final (YYYYMMDD)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/Relatorios/ProdutosReceita")]
        [SwaggerResponse("200", typeof(SegmentoMaiVendidoAgregado))]
        [SwaggerResponse("200", typeof(ProdutosMaisVendidosAgregado))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage ProdutosReceita(string primeiraData, string ultimaData, int arvore, string codLoja = "")
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

            if (string.IsNullOrEmpty(primeiraData) && string.IsNullOrEmpty(ultimaData) && arvore <= 0)
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "É necessário inserir ao menos 1 filtro para a busca."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            if (!string.IsNullOrEmpty(primeiraData) && !string.IsNullOrEmpty(ultimaData))
            {
                if (arvore <= 0 || arvore > 2)
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "Não é possível buscar pela arvore de busca informada."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }

                if (!string.IsNullOrEmpty(codLoja))
                {
                    listaErros.errors.Add(new Erros
                    {
                        code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                        message = "O filtro de loja só é possível com o período > 0."
                    });
                    return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
                }
            }

            if (arvore == null || arvore <= 0 || arvore > 2)
            {
                listaErros.errors.Add(new Erros
                {
                    code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(),
                    message = "Não é possível buscar pela arvore de busca informada."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }


            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("yhub-xapikey");
                string xApiKey = listParam["yhub-xapikey"];

                SegmentoMaiVendidoAgregado segmento = new SegmentoMaiVendidoAgregado();
                ProdutosMaisVendidosAgregado produto = new ProdutosMaisVendidosAgregado();

                RelatoriosPortalRestDAO dao = new RelatoriosPortalRestDAO(sNomeCliente, tokenAutenticacao);
                Tuple<SegmentoMaiVendidoAgregado, ProdutosMaisVendidosAgregado> retorno = dao.ProdutosReceita(arvore, xApiKey, codLoja, primeiraData, ultimaData);

                segmento = retorno.Item1;
                produto = retorno.Item2;

                if (retorno != null)
                {

                    if (retorno.Item1.statusCode == 200 || retorno.Item2.statusCode == 200)
                    {

                        if (arvore == 1)
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, segmento);
                        }

                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK, produto);
                        }
                    }

                    else
                    {
                        DadosLog dadosLog = new DadosLog
                        {
                            des_erro_tecnico = "Ocorreu um erro ao buscar as informações da YHUB no endpoint: ProdutosReceita" + retorno.ToJson()
                        };

                        Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "Ocorreu um erro ao buscar os dados."
                        });
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }

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
        ///     Consulta a relação de gasto por grupo no varejo informado
        /// </summary>
        /// <remarks>
        ///     ### É necessário o Token do Cliente ###
        ///     ### Fluxo de utilização ###
        ///     A API de consulta no lambda as informações dos usuários no endpoint gasto-grupo.
        ///     
        ///     <para>
        ///         Para cada tipo de filtro de entrada é retornado um objeto.
        ///         Fluxo
        ///             codLoja: É possível combinar com o filtro de lojas;
        ///             primeiraData e ultimaData: obrigatórias para o período de análise;
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
        [Route("api/Relatorios/GastoPorGrupo")]
        [SwaggerResponse("200", typeof(GastoPorGrupoAgregado))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage GastoPorGrupo(string primeiraData, string ultimaData, string codLoja = "")
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
                    message = "É necessário inserir a data para pesquisa."
                });
                return Request.CreateResponse(HttpStatusCode.BadRequest, listaErros);
            }

            #endregion

            try
            {
                sNomeCliente = Request.Headers.GetValues("sNomeCliente").First().ToLower();
                tokenAutenticacao = Request.Headers.GetValues("tokenAutenticacao").First();

                ParametroDAO param = new ParametroDAO("Izio");
                Dictionary<string, string> listParam = new Dictionary<string, string>();

                listParam = param.ListarParametros("yhub-xapikey");
                string xApiKey = listParam["yhub-xapikey"];

                GastoPorGrupoAgregado dataAgregado = new GastoPorGrupoAgregado();

                RelatoriosPortalRestDAO dao = new RelatoriosPortalRestDAO(sNomeCliente, tokenAutenticacao);
                GastoPorGrupoAgregado retorno = dao.GastoPorGrupo(xApiKey, codLoja, primeiraData, ultimaData);

                if (retorno != null)
                {
                    if(retorno.statusCode == 200)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, retorno);
                    }
                    else
                    {
                        DadosLog dadosLog = new DadosLog
                        {
                            des_erro_tecnico = "Ocorreu um erro ao buscar as informações da YHUB no endpoint: GastoPorGrupo" + retorno.ToJson()
                        };

                        Log.InserirLogIzio(sNomeCliente, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "Ocorreu um erro ao buscar os dados."
                        });
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                    }
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

