using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OSHIrT.src;
using RestSharp;

namespace OSHIrT.tests
{
    class DcapiClientImplTest
    {
 
        static void testDcapiClientGet()
        {
            DcapiClient dcapiClient = new DcapiClientImpl("http://10.10.3.215:8080/dcapi");
            var response = dcapiClient.login("oliver.harris@elasticpath.com", "password", "mobee");
            Console.WriteLine(response);
        }

        static void testGetTshirtProducts()
        {
            DcapiClient dcapiClient = new DcapiClientImpl("http://10.10.3.215:8080/dcapi");
            ProductService productService = new ProductServiceImpl(dcapiClient);
            List<Product> tshirtProducts = productService.getTshirtProducts();
        }

        static void testCreateOrder()
        {
            DcapiClient dcapiClient = new DcapiClientImpl("http://10.10.3.215:8080/dcapi");
            dcapiClient.login("oliver.harris@elasticpath.com", "password", "mobee");
            CheckoutService checkoutService = new CheckoutServiceImpl(dcapiClient);
            checkoutService.createOrder("mobee");
        }

        static void testBuyAllShirtsAndCompletePurchase()
        {
            DcapiClient dcapiClient = new DcapiClientImpl("http://10.10.3.215:8080/dcapi");
            dcapiClient.login("oliver.harris@elasticpath.com", "password", "mobee");
            ProductService productService = new ProductServiceImpl(dcapiClient);
            List<Product> tshirtProducts = productService.getTshirtProducts();

            CartService cartService = new CartServiceImpl(dcapiClient);
            foreach (Product tshirt in tshirtProducts) {
                String cartLocationUri = cartService.addToCart("mobee",tshirt.itemUri,2);
            }

            CheckoutService checkoutService = new CheckoutServiceImpl(dcapiClient);
            Purchase completedPurchase = checkoutService.createOrder("mobee");
        }

        static void Main(string[] args)
        {
            //testDcapiClientGet();
            //testGetTshirtProducts();
            //testCreateOrder();
            testBuyAllShirtsAndCompletePurchase();
        }
    }
}
