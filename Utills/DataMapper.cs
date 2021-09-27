using AutoMapper;
using ServicesModels;

namespace Services.Server.Utills
{
    public class DataMapper : Profile
    {
        public DataMapper()
        {
            CreateMap<UserServiceCredentials, Data>();
            CreateMap<ServiceSessions, Data>();
        }
    }
}
