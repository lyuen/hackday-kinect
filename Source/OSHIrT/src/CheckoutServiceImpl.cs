using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace OSHIrT.src
{
    public class CheckoutServiceImpl : CheckoutService
    {
        private DcapiClient dcapiClient;
        
        public CheckoutServiceImpl(DcapiClient dcapiClient)
        {
            this.dcapiClient = dcapiClient;
        }


        public Purchase createOrder(string storeCode)
        {
            Cart cart = dcapiClient.get<Cart>(URIUtil.format(new List<String> { "carts", storeCode, "default" }), new Dictionary<String, String>(), new Dictionary<String, String>());
            foreach (Link link in cart.links)
            {
                if (link.rel.Equals("orderform"))
                {
                    Order order = dcapiClient.get<Order>(link.uri, new Dictionary<String, String>(), new Dictionary<String, String>());
                    if (order.links.Count != 0)
                    {
                        IRestResponse submitOrderResponse = dcapiClient.post(order.links.First().uri, URIUtil.createStubParamters(), new Dictionary<String, String>());
                        Order submittedOrder = getOrder(submitOrderResponse);
                        return getPurchase(submittedOrder);
                    }
                    else
                    {
                        throw new System.ArgumentException("Failed submit cart, something went wrong");
                    }
                }
            }
            throw new System.ArgumentException("Failed to get default cart, something went wrong");
        }

        private Purchase getPurchase(Order order)
        {
            foreach(Link orderLink in order.links) {
                if (orderLink.rel.Equals("purchaseform"))
                {

                    Purchase purchaseFormresponse = dcapiClient.get<Purchase>(orderLink.uri, new Dictionary<String, String>(), new Dictionary<String, String>());
                    IRestResponse submitPurchaseResponse =  dcapiClient.post(purchaseFormresponse.links.First().uri, URIUtil.createStubParamters(), new Dictionary<String, String>());
                    String orderLocationUri = URIUtil.getHeaderValue(submitPurchaseResponse.Headers, "Location");
                    Purchase purchase = dcapiClient.get<Purchase>(orderLocationUri, new Dictionary<String, String>(), new Dictionary<String, String>());
                    IRestResponse purchaseRawResponse = dcapiClient.get(orderLocationUri, new Dictionary<String, String>(), new Dictionary<String, String>());
                    return createCompletePurchase(purchase, purchaseRawResponse);
                }
            }
            throw new System.ArgumentException("Failed to create purchase, something went wrong");
        }

        private Purchase createCompletePurchase(Purchase partialPurchase, IRestResponse purchaseRawResponse)
        {
            Purchase completePurchase = new Purchase();
            completePurchase.links = new List<Link>();
            completePurchase.total = new ProductPrice();
            completePurchase.tax = new ProductPrice();

            JObject purchaseJson = JObject.Parse(purchaseRawResponse.Content);
            //get the total
            JArray total = (JArray)purchaseJson["monetary-total"];
            completePurchase.total.amount = (float)total[0]["amount"];
            completePurchase.total.currency = (String)total[0]["currency"];
            completePurchase.total.display = (String)total[0]["display"];
            //get taxes
            JObject taxes = (JObject)purchaseJson["tax-total"];
            completePurchase.tax.amount = (float)taxes["amount"];
            completePurchase.tax.currency = (String)taxes["currency"];
            completePurchase.tax.display = (String)taxes["display"];

            completePurchase.status = partialPurchase.status;
            completePurchase.purchaseNumber = partialPurchase.purchaseNumber;
            return completePurchase;
        }

        private Order getOrder(IRestResponse orderLocation)
        {
            String orderUri = URIUtil.getHeaderValue(orderLocation.Headers, "Location");
            if (String.Empty.Equals(orderUri))
            {
                return null;
            }
            return dcapiClient.get<Order>(orderUri, new Dictionary<String, String>(), new Dictionary<String, String>()); 
        }

    }
}
