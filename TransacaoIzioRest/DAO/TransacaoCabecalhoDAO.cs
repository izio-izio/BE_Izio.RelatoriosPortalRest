using FastMember;
using Izio.Biblioteca;
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

        /// <summary>
        /// Construtor da classe TransacaoCabecalhoDAO
        /// </summary>
        /// <param name="sNomeCliente"></param>
        public TransacaoCabecalhoDAO(string sNomeCliente)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
        }

        /// <summary>
        /// Inserção no banco de dado da transação cabeçalho
        /// </summary>
        /// <param name="dadosTransacaoCabecalho"></param>
        /// <returns></returns>
        public List<DadosTransacaoCabecalho> CadastrarTransacaoCabecalho(DadosTransacaoCabecalho dadosTransacaoCabecalho)
        {
            try
            {
                sqlServer.StartConnection();

                sqlServer.Command.CommandType = CommandType.Text;

                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.Parameters.AddWithValue("@cod_cpf", dadosTransacaoCabecalho.cod_cpf);
                sqlServer.Command.Parameters.AddWithValue("@cupom", dadosTransacaoCabecalho.cupom);
                sqlServer.Command.Parameters.AddWithValue("@cod_loja", dadosTransacaoCabecalho.cod_loja);
                sqlServer.Command.Parameters.AddWithValue("@dat_compra", dadosTransacaoCabecalho.dat_compra);
                sqlServer.Command.Parameters.AddWithValue("@vlr_compra", dadosTransacaoCabecalho.vlr_compra);
                sqlServer.Command.Parameters.AddWithValue("@qtd_itens_compra", dadosTransacaoCabecalho.qtd_itens_compra);
                sqlServer.Command.Parameters.AddWithValue("@dat_cadastro", dadosTransacaoCabecalho.dat_cadastro);


                sqlServer.Command.CommandText = $@"INSERT dbo.tab_transacao_cabecalho
                                                   (
                                                       --cod_transacao_cabecalho - this column value is auto-generated
                                                       cod_cpf,
                                                       cupom,
                                                       cod_loja,
                                                       dat_compra,
                                                       vlr_compra,
                                                       qtd_itens_compra,
                                                       dat_cadastro
                                                   )
                                                   VALUES
                                                   (
                                                       -- cod_transacao_cabecalho - int
                                                       @cod_cpf, -- cod_cpf - varchar
                                                       @cupom, -- cupom - varchar
                                                       @cod_loja, -- cod_loja - int
                                                       @dat_compra, -- dat_compra - datetime
                                                       @vlr_compra, -- vlr_compra - decimal
                                                       @qtd_itens_compra, -- qtd_itens_compra - int
                                                       @dat_cadastro -- dat_cadastro - datetime
                                                   );
                                                   SELECT @@IDENTITY;";

                dadosTransacaoCabecalho.cod_transacao_cabecalho = Convert.ToInt32(sqlServer.Command.ExecuteScalar());

                List<DadosTransacaoCabecalho> listaTransacaoCabecalhos = new List<DadosTransacaoCabecalho>
                {
                    dadosTransacaoCabecalho
                };

                return listaTransacaoCabecalhos;
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

        /// <summary>
        /// Consulta no banco de dados com os parâmetros opcionais
        /// </summary>
        /// <param name="codCpf"></param>
        /// <param name="dataProcessamento"></param>
        /// <returns></returns>
        public List<DadosTransacaoCabecalho> ConsultarTransacaoCabecalho(string codCpf, string dataProcessamento)
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

        /// <summary>
        /// Alteração parcial no banco de dado da transação cabeçalho
        /// </summary>
        /// <param name="dadosTransacaoCabecalhoPatch"></param>
        /// <returns></returns>
        public DadosTransacaoCabecalhoPatch AtualizarTransacaoCabecalho(DadosTransacaoCabecalhoPatch dadosTransacaoCabecalhoPatch)
        {
            try
            {
                sqlServer.StartConnection();

                string set = "";
                string where = "";

                #region Parâmetros Patch
                if (dadosTransacaoCabecalhoPatch.cod_transacao_cabecalho != 0)
                {
                    where = $"dbo.tab_transacao_cabecalho.cod_transacao_cabecalho = {dadosTransacaoCabecalhoPatch.cod_transacao_cabecalho}";
                }

                if (!string.IsNullOrEmpty(dadosTransacaoCabecalhoPatch.cod_cpf))
                {
                    set += $" dbo.tab_transacao_cabecalho.cod_cpf = '{dadosTransacaoCabecalhoPatch.cod_cpf}',";
                }

                if (!string.IsNullOrEmpty(dadosTransacaoCabecalhoPatch.cupom))
                {
                    set += $" dbo.tab_transacao_cabecalho.cupom = '{dadosTransacaoCabecalhoPatch.cupom}',";
                }

                if (dadosTransacaoCabecalhoPatch.cod_loja != null)
                {
                    set += $" dbo.tab_transacao_cabecalho.cod_loja = {dadosTransacaoCabecalhoPatch.cod_loja},";
                }

                if (dadosTransacaoCabecalhoPatch.vlr_compra != null)
                {
                    set += $" dbo.tab_transacao_cabecalho.vlr_compra = '{dadosTransacaoCabecalhoPatch.vlr_compra}',";
                }

                if (dadosTransacaoCabecalhoPatch.qtd_itens_compra != null)
                {
                    set += $" dbo.tab_transacao_cabecalho.qtd_itens_compra = {dadosTransacaoCabecalhoPatch.qtd_itens_compra},";
                }

                if (dadosTransacaoCabecalhoPatch.dat_compra != null)
                {
                    set += $" dbo.tab_transacao_cabecalho.dat_cadastro = {dadosTransacaoCabecalhoPatch.dat_compra},";
                }

                if (set.Contains(","))
                {
                    set = set.Remove(set.LastIndexOf(","), 1);
                }
                #endregion

                sqlServer.Command.CommandType = CommandType.Text;

                sqlServer.Command.CommandText = $@"UPDATE dbo.tab_transacao_cabecalho
                                                   SET 
                                                       {set}
                                                   WHERE {where};";

                sqlServer.Command.ExecuteNonQuery();

                return dadosTransacaoCabecalhoPatch;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    sqlServer.CloseConnection();
                }
            }
        }

        /// <summary>
        /// Realiza a exclusão da Transação Cabeçalho
        /// </summary>
        /// <param name="codTransacaoCabecalho"></param>
        /// <returns></returns>
        public void DeletarTransacaoCabecalho(int codTransacaoCabecalho)
        {
            try
            {
                sqlServer.StartConnection();

                // 20 minutos para o timeout
                sqlServer.Command.CommandTimeout = 1200;

                sqlServer.Command.CommandType = CommandType.Text;

                sqlServer.Command.CommandText = $@"DELETE FROM dbo.tab_transacao_cabecalho
                                                   WHERE dbo.tab_transacao_cabecalho.cod_transacao_cabecalho = {codTransacaoCabecalho};";

                sqlServer.Command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    sqlServer.CloseConnection();
                }
            }
        }

        /// <summary>
        /// Inserção no banco de dados da lista de transações cabeçalhos
        /// </summary>
        /// <param name="listaTransacoesCabecalhos"></param>
        /// <returns></returns>
        public void ImportarLoteTransacaoCabecalho(List<DadosTransacaoCabecalho> listaTransacoesCabecalhos)
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
                    var reader = ObjectReader.Create(listaTransacoesCabecalhos,
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

                throw;
            }
            finally
            {
                sqlServer.CloseConnection();
            }
        }
    }
}