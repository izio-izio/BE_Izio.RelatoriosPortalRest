using System;
using System.Collections.Generic;

namespace TransacaoRest.Models
{
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
    public class DadosProcessamento
    {
        public Payload payload { get; set; }

        public List<Erros> errors { get; set; }
    }

    /// <summary>
    /// Objeto padrão de retorno da API Rest
    /// </summary>
    public class Payload
    {
        public string code { get; set; }
        public string message { get; set; }
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