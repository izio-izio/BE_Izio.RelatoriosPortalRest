using FastMember;
using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using TransacaoIzioRest.Models;

namespace TransacaoRest.DAO
{
    public class TransacaoCabecalhoDAO
    {
        readonly SqlServer sqlServer;
        readonly string NomeClienteWs;

        public TransacaoCabecalhoDAO(string sNomeCliente)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
        }

        /// <summary>
        /// Inserção no banco de dados a lista de transações cabeçalhos
        /// </summary>
        /// <param name="listaTransacaoCabecalhos"></param>
        /// <returns></returns>
        public void CadastrarTransacaoCabecalho(List<DadosTransacaoCabecalho> listaTransacaoCabecalhos)
        {
            try
            {
                sqlServer.StartConnection();
                sqlServer.BeginTransaction();

                #region Bulk Insert da lista
                using (var bcp = new SqlBulkCopy
                            (
                            //Para utilizar o controle de transacao
                            sqlServer.Command.Connection,
                            SqlBulkCopyOptions.TableLock |
                            SqlBulkCopyOptions.FireTriggers,
                            sqlServer.Trans
                            ))
                using (
                    var reader = ObjectReader.Create(listaTransacaoCabecalhos,
                    "cod_transacao_cabecalho",
                    "cod_cpf",
                    "cupom",
                    "cod_loja",
                    "dat_compra",
                    "vlr_compra",
                    "qtd_itens_compra",
                    "dat_cadastro"))
                {
                    bcp.BulkCopyTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 600;
                    bcp.DestinationTableName = "tab_transacao_cabecalho";
                    bcp.WriteToServer(reader);
                }
                #endregion

                sqlServer.Commit();
            }
            catch (Exception ex)
            {
                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog
                {
                    des_erro_tecnico = ex.ToString()
                };

                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
            }
            finally
            {
                sqlServer.CloseConnection();
            }
        }
    }
}