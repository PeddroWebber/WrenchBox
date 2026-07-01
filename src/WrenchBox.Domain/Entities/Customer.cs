using WrenchBox.Domain.Common;
using WrenchBox.Domain.Exceptions;
using WrenchBox.Domain.ValueObjects;

namespace WrenchBox.Domain.Entities;

public class Customer : Entity
{
    public string Document { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    private readonly List<Vehicle> _vehicles = [];
    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();

    private Customer() { }

    public static Customer Create(string document, string name, string email, string phone)
    {
        var doc = ValueObjects.Document.Create(document);

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Customer email is required.");

        return new Customer
        {
            Document = doc.Value,
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim() ?? string.Empty
        };
    }

    public void Update(string name, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Customer email is required.");

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim() ?? string.Empty;
        MarkUpdated();
    }

    public Vehicle AddVehicle(string plate, string brand, string model, int year)
    {
        var vehicle = Vehicle.Create(Id, plate, brand, model, year);
        _vehicles.Add(vehicle);
        return vehicle;
    }
}
