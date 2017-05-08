namespace TransacaoRest.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using Izio.Biblioteca;
    using System.Collections.Generic;
    using Models;

    public class TransacaoController : ApiController
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
        [Route("api/Transacao/ConsultaUltimasCompras/{tokenAutenticacao}/{codigoPessoa}/{anoMes}")]
        public HttpResponseMessage ConsultaUltimasCompras([FromUri]string tokenAutenticacao,[FromUri] string codigoPessoa, [FromUri] string anoMes)
        {
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoDadosTransacao retornoConsulta = new RetornoDadosTransacao();

            //Objeto para processamento local da API
            DadosConsultaTransacao dadosConsulta = new DadosConsultaTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ListaErros listaErros = new ListaErros();

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
                    return Request.CreateResponse(HttpStatusCode.OK, listaErros);
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

                //trocar o status code
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
        [Route("api/Transacao/ConsultaItensCompra/{tokenAutenticacao}/{codigoTransacao}")]
        public HttpResponseMessage ConsultaItensCompra([FromUri]string tokenAutenticacao, [FromUri] string codigoTransacao)
        {
            string sNomeCliente = "";

            //Objeto de retorno do metodo com os publicos cadastrados na campanha
            RetornoDadosItensTransacao retornoConsulta = new RetornoDadosItensTransacao();

            //Objeto para processamento local da API
            DadosConsultaItensTransacao dadosConsulta = new DadosConsultaItensTransacao();

            //Objeto de retorno contendo os erros da execução da API
            ListaErros listaErros = new ListaErros();

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
                    return Request.CreateResponse(HttpStatusCode.OK, listaErros);
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

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, listaErros);
            }
        }

    }
}
