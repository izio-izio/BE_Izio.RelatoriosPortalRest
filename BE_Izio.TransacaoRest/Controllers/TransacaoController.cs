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
        /// <param name="objPessoa">Objeto com codigo de identificacao do cliente (cod_pessoa) </param>
        /// <param name="tokenAutenticacao">Token de autorizacao para utilizacao da API</param>
        /// <returns></returns>
        // POST: api/Pessoa/Autenticar
        [HttpGet]
        [Route("api/Transacao/ConsultaUltimasCompras/{tokenAutenticacao}/{codigoPessoa}")]
        public HttpResponseMessage ConsultaUltimasCompras([FromUri]string tokenAutenticacao,[FromUri] string codigoPessoa)
        {
            string sNomeCliente = "";

            try
            {
                sNomeCliente = Utilidades.AutenticarTokenApiRest(tokenAutenticacao);

                List<DadosTransacao> ListaTransacao = new List<DadosTransacao>();

                //Executa metodo para realizar login
                DAO.TransacaoDAO consulta = new DAO.TransacaoDAO(sNomeCliente);
                ListaTransacao = consulta.ConsultaUltimasTransacao(Convert.ToInt64(codigoPessoa));

                return Request.CreateResponse(HttpStatusCode.Accepted, ListaTransacao);
            }
            catch (TransacaoRest.Exception.ApiException.ExceptionClienteSemCompras exClienteSemCompras)
            {
                HttpError error = new HttpError(exClienteSemCompras.Message);
                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.Unauthorized, error);
            }
            catch (Exception ex)
            {
                //Monta mensagem de erro padrao
                string mensagem = "Erro interno na consulta das ultimas compras, favor contactar o administrador.";
                HttpError error = new HttpError(mensagem);

                //Grava o erro na tabela de log (sis_log)
                Log.inserirLogException(sNomeCliente, ex, 0);

                //trocar o status code
                return Request.CreateResponse(HttpStatusCode.InternalServerError, error);

            }
        }
    }
}
