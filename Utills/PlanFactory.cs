using ServicesModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Server.Utills
{
    public class PlanFactory
    {
        public Plan GetPlan(int likes)
        {
            switch (likes)
            {
                case 1:
                    return new Plan() {Likes= likes, Price = 2.99 };
                case 2:
                    return new Plan() { Likes = likes, Price = 3.99 };
                case 3:
                    return new Plan() { Likes = likes, Price = 4.99 };
                case 4:
                    return new Plan() { Likes = likes, Price = 5.99 };
                default:
                    return new Plan() { Likes = likes, Price = (likes / 500) * 1.5 };
            }
        }
    }
}
