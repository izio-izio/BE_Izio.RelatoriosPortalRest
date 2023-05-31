using System;
using System.Collections.Generic;

// <summary>
// Classe para consulta das ultimas compras do cliente
// </summary>
namespace RelatoriosPortalRest.Models
{
    using System.Collections.Generic;

    public class UsuariosCadastrados
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<UsuariosCadastradosVisaoRapida> dataVisaoRapida { get; set; }
        public List<UsuariosCadastradosMensal> dataMensal { get; set; }
    }

    public class UsuariosCadastradosVisaoRapida
    {
        public int usuarios_cadastrados { get; set; }
        public string flag_dias { get; set; }
    }

    public class UsuariosCadastradosMensal
    {
        public int usuarios_cadastrados { get; set; }
        public string data_cadastro { get; set; }
    }


    public class UsuariosCadastradosData
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<UsuariosCadastradosAgregado> dataAregado { get; set; }
        public List<UsuariosCadastradosDiario> dataDiario { get; set; }
    }

    public class UsuariosCadastradosAgregado
    {
        public int usuarios_cadastrados { get; set; }
    }

    public class UsuariosCadastradosDiario
    {
        public int usuarios_cadastrados { get; set; }
        public DateTime data_cadastro { get; set; }
    }


    public class VendasIdentificadas
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<VendasIdentificadasVisaoRapida> dataVisaoRapida { get; set; }
        public List<VendasIdentificadasMensal> dataMensal { get; set; }
    }

    public class VendasIdentificadasVisaoRapida
    {
        public decimal receita_identificada { get; set; }
        public decimal ticket_medio_identificado { get; set; }
        public decimal ticket_medio_total { get; set; }
        public decimal receita_total { get; set; }
        public decimal porcentagem_receita_identificada { get; set; }
        public decimal num_transacoes_identificadas { get; set; }
        public string flag_dias { get; set; }
    }

    public class VendasIdentificadasMensal
    {
        public decimal receita_identificada { get; set; }
        public decimal ticket_medio_identificado { get; set; }
        public decimal ticket_medio_total { get; set; }
        public decimal receita_total { get; set; }
        public decimal porcentagem_receita_identificada { get; set; }
        public decimal num_transacoes_identificadas { get; set; }
        public string mes { get; set; }
    }


    public class VendasIdentificadasData
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<VendasIdentificadasAregado> dataAregado { get; set; }
        public List<VendasIdentificadasDiario> dataDiario { get; set; }
    }

    public class VendasIdentificadasAregado
    {
        public decimal ticket_medio_identificado { get; set; }
        public decimal ticket_medio_total { get; set; }
        public decimal receita_identificada { get; set; }
        public decimal receita_total { get; set; }
        public decimal porcentagem_receita_identificada { get; set; }
        public decimal num_transacoes_identificadas { get; set; }
    }

    public class VendasIdentificadasDiario
    {
        public decimal receita_identificada { get; set; }
        public decimal receita_total { get; set; }
        public decimal num_transacoes_identificadas { get; set; }
        public decimal num_transacoes_totais { get; set; }
        public decimal ticket_medio_identificado { get; set; }
        public string ano { get; set; }
        public string mes { get; set; }
        public string dia { get; set; }
        public decimal porcentagem_receita_identificada { get; set; }
    }

    public class PessoasAtivas
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<PessoasAtivasVisaoRapida> dataVisaoRapida { get; set; }
        public List<PessoasAtivasMensal> dataMensal { get; set; }
    }

    public class PessoasAtivasVisaoRapida
    {
        public string flag_dias { get; set; }
        public int pessoas_distintas { get; set; }
    }

    public class PessoasAtivasMensal
    {
        public string mes { get; set; }
        public int pessoas_distintas { get; set; }
    }

    public class PessoasAtivasData
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<PessoasAtivasAregado> dataAregado { get; set; }
        public List<PessoasAtivasDiario> dataDiario { get; set; }
    }

    public class PessoasAtivasAregado
    {
        public int pessoas_distintas { get; set; }
    }

    public class PessoasAtivasDiario
    {
        public string dia { get; set; }
        public int pessoas_distintas { get; set; }
    }

    public class FrequenciaGastoMedio
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<FrequenciaGastoMedioVisaoRapida> dataVisaoRapida { get; set; }
        public List<FrequenciaGastoMedioMensal> dataMensal { get; set; }
    }

    public class FrequenciaGastoMedioVisaoRapida
    {
        public string flag_dias { get; set; }
        public decimal frequencia_identificada { get; set; }
        public decimal gasto_medio { get; set; }
    }

    public class FrequenciaGastoMedioMensal
    {
        public string mes { get; set; }
        public decimal frequencia_identificada { get; set; }
        public decimal gasto_medio { get; set; }
    }

    public class FrequenciaGastoMedioData
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
        public List<FrequenciaGastoMedioAregado> dataAregado { get; set; }
        public List<FrequenciaGastoMedioDiario> dataDiario { get; set; }
    }

    public class FrequenciaGastoMedioAregado
    {
        public decimal frequencia_identificada { get; set; }
        public decimal gasto_medio { get; set; }
    }

    public class FrequenciaGastoMedioDiario
    {
        public string dia { get; set; }
        public decimal frequencia_identificada { get; set; }
        public decimal gasto_medio { get; set; }
    }

}