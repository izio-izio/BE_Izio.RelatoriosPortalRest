using Newtonsoft.Json;
using System;

/// <summary>
/// Classe responsável por realizar a exclusão (cancelamento) da compra na base do Izio
/// </summary>
namespace TransacaoIzioRest.Models
{
    /// <summary>
    /// Objeto com os dados da compra cancelada
    /// </summary>
    public class DadosTransacaoCancelada
    {
        /// <summary>
        /// Data e hora da realização da compra
        /// </summary>
        [JsonRequired]
        public DateTime dat_compra { get; set; }

        /// <summary>
        /// Valor da compra
        /// </summary>
        [JsonRequired]
        public decimal vlr_compra { get; set; }

        /// <summary>
        /// Codigo da loja que foi realizado a compra
        /// </summary>
        [JsonRequired]
        public long cod_loja { get; set; }

        /// <summary>
        /// Quantidade de itens da compra
        /// </summary>
        [JsonRequired]
        public Int32 qtd_itens_compra { get; set; }

        /// <summary>
        /// Numero do cupom fiscal 
        /// </summary>
        [JsonRequired]
        public string cupom { get; set; }
    }

    /// <summary>
    /// Objeto para cancelar o crédito gerado na marketpay
    /// </summary>
    public class DadosCancelamentoMarketPAy
    {
        public string cnpjEstabelecimento { get; set; }
        public Dadostransacaooriginal dadosTransacaoOriginal { get; set; }
        public string dataHoraTransacao { get; set; }
        public int idCartao { get; set; }
        public string nsuOrigem { get; set; }
        public int tipoTransacao { get; set; }
        public decimal valor { get; set; }
    }

    /// <summary>
    /// Dados da transação que gerou o crédito
    /// </summary>
    public class Dadostransacaooriginal
    {
        public string dataHoraTransacao { get; set; }
        public string nsuTransacaoOriginal { get; set; }
        public int tipoTransacaoOriginal { get; set; }
    }


}