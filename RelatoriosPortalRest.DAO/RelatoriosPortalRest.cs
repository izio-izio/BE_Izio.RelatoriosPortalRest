using Izio.Biblioteca;
using Izio.Biblioteca.Model;
using Newtonsoft.Json;
using RelatoriosPortalRest.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;


namespace RelatoriosPortalRest.DAO
{
    public class RelatoriosPortalRestDAO
    {
        SqlServer sqlServer;
        string NomeClienteWs;
        string TokenRest;

        public RelatoriosPortalRestDAO(string sNomeCliente, string _token)
        {
            sqlServer = new SqlServer(sNomeCliente);
            NomeClienteWs = sNomeCliente;
            TokenRest = _token;
        }

        public Tuple<UsuariosCadastradosData, UsuariosCadastrados> BuscarUsuariosCadastrados(int periodo, string xApiKey, string codLoja = "", string primeiraData = "", string ultimaData = "")
        {
            string sFiltros = "";
            UsuariosCadastradosData dataAgregado = new UsuariosCadastradosData();
            UsuariosCadastrados data = new UsuariosCadastrados();
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

            if (periodo > 0)
            {
                sFiltros += $@"&periodo={periodo}";
            }

            string url = $@"https://gs5n8yh1ci.execute-api.us-east-1.amazonaws.com/prod/izio/loyalty/v1/usuarios-cadastrados?varejo={NomeClienteWs.ToLower()}{sFiltros}";
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
                    if(periodo == 0)
                    {
                        dataAgregado = JsonConvert.DeserializeObject<UsuariosCadastradosData>(response);
                    }
                    else if(periodo > 0)
                    {
                        data = JsonConvert.DeserializeObject<UsuariosCadastrados>(response);
                    }
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return Tuple.Create(dataAgregado, data);
        }

        public Tuple<VendasIdentificadasData, VendasIdentificadas> ReceitasTransacoesTicketMedio(int periodo, string xApiKey, string codLoja = "", string primeiraData = "", string ultimaData = "")
        {
            string sFiltros = "";
            VendasIdentificadasData dataAgregado = new VendasIdentificadasData();
            VendasIdentificadas data = new VendasIdentificadas();
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

            if (periodo > 0)
            {
                sFiltros += $@"&periodo={periodo}";
            }

            string url = $@"https://gs5n8yh1ci.execute-api.us-east-1.amazonaws.com/prod/izio/loyalty/v1/venda-identificada?varejo={NomeClienteWs.ToLower()}{sFiltros}";
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
                    if (periodo == 0)
                    {
                        dataAgregado = JsonConvert.DeserializeObject<VendasIdentificadasData>(response);
                    }
                    else if (periodo > 0)
                    {
                        data = JsonConvert.DeserializeObject<VendasIdentificadas>(response);
                    }
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return Tuple.Create(dataAgregado, data);
        }

        public Tuple<PessoasAtivasData, PessoasAtivas> PessoasAtivas(int periodo, string xApiKey, string codLoja = "", string primeiraData = "", string ultimaData = "")
        {
            string sFiltros = "";
            PessoasAtivasData dataAgregado = new PessoasAtivasData();
            PessoasAtivas data = new PessoasAtivas();
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

            if (periodo > 0)
            {
                sFiltros += $@"&periodo={periodo}";
            }

            string url = $@"https://gs5n8yh1ci.execute-api.us-east-1.amazonaws.com/prod/izio/loyalty/v1/pessoa-distintas-vendas?varejo={NomeClienteWs.ToLower()}{sFiltros}";
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
                    if (periodo == 0)
                    {
                        dataAgregado = JsonConvert.DeserializeObject<PessoasAtivasData>(response);
                    }
                    else if (periodo > 0)
                    {
                        data = JsonConvert.DeserializeObject<PessoasAtivas>(response);
                    }
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return Tuple.Create(dataAgregado, data);
        }

        public Tuple<FrequenciaGastoMedioData, FrequenciaGastoMedio> FrequenciaDeCompra(int periodo, string xApiKey, string codLoja = "", string primeiraData = "", string ultimaData = "")
        {
            string sFiltros = "";
            FrequenciaGastoMedioData dataAgregado = new FrequenciaGastoMedioData();
            FrequenciaGastoMedio data = new FrequenciaGastoMedio();
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

            if (periodo > 0)
            {
                sFiltros += $@"&periodo={periodo}";
            }

            string url = $@"https://gs5n8yh1ci.execute-api.us-east-1.amazonaws.com/prod/izio/loyalty/v1/frequencia-gasto-medio?varejo={NomeClienteWs.ToLower()}{sFiltros}";
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
                    if (periodo == 0)
                    {
                        dataAgregado = JsonConvert.DeserializeObject<FrequenciaGastoMedioData>(response);
                    }
                    else if (periodo > 0)
                    {
                        data = JsonConvert.DeserializeObject<FrequenciaGastoMedio>(response);
                    }
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return Tuple.Create(dataAgregado, data);
        }

        public Tuple<SegmentoMaiVendidoAgregado, ProdutosMaisVendidosAgregado> ProdutosReceita(int arvore, string xApiKey, string codLoja = "", string primeiraData = "", string ultimaData = "")
        {
            string sFiltros = "";
            SegmentoMaiVendidoAgregado segmento = new SegmentoMaiVendidoAgregado();
            ProdutosMaisVendidosAgregado produto = new ProdutosMaisVendidosAgregado();
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

            if (arvore > 0)
            {
                sFiltros += $@"&arvore={arvore}";
            }

            string url = $@"https://gs5n8yh1ci.execute-api.us-east-1.amazonaws.com/prod/izio/loyalty/v1/produtos-receita?varejo={NomeClienteWs.ToLower()}{sFiltros}";
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
                    if (arvore == 1)
                    {
                        segmento = JsonConvert.DeserializeObject<SegmentoMaiVendidoAgregado>(response);
                    }
                    else if (arvore == 2)
                    {
                        produto = JsonConvert.DeserializeObject<ProdutosMaisVendidosAgregado>(response);
                    }
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }

            return Tuple.Create(segmento, produto);
        }

        public GastoPorGrupoAgregado GastoPorGrupo(string xApiKey, string codLoja = "", string primeiraData = "", string ultimaData = "")
        {
            string sFiltros = "";
            GastoPorGrupoAgregado data = new GastoPorGrupoAgregado();
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


            string url = $@"https://gs5n8yh1ci.execute-api.us-east-1.amazonaws.com/prod/izio/loyalty/v1/gasto-grupo?varejo={NomeClienteWs.ToLower()}{sFiltros}";
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
                   data = JsonConvert.DeserializeObject<GastoPorGrupoAgregado>(response);
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