using System;
using System.Collections.Generic;

namespace TransacaoIzioRest.Models
{

    /// <summary>
    /// Objeto de retorno para quando a execução ocorrer com sucesso
    /// </summary>
    public class RetornoDadosTermino
    {
        /// <summary>
        /// Payload de Retorno.
        /// </summary>
        public PayloadTermino payload { get; set; }
    }

    /// <summary>
    /// Objeto de Retorno para utilização interna, para o processamento na DAO
    /// </summary>
    public class PayloadTermino
    {
        /// <summary>
        /// Quantidade de registros importados.
        /// </summary>
        public Int64 qtd_registros_importados { get; set; }

        /// <summary>
        /// Quantidade de vendas importadas.
        /// </summary>
        public Int64 qtd_vendas { get; set; }

        /// <summary>
        /// Somatório do valor de compras do dia importado.
        /// </summary>
        public Decimal vlr_total_vendas { get; set; }

        /// <summary>
        /// Data das vendas
        /// </summary>
        public DateTime dat_compra { get; set; }

        /// <summary>
        /// Lista de detalhamento dos valores por loja.
        /// </summary>
        public List<ComprasLoja> lst_lojas { get; set; }
    }

    /// <summary>
    /// Dados de Compras da Loja
    /// </summary>
    public class ComprasLoja
    {

        /// <summary>
        /// Código da Loja
        /// </summary>
        public int cod_loja { get; set; }

        /// <summary>
        /// Somatório das compras da Loja.
        /// </summary>
        public Decimal vlr_vendas { get; set; }

        /// <summary>
        /// Quantidade de vendas importadas.
        /// </summary>
        public Int64 qtd_vendas { get; set; }

    }

    public class DadosTransacaoTermino
    {
        /// <summary>
        /// Quantidade de registros enviados para processamento.
        /// </summary>
        public Int64 qtde_registros { get; set; }

        /// <summary>
        /// Somatório do valor de compras do dia enviado.
        /// </summary>
        public Decimal vlr_compras { get; set; }

        /// <summary>
        /// Lista de detalhamento dos valores por loja.
        /// </summary>
        public List<ComprasLojaTermino> lstLoja { get; set; }
    }

    public class ComprasLojaTermino
    {

        /// <summary>
        /// Código da Loja
        /// </summary>
        public int cod_loja { get; set; }

        /// <summary>
        /// Somatório das compras da Loja.
        /// </summary>
        public Decimal vlr_loja { get; set; }


    }



}