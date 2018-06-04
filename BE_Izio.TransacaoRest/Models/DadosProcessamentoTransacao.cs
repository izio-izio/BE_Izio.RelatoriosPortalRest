using Izio.Biblioteca.Model;
using System;
using System.Collections.Generic;

namespace TransacaoRest.Models
{
    /// <summary>
    /// Objeto de retorno para quando a execução ocorrer com sucesso
    /// </summary>
    public class RetornoDadosTransacao
    {
        public Sucesso payload { get; set; }
    }

    /// <summary>
    /// Objeto de Retorno para utilização interna, para o processamento na DAO
    /// </summary>
    public class DadosProcessamento
    {
        public Sucesso payload { get; set; }

        public List<Erros> errors { get; set; }
    }

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