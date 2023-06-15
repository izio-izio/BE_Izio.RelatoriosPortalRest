using System;
using System.Collections.Generic;

// <summary>
// Classe para consulta das ultimas compras do cliente
// </summary>
namespace RelatoriosPortalRest.Models
{
    using System.Collections.Generic;

    public class Headers
    {
        public int statusCode { get; set; }
        public Dictionary<string, string> headers { get; set; }
    }

    public class UsuariosCadastrados: Headers
    {

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


    public class UsuariosCadastradosData : Headers
    {
        public List<UsuariosCadastradosAgregado> dataAgregado { get; set; }
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


    public class VendasIdentificadas : Headers
    {
        public List<VendasIdentificadasVisaoRapida> dataVisaoRapida { get; set; }
        public List<VendasIdentificadasMensal> dataMensal { get; set; }
    }

    public class VendasIdentificadasVisaoRapida
    {
        public decimal? receita_identificada { get; set; }
        public decimal? ticket_medio_identificado { get; set; }
        public decimal? ticket_medio_total { get; set; }
        public decimal? receita_total { get; set; }
        public decimal? porcentagem_receita_identificada { get; set; }
        public decimal? num_transacoes_identificadas { get; set; }
        public string flag_dias { get; set; }
    }

    public class VendasIdentificadasMensal
    {
        public decimal? receita_identificada { get; set; }
        public decimal? ticket_medio_identificado { get; set; }
        public decimal? ticket_medio_total { get; set; }
        public decimal? receita_total { get; set; }
        public decimal? porcentagem_receita_identificada { get; set; }
        public decimal? num_transacoes_identificadas { get; set; }
        public string mes { get; set; }
    }


    public class VendasIdentificadasData : Headers
    {
        public List<VendasIdentificadasAgregado> dataAgregado { get; set; }
        public List<VendasIdentificadasDiario> dataDiario { get; set; }
    }

    public class VendasIdentificadasAgregado
    {
        public decimal? ticket_medio_identificado { get; set; }
        public decimal? ticket_medio_total { get; set; }
        public decimal? receita_identificada { get; set; }
        public decimal? receita_total { get; set; }
        public decimal? porcentagem_receita_identificada { get; set; }
        public decimal? num_transacoes_identificadas { get; set; }
    }

    public class VendasIdentificadasDiario
    {
        public decimal? receita_identificada { get; set; }
        public decimal? receita_total { get; set; }
        public decimal? num_transacoes_identificadas { get; set; }
        public decimal? num_transacoes_totais { get; set; }
        public decimal? ticket_medio_identificado { get; set; }
        public string ano { get; set; }
        public string mes { get; set; }
        public string dia { get; set; }
        public decimal? porcentagem_receita_identificada { get; set; }
    }

    public class PessoasAtivas : Headers
    {
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

    public class PessoasAtivasData : Headers
    {
        public List<PessoasAtivasAgregado> dataAgregado { get; set; }
        public List<PessoasAtivasDiario> dataDiario { get; set; }
    }

    public class PessoasAtivasAgregado
    {
        public int pessoas_distintas { get; set; }
    }

    public class PessoasAtivasDiario
    {
        public string dia { get; set; }
        public int pessoas_distintas { get; set; }
    }

    public class FrequenciaGastoMedio : Headers
    {
        public List<FrequenciaGastoMedioVisaoRapida> dataVisaoRapida { get; set; }
        public List<FrequenciaGastoMedioMensal> dataMensal { get; set; }
    }

    public class FrequenciaGastoMedioVisaoRapida
    {
        public string flag_dias { get; set; }
        public decimal? frequencia_identificada { get; set; }
        public decimal? gasto_medio { get; set; }
    }

    public class FrequenciaGastoMedioMensal
    {
        public string mes { get; set; }
        public decimal? frequencia_identificada { get; set; }
        public decimal? gasto_medio { get; set; }
    }

    public class FrequenciaGastoMedioData : Headers
    {
        public List<FrequenciaGastoMedioAgregado> dataAgregado { get; set; }
        public List<FrequenciaGastoMedioDiario> dataDiario { get; set; }
    }

    public class FrequenciaGastoMedioAgregado
    {
        public decimal? frequencia_identificada { get; set; }
        public decimal? gasto_medio { get; set; }
    }

    public class FrequenciaGastoMedioDiario
    {
        public string dia { get; set; }
        public decimal? frequencia_identificada { get; set; }
        public decimal? gasto_medio { get; set; }
    }

    public class GastoPorGrupoAgregado : Headers
    {
        public List<GastoPorGrupo> dataAgregado { get; set; }
    }


    public class GastoPorGrupo
    {
        public string grupo_gasto_medio { get; set; }
        public double gasto_grupo { get; set; }
        public double porcentagem_gasto_receita { get; set; }
        public double porcentagem_nao_identificada { get; set; }
    }

    public class SegmentoMaiVendidoAgregado: Headers
    {
        public List<SegmentoMaisVendido> dataAgregado { get; set; }
    }


    public class SegmentoMaisVendido
    {
        public double receita { get; set; }
        public string des_secao { get; set; }
    }

    public class ProdutosMaisVendidosAgregado : Headers
    {
        public List<ProdutoMaisVendido> dataAgregado { get; set; }
    }

    public class ProdutoMaisVendido
    {
        public double receita { get; set; }
        public double qtd_vendida { get; set; }
        public string des_produto { get; set; }
    }


    public class LojaReceita : Headers
    {
        public List<LojaReceitaAgregado> dataAgregado { get; set; }
        public List<DataBairro> dataBairro { get; set; }
    }

    public class LojaReceitaAgregado
    {
        public string des_cidade { get; set; }
        public string des_estado { get; set; }
        public string des_nome_loja { get; set; }
        public string des_bairro { get; set; }
        public string cod_loja { get; set; }
        public double receita { get; set; }
    }

    public class DataBairro
    {
        public string des_bairro { get; set; }
        public double receita { get; set; }
    }

    public class SegmentacaoPessoasCategoria : Headers
    {
        public List<SegmentacaoPessoas> data { get; set; }
    }

    public class SegmentacaoPessoas
    {
        public string clube { get; set; }
        public double receita { get; set; }
        public double num_transacoes { get; set; }
        public double num_pessoas { get; set; }
        public double ticket_medio { get; set; }
        public double frequencia { get; set; }
        public double gasto_medio { get; set; }
    }

    public class SegmentacaoPessoasPeriodo: Headers
    {
        public List<SegmentacaoPessoasPeriodoPeriodoData> data { get; set; }
    }

    public class SegmentacaoPessoasPeriodoPeriodoData
    {
        public string periodo { get; set; }
        public double bronze_prata { get; set; }
        public double bronze_ouro { get; set; }
        public double ouro_prata { get; set; }
        public double ouro_bronze { get; set; }
        public double prata_prata { get; set; }
        public double bronze_bronze { get; set; }
        public double prata_bronze { get; set; }
        public double prata_ouro { get; set; }
    }

}