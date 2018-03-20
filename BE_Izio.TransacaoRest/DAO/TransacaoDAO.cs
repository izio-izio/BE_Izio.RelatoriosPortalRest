﻿using Izio.Biblioteca;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using TransacaoIzioRest.Models;

namespace TransacaoIzioRest.DAO
{
    public class TransacaoDAO
    {

        #region Constantes Credito CPF

        private string DadosNaoEncontrados = "Não foram encontrados registros";
        private string DadosNaoEncontradosItens = "Não foram encontrados registros";
        private string CodigoCampanhaInvalido = "Código da campanha não é valido.";
        private string CampanhaInvalida = "Campanha não está mais válida";
        private string ErroBancoDeDados = "Não foi possível realizar consulta das transações";
        private string ErroBancoDeDadosItens = "Não foi possível realizar consulta dos itens da venda";
        private string TipoCampanhaInvalido = "Campanha informada não é gatilho por ticket";

        #endregion


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
                sqlServer.Command.CommandText = @"select trs.cod_transacao,trs.cod_pessoa,trs.dat_compra,trs.vlr_compra,trs.cod_loja, tlj.razao_social des_loja, trs.qtd_itens_compra,trs.cupom 
                                                  from 
                                                     tab_transacao trs with(nolock) 
                                                  left join
                                                     tab_loja tlj with(nolock) on tlj.cod_loja = trs.cod_loja 
                                                  where dat_compra between '" + anoMes+"01 00:00:01' and '" +anoMes + DateTime.DaysInMonth(Convert.ToInt32(anoMes.Substring(0,4)), Convert.ToInt32(anoMes.Substring(4, 2))).ToString() + " 23:59:59' and " +
                                                  "     cod_pessoa = @cod_pessoa order by dat_compra desc";

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
                else
                {
                    if (retornoConsulta.errors == null)
                    {
                        retornoConsulta.errors = new List<ErrosConsultaTransacao>();
                    }

                    retornoConsulta.errors.Add(new ErrosConsultaTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = DadosNaoEncontrados + "." });
                }
            }
            catch (System.Exception ex)
            {
                if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                {
                    sqlServer.Reader.Close();
                }

                sqlServer.Rollback();

                if (!string.IsNullOrEmpty(sqlServer.Command.CommandText))
                {
                    //Salva o command text da requisição
                    Log.inserirLogException(NomeClienteWs, new System.Exception(ErroBancoDeDados + ex.Message, new System.Exception(sqlServer.Command.CommandText)), 0);
                }  
                else
                {
                    //Salva os parametros
                    Log.inserirLogException(NomeClienteWs, new System.Exception(ErroBancoDeDados + ex.Message, new System.Exception("Cod. Pessoa: " + cod_pessoa.ToString() + " | Ano Mes: " + anoMes)), 0);
                }

                if (retornoConsulta.errors == null)
                {
                    retornoConsulta.errors = new List<ErrosConsultaTransacao>();
                }

                //Adiciona o erro de negocio
                retornoConsulta.errors.Add(new ErrosConsultaTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDados + ". Favor contactar o Administrador." });
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
        public DadosConsultaItensTransacao ConsultaItensTransacao(long codigoTransacao)
        {
            DadosConsultaItensTransacao retornoConsulta = new DadosConsultaItensTransacao();

            try
            {
                //Abre a conexao com o banco da dados
                sqlServer.StartConnection();

                //Verifica se o usuario e a senha informado esta correto
                sqlServer.Command.CommandText = @"select 
                                                     tri.cod_transacao,
                                                     tri.cod_produto cod_plu,
                                                     tri.cod_nsu cod_ean,
                                                     tri.des_produto des_produto,
                                                     round((tri.vlr_item_compra * tri.qtd_item_compra-(isnull(tri.vlr_desconto_item,0))),2) vlr_item_compra,
                                                     tri.qtd_item_compra qtd_item_compra,
                                                     tri.vlr_desconto_item
                                                  into 
                                                     #tmp_transacao
                                                  from 
                                                     tab_transacao_itens tri with(nolock)
                                                  where 
                                                     tri.cod_transacao =  @cod_transacao
                                                  
                                                  select
                                                     tmp.cod_transacao,
                                                     case when ltrim(rtrim(coalesce(tmp.cod_plu,tmp.cod_ean))) <> '' then ltrim(rtrim(coalesce(tmp.cod_plu,tmp.cod_ean))) else tmp.cod_ean end cod_plu,
                                                     case when ltrim(rtrim(coalesce(tmp.cod_ean,tmp.cod_plu))) <> '' then ltrim(rtrim(coalesce(tmp.cod_ean,tmp.cod_plu))) else tmp.cod_plu end cod_ean, 
                                                     tmp.des_produto des_produto,
                                                     tmp.vlr_item_compra,
                                                     qtd_item_compra, 
                                                     null img_produto,
                                                     vlr_desconto_item
                                                  from
                                                     #tmp_transacao tmp
                                                  order by 4
                                                  
                                                  drop table #tmp_transacao  ";

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
                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                if (sqlServer.Reader.HasRows)
                {
                    //Cria o payload de retorno
                    retornoConsulta.payload = new PayloadItensTransacao();

                    retornoConsulta.payload.listaItensTransacao = new Izio.Biblioteca.ModuloClasse().PreencheClassePorDataReader<DadosItensTransacao>(sqlServer.Reader);
                }
                else
                {
                    if (retornoConsulta.errors == null)
                    {
                        retornoConsulta.errors = new List<ErrosConsultaTransacao>();
                    }

                    retornoConsulta.errors.Add(new ErrosConsultaTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = DadosNaoEncontradosItens + "." });
                }
            }
            catch (System.Exception ex)
            {
                Log.inserirLogException(NomeClienteWs, ex, 0);

                if (sqlServer.Reader != null && !sqlServer.Reader.IsClosed)
                {
                    sqlServer.Reader.Close();
                }

                sqlServer.Rollback();

                if (retornoConsulta.errors == null)
                {
                    retornoConsulta.errors = new List<ErrosConsultaTransacao>();
                }

                //Adiciona o erro de negocio
                retornoConsulta.errors.Add(new ErrosConsultaTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDadosItens + ". Favor contactar o Administrador." });
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