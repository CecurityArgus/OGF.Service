using System.Configuration;
using System.IO;
using CookComputing.XmlRpc;


class UniversignRequests {
    private string email;
    private string password;

    public UniversignRequests (string email, string password) {
        this.email = email;
        this.password = password;
    }

    private ISignature init () {
        //Initialize the proxy
        ISignature proxy = (ISignature) XmlRpcProxyGen.Create (typeof (ISignature));
        // https://ws.universign.eu/sign/rpc
        // https://sign.test.cryptolog.com/sign/rpc


        proxy.Url = "https://sign.test.cryptolog.com/sign/rpc";


        //set credentials
        proxy.Credentials = new System.Net.NetworkCredential (email, password);
        //The library fails if set to True
        proxy.KeepAlive = false;
      

        return proxy;
    }

    public void CencelTransaction (string id)
    {

        ISignature proxy = init();
        try
        {
            var transaction = proxy.getTransactionInfo(id);
            
            if(transaction.status == "ready" ) 
                proxy.cancelTransaction(id);
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex.ToString());
            throw ex;
        }
    }

    public TransactionInfo GetTransactionInfo(string id)
    {
        ISignature proxy = init();
        try
        {
            return proxy.getTransactionInfo(id);
                        
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex.ToString());
            throw ex;
        }
    }
}