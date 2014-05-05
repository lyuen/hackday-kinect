using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSHIrT.src
{
    public interface CheckoutService
    {

        Purchase createOrder(String storeCode);

    }
}
