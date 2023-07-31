
<h1 align="center">
     BE_Izio.RelatoriosPortalRest
<br>
<img align="center" src="https://i.ibb.co/Yy3g057/ey-Jp-ZF9jb21w-YW55-Ijo0-ODcw-LCJp-ZF9wcm9ma-Wxl-Ijo3-Nz-My-MDMs-In-Rpb-WVzd-GFtc-CI6-MTY2-OTgy-OTQ1.png"> 

## :bookmark_tabs:Tópicos
* [Introdução](#introducao)
* [Tecnologias Empregadas](#tecnologias)
* [Estrutura do Projeto](#estrutura)
* [Configurações](#configuracao)
* [Boas Práticas no Código](#boas-praticas)

<hr id="introducao">

## :mag:Introdução a API RelatoriosPortalRest

- API Rest para parametrização dos relatórios do Portal.

**- Swagger** 
```
swagger/RelatoriosPortalRest
```

**- Orientações importantes :warning::warning:**
```
NÃO EXPOR TOKEN
```
```
NÃO EXPOR URL SEM CRIPTOGRAFIA
```

<hr id="tecnologias">

### :computer: Tecnologias Empregadas:
- C# (Linguagem de programção),
- .NET (Framework),
- SQL Server (Banco de dados),
- Swagger (Ferramenta para desenvolvimento da API),
- Postman (Ferramenta para desenvolvimento da API),
- Git (Ferramenta de versionamento de código).


<hr id="estrutura">

### :page_with_curl: Estrutura do Projeto

O projeto ainda não possui estrutura de diretórios.

<hr id="configuracao">

### :wrench: Configurações 

**- Primeiro acesso ao Repositório**
```TypeScript
1. Clone o `repositório` em sua máquina de trabalho. 
    - (Certifique-se de não clonar em diretórios com conexão com o OneDrive).

2. Crie ou utilize uma branch do repositório baseada na `master`:
    - `git checkout -b 'nomeDaBranch'`.

3. Ao abrir o projeto pela primeira vez, inicie a `Depuração` para carregar as configurações de `NuGet`.

4. Acesse a aplicação no navegador através do endereço `http://localhost:49343`.

5. Acesse o `swagger` da aplicação através do endereço `http://localhost:49343/swagger/` 

6. Para evitar conflitos de branch, antes da `PR` para `hml ou master` certifique-se de que as branchs estejam semelhantes:
    - Utilize o comando `git pull -p`.
```
<br>

**- Após o primeiro acesso**
```TypeScript
1.  Certifique-se de que todas as branchs estejam atualizadas:
    - `git pull -p`.

2. Crie ou utilize uma branch do repositório baseada na `master`:
    - `git checkout -b 'nomeDaBranch'` (Criar uma nova branch)
    - `git checkout 'nomeDaBranch'` (Utilizar uma branch)

3. Para acessar a aplicação após depurar, utilize o navegador através do endereço `http://localhost:49343`.

4. Acesse o `swagger` da aplicação através do endereço `http://localhost:49343/swagger/`

5. Após finalizar as alterações em sua branch utilize esses comandos atráves do Git: 
    - `git add .`, 
    - `git commit -m 'Descrição do que foi feito'`, 
    - `git push origin 'nomeDaBranch'`.

```
>Utilize essas orientações diariamente para que sua rotina de trabalho seja mais tranquila e organizada.

<hr id="boas-praticas">

### :white_check_mark: Boas Práticas no Código


**Gerenciamento de Conexão com Banco de Dados nos métodos `DAO`**

```TypeScript
public void MetodoDAO()
{
    try
    {
        // Estrura que ficará sua chamada do banco de dados 

        //Montando a conexão com o banco
        sqlServer.StartConnection();

        sqlServer.Command.CommandTimeout = Convert.ToInt32(
                TimeSpan.FromMinutes(
                    Convert.ToDouble(5)
                ).TotalMilliseconds);

        sqlServer.Command.CommandType = CommandType.Text;
        sqlServer.Command.Parameters.Clear();

        // Continuação do bloco de código...
    }
    catch (Exception ex)
    {
        throw ex;
    }
}
```

>O bloco `try catch` envolve o código que estabelece a conexão com o banco de dados, realizando a chamada da classe `sqlServer()`. Além de ser responsável por tratar qualquer exceção que possa ser lançada, permitindo ao sistema lidar com as situações de erro sem afetar o fluxo. <br>
:warning::warning: Abrir conexao apenas 1 vez para a consulta e fechar somente quando finalizar todas as consultas :warning::warning:

<br>

**Fechamento do Reader nos Métodos `DAO`** 

```TypeScript
finally
{
    if (sqlServer != null)
    {
        if (sqlServer.Reader != null)
        {
            sqlServer.Reader.Close();
            sqlServer.Reader.Dispose();
        }
        sqlServer.CloseConnection();
    }
}
```
>Dentro da estrutura das classes `DAO`, é importante ressaltar que os métodos realizam a chamada do `Reader` para estabelecer a conexão com o banco de dados `SQL Server`. Lembre-se sempre de, após o bloco `try catch`, fechar a conexão adequadamente utilizando o bloco `finally` para evitar problemas de gerenciamento.

<br>

**Documentação dos Endpoints nas Controllers**
```TypeScript
/// <summary>
///    Título explicando o que endpoint.  
/// </summary>
/// <remarks>
///     ### Fluxo de utilização ###
///     <para>
///         Nesse trecho de documentação deve estar contido: 
///             1-  Orientações de como utilizar o endpoint, 
///             2 - Informações importantes de funcionalidades, 
///             3 - Validações essenciais,
///             4 - Tabelas que estão sendo utilizadas;
///     </para>
///     
///     ### Filtros QueryParam ###
///     <para>
///         Nesse trecho de documentação deve estar contido uma descrição dos parameters do endpoint. 
///             - Exemplo: "cod_ex" (int) - Código de identificação do exemplo, se for informado 0 faz a busca de todos os exemplos;
///     </para>
///     
///     ### Valores de retorno ###
///     <para>
///         Nesse trecho de documentação deve estar contido uma descrição dos valores de retorno do endpoint. 
///             - Exemplo: "cod_ex" (int) - Código de identificação do exemplo;
///     </para>
///     
///     ### Status de retorno da API ###
///     <para>
///         Nesse trecho de documentação deve estar contido uma descrição dos principais Status de retorno da endpoint:
///
///             - Status Code 200 = Sucesso na requisição;
///
///             - Status Code 400 = Bad Request (Dados inseridos para utilização da API incorretos);
///             - Status Code 401 = Unauthorized (Não possui credenciais de autenticação válidas para o recurso de destino);
///         
///             - Status Code 500 = Internal Server Error (Ocorreu um erro no lado do servidor para buscar os dados);
///     </para>
/// </remarks>
/// <param name="">(Descrição do parameters)</param>
```

>Dentro da estrutura das classes `Controllers`, é importante documentar de forma clara e direta os `endpoints` que serão utilizadas por `aplicações externas`. <br>
A documentação pode ser visualizada no cabeçalho do `endpoint` no `swagger`.

* * * 
