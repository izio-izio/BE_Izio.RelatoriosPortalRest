using Izio.Biblioteca;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using TransacaoIzioRest.Models;
using System.Configuration;
using FastMember;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using Izio.Biblioteca.Model;
using System.Text.RegularExpressions;
using Izio.Biblioteca.DAO;
using EmailRest.Models;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace TransacaoIzioRest.DAO
{
    /// <summary>
    /// Classe para importação online do Izio - Os dados já são carregados nas tabelas finais de vendas: TAB_TRANSACAO e TAB_TRANSACAO_CPF
    /// </summary>
    public class ImportaTransacaoDAO
    {
        #region Constantes Processamento Transacao

        private string ErroBancoDeDadosTransacao = "Erro na importação da venda do cupom";
        private string ErroVendaDuplicada = "A Compra já foi processada na base do Izio. Segue os dados da venda duplicada: ";
        private string ErroBancoDeDadosLoteTransacao = "Erro na importação do lote de transação";
        private string ErroDataMaiorDiaAtual = "Venda com data maior que a data do dia processamento.";
        

        #endregion

        SqlServer sqlServer;
        string NomeClienteWs;
        string tokenAutenticacao;
        public ImportaTransacaoDAO(string sNomeCliente,string token = "")
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
            tokenAutenticacao = token;
        }

        /// <summary>
        /// Verifica se o cod. Pessoa informado, possui cadastro no Izio
        /// </summary>
        /// <param name="cod_pessoa"></param>
        /// <param name="cupom"></param>
        /// <param name="dat_compra"></param>
        /// <param name="cod_loja"></param>
        /// <returns></returns>
        #region Verifica se o cod. Pessoa informado, possui cadastro no Izio
        public Boolean VerificaCodPessoaExiste(long? cod_pessoa,string cupom, DateTime dat_compra, long? cod_loja)
        {
            int iCount = 0;

            try
            {
                sqlServer.StartConnection();
                //Verifica se o codigo da pessoa informado existe na base
                sqlServer.Command.CommandType = System.Data.CommandType.Text;
                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandText = @"select count(1) from tab_pessoa with(nolock) where cod_pessoa  = " + cod_pessoa;

                iCount = Convert.ToInt32(sqlServer.Command.ExecuteScalar());
            }
            catch(Exception ex)
            {
                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = " VerificaCodPessoaExiste - [Loja: " + cod_loja +
                                            "  Cupom: " + cupom +
                                            "  Dat. Compra: " + dat_compra.ToString("dd/MM/yyyy HH:mm:ss") + " ], erro: " + ex.ToString();

                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return iCount > 0 ? true : false;
        }

        #endregion

        /// <summary>
        /// Processa de forma online pedido a pedido para as tabelas finais (tab_transacao e tab_transacao_cpf)
        ///  - Processamento sem controle de transalçao
        /// </summary>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <returns></returns>
        #region Processa vendas online - Os dados já são inseridos diretamente nas tabelas finais (tab_transacao e tab_transacao_cpf)
        public ApiErrors ImportaTransacao(DadosTransacaoOnline objTransacao,
                                                        string IpOrigem)
        {
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();
            
            try
            {
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
                //else if (objTransacao.cod_pessoa > 0)
                //{
                //    //Verifica se o codigo da pessoa informado existe na base
                //    sqlServer.Command.CommandType = System.Data.CommandType.Text;
                //    sqlServer.Command.Parameters.Clear();
                //    sqlServer.Command.CommandText = @"select count(1) from tab_pessoa with(nolock) where cod_pessoa  = " + objTransacao.cod_pessoa.ToString();
                //    Int32 iCount = 0;

                //    iCount = Convert.ToInt32(sqlServer.Command.ExecuteScalar());

                //    throw new System.Exception(NaoExisteCodPessoa);

                //}

                //Inicia o controle de transacao
                sqlServer.BeginTransaction();
                sqlServer.Command.CommandType = System.Data.CommandType.Text;

                //Monta script para inserir o cabeçalho (capa) da venda 
                #region Monta script para inserir o cabeçalho (capa) da venda 

                //Codigo pessoa maior que 0, indica que é uma compra identificada
                if (objTransacao.cod_pessoa > 0)
                {
                    //Insere a transacao (capa da venda)
                    sqlServer.Command.CommandText = @"insert into tab_transacao ( 
                                                      cod_pessoa,
                                                      dat_compra,
                                                      vlr_compra,
                                                      cod_loja,
                                                      cod_pdv,
                                                      cod_usuario,
                                                      dat_importacao,
                                                      cod_arquivo,
                                                      qtd_itens_compra,
                                                      cupom,
                                                      vlr_total_desconto,
                                                      vlr_troco) output INSERTED.cod_transacao 
                                                   values (
                                                      @cod_pessoa,
                                                      @dat_compra,
                                                      @vlr_compra,
                                                      @cod_loja,
                                                      @cod_pdv,
                                                      @cod_usuario,
                                                      (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                      @cod_arquivo,
                                                      @qtd_itens_compra,
                                                      @cupom,
                                                      @vlr_total_desconto,
                                                      @vlr_troco) ";
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
                                                      cupom,
                                                      vlr_total_desconto,
                                                      vlr_troco) output INSERTED.cod_tab_transacao_cpf
                                                   values (
                                                      @num_cpf,
                                                      @dat_compra,
                                                      @vlr_compra,
                                                      @cod_loja,
                                                      @cod_usuario,
                                                      (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                      @qtd_itens_compra,
                                                      @cupom,
                                                      @vlr_total_desconto,
                                                      @vlr_troco) ";

                }

                #endregion

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
                //sqlServer.Command.Parameters.AddWithValue("@cod_pdv", objTransacao.cod_pdv);
                sqlServer.Command.Parameters.AddWithValue("@cod_usuario", objTransacao.cod_usuario);
                sqlServer.Command.Parameters.AddWithValue("@qtd_itens_compra", objTransacao.qtd_itens_compra);

                sqlServer.Command.Parameters.AddWithValue("@cupom", objTransacao.cupom);

                if (objTransacao.vlr_total_desconto != null)
                {
                    sqlServer.Command.Parameters.AddWithValue("@vlr_total_desconto", objTransacao.vlr_total_desconto);
                }
                else
                {
                    sqlServer.Command.Parameters.AddWithValue("@vlr_total_desconto", DBNull.Value);
                }


                if (objTransacao.cod_pdv != null)
                {
                    sqlServer.Command.Parameters.AddWithValue("@cod_pdv", objTransacao.cod_pdv);
                }
                else
                {
                    sqlServer.Command.Parameters.AddWithValue("@cod_pdv", DBNull.Value);
                }



                if (objTransacao.vlr_troco != null)
                {
                    sqlServer.Command.Parameters.AddWithValue("@vlr_troco", objTransacao.vlr_troco);
                }
                else
                {
                    sqlServer.Command.Parameters.AddWithValue("@vlr_troco", DBNull.Value);
                }

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
                                                           qtd_item_compra,
                                                           vlr_desconto_item) 
                                                        values (
                                                           (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                           @cod_produto,
                                                           @des_produto,
                                                           @cod_NSU,
                                                           @cod_transacao,
                                                           @vlr_item_compra,
                                                           @qtd_item_compra,
                                                           @vlr_desconto_item) ";
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
                                                           qtd_item_compra,
                                                           vlr_desconto_item) 
                                                        values (
                                                           (CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time'),
                                                           @cod_produto,
                                                           @des_produto,
                                                           @cod_NSU,
                                                           @cod_transacao,
                                                           @vlr_item_compra,
                                                           @qtd_item_compra,
                                                           @vlr_desconto_item) ";

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

                        if (item.vlr_desconto_item != null)
                        {
                            sqlServer.Command.Parameters.AddWithValue("@vlr_desconto_item", item.vlr_desconto_item);
                        }
                        else
                        {
                            sqlServer.Command.Parameters.AddWithValue("@vlr_desconto_item", DBNull.Value);
                        }

                        //Executa a procedure
                        sqlServer.Command.ExecuteNonQuery();
                    }

                    #endregion

                    #region Importa os meios de pagamentos

                    //Insere os meios de pagamentos da transacao
                    if (objTransacao.cod_pessoa > 0)
                    {
                        sqlServer.Command.CommandText = @"insert into tab_transacao_meiopagto (
                                                           cod_transacao,
                                                           nom_tipo_pagamento,
                                                           cod_nsu_cartao,
                                                           dat_nsu_cartao,
                                                           des_bin_cartao,
                                                           vlr_meiopagto) 
                                                        values (
                                                           @cod_transacao,
                                                           @nom_tipo_pagamento,
                                                           @cod_nsu_cartao,
                                                           @dat_nsu_cartao,
                                                           @des_bin_cartao,
                                                           @vlr_meiopagto) ";
                    }
                    else
                    {
                        sqlServer.Command.CommandText = @"insert into tab_transacao_cpf_meiopagto (
                                                           cod_tab_transacao_cpf,
                                                           nom_tipo_pagamento,
                                                           cod_nsu_cartao,
                                                           dat_nsu_cartao,
                                                           des_bin_cartao,
                                                           vlr_meiopagto) 
                                                        values (
                                                           @cod_transacao,
                                                           @nom_tipo_pagamento,
                                                           @cod_nsu_cartao,
                                                           @dat_nsu_cartao,
                                                           @des_bin_cartao,
                                                           @vlr_meiopagto)";
                    }


                    //Monta Array com os meios de pagamento da transacao
                    List<string> ListaMeioPagto = new List<string>();
                    objTransacao.nom_tipo_pagamento = objTransacao.nom_tipo_pagamento.Replace(":", "@");

                    ListaMeioPagto = objTransacao.nom_tipo_pagamento.Split(';').ToList();


                    Int32 posSplitNSU = 0;

                    //Array com os NSUs da compra paga com cartão, cria array para 10 pagamentos em cartão
                    string[] arrayCodNSU = new string[10];
                    string[] arrayVlrMeioPagto = new string[10];
                    //                    Boolean bArrayPreechido = false;

                    if (!string.IsNullOrEmpty(objTransacao.nsu_transacao))
                    {
                        arrayCodNSU = objTransacao.nsu_transacao.Split(';');
                        //bArrayPreechido = true;
                    }

                    if (!string.IsNullOrEmpty(objTransacao.vlr_meiopagto))
                    {
                        objTransacao.vlr_meiopagto = objTransacao.vlr_meiopagto.Replace(":", ";").Replace(",", ".").Trim();

                        arrayVlrMeioPagto = objTransacao.vlr_meiopagto.Split(';');
                        //bArrayPreechido = true;
                    }

                    foreach (string meioPagto in ListaMeioPagto)
                    {
                        sqlServer.Command.Parameters.Clear();

                        sqlServer.Command.Parameters.AddWithValue("@cod_transacao", lCodTransacao);

                        //List<string> itemMeioPagto;
                        string nomePagamennto = "";

                        if (!string.IsNullOrEmpty(meioPagto))
                        {
                            nomePagamennto = meioPagto;
                            //itemMeioPagto = nomePagamennto.Split('@').ToList();
                        }
                        else
                        {
                            nomePagamennto = "Não Informado";
                        }

                        if (arrayVlrMeioPagto.ElementAtOrDefault(posSplitNSU) != null)
                        {
                            sqlServer.Command.Parameters.AddWithValue("@vlr_meiopagto", arrayVlrMeioPagto[posSplitNSU]);
                        }
                        else
                        {
                            sqlServer.Command.Parameters.AddWithValue("@vlr_meiopagto", "0");
                        }


                        if (arrayCodNSU.ElementAtOrDefault(posSplitNSU) != null)
                        {
                            sqlServer.Command.Parameters.AddWithValue("@cod_nsu_cartao", arrayCodNSU[posSplitNSU]);
                            sqlServer.Command.Parameters.AddWithValue("@dat_nsu_cartao", (string.IsNullOrEmpty(objTransacao.dat_geracao_nsu)) || string.IsNullOrEmpty(objTransacao.dat_geracao_nsu.Split(';')[posSplitNSU]) ? (object)DBNull.Value : objTransacao.dat_geracao_nsu.Split(';')[posSplitNSU]);
                        }
                        else
                        {
                            sqlServer.Command.Parameters.AddWithValue("@cod_nsu_cartao", "0");
                            sqlServer.Command.Parameters.AddWithValue("@dat_nsu_cartao", DBNull.Value);
                        }

                        if (string.IsNullOrEmpty(objTransacao.des_bin_cartao))
                        {
                            var des_bin_cartao = Regex.Replace(nomePagamennto.Split('@').Count() == 1 ? "" : nomePagamennto.Split('@')[0], @"\D", "");
                            sqlServer.Command.Parameters.AddWithValue("@des_bin_cartao",des_bin_cartao);
                        }
                        else
                        {
                            sqlServer.Command.Parameters.AddWithValue("@des_bin_cartao", string.IsNullOrEmpty(objTransacao.des_bin_cartao.Split(';')[posSplitNSU]) ? "" : objTransacao.des_bin_cartao.Split(';')[posSplitNSU]);
                        }

                        posSplitNSU += 1;

                        sqlServer.Command.Parameters.AddWithValue("@nom_tipo_pagamento", nomePagamennto.Split('@').Count() == 1 ? nomePagamennto : nomePagamennto.Split('@')[1]);


                        //Executa a procedure
                        sqlServer.Command.ExecuteNonQuery();
                    }

                    #endregion
                }

                //Commit na trnasacao
                sqlServer.Commit();
            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                if (ex.Message.Contains("unq_transacao_001") && (!NomeClienteWs.ToLower().Contains("perim") && !NomeClienteWs.ToLower().Contains("hiperideal") && !NomeClienteWs.ToLower().Contains("panelao")))
                {
                    ErroVendaDuplicada += " [ Loja: " + objTransacao.cod_loja +
                                                      "   Cupom: " + objTransacao.cupom +
                                                      "   Dat. Compra: " + objTransacao.dat_compra.ToString("dd/MM/yyyy HH:mm:ss") +
                                                      "   Vlr. Compra: " + objTransacao.vlr_compra +
                                                      "   Qtd. Itens Compra: " + objTransacao.qtd_itens_compra + " ], favor contactar o administrador.";

                    DadosLog dadosLog = new DadosLog();
                    dadosLog.des_erro_tecnico = "Cupom: [" + objTransacao.cupom + "] " + ex.Message;

                    //InserirFila(NomeClienteWs, $"venda-log-{NomeClienteWs}", objTransacao);
                    

                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroVendaDuplicada + dadosLog.des_erro_tecnico });
                }
                else
                {
                    if ((NomeClienteWs.ToLower().Contains("perim") || NomeClienteWs.ToLower().Contains("hiperideal") || NomeClienteWs.ToLower().Contains("panelao")))
                    {
                        var jsonTransacao = JsonConvert.SerializeObject(objTransacao);
                        listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroVendaDuplicada + "Json: " + jsonTransacao.ToString()});
                    }
                    else
                    {
                        DadosLog dadosLog = new DadosLog();
                        dadosLog.des_erro_tecnico = "Cupom: [" + objTransacao.cupom + "] " + ex.Message;

                        Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        var jsonTransacao = JsonConvert.SerializeObject(objTransacao);
                        dadosLog.des_erro_tecnico = "Json: " + jsonTransacao.ToString();

                        Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                        //Seta a lista de erros com o erro
                        throw new System.Exception(ErroBancoDeDadosTransacao + " [" + objTransacao.cupom + "] , favor contactar o administrador");
                    }
                }
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return listaErros;
        }
        #endregion

        /// <summary>
        /// Processa as vendas em lote de até mil compras, inserido os registros na tabela intermediaria viewizio_3
        ///  - Processamento utiliza controle de transação
        /// </summary>
        /// <param name="objTransacao"></param>
        /// <param name="IpOrigem"></param>
        /// <returns></returns>
        #region Importa as vendas para tabela intermediaria viewizio_3, para ser processado em um segundo momento, pela API REST - RUNDECK
        public ApiErrors ImportaLoteTransacao(List<DadosTransacaoLote> objTransacao,string IpOrigem)
        {
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            //Lista padrão para bulkt Insert na viewizio_3
            List<DadosLoteViewizio_3> listaViewizio_3 = new List<DadosLoteViewizio_3>();
     
            try
            {
                //Insere o request na fila - Azure.Messaging.ServiceBus - Service Bus
                if (!InserirFilaLoteTransacao(objTransacao))
                {
                    enviarEmail("Erro no processamento do lote: </br> </br> " + JsonConvert.SerializeObject(objTransacao), $"{NomeClienteWs} - Erro ao inserir na fila lote de transação");
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
                        vlr_desconto_item = dadosTrans.vlr_desconto_item,
                        Quantidade = dadosTrans.qtd_item_compra,
                        cod_usuario = dadosTrans.cod_usuario == null ? 0 : dadosTrans.cod_usuario.Value,
                        cod_pessoa = 0,
                        item = dadosTrans.nro_item_compra,
                        cod_loja = dadosTrans.cod_loja,
                        nsu_transacao = dadosTrans.nsu_transacao,
                        dat_geracao_nsu = dadosTrans.dat_geracao_nsu,
                        vlr_total_desconto = dadosTrans.vlr_total_desconto,
                        des_bin_cartao = dadosTrans.des_bin_cartao,
                        vlr_meiopagto = dadosTrans.vlr_meiopagto,
                        vlr_troco = dadosTrans.vlr_troco

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
                    "dat_geracao_nsu",
                    "vlr_total_desconto",
                    "des_bin_cartao",
                    "vlr_meiopagto",
                    "vlr_troco"))
                {
                    bcp.BulkCopyTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 600;
                    bcp.DestinationTableName = "viewizio_3";
                    bcp.WriteToServer(reader);
                }

                #endregion

                sqlServer.Commit();
            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                //Seta a lista de erros com o erro
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDadosLoteTransacao + ", favor contactar o administrador" });

                //Envia email para o monitoramento caso de erro ao inserir na fila
                enviarEmail($"Verificar o request na sis_log </br></br>{ex.ToString()}", $"{NomeClienteWs} - Erro enviar lote de compra para fila (ServiceBus)");
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return listaErros;

        }

        private Boolean InserirFilaLoteTransacao(List<DadosTransacaoLote> objTransacao)
        {
            string sEtapa = "";
            List<DadosTransacaoLote> listaCompras = new List<DadosTransacaoLote>();
            List<DadosTransacaoLote> listaFila = new List<DadosTransacaoLote>();

            try
            {
                if (NomeClienteWs.ToLower() == "campelo" || 
                    NomeClienteWs.ToLower() == "clubesuper" || 
                    NomeClienteWs.ToLower() == "costazul" ||
                    NomeClienteWs.ToLower() == "montreal" ||
                    NomeClienteWs.ToLower() == "mendonca" ||
                    NomeClienteWs.ToLower() == "lab")
                {
                    //Seta o protocolo de ssl de envio da mensagem para a fila
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    //A seta a lista que será inserida na fila
                    listaCompras.AddRange(objTransacao);

                    //Coloca em cache os dados de utilizacao da fila
                    sEtapa = "Coloca em cache os dados de utilizacao da fila";
                    Dictionary<string, string> listParam = new Dictionary<string, string>();
                    listParam = consultaParametroFila();
                    string connectionString = listParam.ContainsKey("AzureBusConStr") ? listParam["AzureBusConStr"] : "Endpoint=sb://izioservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Dpnh2JPn9tgXGqRK9afC99bQI5qEDQfS3u55sU6F/oM=";

                    //Cria componente de verificação se a fila já está criada
                    sEtapa = "Cria componente de verificação se a fila já está criada";
                    ServiceBusAdministrationClient queue = new ServiceBusAdministrationClient(connectionString);
                    var existQueue = queue.QueueExistsAsync($"transacao-{NomeClienteWs.ToLower()}").GetAwaiter().GetResult();

                    //Se a fila não existir
                    sEtapa = "Se a fila não existir";
                    if (!existQueue)
                    {
                        //Cria a nova fila
                        sEtapa = "Cria a nova fila";
                        var options = new CreateQueueOptions($"transacao-{NomeClienteWs.ToLower()}");
                        options.MaxDeliveryCount = int.MaxValue;
                        options.LockDuration = TimeSpan.FromMinutes(5);
                        options.MaxSizeInMegabytes = 5 * 1024;
                        options.EnableBatchedOperations = true;
                        queue.CreateQueueAsync(options).GetAwaiter().GetResult();
                    }

                    //Inicia o componente para conexao com a fila
                    sEtapa = "Inicia o componente para conexao com a fila";
                    ServiceBusClient _client = new ServiceBusClient(connectionString);

                    //Cria o componente para envio da mensagem para fila
                    sEtapa = "Cria o componente para envio da mensagem para fila";
                    ServiceBusSender _clientSender = _client.CreateSender($"transacao-{NomeClienteWs.ToLower()}");

                    while (listaCompras.Count() > 0)
                    {
                        //Se o lote de compras tiver mais de 200 regitros, ele é dividido e inserido na fila por lote
                        listaFila = listaCompras.Take(200).ToList();

                        //Converte em json o objeto postado na api
                        sEtapa = "Converte em json o objeto postado na api";
                        string resultado = JsonConvert.SerializeObject(listaFila, Formatting.None);

                        //Cria uma nova mensagem deixanto json em bytes
                        sEtapa = "Cria uma nova mensagem deixanto json em bytes";
                        ServiceBusMessage message = new ServiceBusMessage(Encoding.UTF8.GetBytes(resultado));

                        //Envia a mensagem para a fila
                        sEtapa = "Envia a mensagem para a fila";
                        _clientSender.SendMessageAsync(message).GetAwaiter().GetResult();
                        
                        //Remove da fila de processamento, os registros inseridos na fila
                        if (listaFila.Count > listaCompras.Count) listaCompras.RemoveRange(0, listaFila.Count);
                        else listaCompras.RemoveRange(0, listaFila.Count);
                    }

                    //Fecha os componentes de conexão com a fila
                    _clientSender.CloseAsync();
                    _client.DisposeAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                DadosLog dadosLog = new DadosLog { des_erro_tecnico = $"{sEtapa} - Erro ao enviar mensagem para fila. {ex.Message.ToString()}" };
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                return false;
            }
        }

        public ApiErrors ProcessarFilaLoteTransacao()
        {
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();
            string sEtapa = "";
            int totalLoteFila = 0;

            try
            {
                var retFilaTransacao = ImportarFilaLoteTransacao();
                var retFilaCancelamento = ImportarFilaCancelamentoTransacao();

                if (retFilaTransacao != null && retFilaTransacao.errors != null && retFilaTransacao.errors.Count() > 0)
                {
                    listaErros.errors.AddRange(retFilaTransacao.errors);
                }

                if (retFilaCancelamento != null && retFilaCancelamento.errors != null && retFilaCancelamento.errors.Count() > 0)
                {
                    listaErros.errors.AddRange(retFilaCancelamento.errors);
                }

            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                //Seta a lista de erros com o erro
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(), message = ex.ToString() });

                //Envia email para o monitoramento caso de erro ao inserir na fila
                enviarEmail($"{sEtapa} - Verificar o request na sis_log </br></br>{ex.ToString()}", $"{NomeClienteWs} - Erro consumir lote de compra da fila (ServiceBus)");
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return listaErros;

        }

        public ApiErrors ImportarFilaLoteTransacao()
        {
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            //Lista padrão para bulkt Insert na viewizio_3
            List<DadosLoteViewizio_3> listaViewizio_3 = new List<DadosLoteViewizio_3>();
            List<DadosLoteViewizio_3> listaViewizio_data = new List<DadosLoteViewizio_3>();
            string sEtapa = "";
            int totalLoteFila = 0;
            try
            {
                //Coloca em cache os dados para conexão com a fila
                Dictionary<string, string> listParam = new Dictionary<string, string>();
                listParam = GetFromCache<Dictionary<string, string>>($"{NomeClienteWs}_servicebus_parmetroDAO", 1, () =>
                {
                    ParametroDAO parametrosDAO = new ParametroDAO(NomeClienteWs, tokenAutenticacao);
                    return parametrosDAO.ListarParametros("queue_azure,logarRequest,AzureBusConStr");
                });
                string connectionString = listParam.ContainsKey("AzureBusConStr") ? listParam["AzureBusConStr"] : "Endpoint=sb://izioservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Dpnh2JPn9tgXGqRK9afC99bQI5qEDQfS3u55sU6F/oM=";

                //Cria componente de verificação se a fila já está criada
                sEtapa = "Cria componente de verificação se a fila já está criada";
                ServiceBusAdministrationClient queue = new ServiceBusAdministrationClient(connectionString);
                var existQueue = queue.QueueExistsAsync($"transacao-{NomeClienteWs.ToLower()}").GetAwaiter().GetResult();

                //Se a fila existir, consome as mensagens
                sEtapa = "Se a fila não existir";
                if (existQueue)
                {
                    //Variaveis do processamento
                    var listaLocal = new List<DadosTransacaoLote>();
                    List<ServiceBusReceivedMessage> listaExcluirFila = new List<ServiceBusReceivedMessage>();
                    Boolean temMsg = true;
                    DateTime datInicio = DateTime.Now;

                    //Inicia o componente para conexao com a fila
                    sEtapa = "Inicia o componente para conexao com a fila";
                    ServiceBusClient _client = new ServiceBusClient(connectionString);

                    //Seta que mensagem ficará em bloqueio e saíra da fila, até que seja concluida (CompleteMessageAsync).
                    ServiceBusReceiverOptions option = new ServiceBusReceiverOptions();
                    option.ReceiveMode = ServiceBusReceiveMode.PeekLock;

                    // create a receiver that we can use to receive the message
                    ServiceBusReceiver receiver = _client.CreateReceiver($"transacao-{NomeClienteWs.ToLower()}", option);

                    // Abre a conexao com o banco de dados
                    sqlServer.StartConnection();
                    sqlServer.BeginTransaction();

                    //Enquanto existir mensagem na fila fica no processamento
                    while (temMsg)
                    {
                        // the received message is a different type as it contains some service set properties
                        var messages = receiver.ReceiveMessagesAsync(100).GetAwaiter().GetResult();
                        //var messages = receiver.PeekMessagesAsync(50).GetAwaiter().GetResult();

                        if (messages != null && messages.Count() > 0)
                        {
                            //Insere as mensagens retornadas da fila nas lista para carga na base de dados (viewizio_3)
                            foreach (ServiceBusReceivedMessage message in messages)
                            {
                                //Converte a mensagem de Byte para texto (volta o json)
                                string item = Encoding.UTF8.GetString(message.Body.ToArray());

                                List<DadosTransacaoLote> lote = new List<DadosTransacaoLote>();
                                lote = JsonConvert.DeserializeObject<List<DadosTransacaoLote>>(item);
                                listaLocal.AddRange(lote);
                                listaExcluirFila.Add(message);

                                //Verifica se a data e hora da mensagem é menor que a data do inicio do processamento.
                                //O processamento só ocorre para mensagens recebidas antes da data e hora do inicio do processamento
                                if (temMsg)
                                    temMsg = TimeZoneInfo.ConvertTime(message.EnqueuedTime, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")) < datInicio ? true : false;

                                if (DateTime.Now > datInicio.AddMinutes(5))
                                    temMsg = false;
                            }
                        }
                        else
                        {
                            temMsg = false;
                        }

                        //Verifica se não existe mais mensagem na fila e a lista local está preenchida
                        //ou
                        //Verifica se a lista local tem 100 registros
                        //Sendo afirmativo um dos dois casos, persiste no banco de dados os callbacks de retorno do sefaz
                        //e
                        //apaga da fila as notas persistidas
                        #region Verifica se não existe mais mensagem na fila e a lista local está preenchida
                        if ((!temMsg && listaLocal.Count() > 0) || listaLocal.Count() >= 1000)
                        {
                            //Popula lista padrão para o bulkInsert na viewizio_3
                            #region Monta lista padrão para o bulkInsert na viewizio_3
                            foreach (DadosTransacaoLote dadosTrans in listaLocal.ToList())
                            {
                                if (dadosTrans.dat_compra > new DateTime(1900, 01, 01))
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
                                        vlr_desconto_item = dadosTrans.vlr_desconto_item,
                                        Quantidade = dadosTrans.qtd_item_compra,
                                        cod_usuario = dadosTrans.cod_usuario == null ? 0 : dadosTrans.cod_usuario.Value,
                                        cod_pessoa = 0,
                                        item = dadosTrans.nro_item_compra,
                                        cod_loja = dadosTrans.cod_loja,
                                        nsu_transacao = dadosTrans.nsu_transacao,
                                        dat_geracao_nsu = dadosTrans.dat_geracao_nsu,
                                        vlr_total_desconto = dadosTrans.vlr_total_desconto,
                                        des_bin_cartao = dadosTrans.des_bin_cartao,
                                        vlr_meiopagto = dadosTrans.vlr_meiopagto,
                                        vlr_troco = dadosTrans.vlr_troco

                                    });
                                }
                                else
                                {
                                    listaViewizio_data.Add(new DadosLoteViewizio_3
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
                                        cod_usuario = dadosTrans.cod_usuario == null ? 0 : dadosTrans.cod_usuario.Value,
                                        cod_pessoa = 0,
                                        item = dadosTrans.nro_item_compra,
                                        cod_loja = dadosTrans.cod_loja,
                                        nsu_transacao = dadosTrans.nsu_transacao,
                                        dat_geracao_nsu = dadosTrans.dat_geracao_nsu,
                                        vlr_total_desconto = dadosTrans.vlr_total_desconto,
                                        des_bin_cartao = dadosTrans.des_bin_cartao,
                                        vlr_meiopagto = dadosTrans.vlr_meiopagto,
                                        vlr_troco = dadosTrans.vlr_troco

                                    });
                                }
                            }

                            #endregion

                            totalLoteFila += listaViewizio_3.Count();

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
                                "dat_geracao_nsu",
                                "vlr_total_desconto",
                                "des_bin_cartao",
                                "vlr_meiopagto",
                                "vlr_troco"))
                            {
                                bcp.BulkCopyTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 600;
                                bcp.DestinationTableName = "viewizio_3";
                                bcp.WriteToServer(reader);
                            }

                            #endregion

                            //Exclui da fila as mensagens processadas e inseridas na base
                            Parallel.ForEach(listaExcluirFila, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (message) =>
                            {
                                receiver.CompleteMessageAsync(message);
                            });

                            //limpa a lista para inserir novos callbacks
                            listaViewizio_3.Clear();
                            listaExcluirFila.Clear();
                            listaLocal.Clear();
                        }
                        #endregion
                    }

                    sqlServer.Commit();

                    //Adiciona na lista de retorna a quantidade de lote processados
                    listaErros.errors.Add(new Erros() { code = "200", message = totalLoteFila.ToString() });
                }
            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                //Seta a lista de erros com o erro
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(), message = ex.ToString() });

                //Envia email para o monitoramento caso de erro ao inserir na fila
                enviarEmail($"{sEtapa} - Verificar o request na sis_log </br></br>{ex.ToString()}", $"{NomeClienteWs} - Erro consumir lote de compra da fila (ServiceBus)");
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return listaErros;

        }

        public ApiErrors ImportarFilaCancelamentoTransacao()
        {
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            //Lista padrão para bulkt Insert na viewizio_3
            List<DadosTransacaoCancelada> listaViewizio_3 = new List<DadosTransacaoCancelada>();
            List<DadosTransacaoCancelada> listaViewizio_data = new List<DadosTransacaoCancelada>();
            List<DadosFilaCancelamentoTransacao> listaCancelamento = new List<DadosFilaCancelamentoTransacao>();
            string sEtapa = "";
            int totalLoteFila = 0;
            try
            {
                //Coloca em cache os dados para conexão com a fila
                Dictionary<string, string> listParam = new Dictionary<string, string>();
                listParam = GetFromCache<Dictionary<string, string>>($"{NomeClienteWs}_servicebus_parmetroDAO", 1, () =>
                {
                    ParametroDAO parametrosDAO = new ParametroDAO(NomeClienteWs, tokenAutenticacao);
                    return parametrosDAO.ListarParametros("queue_azure,logarRequest,AzureBusConStr");
                });
                string connectionString = listParam.ContainsKey("AzureBusConStr") ? listParam["AzureBusConStr"] : "Endpoint=sb://izioservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Dpnh2JPn9tgXGqRK9afC99bQI5qEDQfS3u55sU6F/oM=";

                //Cria componente de verificação se a fila já está criada
                sEtapa = "Cria componente de verificação se a fila já está criada";
                ServiceBusAdministrationClient queue = new ServiceBusAdministrationClient(connectionString);
                var existQueue = queue.QueueExistsAsync($"cancelamento-transacao-{NomeClienteWs.ToLower()}").GetAwaiter().GetResult();

                //Se a fila existir, consome as mensagens
                sEtapa = "Se a fila não existir";
                if (existQueue)
                {
                    //Variaveis do processamento
                    var listaLocal = new List<DadosTransacaoCancelada>();
                    List<ServiceBusReceivedMessage> listaExcluirFila = new List<ServiceBusReceivedMessage>();
                    Boolean temMsg = true;
                    DateTime datInicio = DateTime.Now;

                    //Inicia o componente para conexao com a fila
                    sEtapa = "Inicia o componente para conexao com a fila";
                    ServiceBusClient _client = new ServiceBusClient(connectionString);

                    //Seta que mensagem ficará em bloqueio e saíra da fila, até que seja concluida (CompleteMessageAsync).
                    ServiceBusReceiverOptions option = new ServiceBusReceiverOptions();
                    option.ReceiveMode = ServiceBusReceiveMode.PeekLock;

                    // create a receiver that we can use to receive the message
                    ServiceBusReceiver receiver = _client.CreateReceiver($"cancelamento-transacao-{NomeClienteWs.ToLower()}", option);

                    //Enquanto existir mensagem na fila fica no processamento
                    while (temMsg)
                    {
                        // the received message is a different type as it contains some service set properties
                        var messages = receiver.ReceiveMessagesAsync(100).GetAwaiter().GetResult();

                        if (messages != null && messages.Count() > 0)
                        {
                            //Insere as mensagens retornadas da fila nas lista para carga na base de dados (viewizio_3)
                            foreach (ServiceBusReceivedMessage message in messages)
                            {
                                //Converte a mensagem de Byte para texto (volta o json)
                                string item = Encoding.UTF8.GetString(message.Body.ToArray());

                                List<DadosTransacaoCancelada> lote = new List<DadosTransacaoCancelada>();
                                lote = JsonConvert.DeserializeObject<List<DadosTransacaoCancelada>>(item);
                                listaLocal.AddRange(lote);
                                listaExcluirFila.Add(message);

                                listaCancelamento.Add(new DadosFilaCancelamentoTransacao()
                                {
                                    cod_loja = lote.FirstOrDefault().cod_loja,
                                    cupom = lote.FirstOrDefault().cupom,
                                    dat_compra = lote.FirstOrDefault().dat_compra,
                                    dat_exclusao = message.EnqueuedTime.DateTime,
                                    des_json_cancelamento = JsonConvert.SerializeObject(item),
                                    qtd_itens_compra = lote.FirstOrDefault().qtd_itens_compra,
                                    vlr_compra = lote.FirstOrDefault().vlr_compra
                                });

                                //Verifica se a data e hora da mensagem é menor que a data do inicio do processamento.
                                //O processamento só ocorre para mensagens recebidas antes da data e hora do inicio do processamento
                                if (temMsg)
                                    temMsg = TimeZoneInfo.ConvertTime(message.EnqueuedTime, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")) < datInicio ? true : false;

                                if (DateTime.Now > datInicio.AddMinutes(5))
                                    temMsg = false;
                            }
                        }
                        else
                        {
                            temMsg = false;
                        }

                        //Verifica se não existe mais mensagem na fila e a lista local está preenchida
                        //ou
                        //Verifica se a lista local tem 100 registros
                        //Sendo afirmativo um dos dois casos, persiste no banco de dados os callbacks de retorno do sefaz
                        //e
                        //apaga da fila as notas persistidas
                        #region Verifica se não existe mais mensagem na fila e a lista local está preenchida
                        if ((!temMsg && listaLocal.Count() > 0) || listaLocal.Count() >= 1000)
                        {
                            totalLoteFila += listaLocal.Count();

                            TransacaoCanceladaDAO dao = new TransacaoCanceladaDAO(NomeClienteWs,tokenAutenticacao);

                            foreach (DadosFilaCancelamentoTransacao item in listaCancelamento)
                            {
                                DadosTransacaoCancelada dados = new DadosTransacaoCancelada()
                                {
                                    cod_loja = item.cod_loja,
                                    cupom = item.cupom,
                                    dat_compra = item.dat_compra,
                                    qtd_itens_compra = (int)item.qtd_itens_compra,
                                    vlr_compra = item.vlr_compra
                                };

                                //Aplica a exclusão do registro
                                dao.ExcluirRegistroFilaCompraCancelada(dados, listaErros);
                            }

                            //Trocar a execução por bulkInsert da lista
                            #region Bulk Insert da lista

                            // Abre a conexao com o banco de dados
                            sqlServer.StartConnection();
                            sqlServer.BeginTransaction();

                            using (var bcp = new SqlBulkCopy
                                        (
                                        //Para utilizar o controle de transacao
                                        sqlServer.Command.Connection,
                                        SqlBulkCopyOptions.TableLock |
                                        SqlBulkCopyOptions.FireTriggers,
                                        sqlServer.Command.Transaction
                                        ))
                            using (
                                var reader = ObjectReader.Create(listaCancelamento,
                                "cod_compra_cancelada",
                                "dat_compra",
                                "vlr_compra",
                                "cod_loja",
                                "qtd_itens_compra",
                                "cupom",
                                "des_json_cancelamento",
                                "dat_exclusao"))
                            {
                                bcp.BulkCopyTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 600;
                                bcp.DestinationTableName = "tab_compra_cancelada";
                                bcp.WriteToServer(reader);
                            }

                            #endregion

                            //Exclui da fila as mensagens processadas e inseridas na base
                            Parallel.ForEach(listaExcluirFila, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (message) =>
                            {
                                receiver.CompleteMessageAsync(message);
                            });

                            //limpa a lista para inserir novos callbacks
                            listaViewizio_3.Clear();
                            listaExcluirFila.Clear();
                            listaLocal.Clear();

                            //Commit
                            sqlServer.Commit();
                        }
                        #endregion
                    }


                    //Adiciona na lista de retorna a quantidade de lote processados
                    listaErros.errors.Add(new Erros() { code = "200", message = totalLoteFila.ToString() });
                }
            }
            catch (System.Exception ex)
            {
                sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = ex.ToString();
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                //Seta a lista de erros com o erro
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.BadRequest).ToString(), message = ex.ToString() });

                //Envia email para o monitoramento caso de erro ao inserir na fila
                enviarEmail($"{sEtapa} - Verificar o request na sis_log </br></br>{ex.ToString()}", $"{NomeClienteWs} - Erro consumir lote de compra da fila (ServiceBus)");
            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return listaErros;

        }

        public Dictionary<string, string> consultaParametroFila()
        {
            Dictionary<string, string> listParam = new Dictionary<string, string>();
            int i = 0;

            while (i < 3)
            {
                try
                {
                    listParam = GetFromCache<Dictionary<string, string>>($"{NomeClienteWs}_servicebus_parmetroDAO", 1, () =>
                    {
                        ParametroDAO parametrosDAO = new ParametroDAO(NomeClienteWs, tokenAutenticacao);
                        return parametrosDAO.ListarParametros("queue_azure,logarRequest,AzureBusConStr");
                    });
                    i = 4;
                }
                catch (Exception ex)
                {
                    i++;

                    if (!ex.Message.ToLower().Contains("timeout") || i >= 3)
                    {
                        throw;
                    }

                    //Dorme 3 segundos
                    System.Threading.Thread.Sleep(3000);
                }
                
            }

            return listParam;
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
        public ApiErrors ImportaLoteTransacaoSemTransacao(List<DadosTransacaoLote> objTransacao,
                                                                          string IpOrigem,
                                                                          string jsonRequisicao = "")
        {
            ApiErrors listaErros = new ApiErrors();
            listaErros.errors = new List<Erros>();

            //Lista padrão para bulkt Insert na viewizio_3
            List<DadosLoteViewizio_3> listaViewizio_3 = new List<DadosLoteViewizio_3>();
            Boolean DataMaiorQueDiaAtual = false;

            string dat_compra = "", cod_loja = "", cupom = "", vlr_compra = "", qtd_itens_compra = "";

            try
            {
                jsonRequisicao = JsonConvert.SerializeObject(objTransacao);

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
                            dat_geracao_nsu = dadosTrans.dat_geracao_nsu,
                            vlr_total_desconto = dadosTrans.vlr_total_desconto,
                            des_bin_cartao = dadosTrans.des_bin_cartao
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
                    "dat_geracao_nsu",
                    "vlr_total_desconto",
                    "des_bin_cartao"))
                {
                    bcp.BulkCopyTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 600;
                    bcp.DestinationTableName = "viewizio_3";
                    bcp.WriteToServer(reader);
                }

                #endregion

                //sqlServer.Commit();
               
            }
            catch (System.Exception ex)
            {
                //sqlServer.Rollback();

                DadosLog dadosLog = new DadosLog();

                if (DataMaiorQueDiaAtual)
                {
                    dadosLog.des_erro_tecnico = ErroDataMaiorDiaAtual +
                        " {Loja = " + cod_loja + " | Dat.Compra = " + dat_compra + " | Cupom " + cupom + " | Vlr.Compra = " + vlr_compra + " | Qtd.Itens.Compra = " + qtd_itens_compra + " } " +
                        ", favor contactar o administrador";

                    //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                    Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                    listaErros.errors.Add(new Erros { code = "500", message = dadosLog.des_erro_tecnico });
                }
                else
                {
                    //Seta a lista de erros com o erro
                    dadosLog.des_erro_tecnico = ex.ToString();

                    //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                    Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                    throw new System.Exception(ErroBancoDeDadosLoteTransacao + ", favor contactar o administrador");
                }


            }
            finally
            {
                sqlServer.CloseConnection();
            }

            return listaErros;

        }
        #endregion

        private void enviarEmail(string desTexto, string desTitulo)
        {
            try
            {


                List<Header> lstHeader = new List<Header>();
                lstHeader.Add(new Header
                {
                    name = "tokenAutenticacao",
                    value = tokenAutenticacao
                });

                var acesso = Utilidades.ConsultarConfiguracoesCliente(NomeClienteWs);


                int i = 0;

                EmailTemplateEnvio email = new EmailTemplateEnvio
                {
                    des_email = "monitoramento@izio.com.br",
                    des_cod_campanha = 0,
                    cod_tipo_email_template = (int)TipoTemplate.TEMPLATE_CONTEUDO_GENERICO,
                    des_complemneto = desTexto,
                    des_titulo_email = desTitulo

                };

                var result = Utilidades.ChamadaApiExternaStatusCode(
                                                 tipoRequisicao: "POST",
                                                 metodo: "EmailRest/api/Email/EnvioEmailTemplate/",
                                                 body: JsonConvert.SerializeObject(email),
                                                 url: "https://api.izio.com.br/",
                                                 Headers: lstHeader);

                if (result != null)
                {
                    result = result;
                }
            }
            catch (Exception ex)
            {
                DadosLog dadosLog = new DadosLog();
                dadosLog.des_erro_tecnico = "Erro ao enviar email da fila: " +  ex.Message;

                //Pegar a mensagem padrão retornada da api, caso não tenha mensagem de negocio para devolver na API
                Log.InserirLogIzio(NomeClienteWs, dadosLog, System.Reflection.MethodBase.GetCurrentMethod());

                throw;
            }
        }

        public List<DadosClienteIzio> ListarClientes(string ids)
        {
            try
            {
                if (ids == "0")
                {
                    ids = "";
                }
                else
                {
                    ids = $" AND sis.id IN({ids}) ";
                }

                var query = $@"SELECT sis.id,
                            sis.des_chave_ws,
                            sis.des_token_rest,
                            mctp.num_dia_processamento,
                            sis.des_string_conexao
                            FROM sis_clientes sis WITH (NOLOCK)
                            INNER JOIN dbo.mon_cliente_transacao_parametrizacao mctp WITH (NOLOCK)
                            ON mctp.id = sis.id {ids} order by 2 ";

                sqlServer.StartConnection();
                sqlServer.Command.CommandText = query;
                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                var lista = new ModuloClasse().PreencheClassePorDataReader<DadosClienteIzio>(sqlServer.Reader);

                sqlServer.CloseConnection();
                return lista;
            }
            catch (Exception ex)
            {
                Log.inserirLogException(NomeClienteWs, ex, 0);
                throw ex;
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

        public static TEntity GetFromCache<TEntity>(string key, int numHorasCache, Func<TEntity> valueFactory) where TEntity : class
        {
            ObjectCache cache = MemoryCache.Default;
            // the lazy class provides lazy initializtion which will eavaluate the valueFactory expression only if the item does not exist in cache
            var newValue = new Lazy<TEntity>(valueFactory);
            CacheItemPolicy policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(numHorasCache) };
            //The line below returns existing item or adds the new value if it doesn't exist
            var value = cache.AddOrGetExisting(key, newValue, policy) as Lazy<TEntity>;
            return (value ?? newValue).Value; // Lazy<T> handles the locking itself
        }
    }
}