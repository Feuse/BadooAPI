using Services.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Server.Interfaces
{
    public interface IJsonFactory
    {
        public dynamic GetJson(JsonTypes types);
    }
}
