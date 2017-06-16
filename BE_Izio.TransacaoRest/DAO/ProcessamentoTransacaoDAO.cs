using Izio.Biblioteca;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using TransacaoRest.Models;

namespace TransacaoRest.DAO
{
    /// <summary>
    /// Classe para o processamento em lote do IZio - API para importar os dados da VIEWIZIO_3 para as tabelas finais: TAB_TRANSACAO e TAB_TRANSACAO_CPF
    /// </summary>
    public class ProcessamentoTransacaoDAO
    {
        #region Constantes Processamento Transacao

        private string DadosNaoEncontrados = "Não foram encontrados registros";
        private string ErroBancoDeDados = "Não foi possível realizar processamentos da transações para as tabelas finais do Izio";
        private string ErroBancoDeDadosPersistencia = "Não foi possível persistir os dados para o processamentos das transações";
        private string ObjetoPayloadVazio = "Não foi informado loja para o processamento.";
        private string ErroInternoValidaCampos = "Não foi possível realizar a validação das lojas antes do processamento.";
        private string LojaNaoCadastrada = "Loja informada para o processamento, não está cadastrada";

        #endregion

        SqlServer sqlServer;
        string NomeClienteWs;
        public ProcessamentoTransacaoDAO(string sNomeCliente)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
        }

        /// <summary>
        /// Valida se os campos obrigatorios foram informados
        /// </summary>
        /// <param name="sToken"></param>
        /// <param name="objLojas"></param>
        /// <returns></returns>
        #region Valida Campos Obrigatorios

        private ListaErros ValidaCampos(string sToken, Payload objLojas)
        {
            ListaErros listaErros = new ListaErros();
            string sConsultaLoja = "";

            if (listaErros.errors == null)
            {
                listaErros.errors = new List<Erros>();
            }

            if (objLojas == null)
            {
                listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.NoContent).ToString(), message = ObjetoPayloadVazio });
            }
            else
            {
                try
                {
                    //foreach(DadosProcessamentoTransacao loja in objLojas.listaLojas )
                    //{
                    //    //Somente valida se a loja existe, quando o processamento for por loja enviadas na lista 
                    //    //  Quando o codigo da loja for zero, a API irá processar todas as lojas cadastradas na tab_loja
                    //    if (loja.cod_loja > 0)
                    //    {
                    //        sConsultaLoja = ConsultaLojaExiste(loja.cod_loja);

                    //        if (!string.IsNullOrEmpty(sConsultaLoja))
                    //        {
                    //            listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = sConsultaLoja });
                    //        }
                    //    }
                    //    else
                    //    {
                    //        break;
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    if (listaErros.errors == null)
                    {
                        listaErros.errors = new List<Erros>();
                    }

                    listaErros.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = "" });

                    Izio.Biblioteca.Log.inserirLogException(NomeClienteWs, new Exception(ex.Message, new Exception(ex.ToString())), 0);
                }
            }


            return listaErros;
        }

        #endregion

        /// <summary>
        /// Consulta se loja informada está cadastrada na base
        /// </summary>
        /// <param name="codigoLoja">Codigo da Loja</param>
        /// <returns></returns>
        #region Consulta se loja existe

        public string ConsultaLojaExiste(Int64 codigoLoja)
        {
            //
            string sRetorno = "";

            //Cabeçalho da consulta SQL
            string sSql;

            //Join da consulta
            string sWhere = "";

            try
            {
                //Abre conexao com o banco de dados
                sqlServer.StartConnection();

                //Seta os parametros
                sqlServer.Command.CommandType = System.Data.CommandType.Text;
                sqlServer.Command.Parameters.Clear();

                //Consulta padrao
                //Consultas a loja
                sSql = @"select 
                            1
                         from
                            tab_loja with(nolock)
                         where cod_loja  = @cod_loja ";

                #region Parametros
                // **********************************************************************************
                //Monta os parametros
                //Codigo da loja
                IDbDataParameter pcod_loja = sqlServer.Command.CreateParameter();
                pcod_loja.ParameterName = "@cod_loja";
                pcod_loja.Value = codigoLoja;
                sqlServer.Command.Parameters.Add(pcod_loja);

                // **********************************************************************************
                // **********************************************************************************
                #endregion

                //
                sqlServer.Command.CommandText = sSql + sWhere;

                //Executa o select na base
                sqlServer.Reader = sqlServer.Command.ExecuteReader();

                if (sqlServer.Reader.HasRows)
                {
                    sRetorno = "";
                    //
                    sqlServer.Reader.Close();
                }
                //Verifica se a consulta retornou alguma campanha cadastrada
                else
                {
                    sRetorno = LojaNaoCadastrada + ", favor contactar o administrador.";
                }

                //
                sqlServer.CloseConnection();
            }
            catch (System.Exception ex)
            {
                sRetorno = ErroBancoDeDados;
                Izio.Biblioteca.Log.inserirLogException(NomeClienteWs, new Exception(ex.Message, new Exception(ex.ToString())), 0);
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

            return sRetorno;
        }


        #endregion

        /// <summary>
        /// Executa persistencia antes do processamento
        /// 1 - Realiza update no campo cod_pessoa da viewizio_3, para as compras que foram colocadas o CPF
        /// </summary>
        /// <param name="sNomeCliente"></param>
        /// <param name="codLoja"></param>
        /// <returns></returns>
        #region Executa persistencia dos dados antes do processamento das transacoes

        private ListaErros FormatarDados(string sNomeCliente)
        {
            //objeto para guardar o retorno do método
            ListaErros retorno = new ListaErros();

            //bloco de tratamento de erros
            try
            {
                //inicia a conexão
                sqlServer.StartConnection();

                //Seta o tipo de comando e limpa os parâmetros
                sqlServer.Command.CommandType = System.Data.CommandType.Text;
                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                ////adiciona o parâmetro cod_loja
                //IDbDataParameter pCodLoja = sqlServer.Command.CreateParameter();
                //pCodLoja.ParameterName = "@cod_loja";
                //pCodLoja.Value = codLoja;
                //sqlServer.Command.Parameters.Add(pCodLoja);

                #region update 1

                //update para atualizar o cod_pessoa da viewizio_3
                //monta o comando do update 1
                sqlServer.Command.CommandText = @"UPDATE viewizio_3
                                                    SET viewizio_3.cod_pessoa = tab_pessoa.cod_pessoa 
                                                FROM viewizio_3 with(nolock)
                                                INNER JOIN tab_pessoa with(nolock)
                                                    ON tab_pessoa.cod_cpf = replicate('0', (11 -  len(cast(viewizio_3.cpfcliente as varchar)))) + cast(viewizio_3.cpfcliente as varchar) ";
                //if (codLoja != "0")
                //{
                //    sqlServer.Command.CommandText += " AND viewizio_3.cod_loja = @cod_loja ";
                //}

                sqlServer.Command.CommandText += " AND viewizio_3.CpfCliente > 0 ";
                //Executa o comando
                sqlServer.Command.ExecuteNonQuery();

                #endregion

            }
            //se ocorrer algum erro no processamento preenche o objeto 'retorno' com o erro e guarda o log
            catch (Exception ex)
            {
                //grava o log do erro
                Log.inserirLogException(sNomeCliente, ex, 0);

                if (retorno == null)
                {
                    retorno = new ListaErros();
                }

                retorno.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDadosPersistencia });
            }
            //em todos os casos fecha a conexão
            finally
            {
                sqlServer.CloseConnection();
            }

            //retorna o objeto 'retorno' e sai do processamento
            return retorno;
        }

        #endregion

        /// <summary>
        /// Realiza o processamento das transações da viewizio_3, importando as vendas para a tab_transacao e tab_transacao_cpf
        /// </summary>
        /// <param name="sNomeCliente"></param>
        /// <returns></returns>
        #region Processa as transações para as tabelas finais do Izio

        public DadosProcessamento ImportarTransacoes(string sNomeCliente)
        {
            //objeto para guardar o retorno do método
            DadosProcessamento retorno = new DadosProcessamento();

            //objeto para guardar o retorno do update
            ListaErros retornoUpdate = new ListaErros();

            //formata os dados para processar
            retornoUpdate = FormatarDados(sNomeCliente);

            //se ocorrer algum erro na formatação dos dados, finaliza o processamento e retorna o objeto 'retornoUpdate'
            //que contém as informações sobre o erro que ocorreu
            if (retornoUpdate != null && retornoUpdate.errors != null && retornoUpdate.errors.Count > 0)
            {

                if (retorno.errors == null)
                {
                    retorno.errors = new List<Erros>();
                }
                retorno.errors = retorno.errors;

                return retorno;
            }

            //Etapa da execucao
            string sEtapa = "";

            //bloco de tratamento de erros
            try
            {
                //abre a conexão com o banco de dados
                sEtapa = "abre a conexão com o banco de dados";
                sqlServer.StartConnection();

                sqlServer.BeginTransaction();

                //seta o tipo de comando e limpa os parâmetros
                sqlServer.Command.CommandType = CommandType.Text;
                sqlServer.Command.Parameters.Clear();
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;

                //adiciona o parâmetro cod_loja, que será utilizado no decorrer do método
                //IDbDataParameter pCodLoja = sqlServer.Command.CreateParameter();
                //pCodLoja.ParameterName = "@cod_loja";
                //pCodLoja.Value = codLoja;
                //sqlServer.Command.Parameters.Add(pCodLoja);

                //variável para armazenar a maior data de importacao
                string maxDatCompra = "";

                #region verifica se existem registros a serem processados

                //comando para buscar a maior data de compra existente na viewizio_3 para uma determinada loja
                sEtapa = "comando para buscar a maior data de compra existente na viewizio_3 para uma determinada loja";
                //sqlServer.Command.CommandText = string.Format("SELECT MAX(datacompra) FROM viewizio_3 with(nolock) WHERE cod_loja = {0}", codLoja);
                sqlServer.Command.CommandText = "SELECT top 1 datacompra FROM viewizio_3 with(nolock) ";

                //busca a data e guarda na variável
                sEtapa = "busca a data e guarda na variável";
                maxDatCompra = Convert.ToDateTime(sqlServer.Command.ExecuteScalar().ToString()).ToString("yyyy-MM-dd HH:mm:ss");

                //se a maior data for nula, então não existem registros a serem processados
                if (string.IsNullOrEmpty(maxDatCompra))
                {

                    if (retorno.errors == null)
                    {
                        retorno.errors = new List<Erros>();
                    }

                    retorno.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.OK).ToString(), message = DadosNaoEncontrados + ", para processamento das transações." });

                    //insere um log
                    Log.inserirLogException(sNomeCliente, new Exception("viewizio_3 está vazia. " + retorno.errors.FirstOrDefault().message), 0);

                    //retorna o objeto 'retorno' e sai do processamento
                    return retorno;
                }

                #endregion

                //todo o processamento será feito dentro de um begin transaction. Se rodar com sucesso, será executado commit, em caso de erro será executado rollback.
                //sqlConn.BeginTransaction();

                sEtapa = "importa as transações identificadas";
                #region insert na tab_transacao
                sqlServer.Command.CommandTimeout = ConfigurationManager.AppSettings["TimeoutExecucao"] != null ? Convert.ToInt32(ConfigurationManager.AppSettings["TimeoutExecucao"]) : 1200;
                //importa as transações identificadas

                sqlServer.Command.CommandText = @"INSERT INTO tab_transacao(
                                                    cod_pessoa,
	                                                dat_compra,
	                                                vlr_compra,
	                                                cod_loja,
	                                                cod_usuario,
	                                                dat_importacao,
	                                                cod_arquivo,
	                                                qtd_itens_compra,
	                                                cupom)
                                                SELECT
                                                    vwi.cod_pessoa,
	                                                vwi.datacompra,
	                                                vwi.valorcompra,
	                                                vwi.cod_loja,
	                                                vwi.cod_usuario,
	                                                CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time' AS dat_importacao,
                                                    1 AS cod_arquivo,
                                                    COUNT(DISTINCT vwi.item) qtd_itens_compra,
	                                                vwi.cupom
                                                FROM viewizio_3 AS vwi with(nolock)
                                                LEFT JOIN tab_transacao AS trs with(nolock,INDEX (idx_transacao_005))
                                                    ON trs.dat_compra = vwi.datacompra
                                                    AND trs.vlr_compra = vwi.valorcompra
                                                    AND trs.cupom = vwi.cupom
                                                    AND trs.cod_loja = vwi.cod_loja
                                                WHERE trs.dat_compra IS NULL
                                                AND vwi.cod_pessoa IS NOT NULL
                                                AND vwi.cod_pessoa > 0
                                                /* AND vwi.cod_loja = @cod_loja */
                                                AND NOT EXISTS(
                                                    SELECT
                                                        1
                                                    FROM tab_transacao_cpf AS ttc with(nolock,INDEX (transacao_cpf_005))
                                                    WHERE ttc.dat_compra = vwi.datacompra
                                                    AND ttc.vlr_compra = vwi.ValorCompra
                                                    AND ttc.cupom = vwi.cupom
                                                    AND ttc.cod_loja = vwi.cod_loja
                                                )
                                                GROUP BY
                                                    vwi.cod_loja,
	                                                vwi.datacompra,
	                                                vwi.valorcompra,
	                                                vwi.cupom,
	                                                vwi.cod_pessoa,
	                                                vwi.cod_usuario;";

                //executa o insert na tab_transacao
                sqlServer.Command.ExecuteNonQuery();

                #endregion

                sEtapa = "apaga os itens existentes para as transações da viewizio_3 para reinserir.";
                #region insert na tab_transacao_itens
                //apaga os itens existentes para as transações da viewizio_3 para reinserir.
                //isto é feito para garantir que todos os itens sejam importados

                sqlServer.Command.CommandText = @"DELETE tri
                                                  FROM viewizio_3 AS vwi with(nolock, INDEX (idx_viewizio_001))
                                                  INNER JOIN tab_transacao AS trs with(nolock)
	                                                  ON trs.dat_compra = vwi.datacompra
	                                                  AND trs.vlr_compra = vwi.valorcompra
	                                                  AND trs.cupom = vwi.cupom
	                                                  AND trs.cod_loja = vwi.cod_loja
                                                  INNER JOIN tab_transacao_itens AS tri with(nolock)
	                                                  ON trs.cod_transacao = tri.cod_transacao
                                                 /* WHERE vwi.cod_loja = @cod_loja; */";

                //executa o delete
                sqlServer.Command.ExecuteNonQuery();

                //importa os itens das compras identificadas
                sEtapa = "importa os itens das compras identificadas";
                sqlServer.Command.CommandText = @"INSERT INTO tab_transacao_itens (
                                                    dat_cadastro, 
	                                                cod_produto, 
	                                                des_produto, 
	                                                cod_nsu,  
	                                                vlr_item_compra, 
	                                                qtd_item_compra,
	                                                cod_transacao)
                                                SELECT
                                                    CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time' AS dat_cadastro,
                                                    vwi.codproduto, 
	                                                vwi.desproduto, 
	                                                vwi.codean, 
	                                                vwi.ValorUn, 
	                                                vwi.Quantidade,
	                                                trs.cod_transacao
                                                FROM viewizio_3 AS vwi with(nolock)
                                                INNER JOIN tab_transacao AS trs with(nolock, INDEX (idx_transacao_005))
                                                    ON trs.dat_compra = vwi.datacompra
                                                    AND trs.vlr_compra = vwi.valorcompra
                                                    AND trs.cupom = vwi.cupom
                                                    AND trs.cod_loja = vwi.cod_loja
                                                WHERE vwi.cod_pessoa IS NOT NULL
                                                AND vwi.cod_pessoa > 0
                                                /* AND vwi.cod_loja = @cod_loja */
                                                AND NOT EXISTS(
                                                    SELECT
                                                        1
                                                    FROM tab_transacao_cpf AS ttc  with(nolock,INDEX (transacao_cpf_005))
                                                    WHERE ttc.dat_compra = vwi.datacompra
                                                    AND ttc.vlr_compra = vwi.ValorCompra
                                                    AND ttc.cupom = vwi.cupom
                                                    AND ttc.cod_loja = vwi.cod_loja)
                                                GROUP BY
                                                    vwi.codproduto,
                                                    vwi.desproduto,
                                                    vwi.codean,
                                                    vwi.ValorUn,
                                                    vwi.Quantidade,
                                                    vwi.item,
                                                    trs.cod_transacao;";

                //executa o insert
                sqlServer.Command.ExecuteNonQuery();

                #endregion

                sEtapa = "importa as transações não identificadas";
                #region insert na tab_transacao_cpf
                //importa as transações não identificadas
                sqlServer.Command.CommandText = @"INSERT INTO tab_transacao_cpf(
	                                                num_cpf,
	                                                dat_compra,
	                                                vlr_compra,
	                                                cod_loja,
	                                                dat_importacao,
	                                                qtd_itens_compra,
	                                                cod_usuario,
	                                                cupom)
                                                SELECT
                                                    vwi.CpfCliente,
	                                                vwi.datacompra,
	                                                vwi.valorcompra,
	                                                vwi.cod_loja,
	                                                CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time' AS dat_importacao,
                                                    COUNT(DISTINCT vwi.item) AS qtd_itens_compra,
                                                    vwi.cod_usuario,
	                                                vwi.cupom
                                                FROM viewizio_3 AS vwi with(nolock)
                                                LEFT JOIN tab_transacao_cpf AS trs with(nolock,INDEX (transacao_cpf_005))
                                                    ON trs.dat_compra = vwi.datacompra
                                                    AND trs.vlr_compra = vwi.valorcompra
                                                    AND trs.cupom = vwi.cupom
                                                    AND trs.cod_loja = vwi.cod_loja
                                                WHERE trs.dat_compra IS NULL
                                                AND vwi.cod_pessoa IS NOT NULL
                                                AND vwi.cod_pessoa = 0
                                                /* AND vwi.cod_loja = @cod_loja */
                                                AND NOT EXISTS(
                                                    SELECT
                                                        1
                                                    FROM tab_transacao AS ttc with(nolock, INDEX(idx_transacao_005))
                                                    WHERE ttc.dat_compra = vwi.datacompra
                                                    AND ttc.vlr_compra = vwi.ValorCompra
                                                    AND ttc.cupom = vwi.cupom
                                                    AND ttc.cod_loja = vwi.cod_loja)
                                                GROUP BY
                                                    vwi.cod_loja,
                                                    vwi.datacompra,
                                                    vwi.valorcompra,
                                                    vwi.cupom,
                                                    vwi.CpfCliente,
                                                    vwi.cod_usuario;";

                //executa o insert na tab_transacao_cpf
                sqlServer.Command.ExecuteNonQuery();

                #endregion

                sEtapa = "apaga os itens existentes para as transações da viewizio_3 para reinserir. - 2";
                #region insert na tab_transacao_itens_cpf
                //apaga os itens existentes para as transações da viewizio_3 para reinserir.
                //isto é feito para garantir que todos os itens sejam importados
                sqlServer.Command.CommandText = @"DELETE tri
                                                  FROM viewizio_3 AS vwi with(nolock, INDEX (idx_viewizio_001))
                                                  INNER JOIN tab_transacao_cpf AS trs with(nolock,INDEX (transacao_cpf_005))
	                                                  ON trs.dat_compra = vwi.datacompra
	                                                  AND trs.vlr_compra = vwi.valorcompra
	                                                  AND trs.cupom = vwi.cupom
	                                                  AND trs.cod_loja = vwi.cod_loja
                                                  INNER JOIN tab_transacao_itens_cpf AS tri with(nolock)
	                                                  ON trs.cod_tab_transacao_cpf = tri.cod_tab_transacao_cpf
                                                  /* WHERE
                                                      vwi.cod_loja = @cod_loja; */ ";

                //executa o delete
                sqlServer.Command.ExecuteNonQuery();

                //importa os itens das compras não identificadas
                sEtapa = "importa os itens das compras não identificadas";
                sqlServer.Command.CommandText = @"INSERT INTO tab_transacao_itens_cpf (
                                                    cod_tab_transacao_cpf,
	                                                dat_cadastro, 
	                                                cod_produto, 
	                                                des_produto, 
	                                                cod_nsu, 
	                                                vlr_item_compra, 
	                                                qtd_item_compra) 
                                                SELECT
                                                    trs.cod_tab_transacao_cpf,
	                                                CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time' AS dat_cadastro,
                                                    vwi.codproduto,
	                                                vwi.desproduto,
	                                                vwi.codean,
	                                                vwi.ValorUn, 
	                                                vwi.Quantidade
                                                FROM viewizio_3 AS vwi with(nolock)
                                                INNER JOIN tab_transacao_cpf AS trs with(nolock,INDEX (transacao_cpf_005))
                                                    ON trs.dat_compra = vwi.datacompra
                                                    AND trs.vlr_compra = vwi.valorcompra
                                                    AND trs.cupom = vwi.cupom
                                                    AND trs.cod_loja = vwi.cod_loja
                                                WHERE vwi.cod_pessoa IS NOT NULL
                                                AND vwi.cod_pessoa = 0
                                                /* AND vwi.cod_loja = @cod_loja */
                                                AND NOT EXISTS(
                                                    SELECT
                                                        1
                                                    FROM tab_transacao AS ttc with(nolock, INDEX(idx_transacao_005))
                                                    WHERE ttc.dat_compra = vwi.datacompra
                                                    AND ttc.vlr_compra = vwi.ValorCompra
                                                    AND ttc.cupom = vwi.cupom
                                                    AND ttc.cod_loja = vwi.cod_loja)
                                                GROUP BY
                                                    trs.cod_tab_transacao_cpf,
                                                    vwi.codproduto,
                                                    vwi.desproduto,
                                                    vwi.codean,
                                                    vwi.ValorUn,
                                                    vwi.Quantidade,
                                                    vwi.item;";

                //executa o insert
                sqlServer.Command.ExecuteNonQuery();

                #endregion

                sEtapa = "comando para buscar a maior data de compra existente na viewizio_3 para uma determinada loja";
                #region atualiza a tabela de controle de importação
                //comando para buscar a maior data de compra existente na viewizio_3 para uma determinada loja
                sqlServer.Command.CommandText = @"select cod_loja,max(datacompra) datacompra into #tmp_data_processamento from viewizio_3 with(nolock) group by cod_loja

                                                  update tcp
                                                  set 
                                                     tcp.dat_ult_importacao = tmp.datacompra
                                                  from
                                                     tab_controle_importacao tcp with(nolock),
                                                     #tmp_data_processamento tmp
                                                  where
                                                     tcp.cod_loja = tmp.cod_loja

                                                  drop table #tmp_data_processamento ";


                //sqlServer.Command.CommandText = @"declare @maxDatCompraCompleta as datetime
                //                                    declare @maxDatCompra as date
                //                                    declare @DatServidor as date
                //                                    declare @DatProxProcessamento as date

                //                                SELECT 
                //                                   @maxDatCompraCompleta = MAX(datacompra),
                //                                   @maxDatCompra = cast(max(datacompra) as date),
                //                                   @DatProxProcessamento = dateadd(HOUR,5,max(datacompra)), 
                //                                   @DatServidor = convert(varchar,CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time',112)
                //                                FROM viewizio_3 with(nolock) WHERE cod_loja = @cod_loja


                //                                if @maxDatCompra < @DatServidor
                //                                   select cast(concat(dateadd(dd,1,@maxDatCompra),' 06:00') as datetime)
                //                                else 
                //                                   select @maxDatCompraCompleta";

                //busca a data e guarda na variável
                //maxDatCompra = Convert.ToDateTime(sqlServer.Command.ExecuteScalar().ToString()).ToString("yyyy-MM-dd HH:mm:ss");

                ////comando para buscar a data atual no banco de dados
                ////isso é feito para que em caso de divergência de horário no servidor da aplicação, a lógica não seja afetada
                //sEtapa = "comando para buscar a data atual no banco de dados";
                //sqlConn.Command.CommandText = "SELECT CONVERT(datetimeoffset, getdate()) AT TIME ZONE 'E. South America Standard Time' ";

                ////busca a data e guarda na variável
                //sEtapa += System.Environment.NewLine + "busca a data e guarda na variável";
                //string dataAtualBanco = Convert.ToDateTime(sqlConn.Command.ExecuteScalar()).ToString("yyyy-MM-dd HH:mm:ss");

                ////se a maior data de compra desta loja for menor que a data atual, a variável 'maxDatCompra' recebe incremento de 1 dia
                ////isso é feito para que a aplicação vá executando até que a maior data de importação seja igual a data atual
                //sEtapa += System.Environment.NewLine + "se a maior data de compra desta loja for menor que a data atual, a variável 'maxDatCompra' recebe incremento de 1 dia";
                //if (Convert.ToDateTime(Convert.ToDateTime(maxDatCompra).ToString("yyyy-MM-dd")) < Convert.ToDateTime(dataAtualBanco))
                //    maxDatCompra = Convert.ToDateTime(maxDatCompra).AddDays(1).ToString("yyyy-MM-dd 06:00");

                //atualiza a tabela de controle de importação
                //sEtapa = "atualiza a tabela de controle de importação";
                //sqlConn.Command.CommandText = @"UPDATE tab_controle_importacao
                //                                SET dat_ult_importacao = @max_dat_compra,
                //                                 id_processando = 'N', 
                //                                 id_precedencia_executada = 'S',
                //                                 dat_inicio_proc = null
                //                                WHERE
                //                                    cod_loja = @cod_loja;";

                ////parametro max_dat_compra
                //IDbDataParameter pMaxDatCompra = sqlConn.Command.CreateParameter();
                //pMaxDatCompra.ParameterName = "@max_dat_compra";
                //pMaxDatCompra.Value = Convert.ToDateTime(maxDatCompra).ToString("yyyyMMdd HH:mm:ss");
                //sqlConn.Command.Parameters.Add(pMaxDatCompra);

                //executa o upadte
                sqlServer.Command.ExecuteNonQuery();

                #endregion

                #region insert na tab_transacao_meiopagto
                //insert das formas de pagamento das transações identificadas
                sEtapa = "insert das formas de pagamento das transações identificadas";
                sqlServer.Command.CommandText = @"INSERT INTO tab_transacao_meiopagto(
                                                    cod_transacao, 
	                                                nom_tipo_pagamento)
                                                SELECT
                                                    trs.cod_transacao,
	                                                vwi.meiopagto
                                                FROM viewizio_3 AS vwi with(nolock)
                                                INNER JOIN tab_transacao AS trs   with(nolock, INDEX(idx_transacao_005))
                                                    ON trs.dat_compra = vwi.datacompra
                                                    AND trs.vlr_compra = vwi.valorcompra
                                                    AND trs.cupom = vwi.cupom
                                                    AND trs.cod_loja = vwi.cod_loja
                                                LEFT JOIN tab_transacao_meiopagto AS tpg with(nolock, INDEX(idx_meio_pagto_002))
                                                    ON tpg.cod_transacao = trs.cod_transacao
                                                WHERE tpg.cod_transacao IS NULL
                                                /* AND vwi.cod_loja = @cod_loja */
                                                GROUP BY
                                                    trs.cod_transacao,
	                                                vwi.meiopagto; ";

                //executa o insert
                sqlServer.Command.ExecuteNonQuery();


                //insert das formas de pagamento das transações não identificadas
                sEtapa = "insert das formas de pagamento das transações não identificadas";
                sqlServer.Command.CommandText = @"INSERT INTO tab_transacao_cpf_meiopagto(
                                                    cod_tab_transacao_cpf, 
	                                                nom_tipo_pagamento)
                                                SELECT
                                                    trs.cod_tab_transacao_cpf,
	                                                vwi.meiopagto
                                                FROM viewizio_3 AS vwi with(nolock)
                                                INNER JOIN tab_transacao_cpf AS trs with(nolock,INDEX (transacao_cpf_005))
                                                    ON trs.dat_compra = vwi.datacompra
                                                    AND trs.vlr_compra = vwi.valorcompra
                                                    AND trs.cupom = vwi.cupom
                                                    AND trs.cod_loja = vwi.cod_loja
                                                LEFT JOIN tab_transacao_cpf_meiopagto AS tpg with(nolock, INDEX(idx_meio_pagto_002))
                                                    ON tpg.cod_tab_transacao_cpf = trs.cod_tab_transacao_cpf
                                                WHERE tpg.cod_tab_transacao_cpf IS NULL
                                                /* AND vwi.cod_loja = @cod_loja */
                                                GROUP BY
                                                    trs.cod_tab_transacao_cpf,
	                                                vwi.meiopagto; ";

                //executa o insert
                sqlServer.Command.ExecuteNonQuery();

                #endregion;

                sEtapa = "Apaga os registros importados da viewizio_3";
                #region "Apaga os registros importados da viewizio_3, porem faz um backup na viewizio_3_bkp - Otimizar o processamento"

                sqlServer.Command.CommandText = @"insert into viewizio_3_bkp
                                                  select  
                                                     vwi.*
                                                  FROM viewizio_3 AS vwi with(nolock)
                                                  LEFT JOIN tab_transacao AS trs with(nolock,INDEX (idx_transacao_005))
                                                      ON trs.dat_compra = vwi.datacompra
                                                      AND trs.vlr_compra = vwi.valorcompra
                                                      AND trs.cupom = vwi.cupom
                                                      AND trs.cod_loja = vwi.cod_loja
                                                  WHERE trs.dat_compra IS not NULL
                                                  AND vwi.cod_pessoa IS NOT NULL
                                                  AND vwi.cod_pessoa > 0  
                                                  AND NOT EXISTS(
                                                                     SELECT
                                                                         1
                                                                     FROM tab_transacao_cpf AS ttc with(nolock,INDEX (transacao_cpf_005))
                                                                     WHERE ttc.dat_compra = vwi.datacompra
                                                                     AND ttc.vlr_compra = vwi.ValorCompra
                                                                     AND ttc.cupom = vwi.cupom
                                                                     AND ttc.cod_loja = vwi.cod_loja
                                                                 )
                                                  
                                                  
                                                  delete vwi
                                                  FROM viewizio_3 AS vwi with(nolock)
                                                  LEFT JOIN tab_transacao AS trs with(nolock,INDEX (idx_transacao_005))
                                                      ON trs.dat_compra = vwi.datacompra
                                                      AND trs.vlr_compra = vwi.valorcompra
                                                      AND trs.cupom = vwi.cupom
                                                      AND trs.cod_loja = vwi.cod_loja
                                                  WHERE trs.dat_compra IS not NULL
                                                  AND vwi.cod_pessoa IS NOT NULL
                                                  AND vwi.cod_pessoa > 0
                                                  AND NOT EXISTS(
                                                                     SELECT
                                                                         1
                                                                     FROM tab_transacao_cpf AS ttc with(nolock,INDEX (transacao_cpf_005))
                                                                     WHERE ttc.dat_compra = vwi.datacompra
                                                                     AND ttc.vlr_compra = vwi.ValorCompra
                                                                     AND ttc.cupom = vwi.cupom
                                                                     AND ttc.cod_loja = vwi.cod_loja
                                                                 )
                                                  
                                                  insert into viewizio_3_bkp
                                                  SELECT 
                                                     vwi.*
                                                  FROM viewizio_3 AS vwi with(nolock)
                                                  LEFT JOIN tab_transacao_cpf AS trs with(nolock,INDEX (transacao_cpf_005))
                                                      ON trs.dat_compra = vwi.datacompra
                                                      AND trs.vlr_compra = vwi.valorcompra
                                                      AND trs.cupom = vwi.cupom
                                                      AND trs.cod_loja = vwi.cod_loja
                                                  WHERE trs.dat_compra IS not NULL
                                                  AND vwi.cod_pessoa IS NOT NULL
                                                  AND vwi.cod_pessoa = 0
                                                  /* AND vwi.cod_loja = @cod_loja */
                                                  AND NOT EXISTS(
                                                      SELECT
                                                          1
                                                      FROM tab_transacao AS ttc with(nolock, INDEX(idx_transacao_005))
                                                      WHERE ttc.dat_compra = vwi.datacompra
                                                      AND ttc.vlr_compra = vwi.ValorCompra
                                                      AND ttc.cupom = vwi.cupom
                                                      AND ttc.cod_loja = vwi.cod_loja)
                                                  
                                                  delete vwi
                                                  FROM viewizio_3 AS vwi with(nolock)
                                                  LEFT JOIN tab_transacao_cpf AS trs with(nolock,INDEX (transacao_cpf_005))
                                                      ON trs.dat_compra = vwi.datacompra
                                                      AND trs.vlr_compra = vwi.valorcompra
                                                      AND trs.cupom = vwi.cupom
                                                      AND trs.cod_loja = vwi.cod_loja
                                                  WHERE trs.dat_compra IS not NULL
                                                  AND vwi.cod_pessoa IS NOT NULL
                                                  AND vwi.cod_pessoa = 0
                                                  /* AND vwi.cod_loja = @cod_loja */
                                                  AND NOT EXISTS(
                                                      SELECT
                                                          1
                                                      FROM tab_transacao AS ttc with(nolock, INDEX(idx_transacao_005))
                                                      WHERE ttc.dat_compra = vwi.datacompra
                                                      AND ttc.vlr_compra = vwi.ValorCompra
                                                      AND ttc.cupom = vwi.cupom
                                                      AND ttc.cod_loja = vwi.cod_loja)";

                //executa o insert
                sqlServer.Command.ExecuteNonQuery();

                #endregion


                //grava todas as mudanças no banco de dados
                sEtapa = "grava todas as mudanças no banco de dados";
                sqlServer.Commit();

                //retorna o objeto 'retorno' e sai do processamento
                return retorno;
            }
            //se ocorrer algum erro no processamento preenche o objeto 'retorno' com o erro e guarda o log
            catch (Exception ex)
            {
                //desfaz tudo o que foi feito no banco de dados
                sqlServer.Rollback();

                //grava o log do erro
                Log.inserirLogException(sNomeCliente, ex, 0);

                //lança a exception
                //throw new Exception("Etapa: " + sEtapa + System.Environment.NewLine + ex.Message, new Exception(ex.ToString()));

                if (retorno.errors == null)
                {
                    retorno.errors = new List<Erros>();
                }
                retorno.errors.Add(new Erros { code = Convert.ToInt32(HttpStatusCode.InternalServerError).ToString(), message = ErroBancoDeDados });

                return retorno;
            }
            //sempre fecha a conexão com o banco de dados
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

        #endregion

    }
}