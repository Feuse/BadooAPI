using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Server.Utills
{
    public struct test<Tkey,Tvalue>
    {
        public Tkey one;
        public Tvalue two;

        public test(Tkey one, Tvalue two )
        {
            this.one = one;
            this.two = two;
        }
    }
}
