﻿using Izio.Biblioteca;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using TransacaoRest.Models;
using System.Configuration;
using FastMember;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace TransacaoIzioRest.DAO
{
    /// <summary>
    /// Classe para importação online do Izio - Os dados já são carregados nas tabelas finais de vendas: TAB_TRANSACAO e TAB_TRANSACAO_CPF
    /// </summary>
    public class ImportaTransacaoDAO
    {
        #region Constantes Processamento Transacao

        private string DadosNaoEncontrados = "Não foram encontrados registros";
        private string ErroBancoDeDados = "Não foi possível realizar processamentos da transações para as tabelas finais do Izio";

        private string ObjetoTransacaoVazio = "Objeto com os dados das vendas está vazio, impossível realizar o processamento.";
        private string ObjetoItensTransacaoVazio = "Objeto com os itens das vendas está vazio, impossível realizar o processamento.";
        private string ErroInternoValidaCampos = "Não foi possível realizar a validação das lojas antes do processamento.";

        private string ErroBancoDeDadosTransacao = "Erro na importação da venda do cupom";
        private string ErroVendaDuplicada = "A Compra já foi processada na base do Izio. Segue os dados da venda duplicada: ";
        private string ErroBancoDeDadosLoteTransacao = "Erro na importação do lote de transação";
        private string ErroDataMaiorDiaAtual = "Venda com data maior que a data do dia processamento.";
        private string NaoExisteCodPessoa = "Codigo da pessoa informada, não existe na base do Izio";

        #endregion

        SqlServer sqlServer;
        string NomeClienteWs;
        public ImportaTransacaoDAO(string sNomeCliente)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
        }
        
        /// <summary>
        /// Processa de forma online pedido a pedido para as tabelas finais (tab_transacao e tab_transacao_cpf)
        ///  - Processamento sem controle de transalçao
        /// </summary>
        /// <param name="sNomeCliente"></param>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <returns></returns>
        #region Processa vendas online - Os dados já são inseridos diretamente nas tabelas finais (tab_transacao e tab_transacao_cpf)
        public RetornoDadosProcTransacao ImportaTransacao(DadosTransacaoOnline objTransacao,
                                                          string IpOrigem)
        {
            RetornoDadosProcTransacao retornoTransacao = new RetornoDadosProcTransacao();
            retornoTransacao.errors = new List<ErrosTransacao>();
            retornoTransacao.payload = new PayloadTransacao();

            ListaErrosTransacao listaErros = new ListaErrosTransacao();
            listaErros.errors = new List<ErrosTransacao>();

            PayloadTransacao payloadSucesso = new PayloadTransacao();

            try
            {
                //Valida se o objeto com as transações foi preenchido
                if (objTransacao == null)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoTransacaoVazio });
                }
                else if (objTransacao.ListaItens == null)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoItensTransacaoVazio });
                }
                else if (objTransacao.ListaItens != null && objTransacao.ListaItens.Count == 0)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoItensTransacaoVazio });
                }

                //Se a lista estiver preenchida, é por que foi encontrado erros na validação
                if (listaErros.errors.Count > 0)
                {
                    retornoTransacao.errors = listaErros.errors;

                    return retornoTransacao;
                }

                long lCodTransacao;

                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Consulta o codigo pessoa, quando a compra for identificada pelo CPF e variavel cod_pessoa estiver igual a 0
                if ((objTransacao.cod_pessoa == 0 || objTransacao.cod_pessoa.ToString() == objTransacao.cod_cpf) && !string.IsNullOrEmpty(objTransacao.cod_cpf) && objTransacao.cod_cpf.Trim() != "0")
                {
                    //Verifica se o codigo da pessoa informado existe na base
                    sqlServer.Command.CommandType = System.Data.CommandType.Text;
                    sqlServer.Command.Parameters.Clear();
                    sqlServer.Command.CommandText = @"select coalesce(cod_pessoa,0) cod_pessoa from tab_pessoa with(nolock) where cod_cpf  = '" + objTransacao.cod_cpf.Replace(".", "").Replace("-", "").Trim().PadLeft(11, '0') + "'";

                    //Seta o codigo da pessoa do CPF informado
                    objTransacao.cod_pessoa = Convert.ToInt32(sqlServer.Command.ExecuteScalar());
                }
                else if (objTransacao.cod_pessoa > 0)
                {
                    //Verifica se o codigo da pessoa informado existe na base
                    sqlServer.Command.CommandType = System.Data.CommandType.Text;
                    sqlServer.Command.Parameters.Clear();
                    sqlServer.Command.CommandText = @"select count(1) from tab_pessoa with(nolock) where cod_pessoa  = " + objTransacao.cod_pessoa.ToString();
                    Int32 iCount = 0;

                    iCount = Convert.ToInt32(sqlServer.Command.ExecuteScalar());

                    if (iCount == 0)
                    {
                        listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = NaoExisteCodPessoa });
                        //Log.inserirLogException(NomeClienteWs, new Exception(NaoExisteCodPessoa), 0);

                        retornoTransacao.errors = listaErros.errors;

                        return retornoTransacao;
                    }

                }

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();
                sqlServer.Command.CommandType = System.Data.CommandType.Text;

                //Codigo pessoa maior que 0, indica que é uma compra identificada
                if (objTransacao.cod_pessoa > 0)
                {
                    //Insere a transacao (capa da venda)
                    sqlServer.Command.CommandText = @"insert into tab_transacao ( 
                                                      cod_pessoa,
                                                      dat_compra,
                                                      vlr_compra,
                                                      cod_loja,
                                                      cod_usuario,
                                                      dat_importacao,
                                                      cod_arquivo,
                                                      qtd_itens_compra,
                                                      cupom) output INSERTED.cod_transacao 
                                                   values (
                                                      @cod_pessoa,
                                                      @dat_compra,
                                                      @vlr_compra,
                                                      @cod_loja,
                                                      @cod_usuario,
                                                      (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                      @cod_arquivo,
                                                      @qtd_itens_compra,
                                                      @cupom) ";
                }
                else
                {
                    //Insere a transacao (capa da venda)
                    sqlServer.Command.CommandText = @"insert into tab_transacao_cpf ( 
                                                      num_cpf,
                                                      dat_compra,
                                                      vlr_compra,
                                                      cod_loja,
                                                      cod_usuario,
                                                      dat_importacao,
                                                      qtd_itens_compra,
                                                      cupom) output INSERTED.cod_tab_transacao_cpf
                                                   values (
                                                      @num_cpf,
                                                      @dat_compra,
                                                      @vlr_compra,
                                                      @cod_loja,
                                                      @cod_usuario,
                                                      (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                      @qtd_itens_compra,
                                                      @cupom) ";

                }

                #region Monta Parametros

                sqlServer.Command.Parameters.Clear();

                sqlServer.Command.Parameters.AddWithValue("@num_cpf", (string.IsNullOrEmpty(objTransacao.cod_cpf) ? (object)DBNull.Value : objTransacao.cod_cpf.ToString()));
                sqlServer.Command.Parameters.AddWithValue("@cod_pessoa", objTransacao.cod_pessoa);

                sqlServer.Command.Parameters.AddWithValue("@dat_compra", objTransacao.dat_compra);
                sqlServer.Command.Parameters.AddWithValue("@vlr_compra", objTransacao.vlr_compra);
                //sqlServer.Command.Parameters.AddWithValue("@om_Tipo_Pagamento", objTransacao.nom_tipo_pagamento);

                if (objTransacao.cod_pessoa > 0)
                    sqlServer.Command.Parameters.AddWithValue("@cod_arquivo", 0);

                sqlServer.Command.Parameters.AddWithValue("@cod_loja", objTransacao.cod_loja);
                sqlServer.Command.Parameters.AddWithValue("@cod_usuario", objTransacao.cod_usuario);
                sqlServer.Command.Parameters.AddWithValue("@qtd_itens_compra", objTransacao.qtd_itens_compra);

                sqlServer.Command.Parameters.AddWithValue("@cupom", objTransacao.cupom);

                //Log.inserirLogException(NomeClienteWs, new Exception("Carga Transacaco Cupom: " + objTransacao.cupom + "  |  Ip Requisicao: " + IpOrigem), 0);

                #endregion

                //Executa o insert e pega codigo da transacao gerado
                lCodTransacao = Convert.ToInt64(sqlServer.Command.ExecuteScalar());

                //Verifca se foi enviado os itens da transacao
                if (objTransacao.ListaItens != null && objTransacao.ListaItens.Count > 0)
                {
                    #region Insere os itens da Compra
                    //Insere o item da transacao
                    sqlServer.Command.CommandType = System.Data.CommandType.Text;

                    if (objTransacao.cod_pessoa > 0)
                    {
                        sqlServer.Command.CommandText = @"insert into tab_transacao_itens (
                                                           dat_cadastro,
                                                           cod_produto,
                                                           des_produto,
                                                           cod_NSU,
                                                           cod_transacao,
                                                           vlr_item_compra,
                                                           qtd_item_compra) 
                                                        values (
                                                           (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                           @cod_produto,
                                                           @des_produto,
                                                           @cod_NSU,
                                                           @cod_transacao,
                                                           @vlr_item_compra,
                                                           @qtd_item_compra) ";
                    }
                    else
                    {
                        sqlServer.Command.CommandText = @"insert into tab_transacao_itens_cpf (
                                                           dat_cadastro,
                                                           cod_produto,
                                                           des_produto,
                                                           cod_NSU,
                                                           cod_tab_transacao_cpf,
                                                           vlr_item_compra,
                                                           qtd_item_compra) 
                                                        values (
                                                           (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                           @cod_produto,
                                                           @des_produto,
                                                           @cod_NSU,
                                                           @cod_transacao,
                                                           @vlr_item_compra,
                                                           @qtd_item_compra) ";

                    }

                    //Percore a lista com os itens da transacao
                    foreach (DadosItensTransacao item in objTransacao.ListaItens.ToList())
                    {

                        sqlServer.Command.Parameters.Clear();

                        sqlServer.Command.Parameters.AddWithValue("@cod_transacao", lCodTransacao);

                        sqlServer.Command.Parameters.AddWithValue("@cod_produto", item.cod_produto);
                        sqlServer.Command.Parameters.AddWithValue("@des_produto", item.des_produto);
                        sqlServer.Command.Parameters.AddWithValue("@cod_NSU", item.cod_ean);

                        sqlServer.Command.Parameters.AddWithValue("@vlr_item_compra", item.vlr_item_compra);
                        sqlServer.Command.Parameters.AddWithValue("@qtd_item_compra", item.qtd_item_compra);

                        //Executa a procedure
                        sqlServer.Command.ExecuteNonQuery();
                    }

                    #endregion

                    #region Importa os meios de pagamentos

                    //Monta Array com os meios de pagamento da transacao
                    List<string> ListaMeioPagto = new List<string>();
                    ListaMeioPagto = objTransacao.nom_tipo_pagamento.Split(';').ToList();

                    //Insere os meios de pagamentos da transacao
                    if (objTransacao.cod_pessoa > 0)
                    {
                        sqlServer.Command.CommandText = @"insert into tab_transacao_meiopagto (
                                                           cod_transacao,
                                                           nom_tipo_pagamento,
                                                           cod_nsu_cartao,
                                                           dat_nsu_cartao) 
                                                        values (
                                                           @cod_transacao,
                                                           @nom_tipo_pagamento,
                                                           @cod_nsu_cartao,
                                                           @dat_nsu_cartao) ";
                    }
                    else
                    {
                        sqlServer.Command.CommandText = @"insert into tab_transacao_cpf_meiopagto (
                                                           cod_tab_transacao_cpf,
                                                           nom_tipo_pagamento,
                                                           cod_nsu_cartao,
                                                           dat_nsu_cartao) 
                                                        values (
                                                           @cod_transacao,
                                                           @nom_tipo_pagamento,
                                                           @cod_nsu_cartao,
                                                           @dat_nsu_cartao) ";
                    }

                    Int32 posSplitNSU = 0;

                    //Array com os NSUs da compra paga com cartão, cria array para 10 pagamentos em cartão
                    string[] arrayCodNSU = new string[10];
                    Boolean bArrayPreechido = false;
                    if (!string.IsNullOrEmpty(objTransacao.nsu_transacao))
                    {
                        arrayCodNSU = objTransacao.nsu_transacao.Split(';');
                        bArrayPreechido = true;
                    }

                    foreach (string meioPagto in ListaMeioPagto)
                    {
                        sqlServer.Command.Parameters.Clear();

                        sqlServer.Command.Parameters.AddWithValue("@cod_transacao", lCodTransacao);

                        sqlServer.Command.Parameters.AddWithValue("@nom_tipo_pagamento", meioPagto);

                        //Somente para meio de pagamento diferente de dinheiro
                        if (meioPagto.ToUpper().Contains("TEF") || meioPagto.ToUpper().Contains("CARTAO") || meioPagto.ToUpper().Contains("CARTÃO") || meioPagto.ToUpper().Contains("CRED DEMAIS"))
                        {
                            if (!meioPagto.ToUpper().Contains("OFF"))
                            {
                                if (arrayCodNSU.ElementAtOrDefault(posSplitNSU) != null)
                                {
                                    sqlServer.Command.Parameters.AddWithValue("@cod_nsu_cartao", arrayCodNSU[posSplitNSU]);
                                }
                                else
                                {
                                    sqlServer.Command.Parameters.AddWithValue("@cod_nsu_cartao", "0");
                                }
                                
                                posSplitNSU += 1;
                            }
                        }
                        else
                            sqlServer.Command.Parameters.AddWithValue("@cod_nsu_cartao", DBNull.Value);

                        sqlServer.Command.Parameters.AddWithValue("@dat_nsu_cartao", DBNull.Value);

                        //Executa a procedure
                        sqlServer.Command.ExecuteNonQuery();
                    }

                    #endregion
                }

                //Commit na trnasacao
                sqlServer.Commit();

                //Seta o retorno com sucesso
                payloadSucesso.code = Convert.ToInt32(HttpStatusCode.Accepted).ToString();
                payloadSucesso.message = "Transação Importada com sucesso.";

            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                if (ex.Message.Contains("unq_transacao_001"))
                {
                    //Seta a lista de erros com o erro
                    listaErros.errors.Add(new ErrosTransacao
                    {
                        code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                        message = ErroVendaDuplicada + " [ Loja: " + objTransacao.cod_loja + 
                                                       "   Cupom: " + objTransacao.cupom +
                                                       "   Dat. Compra: " + objTransacao.dat_compra.ToString("dd/MM/yyyy HH:mm:ss") +
                                                       "   Vlr. Compra: " + objTransacao.vlr_compra +
                                                       "   Qtd. Itens Compra: " + objTransacao.qtd_itens_compra +
                                                       "   Cpf: " + objTransacao.cod_cpf + " ], favor contactar o administrador."
                    });
                }
                else
                {
                    //Seta a lista de erros com o erro
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDadosTransacao + " [" + objTransacao.cupom + "], favor contactar o administrador" });
                }

                if (objTransacao != null)
                {
                    var jsonTransacao = JsonConvert.SerializeObject(objTransacao);

                    //Salva o Json da requisição
                    Log.inserirLogException(NomeClienteWs, new System.Exception("Cupom: [" + objTransacao.cupom + "] " + ex.Message, new System.Exception(jsonTransacao.ToString())), 0);
                }
                else
                {
                    //Insere o erro na sis_log
                    Log.inserirLogException(NomeClienteWs, new System.Exception("Cupom: [" + objTransacao.cupom + "] " + ex.Message, new System.Exception(ex.ToString())), 0);
                }
                
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            if (listaErros.errors != null && listaErros.errors.Count > 0)
            {
                retornoTransacao.errors = listaErros.errors;
            }
            else
            {
                retornoTransacao.payload = payloadSucesso;
            }

            return retornoTransacao;
        }
        #endregion

        /// <summary>
        /// Processa as vendas em lote de até mil compras, inserido os registros na tabela intermediaria viewizio_3
        ///  - Processamento utiliza controle de transação
        /// </summary>
        /// <param name="sNomeCliente"></param>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <returns></returns>
        #region Importa as vendas para tabela intermediaria viewizio_3, para ser processado em um segundo momento, pela API REST - RUNDECK
        public RetornoDadosProcTransacao ImportaLoteTransacao(List<DadosTransacaoLote> objTransacao,
                                                              string IpOrigem)
        {
            RetornoDadosProcTransacao retornoTransacao = new RetornoDadosProcTransacao();
            retornoTransacao.errors = new List<ErrosTransacao>();
            retornoTransacao.payload = new PayloadTransacao();

            ListaErrosTransacao listaErros = new ListaErrosTransacao();
            listaErros.errors = new List<ErrosTransacao>();

            PayloadTransacao payloadSucesso = new PayloadTransacao();

            //Lista padrão para bulkt Insert na viewizio_3
            List<DadosLoteViewizio_3> listaViewizio_3 = new List<DadosLoteViewizio_3>();

            try
            {
                //Valida se o objeto com as transações foi preenchido
                if (objTransacao == null)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoTransacaoVazio });
                }
                else if (objTransacao.Count == 0)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoItensTransacaoVazio });
                }

                //Se a lista estiver preenchida, é por que foi encontrado erros na validação
                if (listaErros.errors.Count > 0)
                {
                    retornoTransacao.errors = listaErros.errors;

                    return retornoTransacao;
                }


                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();

                //Popula lista padrão para o bulkInsert na viewizio_3
                #region Monta lista padrão para o bulkInsert na viewizio_3
                foreach (DadosTransacaoLote dadosTrans in objTransacao.ToList())
                {
                    listaViewizio_3.Add(new DadosLoteViewizio_3
                    {
                        CpfCliente = Convert.ToInt64(string.IsNullOrEmpty(dadosTrans.cod_cpf) == false ? dadosTrans.cod_cpf : "0"),
                        CpfCliente_2 = Convert.ToInt64(string.IsNullOrEmpty(dadosTrans.cod_cpf) == false ? dadosTrans.cod_cpf : "0"),
                        DataCompra = dadosTrans.dat_compra,
                        ValorCompra = dadosTrans.vlr_compra,
                        cupom = dadosTrans.cupom,
                        Pdv = dadosTrans.Pdv,
                        CodPagto = 0,
                        MeioPagto = dadosTrans.des_tipo_pagamento,
                        QtdeItens = dadosTrans.qtd_itens_compra,
                        CodEAN = dadosTrans.cod_ean,
                        CodProduto = Convert.ToInt64(dadosTrans.cod_produto),
                        DesProduto = dadosTrans.des_produto,
                        ValorItem = dadosTrans.vlr_item_compra,
                        vlr_desconto_item = dadosTrans.vlr_desconto_item != null ? dadosTrans.vlr_desconto_item : 0,
                        Quantidade = dadosTrans.qtd_item_compra,
                        cod_usuario = 1,
                        cod_pessoa = 0,
                        item = dadosTrans.nro_item_compra,
                        cod_loja = dadosTrans.cod_loja,
                        nsu_transacao = dadosTrans.nsu_transacao,
                        dat_geracao_nsu = dadosTrans.dat_geracao_nsu
                    });
                }

                #endregion

                //Trocar a execução por bulkInsert da lista
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
                    var reader = ObjectReader.Create(listaViewizio_3,
                    "CpfCliente",
                    "CpfCliente_2",
                    "DataCompra",
                    "ValorCompra",
                    "cupom",
                    "Pdv",
                    "CodPagto",
                    "MeioPagto",
                    "QtdeItens",
                    "CodEAN",
                    "CodProduto",
                    "DesProduto",
                    "ValorItem",
                    "vlr_desconto_item",
                    "Quantidade",
                    "cod_usuario",
                    "cod_pessoa",
                    "item",
                    "cod_loja",
                    "nsu_transacao",
                    "dat_geracao_nsu"))
                {
                    bcp.BulkCopyTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 600;
                    bcp.DestinationTableName = "viewizio_3";
                    bcp.WriteToServer(reader);
                }

                #endregion
                
                sqlServer.Commit();

                //Seta o retorno com sucesso
                payloadSucesso.code = Convert.ToInt32(HttpStatusCode.Accepted).ToString();
                payloadSucesso.message = "Lote de Transações Importado com sucesso.";
            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                //Seta a lista de erros com o erro
                listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDadosLoteTransacao + ", favor contactar o administrador" });

                //Insere o erro na sis_log
                Log.inserirLogException(NomeClienteWs, ex, 0);
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            if (listaErros.errors != null && listaErros.errors.Count > 0)
            {
                retornoTransacao.errors = listaErros.errors;
            }
            else
            {
                retornoTransacao.payload = payloadSucesso;
            }

            return retornoTransacao;

        }
        #endregion

        /// <summary>
        /// Processa as vendas em lote de até mil compras, inserido os registros na tabela intermediaria viewizio_3
        ///  - Processamento utiliza SEM controle de transação
        /// </summary>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <param name="jsonRequisicao"></param>
        /// <returns></returns>
        #region Importa as vendas para tabela intermediaria viewizio_3 SEM CONTROLE DE TRANSACAO, para ser processado em um segundo momento, pela API REST - RUNDECK
        public RetornoDadosProcTransacao ImportaLoteTransacaoSemTransacao(List<DadosTransacaoLote> objTransacao,
                                                                          string IpOrigem,
                                                                          string jsonRequisicao = "")
        {
            RetornoDadosProcTransacao retornoTransacao = new RetornoDadosProcTransacao();
            retornoTransacao.errors = new List<ErrosTransacao>();
            retornoTransacao.payload = new PayloadTransacao();

            ListaErrosTransacao listaErros = new ListaErrosTransacao();
            listaErros.errors = new List<ErrosTransacao>();

            PayloadTransacao payloadSucesso = new PayloadTransacao();

            //Lista padrão para bulkt Insert na viewizio_3
            List<DadosLoteViewizio_3> listaViewizio_3 = new List<DadosLoteViewizio_3>();
            Boolean DataMaiorQueDiaAtual = false;

            string dat_compra = "", cod_loja = "", cupom = "", vlr_compra = "", qtd_itens_compra = "";

            try
            {
                jsonRequisicao = JsonConvert.SerializeObject(objTransacao); 

                //Valida se o objeto com as transações foi preenchido
                if (objTransacao == null)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoTransacaoVazio });
                }
                else if (objTransacao.Count == 0)
                {
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ObjetoItensTransacaoVazio });
                }

                //Se a lista estiver preenchida, é por que foi encontrado erros na validação
                if (listaErros.errors.Count > 0)
                {
                    retornoTransacao.errors = listaErros.errors;

                    return retornoTransacao;
                }


                // Abre a conexao com o banco de dados
                sqlServer.StartConnection();

                //Inicia o controle de transacao
                //sqlServer.BeginTransaction();

                //Popula lista padrão para o bulkInsert na viewizio_3
                #region Monta lista padrão para o bulkInsert na viewizio_3

                foreach (DadosTransacaoLote dadosTrans in objTransacao.ToList())
                {

                    //Verifica se a data da compra for maior que o dia do processamento, indica data incorreta
                    // neste caso é rejeitado o log e se tiver com auditória, é salvo o json da requisição
                    if (dadosTrans.dat_compra.Date > DateTime.Now.Date)
                    {
                        //Insere json na tabela de auditória
                        if (ConfigurationManager.AppSettings["ClienteAuditoria"] != null && ConfigurationManager.AppSettings["ClienteAuditoria"].ToString().ToUpper().Contains(NomeClienteWs.ToUpper()))
                        {
                            sqlServer.Command.Parameters.Clear();
                            sqlServer.Command.CommandText = "insert into tab_viewizio_auditoria (dat_compra,cod_loja,cupom,vlr_compra,qtd_itens_compra,des_json_requisicao) values (@dat_compra,@cod_loja,@cupom,@vlr_compra,@qtd_itens_compra,@des_json_requisicao)";

                            #region Parametros

                            IDbDataParameter pdat_compra = sqlServer.Command.CreateParameter();
                            pdat_compra.ParameterName = "@dat_compra";
                            pdat_compra.Value = dadosTrans.dat_compra;
                            sqlServer.Command.Parameters.Add(pdat_compra);

                            IDbDataParameter pcod_loja = sqlServer.Command.CreateParameter();
                            pcod_loja.ParameterName = "@cod_loja";
                            pcod_loja.Value = dadosTrans.cod_loja;
                            sqlServer.Command.Parameters.Add(pcod_loja);

                            IDbDataParameter pcupom = sqlServer.Command.CreateParameter();
                            pcupom.ParameterName = "@cupom";
                            pcupom.Value = dadosTrans.cupom;
                            sqlServer.Command.Parameters.Add(pcupom);

                            IDbDataParameter pvlr_compra = sqlServer.Command.CreateParameter();
                            pvlr_compra.ParameterName = "@vlr_compra";
                            pvlr_compra.Value = dadosTrans.vlr_compra;
                            sqlServer.Command.Parameters.Add(pvlr_compra);

                            IDbDataParameter pqtd_itens_compra = sqlServer.Command.CreateParameter();
                            pqtd_itens_compra.ParameterName = "@qtd_itens_compra";
                            pqtd_itens_compra.Value = dadosTrans.qtd_itens_compra;
                            sqlServer.Command.Parameters.Add(pqtd_itens_compra);

                            //Json da requisição
                            IDbDataParameter pdes_json_requisicao = sqlServer.Command.CreateParameter();
                            pdes_json_requisicao.ParameterName = "@des_json_requisicao";
                            pdes_json_requisicao.Value = jsonRequisicao.Length > 4000 ? jsonRequisicao.Substring(0, 3999) : jsonRequisicao;
                            sqlServer.Command.Parameters.Add(pdes_json_requisicao);

                            #endregion

                            sqlServer.Command.ExecuteNonQuery();

                        }

                        dat_compra = dadosTrans.dat_compra.ToString("dd/MM/yyyy HH:mm:ss");
                        cod_loja = dadosTrans.cod_loja.ToString();
                        cupom = dadosTrans.cupom;
                        vlr_compra = dadosTrans.vlr_compra.ToString();
                        qtd_itens_compra = dadosTrans.qtd_itens_compra.ToString();

                        DataMaiorQueDiaAtual = true;

                        throw new System.Exception(ErroDataMaiorDiaAtual);

                    }
                    else
                    {
                        //Adiciona o registro na lista somente se a data da compra for menor ou igual a data do dia do processamento
                        listaViewizio_3.Add(new DadosLoteViewizio_3
                        {
                            CpfCliente = Convert.ToInt64(string.IsNullOrEmpty(dadosTrans.cod_cpf) == false ? dadosTrans.cod_cpf : "0"),
                            CpfCliente_2 = Convert.ToInt64(string.IsNullOrEmpty(dadosTrans.cod_cpf) == false ? dadosTrans.cod_cpf : "0"),
                            DataCompra = dadosTrans.dat_compra,
                            ValorCompra = dadosTrans.vlr_compra,
                            cupom = dadosTrans.cupom,
                            Pdv = dadosTrans.Pdv,
                            CodPagto = 0,
                            MeioPagto = dadosTrans.des_tipo_pagamento,
                            QtdeItens = dadosTrans.qtd_itens_compra,
                            CodEAN = dadosTrans.cod_ean,
                            CodProduto = Convert.ToInt64(dadosTrans.cod_produto),
                            DesProduto = dadosTrans.des_produto,
                            ValorItem = dadosTrans.vlr_item_compra,
                            vlr_desconto_item = dadosTrans.vlr_desconto_item,
                            Quantidade = dadosTrans.qtd_item_compra,
                            cod_usuario = 1,
                            cod_pessoa = 0,
                            item = dadosTrans.nro_item_compra,
                            cod_loja = dadosTrans.cod_loja,
                            nsu_transacao = dadosTrans.nsu_transacao,
                            dat_geracao_nsu = dadosTrans.dat_geracao_nsu
                        });
                    }
                }

                #endregion

                //Trocar a execução por bulkInsert da lista
                #region Bulk Insert da lista

                using (var bcp = new SqlBulkCopy
                            (
                            //Para utilizar SEM controle de transacao
                            sqlServer.Command.Connection,
                            SqlBulkCopyOptions.TableLock |
                            SqlBulkCopyOptions.FireTriggers,
                            null
                            ))
                using (
                    var reader = ObjectReader.Create(listaViewizio_3,
                    "CpfCliente",
                    "CpfCliente_2",
                    "DataCompra",
                    "ValorCompra",
                    "cupom",
                    "Pdv",
                    "CodPagto",
                    "MeioPagto",
                    "QtdeItens",
                    "CodEAN",
                    "CodProduto",
                    "DesProduto",
                    "ValorItem",
                    "vlr_desconto_item",
                    "Quantidade",
                    "cod_usuario",
                    "cod_pessoa",
                    "item",
                    "cod_loja",
                    "nsu_transacao",
                    "dat_geracao_nsu"))
                {
                    bcp.BulkCopyTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 600;
                    bcp.DestinationTableName = "viewizio_3";
                    bcp.WriteToServer(reader);
                }

                #endregion

                //sqlServer.Commit();

                //Seta o retorno com sucesso
                payloadSucesso.code = Convert.ToInt32(HttpStatusCode.Accepted).ToString();
                payloadSucesso.message = "Lote de Transações Importado com sucesso.";
            }
            catch (System.Exception ex)
            {
                //sqlServer.Rollback();

                if (DataMaiorQueDiaAtual)
                {
                    //Seta a lista de erros com o erro
                    listaErros.errors.Add(new ErrosTransacao
                    {
                        code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(),
                        message = ErroDataMaiorDiaAtual +
                        " {Loja = " + cod_loja + " | Dat.Compra = " + dat_compra + " | Cupom " + cupom + " | Vlr.Compra = " + vlr_compra + " | Qtd.Itens.Compra = " + qtd_itens_compra + " } " +
                        ", favor contactar o administrador"
                    });
                }
                else
                {
                    //Seta a lista de erros com o erro
                    listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDadosLoteTransacao + ", favor contactar o administrador" });
                }

                //Insere o erro na sis_log
                Log.inserirLogException(NomeClienteWs, ex, 0);
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            if (listaErros.errors != null && listaErros.errors.Count > 0)
            {
                retornoTransacao.errors = listaErros.errors;
            }
            else
            {
                retornoTransacao.payload = payloadSucesso;
            }

            return retornoTransacao;

        }
        #endregion

        #region Remove Acentos e caracteres especiais antes do envio do SMS

        public string GetStringNoAccents(string str)
        {
            /** Troca os caracteres acentuados por não acentuados **/
            string[] acentos = new string[] { "ç", "Ç", "á", "é", "í", "ó", "ú", "ý", "Á", "É", "Í", "Ó", "Ú", "Ý", "à", "è", "ì", "ò", "ù", "À", "È", "Ì", "Ò", "Ù", "ã", "õ", "ñ", "ä", "ë", "ï", "ö", "ü", "ÿ", "Ä", "Ë", "Ï", "Ö", "Ü", "Ã", "Õ", "Ñ", "â", "ê", "î", "ô", "û", "Â", "Ê", "Î", "Ô", "Û" };
            string[] semAcento = new string[] { "c", "C", "a", "e", "i", "o", "u", "y", "A", "E", "I", "O", "U", "Y", "a", "e", "i", "o", "u", "A", "E", "I", "O", "U", "a", "o", "n", "a", "e", "i", "o", "u", "y", "A", "E", "I", "O", "U", "A", "O", "N", "a", "e", "i", "o", "u", "A", "E", "I", "O", "U" };
            for (int i = 0; i < acentos.Length; i++)
            {
                str = str.Replace(acentos[i], semAcento[i]);
            }
            /** Troca os caracteres especiais da string por "" **/
            string[] caracteresEspeciais = { "\\.", "-", ":", "\\(", "\\)", "ª", "\\|", "\\\\", "°", "'" };
            for (int i = 0; i < caracteresEspeciais.Length; i++)
            {
                str = str.Replace(caracteresEspeciais[i], "");
            }
            /** Troca os espaços no início por "" **/
            str = str.Replace("^\\s+", "");
            /** Troca os espaços no início por "" **/
            str = str.Replace("\\s+$", "");
            /** Troca os espaços duplicados, tabulações e etc por  " " **/
            str = str.Replace("\\s+", " ");
            return str;
        }

        #endregion

    }
}