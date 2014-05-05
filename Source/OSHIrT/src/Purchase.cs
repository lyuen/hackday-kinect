using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSHIrT.src
{
    public class Purchase
    {
        public List<Link> links { get; set; }

        public String status { get; set; }

        public ProductPrice total { get; set; }
        
        public ProductPrice tax { get; set; }

        public String purchaseNumber { get; set; }
        
    }
}
