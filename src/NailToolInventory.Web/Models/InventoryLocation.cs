namespace NailToolInventory.Models;

public class InventoryLocation
{
    public int Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public bool CanFulfillOnlineOrders { get; private set; }

    public int FulfillmentPriority { get; private set; }


    private InventoryLocation()
    {
    }


    public InventoryLocation(
        string code,
        string name,
        string? address = null,
        bool canFulfillOnlineOrders = true,
        int fulfillmentPriority = 100)
    {
        Code = NormalizeCode(code);

        UpdateDetails(
            name,
            address,
            canFulfillOnlineOrders,
            fulfillmentPriority);

        IsActive = true;
    }


    public void UpdateDetails(
        string name,
        string? address,
        bool canFulfillOnlineOrders,
        int fulfillmentPriority)
    {
        Name = NormalizeName(name);

        var normalizedAddress =
            string.IsNullOrWhiteSpace(address)
                ? null
                : address.Trim();

        if (normalizedAddress?.Length > 500)
        {
            throw new ArgumentException(
                "Address cannot exceed 500 characters.",
                nameof(address));
        }

        if (fulfillmentPriority <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fulfillmentPriority),
                "Fulfillment priority must be greater than zero.");
        }

        Address = normalizedAddress;
        CanFulfillOnlineOrders = canFulfillOnlineOrders;
        FulfillmentPriority = fulfillmentPriority;
    }


    public void Activate()
    {
        IsActive = true;
    }


    public void Deactivate()
    {
        IsActive = false;
    }


    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Location code is required.",
                nameof(code));
        }

        var normalizedCode =
            code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > 30)
        {
            throw new ArgumentException(
                "Location code cannot exceed 30 characters.",
                nameof(code));
        }

        return normalizedCode;
    }


    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Location name is required.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > 150)
        {
            throw new ArgumentException(
                "Location name cannot exceed 150 characters.",
                nameof(name));
        }

        return normalizedName;
    }
}