using WrenchBox.Domain.Common;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Domain.Entities;

public class Vehicle : Entity
{
    public Guid CustomerId { get; private set; }
    public string Plate { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }

    public Customer? Customer { get; private set; }

    private Vehicle() { }

    public static Vehicle Create(Guid customerId, string plate, string brand, string model, int year)
    {
        var plateVo = ValueObjects.Plate.Create(plate);

        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException("Vehicle brand is required.");

        if (string.IsNullOrWhiteSpace(model))
            throw new DomainException("Vehicle model is required.");

        var currentYear = DateTime.UtcNow.Year;
        if (year < 1900 || year > currentYear + 1)
            throw new DomainException($"Vehicle year must be between 1900 and {currentYear + 1}.");

        return new Vehicle
        {
            CustomerId = customerId,
            Plate = plateVo.Value,
            Brand = brand.Trim(),
            Model = model.Trim(),
            Year = year
        };
    }

    public void Update(string brand, string model, int year)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException("Vehicle brand is required.");

        if (string.IsNullOrWhiteSpace(model))
            throw new DomainException("Vehicle model is required.");

        var currentYear = DateTime.UtcNow.Year;
        if (year < 1900 || year > currentYear + 1)
            throw new DomainException($"Vehicle year must be between 1900 and {currentYear + 1}.");

        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        MarkUpdated();
    }
}
