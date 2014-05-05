using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OSHIrT.src;


namespace OSHIrT.tests
{
    [TestFixture]
    class DcapiClientImplTest
    {
        [Test]
        public void testDcapiClientGet()
        {
            DcapiClient dcapiClient = new DcapiClientImpl("http://localhost:9080/dcapi");
            ProductService productService = new ProductServiceImpl(dcapiClient);
            Product product = productService.getProduct("or2dsnrwgaydcylw==========", "mobee");
        }
    }
}
