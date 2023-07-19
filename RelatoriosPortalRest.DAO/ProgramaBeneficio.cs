using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using Newtonsoft.Json;
using ProgramaBeneficioController.Models;
using RelatoriosPortalRest.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;


namespace RelatoriosPortalRest.DAO
{
    public class ProgramaBeneficioDAO
    {
        SqlServer sqlServer;
        string NomeClienteWs;
        string TokenRest;

        public ProgramaBeneficioDAO(string sNomeCliente, string _token)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
            TokenRest = _token;
        }

        public PessoasDistintas BuscarTotalDeClientes(string xApiKey, string primeiraData, string ultimaData)
        {
            string sFiltros = "";
            PessoasDistintas data = new PessoasDistintas();
            List<Header> lstHeader = new List<Header>();
            if (!String.IsNullOrEmpty(primeiraData))
            {
                sFiltros += $@"&primeiraData={primeiraData}";
            }

            if (!String.IsNullOrEmpty(ultimaData))
            {
                sFiltros += $@"&ultimaData={ultimaData}";
            }


            string url = $@"https://bmil5p9rj7.execute-api.us-east-1.amazonaws.com/prod/izio/progbeneficios/pessoa-distintas-vendas?varejo={NomeClienteWs.ToLower()}{sFiltros}";
            try
            {
                lstHeader.Add(new Header { name = "x-api-key", value = xApiKey });

                var response = Utilidades.ChamadaApiExterna
                    (
                        tipoRequisicao: "GET",
                        url: url,
                        metodo: "",
                        Headers: lstHeader

                    );

                if (response != null)
                {
                        data = JsonConvert.DeserializeObject<PessoasDistintas>(response);
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return data;
        }

        public VendasTicketMedio VendasTicketMedio(string xApiKey, string primeiraData, string ultimaData, string codLoja = "", string codCampanha = "")
        {
            string sFiltros = "";
            VendasTicketMedio data = new VendasTicketMedio();
            List<Header> lstHeader = new List<Header>();

            if (!String.IsNullOrEmpty(codLoja))
            {
                sFiltros += $@"&codLoja={codLoja}";
            }

            if (!String.IsNullOrEmpty(primeiraData))
            {
                sFiltros += $@"&primeiraData={primeiraData}";
            }

            if (!String.IsNullOrEmpty(ultimaData))
            {
                sFiltros += $@"&ultimaData={ultimaData}";
            }

            if (!String.IsNullOrEmpty(codCampanha))
            {
                sFiltros += $@"&codCampanha={codCampanha}";
            }

            string url = $@"https://bmil5p9rj7.execute-api.us-east-1.amazonaws.com/prod/izio/progbeneficios/loja-receita?varejo={NomeClienteWs.ToLower()}{sFiltros}";
            try
            {
                lstHeader.Add(new Header { name = "x-api-key", value = xApiKey });

                var response = Utilidades.ChamadaApiExterna
                    (
                        tipoRequisicao: "GET",
                        url: url,
                        metodo: "",
                        Headers: lstHeader

                    );

                if (response != null)
                {
                    data = JsonConvert.DeserializeObject<VendasTicketMedio>(response);
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return data;
        }
    }
}