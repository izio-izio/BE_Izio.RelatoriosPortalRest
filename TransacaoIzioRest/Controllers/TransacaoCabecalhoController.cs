using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text.RegularExpressions;
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
        /// Cadastrar uma ou mais Transações Cabeçalhos
        /// </summary>
        /// <param name="dadosTransacaoCabecalho">Lista com os dados da transação cabeçalho</param>        
        /// <response code="400">Bad request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoCabecalho")]
        [SwaggerResponse("200", typeof(RetornoDadosTransacaoCabecalho))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage CadastrarTransacaoCabecalho([FromBody] DadosTransacaoCabecalho dadosTransacaoCabecalho)
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
                    if (dadosTransacaoCabecalho == null)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "Objeto com a transação cabeçalho precisa estar formatado e não pode ser nulo."
                            });
                    }

                    if (string.IsNullOrEmpty(dadosTransacaoCabecalho.cod_cpf))
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "O campo 'cod_cpf' não pode ser nulo ou vazio."
                            });
                    }

                    if (string.IsNullOrEmpty(dadosTransacaoCabecalho.cupom))
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "O campo 'cupom' não pode ser nulo ou vazio."
                            });
                    }

                    if (dadosTransacaoCabecalho.cod_loja == 0)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "O campo 'cod_loja' não pode ser nulo e precisa de um valor maior que 0."
                            });
                    }

                    if (dadosTransacaoCabecalho.vlr_compra == 0)
                    {
                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "O campo 'vlr_compra' não pode ser nulo e precisa de um valor maior que 0."
                        });
                    }

                    if (dadosTransacaoCabecalho.qtd_itens_compra == 0)
                    {
                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "O campo 'qtd_itens_compra' não pode ser nulo e precisa de um valor maior que 0."
                        });
                    }

                    if (dadosTransacaoCabecalho.dat_compra == DateTime.MinValue)
                    {
                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "O campo 'dat_compra' não pode ser nulo ou vazio."
                        });
                    }

                    if (dadosTransacaoCabecalho.dat_cadastro == DateTime.MinValue)
                    {
                        listaErros.errors.Add(new Erros
                        {
                            code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                            message = "O campo 'dat_cadastro' não pode ser nulo ou vazio."
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
                        
                        RetornoDadosTransacaoCabecalho retornoDadosTransacaoCabecalho = new RetornoDadosTransacaoCabecalho
                        {
                            payload = transacaoCabecalhoDAO.CadastrarTransacaoCabecalho(dadosTransacaoCabecalho)
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
                        message = "Não foi possível cadastrar a transação do cabeçalho. Por favor, tente novamente ou entre em contato com o administrador."
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

        /// <summary>
        /// Consultar uma ou mais Transações Cabeçalhos
        /// </summary>
        /// <param name="codCpf">Cpf do cliente</param>
        /// <param name="dataProcessamento">Data para o processamento (yyyyMMdd)</param>
        /// <returns></returns>
        [HttpGet, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoCabecalho")]
        [SwaggerResponse("200", typeof(RetornoDadosTransacaoCabecalho))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage ConsultarTransacaoCabecalho([FromUri] string codCpf = "", [FromUri] string dataProcessamento = "")
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
                    if (!string.IsNullOrEmpty(dataProcessamento))
                    {
                        // Regex para validação da data (yyyymmdd)
                        var regex = new Regex(@"([12]\d{3}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01]))");
                        var validacaoData = regex.IsMatch(dataProcessamento);

                        if (!validacaoData)
                        {
                            listaErros.errors.Add(new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "O campo 'dataProcessamento' precisa estar no formato yyyymmdd."
                            });
                        }
                    }
                    #endregion

                    #region Realiza a busca no banco de dados e retorna o resultado
                    if (!string.IsNullOrEmpty(sNomeCliente))
                    {
                        TransacaoCabecalhoDAO transacaoCabecalhoDAO = new TransacaoCabecalhoDAO(sNomeCliente);

                        RetornoDadosTransacaoCabecalho retornoDadosTransacaoCabecalho = new RetornoDadosTransacaoCabecalho
                        {
                            payload = transacaoCabecalhoDAO.ConsultarTransacaoCabecalho(codCpf, dataProcessamento)
                        };

                        if (retornoDadosTransacaoCabecalho.payload == null || retornoDadosTransacaoCabecalho.payload.Count == 0)
                        {
                            listaErros.errors.Add(
                                new Erros
                                {
                                    code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                    message = "Não foi encontrado transações cabeçalhos com o domínio solicitado."
                                });

                            return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
                        }

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
                        message = "Não foi possível consultar as transações do cabeçalho. Por favor, tente novamente ou entre em contato com o administrador."
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

        /// <summary>
        /// Atualizar parcialmente a Transação Cabeçalho
        /// </summary>
        /// <param name="dadosTransacaoCabecalhoPatch">Lista com os dados para atualização da transação cabeçalho</param>        
        /// <response code="400">Bad request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPatch, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoCabecalho")]
        [SwaggerResponse("200", typeof(RetornoPatchTransacaoCabecalho))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage AtualizarTransacaoCabecalho([FromBody] DadosTransacaoCabecalhoPatch dadosTransacaoCabecalhoPatch)
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
                    if (dadosTransacaoCabecalhoPatch == null)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "Objeto com a transação cabeçalho precisa estar formatado e não pode ser nulo."
                            });
                    }

                    if (dadosTransacaoCabecalhoPatch.cod_transacao_cabecalho == 0)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "O campo 'cod_transacao_cabecalho' não pode ser nulo e precisa de um valor maior que 0."
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

                        RetornoPatchTransacaoCabecalho retornoPatchTransacaoCabecalho = new RetornoPatchTransacaoCabecalho
                        {
                            payload = transacaoCabecalhoDAO.AtualizarTransacaoCabecalho(dadosTransacaoCabecalhoPatch)
                        };

                        return Request.CreateResponse(HttpStatusCode.OK, retornoPatchTransacaoCabecalho);
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
                        message = "Não foi possível atualizar a transação do cabeçalho. Por favor, tente novamente ou entre em contato com o administrador."
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

        /// <summary>
        /// Deletar uma Transação Cabeçalho
        /// </summary>
        /// <param name="codTransacaoCabecalho">Código da Transação Cabeçalho</param>        
        /// <response code="400">Bad request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete, Utilidades.ValidaTokenAutenticacao]
        [Route("api/TransacaoCabecalho")]
        [SwaggerResponse("200", typeof(ApiSuccess))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage DeletarTransacaoCabecalho([FromUri] int codTransacaoCabecalho)
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
                    if (codTransacaoCabecalho == 0)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "O campo 'codTransacaoCabecalho' precisa de um valor maior que 0."
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
                        transacaoCabecalhoDAO.DeletarTransacaoCabecalho(codTransacaoCabecalho);

                        ApiSuccess retornoTransacaoCabecalho = new ApiSuccess()
                        {
                            payload = new Sucesso
                            {
                                code = "200",
                                message = "A Transação Cabeçalho " + codTransacaoCabecalho + " foi excluida com sucesso."
                            }
                        };

                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new ObjectContent<ApiSuccess>(retornoTransacaoCabecalho, new JsonMediaTypeFormatter())
                        };
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
                        message = "Não foi possível excluir a transação do cabeçalho. Por favor, tente novamente ou entre em contato com o administrador."
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

        /// <summary>
        /// Realiza a importação em lote das transações cabeçalhos
        /// </summary>
        /// <param name="listaTransacoesCabecalhos">Lista com os dados das transações cabeçalhos</param>        
        /// <remarks>
        /// Método para importar as vendas em lote de 1000 em 1000 registros
        /// Cadastrar uma ou mais Transações Cabeçalhos
        /// </remarks>
        /// <response code="400">Bad request</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost, Utilidades.ValidaTokenAutenticacao]
        [Route("api/ImportarLoteTransacaoCabecalho")]
        [SwaggerResponse("200", typeof(RetornoDadosTransacaoCabecalho))]
        [SwaggerResponse("500", typeof(ApiErrors))]
        [SwaggerResponse("401", typeof(ApiErrors))]
        public HttpResponseMessage ImportarLoteTransacaoCabecalho([FromBody] List<DadosTransacaoCabecalho> listaTransacoesCabecalhos)
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
                    if (listaTransacoesCabecalhos == null || listaTransacoesCabecalhos.Count == 0)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "Objeto com as transações cabeçalhos está vazio, impossível realizar o processamento."
                            });
                    }

                    if (listaTransacoesCabecalhos.Count > 1000)
                    {
                        listaErros.errors.Add(
                            new Erros
                            {
                                code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                                message = "Objeto com as transações cabeçalhos só pode ter 1000 registros por lote."
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
                        transacaoCabecalhoDAO.ImportarLoteTransacaoCabecalho(listaTransacoesCabecalhos);

                        RetornoDadosTransacaoCabecalho retornoDadosTransacaoCabecalho = new RetornoDadosTransacaoCabecalho
                        {
                            payload = listaTransacoesCabecalhos
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
                        message = "Não foi possível cadastrar o lote das transações cabeçalhos. Por favor, tente novamente ou entre em contato com o administrador."
                    });

                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }
    }
}
