using System;
using System.Collections.Generic;

namespace TransacaoRest.Models
{
    /// <summary>
    /// Dados credito para o cpf
    /// </summary>
    public class DadosProcessamentoTransacao
    {
        /// <summary>
        /// Codigo da loja que está sendo feito o processamento
        /// </summary>
        public Int64 cod_loja { get; set; }
    }
}