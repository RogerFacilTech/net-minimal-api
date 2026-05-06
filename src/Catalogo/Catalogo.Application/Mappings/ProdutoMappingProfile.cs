using AutoMapper;
using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Produto;

namespace FacShopAPI.Catalogo.Application.Mappings;

public class ProdutoMappingProfile : Profile
{
    public ProdutoMappingProfile()
    {
        CreateMap<Produto, ProdutoResponse>()
            .ForMember(dest => dest.Descricao, opt => opt.MapFrom(src => src.Descricao.Value))
            .ForMember(dest => dest.Preco, opt => opt.MapFrom(src => src.Preco.Value))
            .ForMember(dest => dest.Categoria, opt => opt.MapFrom(src => src.Categoria.Value))
            .ForMember(dest => dest.Estoque, opt => opt.MapFrom(src => src.Estoque.Value));
    }
}
