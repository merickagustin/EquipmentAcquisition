namespace EquipmentAcquisition.Core.Dtos;

public record VendorDto(int Id, string Name, string? ContactEmail);

public record CreateVendorDto(string Name, string? ContactEmail);

public record UpdateVendorDto(string Name, string? ContactEmail);
