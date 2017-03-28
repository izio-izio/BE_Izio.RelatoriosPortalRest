using Izio.Biblioteca;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace TransacaoRest.DAO
{
    public class TransacaoDAO
    {
        SqlServer sqlServer;
        string NomeClienteWs;
        public TransacaoDAO(string sNomeCliente)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
        }
        

        /// <summary>
        /// Metodo retorna as ultimos 6 meses de compra do cliente
        /// </summary>
        /// <returns></returns>
        public List<TransacaoRest.Models.DadosTransacao> ConsultaUltimasTransacao(long cod_pessoa)
        {
            List<TransacaoRest.Models.DadosTransacao> listaTransacao = new List<Models.DadosTransacao>();

            try
            {
                //Abre a conexao com o banco da dados
                sqlServer.StartConnection();

                //Verifica se o usuario e a senha informado esta correto
                sqlServer.Command.CommandText = @"select cod_transacao,cod_pessoa,dat_compra,vlr_compra,cod_loja,qtd_itens_compra,cupom from tab_transacao with(nolock) where dat_compra >=  dateadd(mm,-6,cast(getdate() as date)) and cod_pessoa = @cod_pessoa order by dat_compra desc";

                // **********************************************************************************
                //Monta os parametros
                //Codigo da Pessoa
                IDbDataParameter pcod_cpf = sqlServer.Command.CreateParameter();
                pcod_cpf.ParameterName = "@cod_pessoa";
                pcod_cpf.Value = cod_pessoa;
                sqlServer.Command.Parameters.Add(pcod_cpf);

                // **********************************************************************************
                // **********************************************************************************

                //Executa a consulta
                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                listaTransacao = new Izio.Biblioteca.ModuloClasse().PreencheClassePorDataReader<TransacaoRest.Models.DadosTransacao>(sqlServer.Reader);

                if (listaTransacao != null && listaTransacao.Count > 0)
                {
                    return listaTransacao;
                }
                else
                {
                    throw new TransacaoRest.Exception.ApiException.ExceptionClienteSemCompras();
                }

            }
            catch (TransacaoRest.Exception.ApiException.ExceptionClienteSemCompras)
            {
                throw;
            }
            finally
            {
                sqlServer.CloseConnection();
            }
        }

    }
}