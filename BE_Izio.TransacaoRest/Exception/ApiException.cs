using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransacaoIzioRest.Exception
{
    public class ApiException
    {
        public class ExceptionClienteSemCompras : System.Exception
        {
            public override string Message
            {
                get
                {
                    return "Cliente não realizou compras nos ultimos 6 meses.";
                }
            }
        }

    }
}