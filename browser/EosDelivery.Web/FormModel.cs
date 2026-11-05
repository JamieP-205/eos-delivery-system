namespace EosDelivery.Web;

/// <summary>Editable copy of the Windows form's fields. <see cref="Shipment"/> itself is immutable.</summary>
public sealed class FormModel
{
    public string CustomerTitle { get; set; } = "Mr";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime Birthday { get; set; } = new(1990, 1, 1);
    public string DriverTitle { get; set; } = "Mr";
    public string DriverFirst { get; set; } = "";
    public string DriverMiddle { get; set; } = "";
    public string DriverSurname { get; set; } = "";
    public string DriverPhone { get; set; } = "";
    public string DriverEmail { get; set; } = "";
    public HashSet<string> Categories { get; set; } = [];
    public string Size { get; set; } = "Small";
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string WeightUnit { get; set; } = "kg";
    public int Count { get; set; }
    public DateTime Shipped { get; set; } = DateTime.Today;
    public DateTime Expected { get; set; } = DateTime.Today.AddDays(5);
    public string CountryTo { get; set; } = "Ireland";
    public string CountryFrom { get; set; } = "Ireland";
    public string Street { get; set; } = "";
    public string Street2 { get; set; } = "";
    public string City { get; set; } = "";
    public string Region { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Insurance { get; set; } = "1st Class";
    public string Request { get; set; } = "";
    public string Currency { get; set; } = "£";
    public decimal AdditionalCharge { get; set; }
    public string DiscountCode { get; set; } = "";
    public bool SignedFor { get; set; }

    public Shipment ToShipment() => new()
    {
        CustomerTitle = CustomerTitle, FirstName = FirstName, MiddleName = MiddleName, Surname = Surname, Phone = Phone, Email = Email,
        Birthday = Birthday.Date, DriverTitle = DriverTitle, DriverFirst = DriverFirst, DriverMiddle = DriverMiddle, DriverSurname = DriverSurname,
        DriverPhone = DriverPhone, DriverEmail = DriverEmail,
        // Same order as the Windows checkboxes (the tariff's order), whatever order they were ticked in.
        Categories = Quote.CategoryPrices.Keys.Where(Categories.Contains).ToArray(),
        Size = Size, Length = Length, Width = Width, Height = Height, Weight = Weight, WeightUnit = WeightUnit, Count = Count,
        Shipped = Shipped.Date, Expected = Expected.Date, CountryTo = CountryTo, CountryFrom = CountryFrom, Street = Street, Street2 = Street2,
        City = City, Region = Region, Postcode = Postcode, Insurance = Insurance, Request = Request, Currency = Currency,
        AdditionalCharge = AdditionalCharge, DiscountCode = DiscountCode, SignedFor = SignedFor
    };

    public static FormModel From(Shipment s) => new()
    {
        CustomerTitle = s.CustomerTitle, FirstName = s.FirstName, MiddleName = s.MiddleName, Surname = s.Surname, Phone = s.Phone, Email = s.Email,
        Birthday = s.Birthday, DriverTitle = s.DriverTitle, DriverFirst = s.DriverFirst, DriverMiddle = s.DriverMiddle, DriverSurname = s.DriverSurname,
        DriverPhone = s.DriverPhone, DriverEmail = s.DriverEmail, Categories = [.. s.Categories], Size = s.Size, Length = s.Length, Width = s.Width,
        Height = s.Height, Weight = s.Weight, WeightUnit = s.WeightUnit, Count = s.Count, Shipped = s.Shipped, Expected = s.Expected,
        CountryTo = s.CountryTo, CountryFrom = s.CountryFrom, Street = s.Street, Street2 = s.Street2, City = s.City, Region = s.Region,
        Postcode = s.Postcode, Insurance = s.Insurance, Request = s.Request, Currency = s.Currency, AdditionalCharge = s.AdditionalCharge,
        DiscountCode = s.DiscountCode, SignedFor = s.SignedFor
    };
}
