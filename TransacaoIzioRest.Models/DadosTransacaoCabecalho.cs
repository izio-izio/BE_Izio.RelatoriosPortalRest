using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public decimal vlr_compra { get; set; }

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
    /// Objeto de retorno com os dados das transacões cabeçalhos
    /// </summary>
    public class RetornoDadosTransacaoCabecalho
    {
        /// <summary>
        /// Lista com os dados das transações cabeçalhos
        /// </summary>
        public List<DadosTransacaoCabecalho> payload { get; set; }
    }

    /// <summary>
    /// Dados para atualização da transação cabeçalho
    /// </summary>
    public class DadosTransacaoCabecalhoPatch
    {
        /// <summary>
        /// Primary key da tabela tab_transacao_cabecalho
        /// </summary>
        public int cod_transacao_cabecalho { get; set; }

        /// <summary>
        /// Codigo do CPF preenchido no PDV no inicio da compra
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string cod_cpf { get; set; }

        /// <summary>
        /// Número do cupom fiscal
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string cupom { get; set; }

        /// <summary>
        /// Código da loja que foi realizado a compra
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? cod_loja { get; set; }

        /// <summary>
        /// Valor da compra
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? vlr_compra { get; set; }

        /// <summary>
        /// Quantidade de itens da compra
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? qtd_itens_compra { get; set; }

        /// <summary>
        /// Data e hora da compra (yyyy-MM-dd HH:mm:ss)
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? dat_compra { get; set; }
    }

    /// <summary>
    /// Objeto de retorno com os dados atualizado da transacão cabeçalho
    /// </summary>
    public class RetornoPatchTransacaoCabecalho
    {
        /// <summary>
        /// Lista com os dados da transação cabeçalho
        /// </summary>
        public DadosTransacaoCabecalhoPatch payload { get; set; }
    }
}
