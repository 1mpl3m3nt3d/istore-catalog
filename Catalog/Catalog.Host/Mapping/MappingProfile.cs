using Catalog.Host.Data.Entities;
using Catalog.Host.Models.Dtos;

namespace Catalog.Host.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CatalogItem, CatalogItemDto>()
            .ForMember("PictureUrl", opt =>
                opt.MapFrom<CatalogItemPictureResolver, string>(ci =>
                    ci.PictureFileName ?? string.Empty)).ReverseMap();

        CreateMap<CatalogBrand, CatalogBrandDto>().ReverseMap();
        CreateMap<CatalogType, CatalogTypeDto>().ReverseMap();
        CreateMap<IEnumerable<CatalogBrand>?, IEnumerable<CatalogBrandDto>?>().ReverseMap();
        CreateMap<IEnumerable<CatalogItem>?, IEnumerable<CatalogItemDto>?>().ReverseMap();
        CreateMap<IEnumerable<CatalogType>?, IEnumerable<CatalogTypeDto>?>().ReverseMap();
    }
}
