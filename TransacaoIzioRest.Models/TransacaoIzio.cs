using System;
using System.Collections.Generic;

// <summary>
// Classe para consulta das ultimas compras do cliente
// </summary>
namespace TransacaoIzioRest.Models
{
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
        /// Valor total de desconto aplicado nos itens vendidos
        /// </summary>
        public decimal? vlr_total_desconto { get; set; }

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

        /// <summary>
        /// Valor de cashback gerado na compra
        /// </summary>
        public decimal? vlr_credito_cashback { get; set; }

        /// <summary>
        /// Data de validade para o cashback gerado na compra
        /// </summary>
        public DateTime? dat_validade_cashback { get; set; }
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
    /// Objeto padrão de retorno da API Rest
    /// </summary>
    public class PayloadItensTransacao
    {
        public List<DadosConsultaItensTransacao> listaItensTransacao { get; set; }
    }

    /// <summary>
    /// Dados com os itens da transacao
    /// </summary>
    public class DadosConsultaItensTransacao
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

        /// <summary>
        /// Valor do desconto aplicado no item
        /// </summary>
        public decimal? vlr_desconto_item { get; set; }

        /// <summary>
        /// Valor do cashback do item
        /// </summary>
        public decimal? vlr_credito { get; set; }
    }

    #endregion

    #region Total Desconto Por Pessoa

    /// <summary>
    /// Objeto de retorno para quando a execução ocorrer com sucesso
    /// </summary>
    public class RetornoDadosTotalDesconto
    {
        public List<DadosConsultaTotalDesconto> payload { get; set; }
    }


    /// <summary>
    /// Objeto de Retorno para utilização interna, para o processamento na DAO
    /// </summary>
    public class DadosConsultaDesconto
    {
        public List<DadosConsultaTotalDesconto> payload { get; set; }
    }

    /// <summary>
    /// Objeto padrão de retorno da API Rest
    /// </summary>
    public class PayloadTotalDesconto
    {
        public List<DadosConsultaTotalDesconto> listaTotalDesconto { get; set; }
    }

    /// <summary>
    /// Dados com os itens da transacao
    /// </summary>
    public class DadosConsultaTotalDesconto
    {


        /// <summary>
        /// Codigo da pessoa 
        /// </summary>
        public long cod_pessoa { get; set; }

        /// <summary>
        /// Codigo da Transação - Chave para os itens da compra
        /// </summary>
        public Int64 qtd_transacao { get; set; }

        /// <summary>
        /// Código interno do produto
        /// </summary>
        public decimal vlr_total_desconto { get; set; }

        
    }

    #endregion

    public class DadosClienteIzio
    {
        public int id { get; set; }
        public string des_chave_ws { get; set; }
        public string des_token_rest { get; set; }
        public string des_nome_cliente { get; set; }
        public string des_string_conexao { get; set; }
    }

    public class DadosConsumirFila
    {
        /// <summary>
        /// Nome do cliente
        /// </summary>
        public string des_nome_cliente { get; set; }
        /// <summary>
        /// Quantidade de mensagens lidas da fila
        /// </summary>
        public int qtd_mensagens_fila { get; set; }
        /// <summary>
        /// Observação sobre o consumo da fila, usado para quando dá erro no processamento
        /// </summary>
        public string des_observacao { get; set; }
    }

    public class RetornoTransacao<T>
    {
        public List<T> payload { get; set; }
    }
}