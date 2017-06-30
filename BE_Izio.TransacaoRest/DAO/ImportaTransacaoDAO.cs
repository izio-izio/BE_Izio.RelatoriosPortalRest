using Izio.Biblioteca;
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
        private string ErroBancoDeDadosLoteTransacao = "Erro na importação do lote de transação";
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
                        if (!meioPagto.ToUpper().Contains("DINHEIRO") && !meioPagto.ToUpper().Contains("DINHEIROS") && bArrayPreechido)
                        {
                            sqlServer.Command.Parameters.AddWithValue("@cod_nsu_cartao", arrayCodNSU[posSplitNSU]);
                            posSplitNSU += 1;
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

                //Seta a lista de erros com o erro
                listaErros.errors.Add(new ErrosTransacao { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDadosTransacao + " [" + objTransacao.cupom + "], favor contactar o administrador" });

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
        ///  - Processamento utiliza controle de transação
        /// </summary>
        /// <param name="sNomeCliente"></param>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <returns></returns>
        #region Importa as vendas para tabela intermediaria viewizio_3, para ser processado em um segundo momento, pela API REST - DIKRON
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
                        ValorUN = dadosTrans.vlr_item_compra,
                        ValorItem = dadosTrans.vlr_item_compra,
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
                    "ValorUN",
                    "ValorItem",
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

                //***************************
                //Metodo descontinuado
                #region Insert aninhado - PROCEDIMENTO TODO COMENTADO
                ////Monta Insert Aninhado...
                //StringBuilder queryBuilder = new StringBuilder();

                //Int32 iContador = 0;

                ////Percorre lista com os cpfs
                //foreach (DadosTransacaoLote dadosTrans in objTransacao.ToList())
                //{
                //    if (iContador == 0)
                //    {
                //        queryBuilder.Clear();
                //        queryBuilder.AppendFormat("insert into viewizio_3 values");
                //    }

                //    //if (dadosTrans.cod_cpf.Contains("5999744601"))
                //    //{
                //    //    dadosTrans.cod_cpf = dadosTrans.cod_cpf;
                //    //}

                //    if (iContador < 999)
                //    {
                //        queryBuilder.AppendFormat("('{0}','{1}','{2}',{3},'{4}','{5}',{6},'{7}',{8},{9},'{10}','{11}',{12},{13},{14},{15},{16},{17},{18},'{19}','{20}'),",
                //                                     dadosTrans.cod_cpf, dadosTrans.cod_cpf,
                //                                     dadosTrans.dat_compra.ToString("yyyyMMdd HH:mm:ss"),
                //                                     dadosTrans.vlr_compra.ToString().Replace(",", "."),
                //                                     dadosTrans.cupom,
                //                                     dadosTrans.Pdv,
                //                                     "0",
                //                                     dadosTrans.des_tipo_pagamento,
                //                                     dadosTrans.qtd_itens_compra,
                //                                     dadosTrans.cod_ean,
                //                                     dadosTrans.cod_produto,
                //                                     GetStringNoAccents(dadosTrans.des_produto),
                //                                     dadosTrans.vlr_item_compra.ToString().Replace(",", "."),
                //                                     dadosTrans.vlr_item_compra.ToString().Replace(",", "."),
                //                                     dadosTrans.qtd_item_compra.ToString().Replace(",", "."),
                //                                     "1",
                //                                     dadosTrans.cod_pessoa,
                //                                     dadosTrans.nro_item_compra,
                //                                     dadosTrans.cod_loja,
                //                                     dadosTrans.nsu_transacao,
                //                                     dadosTrans.dat_geracao_nsu);

                //        iContador += 1;
                //    }
                //    else
                //    {
                //        //Adiciona a linha 1000 e faz o insert de 1000 registros
                //        queryBuilder.AppendFormat("('{0}','{1}','{2}',{3},'{4}','{5}',{6},'{7}',{8},{9},'{10}','{11}',{12},{13},{14},{15},{16},{17},{18},'{19}','{20}'),",
                //                                    dadosTrans.cod_cpf, dadosTrans.cod_cpf,
                //                                    dadosTrans.dat_compra.ToString("yyyyMMdd HH:mm:ss"),
                //                                    dadosTrans.vlr_compra.ToString().Replace(",", "."),
                //                                    dadosTrans.cupom,
                //                                    dadosTrans.Pdv,
                //                                    "0",
                //                                    dadosTrans.des_tipo_pagamento,
                //                                    dadosTrans.qtd_itens_compra,
                //                                    dadosTrans.cod_ean,
                //                                    dadosTrans.cod_produto,
                //                                    GetStringNoAccents(dadosTrans.des_produto),
                //                                    dadosTrans.vlr_item_compra.ToString().Replace(",", "."),
                //                                    dadosTrans.vlr_item_compra.ToString().Replace(",", "."),
                //                                    dadosTrans.qtd_item_compra.ToString().Replace(",", "."),
                //                                    "1",
                //                                    dadosTrans.cod_pessoa,
                //                                    dadosTrans.nro_item_compra,
                //                                    dadosTrans.cod_loja,
                //                                    dadosTrans.nsu_transacao,
                //                                    dadosTrans.dat_geracao_nsu);

                //        queryBuilder.Replace(',', ';', queryBuilder.Length - 1, 1);
                //        sqlServer.Command.CommandText = queryBuilder.ToString();
                //        sqlServer.Command.ExecuteNonQuery();

                //        //Zera o contador para inserir os proximos 1000
                //        iContador = 0;
                //    }
                //}

                ////Executa o insert do restante dos registros
                //queryBuilder.Replace(',', ';', queryBuilder.Length - 1, 1);
                //sqlServer.Command.CommandText = queryBuilder.ToString();
                //sqlServer.Command.ExecuteNonQuery();

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