using Catalog.Host.Data.Entities;

namespace Catalog.Host.Data.EntityConfigurations;

public class CatalogTypeEntityTypeConfiguration
    : IEntityTypeConfiguration<CatalogType>
{
    public void Configure(EntityTypeBuilder<CatalogType> builder)
    {
        builder
            .ToTable("CatalogType");

        builder
            .HasKey(ci => ci.Id);

        builder
            .Property(ci => ci.Id)
            .UseHiLo("catalog_type_hilo")
            .IsRequired();

        builder
            .Property(cb => cb.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .HasData(new List<CatalogType>()
        {
            new CatalogType() { Id = 1, Type = "Computer Case" },             // 1
            new CatalogType() { Id = 2, Type = "Desk Mount" },                // 2
            new CatalogType() { Id = 3, Type = "Gamepad" },                   // 3
            new CatalogType() { Id = 4, Type = "Graphics Card (GPU)" },       // 4
            new CatalogType() { Id = 5, Type = "Hard-Disk Drive (HDD)" },     // 5
            new CatalogType() { Id = 6, Type = "Headphones" },                // 6
            new CatalogType() { Id = 7, Type = "Keyboard" },                  // 7
            new CatalogType() { Id = 8, Type = "Laptop" },                    // 8
            new CatalogType() { Id = 9, Type = "Memory (RAM)" },              // 9
            new CatalogType() { Id = 10, Type = "Microphone" },                // 10
            new CatalogType() { Id = 11, Type = "Monitor" },                   // 11
            new CatalogType() { Id = 12, Type = "Motherboard" },               // 12
            new CatalogType() { Id = 13, Type = "Mouse" },                     // 13
            new CatalogType() { Id = 14, Type = "MousePad" },                  // 14
            new CatalogType() { Id = 15, Type = "Processor (CPU)" },           // 15
            new CatalogType() { Id = 16, Type = "Power Supply Unit (PSU)" },   // 16
            new CatalogType() { Id = 17, Type = "Solid-State Drive (SSD)" },   // 17
            new CatalogType() { Id = 18, Type = "SmartPhone" },                // 18
            new CatalogType() { Id = 19, Type = "SmartWatch" },                // 19
            new CatalogType() { Id = 20, Type = "Speakers" },                  // 20
            new CatalogType() { Id = 21, Type = "Subwoofer" },                 // 21
            new CatalogType() { Id = 22, Type = "Tablet" },                    // 22
            new CatalogType() { Id = 23, Type = "Web Camera" },                // 23
            new CatalogType() { Id = 24, Type = "Wrist Rest" },                // 24
        });
    }
}
