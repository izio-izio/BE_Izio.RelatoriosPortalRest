﻿using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using TransacaoRest.Models;

namespace TransacaoIzioRest.DAO
{
    /// <summary>
    /// Classe para remover os registros de uma venda cancelada
    ///  - Remove o registra da venda na Viewizio_3
    ///  - Remove o registra da venda na tab_transacao/tab_transacao_itens
    ///  - Remove o registra da venda na tab_transacao_cpf/tab_transacao_itens_cpf
    /// </summary>
    public class TransacaoCanceladaDAO
    {
        #region Constantes Remover Venda Cancelada

        private string DadosNaoEncontrados = "Não foram encontrados registros";

        #endregion

        SqlServer sqlServer;
        string NomeClienteWs;
        public TransacaoCanceladaDAO(string sNomeCliente)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
        }

        /// <summary>
        /// Apaga os registros da venda cancela no Izio:
        ///  - Remove o registra da venda na Viewizio_3
        ///  - Remove o registra da venda na tab_transacao/tab_transacao_itens
        ///  - Remove o registra da venda na tab_transacao_cpf/tab_transacao_itens_cpf
        /// </summary>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <returns></returns>
        #region Apaga os registros da venda cancelada na base do Izio
        public ApiErrors ExcluirRegistrosCompraCancelada(DadosTransacaoCancelada objTransacao, string IpOrigem)
        {
            //Total de registros deletados
            Int32 totalRegistrosExcluidos = 0;

            ApiErrors retorno = new ApiErrors();

            try
            {
                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();

                #region Executa a execução dos registros da venda cancelada

                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                //Monta os parametros
                #region Parametros
                //Data da compra
                IDbDataParameter pdat_compra = sqlServer.Command.CreateParameter();
                pdat_compra.ParameterName = "@datacompra";
                pdat_compra.Value = objTransacao.dat_compra;
                sqlServer.Command.Parameters.Add(pdat_compra);

                IDbDataParameter pvalorcompra = sqlServer.Command.CreateParameter();
                pvalorcompra.ParameterName = "@valorcompra";
                pvalorcompra.Value = objTransacao.vlr_compra;
                sqlServer.Command.Parameters.Add(pvalorcompra);

                IDbDataParameter pcupom = sqlServer.Command.CreateParameter();
                pcupom.ParameterName = "@cupom";
                pcupom.Value = objTransacao.cupom;
                sqlServer.Command.Parameters.Add(pcupom);

                IDbDataParameter pcod_loja = sqlServer.Command.CreateParameter();
                pcod_loja.ParameterName = "@cod_loja";
                pcod_loja.Value = objTransacao.cod_loja;
                sqlServer.Command.Parameters.Add(pcod_loja);

                // **********************************************************************************
                // **********************************************************************************
                #endregion
                
                //Exclui os registros da compra ainda não processados na viewizio_3
                sqlServer.Command.CommandText = @"delete 
                                                  from 
                                                     viewizio_3 
                                                  where
                                                     datacompra = @datacompra and
                                                     valorcompra = @valorcompra and
                                                     cupom = @cupom and
                                                     cod_loja = @cod_loja ";

                //executa o delete e retorna o total de linhas afetatas
                totalRegistrosExcluidos += sqlServer.Command.ExecuteNonQuery();

                //Se não excluiu nenhum registros passa para as tabelas finais (tab_transacao/tab_transacao_itens e tab_transacao_cpf/tab_transacao_itens_cpf)
                if (totalRegistrosExcluidos == 0)
                {

                    //Exclui os registros da compra da tabela de compra identificada
                    sqlServer.Command.CommandText = @"delete tri
                                                  from 
                                                     tab_transacao_itens tri with(nolock),
                                                     tab_transacao trs with(nolock)
                                                  where
                                                     trs.dat_compra = CAST(@datacompra AS DATETIME2(0))  and
                                                     trs.vlr_compra = @valorcompra and
                                                     trs.cupom = @cupom and
                                                     trs.cod_loja = @cod_loja and
                                                     trs.cod_transacao = tri.cod_transacao ";

                    //executa o delete e retorna o total de linhas afetatas
                    totalRegistrosExcluidos += sqlServer.Command.ExecuteNonQuery();

                    //Se o total de registros for maior que zero, indica que a compra cancelada era identifica e agora exclui os registros do
                    //  cabeçalho da compra
                    if (totalRegistrosExcluidos > 0)
                    {
                        //Exclui os registros da compra da tabela de compra identificada
                        sqlServer.Command.CommandText = @"delete
                                                          from 
                                                             tab_transacao 
                                                          where
                                                             dat_compra = CAST(@datacompra AS DATETIME2(0)) and
                                                             vlr_compra = @valorcompra and
                                                             cupom = @cupom and
                                                             cod_loja = @cod_loja ";

                        //executa o delete e retorna o total de linhas afetatas
                        totalRegistrosExcluidos += sqlServer.Command.ExecuteNonQuery();
                    }
                    else
                    {
                        //Exclui os registros da compra não identificada
                        sqlServer.Command.CommandText = @"delete tri
                                                          from 
                                                             tab_transacao_itens_cpf tri with(nolock),
                                                             tab_transacao_cpf trs with(nolock)
                                                          where
                                                             trs.dat_compra = CAST(@datacompra AS DATETIME2(0)) and
                                                             trs.vlr_compra = @valorcompra and
                                                             trs.cupom = @cupom and
                                                             trs.cod_loja = @cod_loja and
                                                             trs.cod_tab_transacao_cpf = tri.cod_tab_transacao_cpf 

                                                          delete 
                                                          from 
                                                             tab_transacao_cpf
                                                          where
                                                             dat_compra = CAST(@datacompra AS DATETIME2(0)) and
                                                             vlr_compra = @valorcompra and
                                                             cupom = @cupom and
                                                             cod_loja = @cod_loja ";

                        //executa o delete e retorna o total de linhas afetatas
                        totalRegistrosExcluidos += sqlServer.Command.ExecuteNonQuery();
                    }
                }

                #endregion

                sqlServer.Commit();

                retorno.errors = new List<Erros>();

                //Se total de linhas afetadas for igual a zero, indica que não foi excluido nenhum registros
                if (totalRegistrosExcluidos == 0)
                {
                    //Seta a lista de erros com o erro
                    retorno.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = DadosNaoEncontrados + ", para exclusão da venda Cancelada." });
                }
            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
            finally
            {
                sqlServer.CloseConnection();
            }
            
            return retorno;

        }
        #endregion


        /// <summary>
        ///  - Remove o registra da venda na Viewizio_3
        /// </summary>
        /// <param name="dataProcessamento"></param>
        /// <returns></returns>
        #region Apaga os registros da viewizio3 para reprocessamento.
        public string ExcluirRegistrosIntermediarios(string dataProcessamento)
        {
            string retorno = "";
            try
            {
                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();

                #region Limpa ou Trunca a viewizio3 de acordo com o parâmetro enviado

                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                if (string.IsNullOrEmpty(dataProcessamento))
                {
                    //Trunca a table viewizio_3
                    sqlServer.Command.CommandText = @"truncate table
                                                     viewizio_3";                                          
                }
                else
                {
                    //Exclui os registros na viewizio_3 com base na data enviada
                    sqlServer.Command.CommandText = @"delete 
                                                      from 
                                                         viewizio_3 
                                                      where
                                                         datacompra between '" + dataProcessamento + "' and '" + dataProcessamento +" 23:59:59' " +
                                                     "select @@rowcount";

                }
     
                //executa o delete e retorna o total de linhas afetatas
                retorno = "Total de linhas Excluídas: " + (int)sqlServer.Command.ExecuteScalar();

                #endregion

                sqlServer.Commit();

            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();


                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return retorno;
        }
        #endregion


    }
}