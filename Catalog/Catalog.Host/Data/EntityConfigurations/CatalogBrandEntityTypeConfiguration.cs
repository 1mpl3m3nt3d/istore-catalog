using Catalog.Host.Data.Entities;

namespace Catalog.Host.Data.EntityConfigurations;

public class CatalogBrandEntityTypeConfiguration
    : IEntityTypeConfiguration<CatalogBrand>
{
    public void Configure(EntityTypeBuilder<CatalogBrand> builder)
    {
        builder
            .ToTable("CatalogBrand");

        builder
            .HasKey(ci => ci.Id);

        builder
            .Property(ci => ci.Id)
            .UseHiLo("catalog_brand_hilo")
            .IsRequired();

        builder
            .Property(cb => cb.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .HasData(new List<CatalogBrand>()
        {
            new CatalogBrand() { Id = 1, Brand = "A4Tech" },                  // 1
            new CatalogBrand() { Id = 2, Brand = "AMD" },                     // 2
            new CatalogBrand() { Id = 3, Brand = "Aorus" },                   // 3
            new CatalogBrand() { Id = 4, Brand = "Apple" },                   // 4
            new CatalogBrand() { Id = 5, Brand = "Asus" },                    // 5
            new CatalogBrand() { Id = 6, Brand = "Bloody" },                  // 6
            new CatalogBrand() { Id = 7, Brand = "Edifier" },                 // 7
            new CatalogBrand() { Id = 8, Brand = "Gigabyte" },                // 8
            new CatalogBrand() { Id = 9, Brand = "Hator" },                   // 9
            new CatalogBrand() { Id = 10, Brand = "Honor" },                   // 10
            new CatalogBrand() { Id = 11, Brand = "Huawei" },                  // 11
            new CatalogBrand() { Id = 12, Brand = "HyperX" },                  // 12
            new CatalogBrand() { Id = 13, Brand = "Intel" },                   // 13
            new CatalogBrand() { Id = 14, Brand = "Keychron" },                // 14
            new CatalogBrand() { Id = 15, Brand = "Kingston" },                // 15
            new CatalogBrand() { Id = 16, Brand = "Logitech" },                // 16
            new CatalogBrand() { Id = 17, Brand = "MSI" },                     // 17
            new CatalogBrand() { Id = 18, Brand = "Razer" },                   // 18
            new CatalogBrand() { Id = 19, Brand = "Samsung" },                 // 19
            new CatalogBrand() { Id = 20, Brand = "Seagate" },                 // 20
            new CatalogBrand() { Id = 21, Brand = "Sony" },                    // 21
            new CatalogBrand() { Id = 22, Brand = "SteelSeries" },             // 22
            new CatalogBrand() { Id = 23, Brand = "Varmilo" },                 // 23
            new CatalogBrand() { Id = 24, Brand = "Western Digital" },         // 24
        });
    }
}
