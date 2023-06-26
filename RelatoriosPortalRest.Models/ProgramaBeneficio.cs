using System;
using System.Collections.Generic;

// <summary>
// Classe para consulta das ultimas compras do cliente
// </summary>
namespace ProgramaBeneficioController.Models
{
    using System.Collections.Generic;

    public class Headers
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
    }

    public class PessoasDistintas : Headers
    {
        public List<DataAgregadoPessoasDistintas> dataAgregado { get; set; }
        public List<DataDiarioPessoasDistintas> dataDiario { get; set; }
    }

    public class DataAgregadoPessoasDistintas
    {
        public int pessoas_distintas { get; set; }
    }

    public class DataDiarioPessoasDistintas
    {
        public string dia { get; set; }
        public int cod_pessoa_optin_compra { get; set; }
    }


    public class VendasTicketMedio : Headers
    {
        public List<DataAgregadoVendasTicketMedio> dataAgregado { get; set; }
        public List<DataDiarioVendasTicketMedio> dataDiario { get; set; }
    }

    public class DataAgregadoVendasTicketMedio
    {
        public decimal ticket_medio_total { get; set; }
        public decimal receita_total { get; set; }
    }

    public class DataDiarioVendasTicketMedio
    {
        public decimal receita_total { get; set; }
        public decimal ticket_medio { get; set; }
        public string ano { get; set; }
        public string mes { get; set; }
        public string dia { get; set; }
    }

}