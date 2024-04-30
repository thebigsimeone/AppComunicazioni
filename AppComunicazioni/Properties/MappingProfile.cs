using AutoMapper;
using AppComunicazioni.Models;
using AppComunicazioni.Models.DTO_s;

namespace AppComunicazioni.Properties
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Comunicazioni, ComunicazioniDTO>().ReverseMap();
            CreateMap<Destinatari, DestinatariDTO>().ReverseMap();
        }
    }
}
