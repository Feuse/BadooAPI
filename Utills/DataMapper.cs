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
            CreateMap<UserCredentials, Data>().ForMember(x => x.About, opt => opt.Ignore());
        }
    }
}
