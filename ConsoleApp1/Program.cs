using System;
using TransacaoIzioRest.DAO;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            ImportaTransacaoDAO dao = new ImportaTransacaoDAO("redeMarket");
        }
    }
}
