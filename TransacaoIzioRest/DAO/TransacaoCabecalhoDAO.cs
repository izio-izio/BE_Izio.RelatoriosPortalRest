using FastMember;
using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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

        /// <summary>
        /// Faz a consulta no banco de dados com os parâmetros opcionais
        /// </summary>
        /// <param name="codCpf"></param>
        /// <param name="dataProcessamento"></param>
        /// <returns></returns>
        public List<DadosTransacaoCabecalho> ConsultaTransacaoCabecalho(string codCpf, string dataProcessamento)
        {
            #region Monta a condição da query com os parâmetros
            string where = codCpf != "" ? $" WHERE ttc.cod_cpf = '{codCpf}'" : "";
            string and = (codCpf != "" && dataProcessamento != "") ? $" AND ttc.dat_compra BETWEEN '{dataProcessamento}' AND '{dataProcessamento} 23:59:59'" : "";

            // Caso o parâmetro codCpf seja vazio e o cupom não seja vazio, muda o comando sWhere para buscar pelo cupom ao invés do código do cpf
            if (string.IsNullOrEmpty(where) && dataProcessamento != "")
            {
                where = $" WHERE ttc.dat_compra BETWEEN '{dataProcessamento}' AND '{dataProcessamento} 23:59:59'";
            }
            #endregion

            try
            {
                sqlServer.StartConnection();

                // 20 minutos para o timeout
                sqlServer.Command.CommandTimeout = 1200;

                sqlServer.Command.CommandType = CommandType.Text;

                sqlServer.Command.CommandText = @"SELECT * 
                                                  FROM dbo.tab_transacao_cabecalho ttc WITH (NOLOCK)" 
                                                  + where + and;


                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                List<DadosTransacaoCabecalho> dadosTransacaoCabecalhos = new ModuloClasse().PreencheClassePorDataReader<DadosTransacaoCabecalho>(sqlServer.Reader);

                return dadosTransacaoCabecalhos;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();
                }
            }
        }
    }
}