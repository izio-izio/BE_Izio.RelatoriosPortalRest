﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

/// <summary>
/// Classe para importação das vendas dos clientes. Online (compra a compra no final da venda) ou em lote (de tempos em tempos)
/// </summary>
namespace TransacaoIzioRest.Models
{
    /// <summary>
    /// Dados da transação importada On-Line
    /// </summary>
    public class DadosTransacaoOnline
    {
        /// <summary>
        /// Codigo do CPF preenchido no PDV no inicio da compra
        /// </summary>
        [JsonRequired]
        public string cod_cpf { get; set; }
        /// <summary>
        /// Codigo da equipe (enviar 100)
        /// </summary>
        public long? cod_equipe { get; set; }

        /// <summary>
        /// Codigo da pessoa,se não tiver enviar 0
        /// </summary>
        public long? cod_pessoa { get; set; } = 0;

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
        /// Meios de pagamentos utilizados na compra (quando tiver mais de 1, separar eles por ";". Ex: Cartão Visa; Cartão Master; Dinheiro)
        /// </summary>
        [JsonRequired]
        public string nom_tipo_pagamento { get; set; }

        /// <summary>
        /// Valor pago em cada meio de pagamento (quando tiver mais de 1, separar eles por ";". Ex: 11.00; 19.00; 20.00)
        /// </summary>
        public string vlr_meiopagto { get; set; }

        /// <summary>
        /// Codigo da loja que foi realizado a compra
        /// </summary>
        [JsonRequired]
        public long cod_loja { get; set; }

        /// <summary>
        /// Numero do PDV da realização da compraS
        /// </summary>
        public string cod_pdv { get; set; }
        /// <summary>
        /// Codigo do operador do pdv
        /// </summary>
        public long? cod_usuario { get; set; } = 1;

        /// <summary>
        /// Quantidade de itens da compra
        /// </summary>
        [JsonRequired]
        public Int32 qtd_itens_compra { get; set; }

        /// <summary>
        /// Valor total de desconto aplicado nos itens vendidos
        /// </summary>
        public decimal? vlr_total_desconto { get; set; }

        /// <summary>
        /// Numero do cupom fiscal 
        /// </summary>
        [JsonRequired]
        public string cupom { get; set; }

        /// <summary>
        /// Nsu da transação quando for pago em cartão. Caso a compra seja paga em mais de um cartão, os NSU precisam vir separados por ";"
        /// </summary>
        public string nsu_transacao { get; set; }

        /// <summary>
        /// Data e Hota da geração Nsu da transação quando for pago em cartão. Caso a compra seja paga em mais de um cartão, as datas de geração do NSU precisam vir separados por ";"
        /// </summary>
        public string dat_geracao_nsu { get; set; }

        /// <summary>
        /// BIN do cartão utilizado no pagamento da venda. Caso a compra seja paga em mais de um cartão, os BINs precisam vir separados por ";"
        /// </summary>
        public string des_bin_cartao { get; set; }

        /// <summary>
        /// Valor para campanha do tipo do troco
        /// </summary>
        public decimal? vlr_troco { get; set; }

        /// <summary>
        /// Valor para campanha do tipo do troco
        /// </summary>
        [JsonRequired]
        public List<DadosItensTransacao> ListaItens { get; set; }
    }

    /// <summary>
    /// Itens da transação para importacao On-Line
    /// </summary>
    public class DadosItensTransacao
    {
        /// <summary>
        /// Codigo interno do produto
        /// </summary>
        [JsonRequired]
        public long cod_produto { get; set; }

        /// <summary>
        /// Nome completo do produto
        /// </summary>
        [JsonRequired]
        public string des_produto { get; set; }

        /// <summary>
        /// Codigo de barras do produto, quando não tiver enviar o mesmo do codigo do produto
        /// </summary>
        [JsonRequired]
        public string cod_ean { get; set; }

        /// <summary>
        /// Valor de venda do produto
        /// </summary>
        [JsonRequired]
        public decimal vlr_item_compra { get; set; }

        /// <summary>
        /// Quantidade de produtos comprados
        /// </summary>
        [JsonRequired]
        public decimal qtd_item_compra { get; set; }

        /// <summary>
        /// Valor do desconto aplicado no item
        /// </summary>
        public decimal? vlr_desconto_item { get; set; }
    }

    /// <summary>
    /// Dados para importação em Lote das vendas Vendas
    /// </summary>
    public class DadosTransacaoLote
    {
        /// <summary>
        /// Codigo da pessoa, se não tiver enviar 0
        /// </summary>
        [JsonRequired]
        public long cod_pessoa { get; set; }

        /// <summary>
        /// Codigo do cpf do cliente informado no PDV
        /// </summary>
        [JsonRequired]
        public string cod_cpf { get; set; }

        /// <summary>
        /// Data e hora da compra
        /// </summary>
        [JsonRequired]
        public DateTime dat_compra { get; set; }

        /// <summary>
        /// Valor da compra
        /// </summary>
        [JsonRequired]
        public decimal vlr_compra { get; set; }

        /// <summary>
        /// Numero do cupom fiscal
        /// </summary>
        [JsonRequired]
        public string cupom { get; set; }

        /// <summary>
        /// Nsu da transação quando for pago em cartão. Caso a compra seja paga em mais de um cartão, os NSU precisam vir separados por ";"
        /// </summary>
        public string nsu_transacao { get; set; }

        /// <summary>
        /// Data e Hota da geração Nsu da transação quando for pago em cartão. Caso a compra seja paga em mais de um cartão, as datas de geração do NSU precisam vir separados por ";"
        /// </summary>
        public string dat_geracao_nsu { get; set; }

        /// <summary>
        /// Numero do PDV da realização da compraS
        /// </summary>
        public string Pdv { get; set; }

        /// <summary>
        /// Meios de pagamanto da compra. Quando tiver mais de 1 separar com ";". Ex: Cartão Visa; Cartão Master; Dinheiro
        /// </summary>
        [JsonRequired]
        public string des_tipo_pagamento { get; set; }

        /// <summary>
        /// Valor pago em cada meio de pagamento (quando tiver mais de 1, separar eles por ";". Ex: 11.00; 19.00; 20.00)
        /// </summary>
        public string vlr_meiopagto { get; set; }

        /// <summary>
        /// Quantidade de itens da compra
        /// </summary>
        [JsonRequired]
        public Int32 qtd_itens_compra { get; set; }

        /// <summary>
        /// Valor total de desconto aplicado nos itens vendidos
        /// </summary>
        public decimal? vlr_total_desconto { get; set; }

        /// <summary>
        /// Codigo de barras. Se não tiver enviar o codigo interno do produto
        /// </summary>
        [JsonRequired]
        public string cod_ean { get; set; }

        /// <summary>
        /// Codigo interno do produto
        /// </summary>
        [JsonRequired]
        public string cod_produto { get; set; }

        /// <summary>
        /// Nome Completo do produto
        /// </summary>
        [JsonRequired]
        public string des_produto { get; set; }

        /// <summary>
        /// Valor de venda do produto
        /// </summary>
        [JsonRequired]
        public decimal vlr_item_compra { get; set; }

        /// <summary>
        /// Valor do desconto aplicado no item
        /// </summary>
        public decimal? vlr_desconto_item { get; set; }
        
        /// <summary>
        /// Quantidade do produto comprado
        /// </summary>
        [JsonRequired]
        public decimal qtd_item_compra { get; set; }

        /// <summary>
        /// Numero do item na compra. Ex: Compra com 10 itens, teremos o item 1, item2, etc...
        /// </summary>
        [JsonRequired]
        public decimal nro_item_compra { get; set; }

        /// <summary>
        /// Codigo da loja que foi realizado a compra
        /// </summary>
        [JsonRequired]
        public long cod_loja { get; set; }

        /// <summary>
        /// BIN do cartão utilizado no pagamento da venda. Caso a compra seja paga em mais de um cartão, os BINs precisam vir separados por ";"
        /// </summary>
        public string des_bin_cartao { get; set; }

        /// <summary>
        /// Valor para campanha do tipo do troco
        /// </summary>
        public decimal? vlr_troco { get; set; }

    }

    /// <summary>
    /// Classe padrão com os campos viewizio_3
    /// </summary>
    public class DadosLoteViewizio_3
    {
        public long CpfCliente { get; set; }
        public long CpfCliente_2 { get; set; }

        public DateTime DataCompra { get; set; }
        public decimal ValorCompra { get; set; }
        public string cupom { get; set; }

        public string Pdv { get; set; }
        public Int32 CodPagto { get; set; }

        public string vlr_meiopagto { get; set; }
        public string MeioPagto { get; set; }

        public Int32 QtdeItens { get; set; }
        /// <summary>
        /// Valor total de desconto aplicado nos itens vendidos
        /// </summary>
        public decimal? vlr_total_desconto { get; set; }
        public decimal? vlr_troco { get; set; }
        public string CodEAN { get; set; }
        public long CodProduto { get; set; }
        public string DesProduto { get; set; }

        public decimal ValorItem { get; set; }
        public decimal? vlr_desconto_item { get; set; }
        public decimal Quantidade { get; set; }
        /// <summary>
        /// Codigo do operador do pdv
        /// </summary>
        public long cod_usuario { get; set; }
        public long cod_pessoa { get; set; }

        /// <summary>
        /// Numero do item na compra. Ex: Compra com 10 itens, teremos o item 1, item2, etc...
        /// </summary>
        public decimal item { get; set; }
        public long cod_loja { get; set; }

        /// <summary>
        /// Nsu da transação quando for pago em cartão. Caso a compra seja paga em mais de um cartão, os NSU precisam vir separados por ";"
        /// </summary>
        public string nsu_transacao { get; set; }

        /// <summary>
        /// Data e Hota da geração Nsu da transação quando for pago em cartão. Caso a compra seja paga em mais de um cartão, as datas de geração do NSU precisam vir separados por ";"
        /// </summary>
        public string dat_geracao_nsu { get; set; }

        /// <summary>
        /// BIN do cartão utilizado no pagamento da venda. Caso a compra seja paga em mais de um cartão, os BINs precisam vir separados por ";"
        /// </summary>
        public string des_bin_cartao { get; set; }
    }
}