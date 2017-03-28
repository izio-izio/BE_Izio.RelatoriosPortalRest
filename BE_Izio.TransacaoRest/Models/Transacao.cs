namespace TransacaoRest.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public class DadosPessoa
    {
        DadosPessoa()
        {
            cod_pessoa = 0;
        }

        public long cod_pessoa { get; set; }
    }

    public class DadosTransacao
    {
        public long cod_transacao { get; set; }
        public long cod_pessoa { get; set; }
        public DateTime dat_compra { get; set; }
        public decimal vlr_compra	 { get; set; }
        public long cod_loja	 { get; set; }
        public Int32 qtd_itens_compra	 { get; set; }
        public string cupom	 { get; set; }
    }
}