namespace TransacaoIzioRest.Models
{
    using System;
    using System.Collections.Generic;

    #region Dados da Transacao
    /// <summary>
    /// Objeto de retorno para quando a execução ocorrer com sucesso
    /// </summary>
    public class RetornoConsultaTransacao
    {
        public Payload payload { get; set; }
    }

    /// <summary>
    /// Objeto de Retorno para utilização interna, para o processamento na DAO
    /// </summary>
    public class DadosConsultaTransacao
    {
        public Payload payload { get; set; }

        public List<ErrosConsultaTransacao> errors { get; set; }
    }

    /// <summary>
    /// Objeto padrão de retorno da API Rest
    /// </summary>
    public class Payload
    {
        public List<DadosTransacao> listaTransacao { get; set; }
    }
    
    /// <summary>
    /// Dados da Transacao
    /// </summary>
    public class DadosTransacao
    {
        /// <summary>
        /// Codigo da Transação
        /// </summary>
        public long cod_transacao { get; set; }

        /// <summary>
        /// Codigo da pessoa 
        /// </summary>
        public long cod_pessoa { get; set; }

        /// <summary>
        /// Data e hora da compra
        /// </summary>
        public DateTime dat_compra { get; set; }

        /// <summary>
        /// Valor da compra
        /// </summary>
        public decimal vlr_compra	 { get; set; }

        /// <summary>
        /// Código da Loja
        /// </summary>
        public long cod_loja	 { get; set; }

        /// <summary>
        /// Nome da loja
        /// </summary>
        public string des_loja { get; set; }

        /// <summary>
        /// Quantidade de Itens da Compra
        /// </summary>
        public Int32 qtd_itens_compra	 { get; set; }

        /// <summary>
        /// Numero do cupom da compra
        /// </summary>
        public string cupom	 { get; set; }
    }

    #endregion

    #region Itens da Transacao

    /// <summary>
    /// Objeto de retorno para quando a execução ocorrer com sucesso
    /// </summary>
    public class RetornoDadosItensTransacao
    {
        public PayloadItensTransacao payload { get; set; }
    }

    /// <summary>
    /// Objeto de Retorno para utilização interna, para o processamento na DAO
    /// </summary>
    public class DadosConsultaItensTransacao
    {
        public PayloadItensTransacao payload { get; set; }

        public List<ErrosConsultaTransacao> errors { get; set; }
    }

    /// <summary>
    /// Objeto padrão de retorno da API Rest
    /// </summary>
    public class PayloadItensTransacao
    {
        public List<DadosItensTransacao> listaItensTransacao { get; set; }
    }

    /// <summary>
    /// Dados com os itens da transacao
    /// </summary>
    public class DadosItensTransacao
    {
        /// <summary>
        /// Codigo da Transação - Chave para os itens da compra
        /// </summary>
        public long cod_transacao { get; set; }

        /// <summary>
        /// Código interno do produto
        /// </summary>
        public Int64 cod_plu { get; set; }

        /// <summary>
        /// Codigo de barras
        /// </summary>
        public Int64 cod_ean { get; set; }

        /// <summary>
        /// Nome do produto
        /// </summary>
        public string des_produto { get; set; }

        /// <summary>
        /// Valor de venda do produto
        /// </summary>
        public decimal vlr_item_compra { get; set; }

        /// <summary>
        /// Quantidade de itens vendidas do produto
        /// </summary>
        public decimal qtd_item_compra { get; set; }

        /// <summary>
        /// Imagem do produto em Base64
        /// </summary>
        public string img_produto { get; set; }
    }

    #endregion
    
    /// <summary>
    /// Retorna objeto com os erros ocorridos
    /// </summary>
    public class ListaErrosConsultaTransacao
    {
        public List<ErrosConsultaTransacao> errors { get; set; }
    }

    /// <summary>
    /// Classe que retornas as lista de erros 
    /// </summary>
    public class ErrosConsultaTransacao
    {
        public string code { get; set; }
        public string message { get; set; }
    }

}