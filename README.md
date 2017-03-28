# BE_Izio.TransacaoRest

## O que faz
Api para retorno de informações referentes as transações de vendas na base do Izio. 

Metodos disponiveis:

1. ConsultaUltimasCompras - Metodo recebe codigo pessoa e retorna as compras do cliente dos ultimos 6 meses (https://apis.izio.com.br/TransacaoRest/swagger/index.html?url=/TransacaoRest/swagger/v1/swagger.json) 


## Qual BD usa
A aplicação está configurada para realizar processamento em ```SQL SERVER```. As strings de conexão para o SQL SERVER devem possuir o sufixo "SqlServer". Com esta configuração a API consegue entender em qual BD o processamento deve ser feito.

Um exemplo de nome de conexão é: "MazaIzioSqlServer".

## Onde e quando roda
Está hospedada na VM Izio 1, no diretório virtual apis.izio.com.br.


