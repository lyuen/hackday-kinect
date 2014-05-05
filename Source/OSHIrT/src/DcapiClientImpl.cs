using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using OSHIrT.src;


namespace OSHIrT
{
    public class DcapiClientImpl : DcapiClient
    {

        private RestClient restClient;
        private String baseUri;
        private String authToken;

        public DcapiClientImpl(String baseUri)
        {
            restClient = new RestClient();
            restClient.BaseUrl = baseUri;
            this.baseUri = baseUri;
        }

        public Token login(string userName, String password, String storeCode)
        {
            Dictionary<String, String> authenticationParameters = new Dictionary<String, String>();
            authenticationParameters.Add("username",userName);
            authenticationParameters.Add("password",password);

            String authUri = URIUtil.format(new List<String>() { "authentication", storeCode });
            Token response = post<Token>(authUri, authenticationParameters, new Dictionary<String, String>());
            authToken = response.access_token;
            restClient.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(authToken);

            return response;
        }

        public T get<T>(String uri, Dictionary<String,String> paramters, Dictionary<String, String> queryParameters) where T : new()
        {
           RestRequest getRequest = createRestRequest(uri, paramters, queryParameters, Method.GET);
           return ((IRestResponse <T>) restClient.Execute<T>(getRequest)).Data;
 
        }

        public IRestResponse get(String uri, Dictionary<String, String> parameters, Dictionary<String, String> queryParameters)
        {
            RestRequest getRequest = createRestRequest(uri, parameters, queryParameters, Method.GET);
            return restClient.Execute(getRequest);
        }

        public T post<T>(String uri, Dictionary<String, String> parameters, Dictionary<String, String> queryParameters) where T : new()
        {
            RestRequest postRequest = createRestRequest(uri, parameters, queryParameters, Method.POST);
            return ((IRestResponse <T>) restClient.Execute<T>(postRequest)).Data;
        }

        public IRestResponse post(String uri, Dictionary<String, String> parameters, Dictionary<String, String> queryParameters)
        {
            RestRequest postRequest = createRestRequest(uri, parameters, queryParameters, Method.POST);
            return restClient.Execute(postRequest);
        }

        private RestRequest createRestRequest(String uri, Dictionary<String, String> parameters, Dictionary<String, String> queryParameters, Method method)
        {
            if (uri.IndexOf(baseUri) >= 0)
            {
                //remove duplicate base uri.
                uri = uri.Replace(baseUri, null);
            }

            RestRequest request = new RestRequest(uri, method);
            StringBuilder stringBuilder = new StringBuilder();
            if (parameters.Count != 0)
            {
                stringBuilder.Append("{");
                foreach (KeyValuePair<String, String> parameter in parameters)
                {
                    stringBuilder.Append("\"" + parameter.Key + "\"" + ":" + "\"" + parameter.Value + "\"");
                    stringBuilder.Append(",");

                }
                stringBuilder.Append("}");
                stringBuilder.Remove(stringBuilder.ToString().LastIndexOf(","), 1);
            }
           
            if (queryParameters.Count != 0) 
            {
                stringBuilder.Append("?");
                foreach(KeyValuePair<String, String> parameter in queryParameters)
                {
                    stringBuilder.Append(parameter.Key + "=" + parameter.Value);
                    stringBuilder.Append("&");
                }
                stringBuilder.Remove(stringBuilder.ToString().LastIndexOf("&"), 1);
            }

            if (parameters.Count != 0)
            {
                request.AddParameter("application/json", stringBuilder.ToString(), ParameterType.RequestBody);
            }
            else
            {
                request.AddParameter("application/json", stringBuilder.ToString(), ParameterType.GetOrPost);
            }
            request.AddHeader("Content-type", "application/json");
            if (authToken != null)
            {
                request.AddCookie("EP-OAuth2-Token", authToken);
            }
            request.RequestFormat = DataFormat.Json;
  
            return request;


        }

        public void setAuthToken(String authToken)
        {
            this.authToken = authToken;
        }

        public String getAuthToken()
        {
            return authToken;
        }
    }
}
