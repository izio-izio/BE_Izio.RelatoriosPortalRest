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

        /// <summary>
        /// Consulta  lote e paginada de clientes cadastrados/alterados entre o range de datas informadas
        /// </summary>
        /// <param name="dadosConsulta">Objeto para a consulta em lote e paginado de pessoa</param>
        /// <returns></returns>
        //public RetornoLotePaginado ConsultarLoteTransacaoCabecalho(DadosConsultaPaginadoTransacaoCabecalho dadosConsulta)
        //{
        //    DadosLoteTransacaoCabecalhoPaginado dadosLote = new DadosLoteTransacaoCabecalhoPaginado();
        //    RetornoLotePaginado retornoLote = new RetornoLotePaginado();
        //    retornoLote.payload = new DadosLoteTransacaoCabecalhoPaginado();
        //    retornoLote.payload.listaTransacaoCabecalho = new List<DadosPessoaPaginado>();

        //    //List<DadosLotePessoas> retLotePessoa = new List<DadosLotePessoas>();
        //    List<LotePessoa> lstlotePessoa = new List<LotePessoa>();
        //    List<Erros> listaErros = new List<Erros>();

        //    try
        //    {
        //        //Abre a conexao com o banco da dados
        //        sqlServer.StartConnection();

        //        // **********************************************************************************
        //        //Monta os parametros
        //        #region Monta os parametros
        //        sqlServer.Command.Parameters.Clear();
        //        sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;
        //        sqlServer.Command.Parameters.AddWithValue("@dat_inicio_consulta", dadosConsulta.dat_inicio_consulta);
        //        sqlServer.Command.Parameters.AddWithValue("@dat_final_consulta", dadosConsulta.dat_final_consulta);
        //        sqlServer.Command.Parameters.AddWithValue("@pageIndex", (((dadosConsulta.pageIndex == 1 ? dadosConsulta.pageIndex : dadosConsulta.pageIndex) - 1) * dadosConsulta.pageSize + 1));
        //        sqlServer.Command.Parameters.AddWithValue("@pageSize", ((dadosConsulta.pageIndex == 1 ? dadosConsulta.pageIndex : dadosConsulta.pageIndex) * dadosConsulta.pageSize));
        //        #endregion

        //        //Executa a consulta, cria tabelas temporárias para o processo e retorna a quantidade de registros da consulta
        //        #region Executa a consulta, cria tabelas temporárias para o processo e retorna a quantidade de registros da consulta
        //        sqlServer.Command.CommandText = @"declare @totalRegistros int

        //                                          /* Cria tabela temporaria com a quantidade de registros retornados da consulta */
        //                                          DROP TABLE IF EXISTS ##tempRows
        //                                          SELECT * INTO ##tempRows FROM (
        //                                              select * from tab_pessoa with (nolock) where ((dat_cadastro between @dat_inicio_consulta and @dat_final_consulta) or (dat_alteracao between @dat_inicio_consulta and @dat_final_consulta)) 
        //                                          ) as tmp; 

        //                                          /* seta quantidade de registros retornados da consulta */
        //                                          set @totalRegistros = @@ROWCOUNT;

        //                                          /* Monta tabela temporária com os registros da pagina solicitada */
        //                                          DROP TABLE IF EXISTS ##tmp_pessoa_paginado;
        //                                          WITH results AS ( 
        //                                           SELECT DISTINCT 
        //                                           rowNo = ROW_NUMBER() OVER(ORDER BY tr.dat_cadastro desc ), 
        //                                             tr.* FROM ##tempRows tr) 
        //                                           SELECT * into ##tmp_pessoa_paginado FROM results WITH (nolock) 
        //                                          where rowNo between @pageIndex and @pageSize;  

        //                                          drop table ##tempRows;

        //                                          /*Retorna a quantidade registros da consulta */
        //                                          select isnull(@totalRegistros,0) totalRegistros ";

        //        //Executa a consulta, cria tabelas temporárias para o processo e retorna a quantidade de registros da consulta
        //        var totalRegistros = sqlServer.Command.ExecuteScalar();

        //        if (totalRegistros != null)
        //        {
        //            retLotePessoa.payload.totalRegistros = (int)totalRegistros;
        //            retLotePessoa.payload.totalPaginas = (int)Math.Ceiling(((double)retLotePessoa.payload.totalRegistros / dadosConsulta.pageSize));
        //        }

        //        #endregion

        //        //Se não encontrou registros na consulta, finaliza a execução do método
        //        if (retLotePessoa.payload.totalRegistros == 0)
        //        {
        //            return retLotePessoa;
        //        }
        //        else
        //        {
        //            //Consulta a pagina solicita e popula o objeto de retorno do método
        //            #region Consulta a pagina solicita e popula o objeto de retorno do método
        //            if (sqlServer.Reader != null)
        //            {
        //                sqlServer.Reader.Close();
        //                sqlServer.Reader.Dispose();
        //            }

        //            sqlServer.Command.CommandText = @"select * from ##tmp_pessoa_paginado with(nolock) ";
        //            sqlServer.Reader = sqlServer.Command.ExecuteReader();

        //            //Popula lista com retorno da consulta de pessoa
        //            lstlotePessoa = new Izio.Biblioteca.ModuloClasse().PreencheClassePorDataReader<LotePessoa>(sqlServer.Reader);
        //            #endregion

        //            // Serializa para fazer o Cast da lista original com a lista que contém o objeto Pessoa Complementar
        //            var lstSerializada = JsonConvert.SerializeObject(lstlotePessoa);
        //            dados.listaPessoa = new List<DadosPessoaPaginado>();
        //            dados.listaPessoa = JsonConvert.DeserializeObject<List<DadosPessoaPaginado>>(lstSerializada);

        //            //Consulta os dados complementares do cadastro do cliente
        //            #region Consulta os dados complementares do cadastro do cliente
        //            if (sqlServer.Reader != null)
        //            {
        //                sqlServer.Reader.Close();
        //                sqlServer.Reader.Dispose();
        //            }

        //            // Faz a Consulta para buscar os dados Complementares da Pessoa da pagina retornada
        //            sqlServer.Command.CommandText = @"select 
        //                                                 tpc.* 
        //                                              from 
        //                                                 tab_pessoa_complemento tpc with (nolock) 
        //                                              inner join
        //                                                  ##tmp_pessoa_paginado tps  with(nolock) on tps.cod_pessoa = tpc.cod_pessoa 
        //                                              where ((dat_cadastro between @dat_inicio_consulta and @dat_final_consulta) or (dat_alteracao between @dat_inicio_consulta and @dat_final_consulta)) ";

        //            //Executa a consulta
        //            sqlServer.Reader = sqlServer.Command.ExecuteReader();

        //            List<PessoaComplemento> lstComplemento = new List<PessoaComplemento>();
        //            //Popula lista com retorno da consulta de pessoa complemento
        //            lstComplemento = new Izio.Biblioteca.ModuloClasse().PreencheClassePorDataReader<PessoaComplemento>(sqlServer.Reader);

        //            #endregion

        //            //Consulta os dados da processadora do cliente
        //            #region Consulta os dados da processadora do cliente
        //            if (sqlServer.Reader != null)
        //            {
        //                sqlServer.Reader.Close();
        //                sqlServer.Reader.Dispose();
        //            }

        //            sqlServer.Command.CommandText = @"select 
        //                                                 tpc.cod_pessoa,
        //                                                 tpc.num_dia_vencto_fatura,
        //                                                 tpc.num_dia_fechamento_fatura,
        //                                                 tpc.dat_pagamento_fatura,
        //                                                 tpc.qtd_dia_atraso,
        //                                                 tpc.vlr_limite_maximo,
        //                                                 tpc.cod_produto_cartao,
        //                                                 tpc.des_produto_cartao,
        //                                                 tpc.cod_status_cartao,
        //                                                 tpc.des_status_cartao,
        //                                                 tpc.dat_alteracao,
        //                                                 tpc.vlr_limite_antigo,
        //                                                 max(tpp.cod_nro_cartao) cod_nro_cartao 
        //                                              from 
        //                                                 tab_pessoa_cartao tpc with (nolock) 
        //                                              inner join
        //                                                 ##tmp_pessoa_paginado tps  with(nolock) on tps.cod_pessoa = tpc.cod_pessoa 
        //                                              inner join
        //                                                 tab_pessoa_processadora_bkp tpp with(nolock) on tps.cod_cpf = tpp.cod_cpf
        //                                              where ((tps.dat_cadastro between @dat_inicio_consulta and @dat_final_consulta) or (tps.dat_alteracao between @dat_inicio_consulta and @dat_final_consulta))
        //                                              group by
        //                                                 tpc.cod_pessoa,
        //                                                 tpc.num_dia_vencto_fatura,
        //                                                 tpc.num_dia_fechamento_fatura,
        //                                                 tpc.dat_pagamento_fatura,
        //                                                 tpc.qtd_dia_atraso,
        //                                                 tpc.vlr_limite_maximo,
        //                                                 tpc.cod_produto_cartao,
        //                                                 tpc.des_produto_cartao,
        //                                                 tpc.cod_status_cartao,
        //                                                 tpc.des_status_cartao,
        //                                                 tpc.dat_alteracao,
        //                                                 tpc.vlr_limite_antigo;

        //                                              drop table ##tmp_pessoa_paginado; ";

        //            //Executa a consulta
        //            sqlServer.Reader = sqlServer.Command.ExecuteReader();

        //            List<DadosPessoaProcessadora> lstProcessadora = new List<DadosPessoaProcessadora>();

        //            //Popula lista com retorno da consulta de pessoa processadora
        //            lstProcessadora = new Izio.Biblioteca.ModuloClasse().PreencheClassePorDataReader<DadosPessoaProcessadora>(sqlServer.Reader);
        //            #endregion

        //            //Popula lista de retorno do método
        //            #region Popula lista de retorno do método
        //            foreach (LotePessoa dadosLotePessoa in lstlotePessoa)
        //            {
        //                DadosPessoaPaginado dadosForeach = new DadosPessoaPaginado();

        //                dadosForeach.dadosCadatrais = new LotePessoa();
        //                dadosForeach.dadosCadatrais = dadosLotePessoa;

        //                if (lstComplemento != null && lstComplemento.Count > 0)
        //                {
        //                    dadosForeach.dadosComplementares = new PessoaComplemento();
        //                    dadosForeach.dadosComplementares = lstComplemento.FirstOrDefault(x => x.cod_pessoa == dadosLotePessoa.cod_pessoa);
        //                }

        //                if (lstProcessadora != null && lstProcessadora.Count > 0)
        //                {
        //                    dadosForeach.dadosProcessadora = new DadosPessoaProcessadora();

        //                    dadosForeach.dadosProcessadora = lstProcessadora.FirstOrDefault(x => x.cod_pessoa == dadosLotePessoa.cod_pessoa);
        //                }

        //                retLotePessoa.payload.listaPessoa.Add(dadosForeach);
        //            }

        //            #endregion

        //        }
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Izio.Biblioteca.Model.DadosLog dadosLog = new Izio.Biblioteca.Model.DadosLog();
        //        dadosLog.des_erro_tecnico = ex.ToString();

        //        //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
        //        Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
        //        throw;
        //    }
        //    finally
        //    {
        //        if (sqlServer != null)
        //        {
        //            if (sqlServer.Reader != null)
        //            {
        //                sqlServer.Reader.Close();
        //                sqlServer.Reader.Dispose();
        //            }

        //            sqlServer.CloseConnection();
        //        }
        //    }

        //    return retLotePessoa;
        //}
    }
}