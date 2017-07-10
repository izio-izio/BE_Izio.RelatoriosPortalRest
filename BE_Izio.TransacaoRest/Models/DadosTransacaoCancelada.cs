using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TransacaoRest.Models
{
    /// <summary>
    /// Objeto de Retorno para utilização interna, para o processamento na DAO
    /// </summary>
    public class RetornoRemoveTransacao
    {
        public Payload payload { get; set; }

        public List<Erros> errors { get; set; }
    }

    /// <summary>
    /// Objeto de retorno de sucesso na exclusão das compras canceladas
    /// </summary>
    public class RetornoSucessoRemoverTransacao
    {
        public Payload payload { get; set; }
    }

    /// <summary>
    /// Objeto de retorno de sucesso na exclusão das compras canceladas
    /// </summary>
    public class RetornoErroRemoverTransacao
    {
        public List<Erros> errors { get; set; }
    }
    
    /// <summary>
    /// Objeto com os dados da compra cancelada
    /// </summary>
    public class DadosTransacaoCancelada
    {
        /// <summary>
        /// Data e hora da realização da compra
        /// </summary>
        [Required]
        public DateTime dat_compra { get; set; }

        /// <summary>
        /// Valor da compra
        /// </summary>
        [Required]
        public decimal vlr_compra { get; set; }

        /// <summary>
        /// Codigo da loja que foi realizado a compra
        /// </summary>
        [Required]
        public long cod_loja { get; set; }

        /// <summary>
        /// Quantidade de itens da compra
        /// </summary>
        [Required]
        public Int32 qtd_itens_compra { get; set; }

        /// <summary>
        /// Numero do cupom fiscal 
        /// </summary>
        [Required]
        public string cupom { get; set; }
    }
}