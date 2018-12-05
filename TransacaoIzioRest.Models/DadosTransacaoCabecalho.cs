using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransacaoIzioRest.Models
{
    /// <summary>
    /// Dados das transações cabeçalhos
    /// </summary>
    public class DadosTransacaoCabecalho
    {
        /// <summary>
        /// Primary key da tabela tab_transacao_cabecalho
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? cod_transacao_cabecalho { get; set; }

        /// <summary>
        /// Codigo do CPF preenchido no PDV no inicio da compra
        /// </summary>
        [JsonRequired]
        public string cod_cpf { get; set; }

        /// <summary>
        /// Número do cupom fiscal
        /// </summary>
        [JsonRequired]
        public string cupom { get; set; }

        /// <summary>
        /// Código da loja que foi realizado a compra
        /// </summary>
        [JsonRequired]
        public int cod_loja { get; set; }

        /// <summary>
        /// Valor da compra
        /// </summary>
        [JsonRequired]
        public int vlr_compra { get; set; }

        /// <summary>
        /// Quantidade de itens da compra
        /// </summary>
        [JsonRequired]
        public int qtd_itens_compra { get; set; }

        /// <summary>
        /// Data e hora da compra (yyyy-MM-dd HH:mm:ss)
        /// </summary>
        [JsonRequired]
        public DateTime dat_compra { get; set; }

        /// <summary>
        /// Data da inserção do registro
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? dat_cadastro { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Objeto de retorno com os dados das transacões cabeçalho
    /// </summary>
    public class RetornoDadosTransacaoCabecalho
    {
        /// <summary>
        /// Lista com os dados das transações cabeçalho
        /// </summary>
        public List<DadosTransacaoCabecalho> payload { get; set; }
    }

    /// <summary>
    /// Objeto com os campos para a consulta em lote e paginada das transações cabeçalhos
    /// </summary>
    public class DadosConsultaPaginadoTransacaoCabecalho
    {
        /// <summary>
        /// Data e hora para inicio da consulta
        /// </summary>
        public DateTime dat_inicio_consulta { get; set; }

        /// <summary>
        /// Data e hora para o final da consulta
        /// </summary>
        public DateTime dat_final_consulta { get; set; }

        /// <summary>
        /// Pagina inicial da consulta (inicia em 1)
        /// </summary>
        public int pageIndex { get; set; }

        /// <summary>
        /// Tamanho maximo da página (lote)
        ///    - limitado a 1000 por página (lote)
        /// </summary>
        public int pageSize { get; set; }
    }

    /// <summary>
    /// Dados da consulta em lote e paginada de pessoas
    /// </summary>
    public class DadosLoteTransacaoCabecalhoPaginado
    {
        /// <summary>
        /// Quantidade de Pessoas encontradas na consulta
        /// </summary>
        public int totalRegistros { get; set; }
        /// <summary>
        /// Total de paginas, para a busca paginada
        /// </summary>
        public int totalPaginas { get; set; }
        /// <summary>
        /// Lista paginada (lote) das transações cabeçalhos
        /// </summary>
        public List<DadosTransacaoCabecalho> listaTransacaoCabecalho { get; set; }
    }

    /// <summary>
    /// Objeto de retorno de sucesso na consulta em lote e paginada das transações cabeçalhos
    /// </summary>
    public class RetornoLotePaginado
    {
        /// <summary>
        /// Payload de saida do lote paginado
        /// </summary>
        public DadosLoteTransacaoCabecalhoPaginado payload { get; set; }
    }
}
