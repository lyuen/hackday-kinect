using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSHIrT.src
{
    public class Product
    {
        public String name { get; set; }

        public ProductPrice purchase_price { get; set; }

        public List<Link> links { get; set; }

        public String itemUri { get; set; }
        
    }
}
