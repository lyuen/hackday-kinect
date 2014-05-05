using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace OSHIrT.src
{
    public class URIUtil
    {
        public static String format(List<String> uriTokens)
        {
            List<String> trimmedTokens = new List<String>();
            String loopToken;
            foreach(String uriToken in uriTokens) 
            {
                loopToken = uriToken.TrimEnd('/').TrimStart('/');
                if (String.IsNullOrEmpty(loopToken))
                {
                    continue;
                }

                if (uriToken.Contains("//"))
                {
                    String [] tokenArray = loopToken.Split('/');
                    foreach (String token in tokenArray) 
                    {
                        if(!String.IsNullOrEmpty(token))
                        {
                            trimmedTokens.Add(token);
                        }
                    }
                } else {
                    trimmedTokens.Add(uriToken);
                }
            }
            return "/" + string.Join("/", trimmedTokens.ToArray());
        }

        public static String formatHref(String baseUri, List<String> uriTokens)
        {
            String urlPrefix = baseUri;
            while (urlPrefix.EndsWith("/"))
            {
                urlPrefix = urlPrefix.Substring(0, urlPrefix.Length - 1);
            }

            return urlPrefix + format(uriTokens);
        }

        public static String getHeaderValue(IList<Parameter> headers, String headerName)
        {
            foreach (Parameter header in headers)
            {
                if (((String)header.Name).Equals(headerName))
                {
                    return (String)header.Value;
                }
            }
            return String.Empty;
        }

        public static Dictionary<String, String> createStubParamters()
        {
            Dictionary<String, String> stubParameters = new Dictionary<String, String>();
            stubParameters.Add("Stub", "Stub");
            return stubParameters;
        }
        
    }
}
