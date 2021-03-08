using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using TransacaoIzioRest.Models;

namespace TransacaoIzioRest.DAO
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
        /// Metodo retorna as compras do mês informado
        /// </summary>
        /// <returns></returns>
        #region Consulta Ultimas Compras

        public DadosConsultaTransacao ConsultaUltimasTransacao(long cod_pessoa,string anoMes)
        {
            DadosConsultaTransacao retornoConsulta = new DadosConsultaTransacao();

            try
            {
                //Abre a conexao com o banco da dados
                sqlServer.StartConnection();

                //Verifica se o usuario e a senha informado esta correto
                sqlServer.Command.CommandText = @"select distinct
                                                     trs.cod_transacao,
                                                     trs.cod_pessoa,
                                                     trs.dat_compra,
                                                     trs.vlr_compra,
                                                     trs.cod_loja, 
                                                     tlj.razao_social des_loja, 
                                                     trs.qtd_itens_compra,
                                                     trs.cupom ,
                                                     trs.vlr_total_desconto,
                                                     tlc.vlr_credito as vlr_credito_cashback,
                                                     tlc.dat_validade as dat_validade_cashback
                                                  from 
                                                     tab_transacao trs with(nolock) 
                                                  left join
                                                     tab_loja tlj with(nolock) on tlj.cod_loja = trs.cod_loja 
                                                  left join
                                                     tab_lancamento_credito_campanha tlc with(nolock) on tlc.cod_transacao = trs.cod_transacao
                                                  where 
                                                      trs.dat_compra between '" + anoMes+"01 00:00:01' and '" +anoMes + DateTime.DaysInMonth(Convert.ToInt32(anoMes.Substring(0,4)), Convert.ToInt32(anoMes.Substring(4, 2))).ToString() + " 23:59:59' and " +
                                                  "   trs.cod_pessoa = @cod_pessoa order by trs.dat_compra desc";

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

                if (sqlServer.Reader.HasRows)
                {
                    //Cria o payload de retorno
                    retornoConsulta.payload = new Payload();

                    retornoConsulta.payload.listaTransacao = new Izio.Biblioteca.ModuloClasse().PreencheClassePorDataReader<TransacaoIzioRest.Models.DadosTransacao>(sqlServer.Reader);
                }

            }
            catch (System.Exception ex)
            {
                if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                {
                    sqlServer.Reader.Close();
                }

                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();

                }
            }
                return retornoConsulta;
        }
        #endregion

        /// <summary>
        /// Metodo retorna os itens de uma compra
        /// </summary>
        /// <returns></returns>
        #region Consultas itens de uma compra
        public RetornoDadosItensTransacao ConsultaItensTransacao(long codigoTransacao)
        {
            RetornoDadosItensTransacao retornoConsulta = new RetornoDadosItensTransacao();

            try
            {
                //Abre a conexao com o banco da dados
                sqlServer.StartConnection();

                if (ConfigurationManager.AppSettings["clientesCRE"] != null && ConfigurationManager.AppSettings["clientesCRE"].ToString().ToUpper().Contains(NomeClienteWs.ToUpper()))
                {
                    sqlServer.Command.CommandText = @"select 
                                                     tri.cod_transacao,
                                                     tri.cod_produto cod_plu,
                                                     tri.cod_nsu cod_ean,
                                                     tri.des_produto des_produto,
                                                     round(tri.vlr_item_compra-(isnull(tri.vlr_desconto_item,0)),2) vlr_item_compra,
                                                     tri.qtd_item_compra qtd_item_compra,
                                                     tri.vlr_desconto_item,
                                                     sum(isnull(tlcce.vlr_credito, 0)) as vlr_credito
                                                  into 
                                                     #tmp_transacao
                                                  from 
                                                     tab_transacao_itens tri with(nolock)
                                                     left join tab_lancamento_credito_campanha_ean tlcce with(nolock) on (tri.cod_NSU = tlcce.cod_ean_produto and tri.cod_tab_transacao_itens = tlcce.cod_tab_transacao_itens)
                                                  where 
                                                     tri.cod_transacao =  @cod_transacao
                                                  group by tri.cod_transacao,
                                                           tri.cod_produto,
                                                           tri.cod_nsu,
                                                           tri.des_produto,
                                                           tri.vlr_item_compra,
                                                           tri.qtd_item_compra,
                                                           tri.vlr_desconto_item

                                                  
                                                  select
                                                     tmp.cod_transacao,
                                                     case when ltrim(rtrim(coalesce(tmp.cod_plu,tmp.cod_ean))) <> '' then ltrim(rtrim(coalesce(tmp.cod_plu,tmp.cod_ean))) else tmp.cod_ean end cod_plu,
                                                     case when ltrim(rtrim(coalesce(tmp.cod_ean,tmp.cod_plu))) <> '' then ltrim(rtrim(coalesce(tmp.cod_ean,tmp.cod_plu))) else tmp.cod_plu end cod_ean, 
                                                     tmp.des_produto des_produto,
                                                     tmp.vlr_item_compra,
                                                     qtd_item_compra, 
                                                     null img_produto,
                                                     vlr_desconto_item,
                                                     vlr_credito
                                                  from
                                                     #tmp_transacao tmp
                                                  order by 4
                                                  
                                                  drop table #tmp_transacao  ";
                }
                else
                {

                    //Verifica se o usuario e a senha informado esta correto
                    sqlServer.Command.CommandText = @"select 
                                                     tri.cod_transacao,
                                                     tri.cod_produto cod_plu,
                                                     tri.cod_nsu cod_ean,
                                                     tri.des_produto des_produto,
                                                     round((tri.vlr_item_compra * tri.qtd_item_compra-(isnull(tri.vlr_desconto_item,0))),2) vlr_item_compra,
                                                     tri.qtd_item_compra qtd_item_compra,
                                                     tri.vlr_desconto_item,
                                                     sum(isnull(tlcce.vlr_credito, 0)) as vlr_credito
                                                  into 
                                                     #tmp_transacao
                                                  from 
                                                     tab_transacao_itens tri with(nolock)
                                                     left join tab_lancamento_credito_campanha_ean tlcce with(nolock) on (tri.cod_NSU = tlcce.cod_ean_produto and tri.cod_tab_transacao_itens = tlcce.cod_tab_transacao_itens)
                                                  where 
                                                     tri.cod_transacao =  @cod_transacao
                                                  group by tri.cod_transacao,
                                                           tri.cod_produto,
                                                           tri.cod_nsu,
                                                           tri.des_produto,
                                                           tri.vlr_item_compra,
                                                           tri.qtd_item_compra,
                                                           tri.vlr_desconto_item
                                                   
                                                  select
                                                     tmp.cod_transacao,
                                                     case when ltrim(rtrim(coalesce(tmp.cod_plu,tmp.cod_ean))) <> '' then ltrim(rtrim(coalesce(tmp.cod_plu,tmp.cod_ean))) else tmp.cod_ean end cod_plu,
                                                     case when ltrim(rtrim(coalesce(tmp.cod_ean,tmp.cod_plu))) <> '' then ltrim(rtrim(coalesce(tmp.cod_ean,tmp.cod_plu))) else tmp.cod_plu end cod_ean, 
                                                     tmp.des_produto des_produto,
                                                     tmp.vlr_item_compra,
                                                     qtd_item_compra, 
                                                     null img_produto,
                                                     vlr_desconto_item,
                                                     vlr_credito
                                                  from
                                                     #tmp_transacao tmp
                                                  order by 4
                                                  
                                                  drop table #tmp_transacao  ";
                }

                // **********************************************************************************
                //Monta os parametros
                //Codigo da transacao
                IDbDataParameter pcod_transacao = sqlServer.Command.CreateParameter();
                pcod_transacao.ParameterName = "@cod_transacao";
                pcod_transacao.Value = codigoTransacao;
                sqlServer.Command.Parameters.Add(pcod_transacao);

                // **********************************************************************************
                // **********************************************************************************

                //Executa a consulta
                sqlServer.Command.CommandTimeout = 120;
                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                if (sqlServer.Reader.HasRows)
                {
                    //Cria o payload de retorno
                    retornoConsulta.payload = new PayloadItensTransacao();

                    retornoConsulta.payload.listaItensTransacao = new ModuloClasse().PreencheClassePorDataReader<DadosConsultaItensTransacao>(sqlServer.Reader);
                }
            }
            catch (System.Exception ex)
            {
                if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                {
                    sqlServer.Reader.Close();
                }

                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();

                }
            }
            return retornoConsulta;
        }

        #endregion

        /// <summary>
        /// Consultar os registros que foram enviados para Izio para a tabela intermédiária - Viewizio_3.
        /// </summary>
        /// <returns></returns>
        #region Consultar os registros que foram enviados para Izio para a tabela intermédiária - Viewizio_3.
        public RetornoDadosTermino ConsultarTransacoesCarregadaEmLote(DateTime dataCompra)
        {
            RetornoDadosTermino retornoConsulta = new RetornoDadosTermino();

            retornoConsulta.payload = new PayloadTermino();
            retornoConsulta.payload.dat_compra = dataCompra;

            try
            {
                //Abre a conexao com o banco da dados
                sqlServer.StartConnection();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                //Consulta a quantidade de registros importados na viewizio_3
                sqlServer.Command.CommandText = string.Format("select count(1) from viewizio_3 with(nolock) where datacompra between '{0}' and '{0} 23:59:59' and cod_loja > 0 ",dataCompra.ToString("yyyyMMdd"));
                retornoConsulta.payload.qtd_registros_importados = (int)sqlServer.Command.ExecuteScalar();

                //Consulta o valor vendido por loja e popula a lista de saida
                sqlServer.Command.CommandText = string.Format(@"select cod_loja,sum(valorcompra) vlr_vendas, count(1) qtd_vendas from (
                                                                   select
                                                                   cod_loja,
                                                                   cupom,
                                                                   ValorCompra,
                                                                   datacompra,
                                                                   QtdeItens
                                                                from
                                                                   viewizio_3 with(nolock)
                                                                where
                                                                   datacompra between '{0}' and '{0} 23:59:59' and cod_loja > 0
                                                                group by
                                                                   cod_loja,
                                                                   cupom,
                                                                   ValorCompra,
                                                                   datacompra,
                                                                   QtdeItens)  tab group by cod_loja ", dataCompra.ToString("yyyyMMdd")); 

                //Executa a consulta
                sqlServer.Reader = sqlServer.Command.ExecuteReader();
                retornoConsulta.payload.lst_lojas = new ModuloClasse().PreencheClassePorDataReader<ComprasLoja>(sqlServer.Reader);

                retornoConsulta.payload.qtd_vendas = retornoConsulta.payload.lst_lojas.AsEnumerable().Sum(x => x.qtd_vendas);
                retornoConsulta.payload.vlr_total_vendas = retornoConsulta.payload.lst_lojas.AsEnumerable().Sum(x => x.vlr_vendas);

            }
            catch (System.Exception ex)
            {
                if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                {
                    sqlServer.Reader.Close();
                }

                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();

                }
            }
            return retornoConsulta;
        }

        #endregion


        /// <summary>
        /// Metodo retorna total de desconto da pessoa por periodo
        /// </summary>
        /// <returns></returns>
        #region Consulta Total Descontos    

        public DadosConsultaDesconto ConsultaTotalDesconto(long cod_pessoa, int qtdMes)
        {
            DadosConsultaDesconto retornoConsulta = new DadosConsultaDesconto();

            try
            {
                //Abre a conexao com o banco da dados
                sqlServer.StartConnection();

                //Verifica se o usuario e a senha informado esta correto
                sqlServer.Command.CommandText = @"SELECT tp.cod_pessoa,
                                                  		COUNT(DISTINCT tt.cod_transacao) as qtd_transacao,
                                                         SUM(tti.vlr_desconto_item) AS vlr_total_desconto
                                                  FROM dbo.tab_transacao AS tt WITH (NOLOCK)
                                                      INNER JOIN dbo.tab_transacao_itens AS tti
                                                          ON tti.cod_transacao = tt.cod_transacao
                                                      INNER JOIN dbo.tab_pessoa AS tp
                                                          ON tp.cod_pessoa = tt.cod_pessoa
                                                  WHERE tt.dat_compra BETWEEN DATEADD(MONTH, @qtdmes, GETDATE()) AND GETDATE() AND tp.cod_pessoa = @cod_pessoa GROUP BY tp.cod_pessoa";

                // **********************************************************************************
                //Monta os parametros
                //Codigo da Pessoa
                IDbDataParameter pcod_cpf = sqlServer.Command.CreateParameter();
                pcod_cpf.ParameterName = "@cod_pessoa";
                pcod_cpf.Value = cod_pessoa;
                sqlServer.Command.Parameters.Add(pcod_cpf);

                sqlServer.Command.Parameters.AddWithValue("@qtdmes", qtdMes * -1);
                // **********************************************************************************
                // **********************************************************************************

                //Executa a consulta
                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                if (sqlServer.Reader.HasRows)
                {
                    //Cria o payload de retorno
                   //retornoConsulta.payload = new PayloadTotalDesconto();

                    retornoConsulta.payload = new Izio.Biblioteca.ModuloClasse().PreencheClassePorDataReader<TransacaoIzioRest.Models.DadosConsultaTotalDesconto>(sqlServer.Reader);
                }

            }
            catch (System.Exception ex)
            {
                if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                {
                    sqlServer.Reader.Close();
                }

                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
            finally
            {
                if (sqlServer != null)
                {
                    if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                    {
                        sqlServer.Reader.Close();
                        sqlServer.Reader.Dispose();
                    }

                    sqlServer.CloseConnection();

                }
            }
            return retornoConsulta;
        }
        #endregion


    }
}