using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSHIrT.src;
using RestSharp;


namespace OSHIrT
{
    public interface DcapiClient
    {
        Token login(String userName, String password, String storeCode);

        T get<T>(String uri, Dictionary<String, String> paramters, Dictionary<String, String> queryParamters) where T : new();

        IRestResponse get(String uri, Dictionary<String, String> parameters, Dictionary<String, String> queryParameters);

        T post<T>(String uri, Dictionary<String, String> parameters, Dictionary<String, String> queryParamters) where T : new();

        IRestResponse post(String uri, Dictionary<String, String> parameters, Dictionary<String, String> queryParameters);

        void setAuthToken(String authToken);

        String getAuthToken();
    }
}
