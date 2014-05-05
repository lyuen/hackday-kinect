using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace OSHIrT.src
{
    public class ProductServiceImpl : ProductService
    {
        private DcapiClient dcapiClient;

        public ProductServiceImpl(DcapiClient dcapiClient)
        {
            this.dcapiClient = dcapiClient;
        }

        public List<Product> getTshirtProducts() 
        {
            TShirtLinks tshirtLinks = getProductList();
            List<Product> tshirtProducts = new List<Product>();

            foreach (Link tshirtLink in tshirtLinks.links) {
                Product tshirtProduct = getTshirtProduct(tshirtLink.uri);
                ProductPrice tshirtPrice = getTshirtPrice(tshirtProduct);
                tshirtProduct.purchase_price = tshirtPrice;
                foreach (Link link in tshirtProduct.links) {
                    if (link.rel.Equals("item")) {
                        tshirtProduct.itemUri = link.uri;
                    }
                }
                tshirtProducts.Add(tshirtProduct);
            }
            return tshirtProducts;
        }

        private TShirtLinks getProductList()
        {
            return dcapiClient.get<TShirtLinks>(URIUtil.format(new List<String> { "searches/mobee/keywords/products/qgugwzlzo5xxezdtuvzwq2lsoq" }), new Dictionary<String, String>(), new Dictionary<String, String>());
        }

        private ProductPrice getTshirtPrice(Product tshirtProduct)
        {
            ProductPrice productPrice = new ProductPrice();
            foreach (Link priceLink in tshirtProduct.links) {
                if (priceLink.rel.Equals("price"))
                {
                    IRestResponse tshirtPriceResponse = dcapiClient.get(priceLink.uri, new Dictionary<String, String>(), new Dictionary<String, String>());

                    JObject tshirtPriceJson = JObject.Parse(tshirtPriceResponse.Content);
                    JArray pricesList = (JArray)tshirtPriceJson["purchase-price"];
                    productPrice.amount = (float) pricesList[0]["amount"];
                    productPrice.currency = (String) pricesList[0]["currency"];
                    productPrice.display = (String) pricesList[0]["display"];
                }
            }
            return productPrice;
        }

      

        private Product getTshirtProduct(String tshirtUri)
        {
            Product tshirtProduct = dcapiClient.get<Product>(tshirtUri, new Dictionary<String, String>(), new Dictionary<String, String>());
            getTshirtPrice(tshirtProduct);
            return tshirtProduct;
        }
    }
}
