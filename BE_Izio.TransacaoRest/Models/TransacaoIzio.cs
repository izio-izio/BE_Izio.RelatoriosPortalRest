namespace TransacaoIzioRest.Models
{
    using System;
    using System.Collections.Generic;

    #region Dados da Transacao
    /// <summary>
    /// Objeto de retorno para quando a execução ocorrer com sucesso
    /// </summary>
    public class RetornoDadosTransacao
    {
        public Payload payload { get; set; }
    }

    /// <summary>
    /// Objeto de Retorno para utilização interna, para o processamento na DAO
    /// </summary>
    public class DadosConsultaTransacao
    {
        public Payload payload { get; set; }

        public List<Erros> errors { get; set; }
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
        public long cod_transacao { get; set; }
        public long cod_pessoa { get; set; }
        public DateTime dat_compra { get; set; }
        public decimal vlr_compra	 { get; set; }
        public long cod_loja	 { get; set; }
        public string des_loja { get; set; }
        public Int32 qtd_itens_compra	 { get; set; }
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

        public List<Erros> errors { get; set; }
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
        public long cod_transacao { get; set; }
        public Int64 cod_plu { get; set; }
        public Int64 cod_ean { get; set; }
        public string des_produto { get; set; }
        public decimal vlr_item_compra { get; set; }
        public decimal qtd_item_compra { get; set; }
        public string img_produto { get; set; }
    }

    #endregion
    
    /// <summary>
    /// Retorna objeto com os erros ocorridos
    /// </summary>
    public class ListaErros
    {
        public List<Erros> errors { get; set; }
    }

    /// <summary>
    /// Classe que retornas as lista de erros 
    /// </summary>
    public class Erros
    {
        public string code { get; set; }
        public string message { get; set; }
    }

}