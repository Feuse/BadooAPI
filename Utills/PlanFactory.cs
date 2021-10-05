using ServicesModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Server.Utills
{
    public class PlanFactory
    {
        public Plan GetPlan(int likes, int repeat, int plan)
        {
            switch (plan)
            {
                case 1:
                    return new Plan() { Likes = likes, Repeat = repeat, Price = repeat == 0 ? 2.99 : 2.99 * repeat };
                case 2:
                    return new Plan() { Likes = likes, Repeat = repeat, Price = repeat == 0 ? 3.99 : 3.99 * repeat };
                case 3:
                    return new Plan() { Likes = likes, Repeat = repeat, Price = repeat == 0 ? 4.99 : 4.99 * repeat };
                case 4:
                    return new Plan() { Likes = likes, Repeat = repeat, Price = repeat == 0 ? 5.99 : 5.99 * repeat };
                default:
                    return new Plan() { Likes = likes, Repeat = repeat, Price = repeat == 0 ? ((likes / 500) * 1.5)  : ((likes / 500) * 1.5) * repeat };
            }
        }
    }
}
