using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;

namespace OSHIrT.src
{
    public class CartServiceImpl : CartService
    {
        DcapiClient dcapiClient;

        public CartServiceImpl(DcapiClient dcapiClient)
        {
            this.dcapiClient = dcapiClient;
        }

        public String addToCart(string storeCode, string itemUri, int quantity)
        {
            String addToDefaultCartUri = URIUtil.format(new List<String>() { "carts", storeCode, "default", "lineitems", itemUri });
            Dictionary<String, String> quantityparameter = new Dictionary<string, string>();
            quantityparameter.Add("quantity", quantity.ToString());

            IRestResponse addToCartResponse = dcapiClient.post(addToDefaultCartUri, quantityparameter, new Dictionary<string, string>());
            return URIUtil.getHeaderValue(addToCartResponse.Headers, "Location");
        }
    }
}
