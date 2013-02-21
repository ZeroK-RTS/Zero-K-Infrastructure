using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NightWatch;
using PlasmaShared;

namespace Tests
{
    [TestFixture]
    public class PaypalTests
    {
        
        
        [Test]
        public void CheckConversion() {
            Console.WriteLine("100 CZK in EUR = " + PayPalInterface.ConvertToEuros("CZK", 100));
        }

    }
}
