using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSHIrT.src
{
    public interface CartService
    {
        String addToCart(string storeCode, string itemUri, int quantity);
    }
}
