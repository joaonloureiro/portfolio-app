using AutoMapper;
using JoaoLoureiro.Portfolio.Api.Dtos;
using JoaoLoureiro.Portfolio.Core.Models;

namespace JoaoLoureiro.Portfolio.Api.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile() {
        CreateMap<ContactMessageRequest, EmailToSend>()
        .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Name))
        .ForMember(dest => dest.ReplyToEmail, opt => opt.MapFrom(src => src.Email))
        .ForMember(dest => dest.MessageBody, opt => opt.MapFrom(src => src.Message));
    }
}
