using Newtonsoft.Json;
using System;
//using System.ComponentModel.DataAnnotations;

namespace TransacaoRest.Models
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
}