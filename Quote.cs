using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace EosDelivery;

public sealed record Shipment
{
    public string CustomerTitle { get; init; } = "Mr";
    public string FirstName { get; init; } = "";
    public string MiddleName { get; init; } = "";
    public string Surname { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Email { get; init; } = "";
    public DateTime Birthday { get; init; } = new(1990, 1, 1);
    public string DriverTitle { get; init; } = "Mr";
    public string DriverFirst { get; init; } = "";
    public string DriverMiddle { get; init; } = "";
    public string DriverSurname { get; init; } = "";
    public string DriverPhone { get; init; } = "";
    public string DriverEmail { get; init; } = "";
    public string[] Categories { get; init; } = [];
    public string Size { get; init; } = "Small";
    public decimal Length { get; init; }
    public decimal Width { get; init; }
    public decimal Height { get; init; }
    public decimal Weight { get; init; }
    public string WeightUnit { get; init; } = "kg";
    public int Count { get; init; }
    public DateTime Shipped { get; init; } = DateTime.Today;
    public DateTime Expected { get; init; } = DateTime.Today.AddDays(5);
    public string CountryTo { get; init; } = "Ireland";
    public string CountryFrom { get; init; } = "England";
    public string Street { get; init; } = "";
    public string Street2 { get; init; } = "";
    public string City { get; init; } = "";
    public string Region { get; init; } = "";
    public string Postcode { get; init; } = "";
    public string Insurance { get; init; } = "1st Class";
    public string Request { get; init; } = "";
    public string Currency { get; init; } = "£";
    public decimal AdditionalCharge { get; init; }
    public string DiscountCode { get; init; } = "";
    public bool SignedFor { get; init; }

    public static Shipment Example => new()
    {
        FirstName = "John", MiddleName = "A.", Surname = "Doe", Phone = "01234567890",
        Email = "john.doe@example.com", DriverFirst = "Jane", DriverMiddle = "B.",
        DriverSurname = "Smith", DriverPhone = "09876543210", DriverEmail = "jane.smith@example.com",
        Categories = ["Normal", "Electronic", "Liquids", "Expensive"], Length = 10, Width = 20,
        Height = 30, Weight = 20, Count = 2, Street = "123 Main St", Street2 = "Apt 4",
        City = "Dublin", Region = "Dublin", Postcode = "D02 XY12", Request = "Handle with care",
        DiscountCode = "discount", AdditionalCharge = 5, Expected = DateTime.Today.AddDays(14)
    };
}

public record QuoteLine(string Description, decimal Amount);
public record QuoteResult(QuoteLine[] Lines, decimal Subtotal, decimal Vat, decimal Discount, decimal Total);

public static class Quote
{
    // Demonstration tariff, in the selected display currency.
    public static readonly IReadOnlyDictionary<string, decimal> CategoryPrices = new Dictionary<string, decimal>
    {
        ["Normal"] = 5, ["Electronic"] = 10, ["Fragile"] = 15, ["Explosive"] = 20,
        ["Liquids"] = 25, ["Flammable"] = 30, ["Expensive"] = 35
    };
    public static readonly IReadOnlyDictionary<string, decimal> SizePrices = new Dictionary<string, decimal>
    { ["Small"] = 5, ["Medium"] = 10, ["Large"] = 15 };
    public static readonly string[] Currencies = ["£", "$", "€", "¥", "₽", "₹", "₩"];
    public static readonly string[] Units = ["kg", "g", "lbs", "oz"];
    public const decimal VatRate = .20m; // Demonstration VAT rate.

    public static int Days(Shipment s) => (s.Expected.Date - s.Shipped.Date).Days;
    public static string DeliveryType(Shipment s) => Days(s) <= 2 ? "1-2 day delivery" : Days(s) <= 5 ? "3-5 day delivery" : "5-7 day delivery";
    public static decimal Kg(Shipment s) => s.WeightUnit switch
    { "g" => s.Weight / 1000m, "lbs" => s.Weight * .45359237m, "oz" => s.Weight * .028349523125m, _ => s.Weight };
    public static string[] Validate(Shipment s, DateTime today)
    {
        List<string> errors = [];
        void Required(string value, string label) { if (string.IsNullOrWhiteSpace(value)) errors.Add($"Enter {label}."); }
        Required(s.FirstName, "the customer's first name"); Required(s.Surname, "the customer's surname");
        Required(s.Phone, "a phone number"); Required(s.Street, "a street address"); Required(s.City, "a city");
        Required(s.Postcode, "a postcode"); Required(s.CountryTo, "the destination country"); Required(s.CountryFrom, "the origin country");
        if (!MailAddress.TryCreate(s.Email, out var email) || email.Address != s.Email.Trim()) errors.Add("Enter a valid customer email address.");
        if (s.DriverEmail.Length > 0 && !MailAddress.TryCreate(s.DriverEmail, out _)) errors.Add("Enter a valid driver email address.");
        if (s.Birthday.Date > today.Date.AddYears(-18)) errors.Add("The customer must be at least 18.");
        if (s.Count is < 1 or > 15) errors.Add("Enter between 1 and 15 packages.");
        if (!Units.Contains(s.WeightUnit) || Kg(s) <= 0 || Kg(s) > 100) errors.Add("Enter a package weight above zero and at most 100 kg (or equivalent).");
        if (s.Length <= 0 || s.Width <= 0 || s.Height <= 0) errors.Add("Enter positive package dimensions in centimetres.");
        if (!SizePrices.ContainsKey(s.Size)) errors.Add("Choose Small, Medium or Large.");
        if (s.Categories.Any(c => !CategoryPrices.ContainsKey(c))) errors.Add("Choose a recognised package category.");
        if (Days(s) < 0) errors.Add("Expected delivery cannot be before dispatch.");
        if (s.AdditionalCharge is < 0 or > 100) errors.Add("Additional shipping charges must be between 0 and 100.");
        if (!Currencies.Contains(s.Currency)) errors.Add("Choose a display currency.");
        return errors.ToArray();
    }
    public static QuoteResult Calculate(Shipment s, DateTime today)
    {
        var errors = Validate(s, today);
        if (errors.Length > 0) throw new ArgumentException(string.Join(Environment.NewLine, errors));
        List<QuoteLine> lines = s.Categories.Distinct().Select(c => new QuoteLine(c, CategoryPrices[c])).ToList();
        lines.Add(new("Package size: " + s.Size, SizePrices[s.Size]));
        lines.Add(new($"{s.Count} packages × 5.00", s.Count * 5m));
        if (s.SignedFor) lines.Add(new("Signed for", 40));
        lines.Add(new("Additional shipping charge (entered)", s.AdditionalCharge));
        decimal subtotal = lines.Sum(x => x.Amount), vat = Money(subtotal * VatRate);
        decimal discount = s.DiscountCode.Trim().Equals("discount", StringComparison.OrdinalIgnoreCase) ? Money((subtotal + vat) * .20m) : 0;
        return new(lines.ToArray(), subtotal, vat, discount, Money(subtotal + vat - discount));
    }
    static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    public static string Invoice(Shipment s, QuoteResult q)
    {
        var b = new StringBuilder();
        b.AppendLine("EOS INVOICE (DEMO)");
        b.AppendLine("Currency selection changes the symbol only.");
        b.AppendLine("Extra shipping fees are entered manually.");
        b.AppendLine();
        b.AppendLine("SHIPPING DATES");
        b.AppendLine($"Dispatched: {s.Shipped:dd/MM/yyyy}   Expected: {s.Expected:dd/MM/yyyy}");
        b.AppendLine($"Duration: {Days(s)} day(s)   Band: {DeliveryType(s)}");
        if (Days(s) > 7) b.AppendLine("Actual duration exceeds the 5-7 day band.");
        b.AppendLine("Future dispatch options: " + string.Join(", ", new[] {7,14,21}.Select(d => s.Shipped.AddDays(d).ToString("dd/MM/yyyy"))));
        b.AppendLine(); b.AppendLine("CUSTOMER");
        b.AppendLine($"{s.CustomerTitle} {s.FirstName} {s.MiddleName} {s.Surname}");
        b.AppendLine($"{s.Phone} | {s.Email} | Born {s.Birthday:dd/MM/yyyy}");
        b.AppendLine($"Driver: {s.DriverTitle} {s.DriverFirst} {s.DriverMiddle} {s.DriverSurname}");
        b.AppendLine($"{s.DriverPhone} | {s.DriverEmail}");
        b.AppendLine(); b.AppendLine("DELIVERY");
        b.AppendLine($"From {s.CountryFrom} to {s.CountryTo}");
        b.AppendLine($"{s.Street}, {s.Street2}\n{s.City}, {s.Region}, {s.Postcode}");
        b.AppendLine($"{s.Count} × {s.Size}; {s.Length} × {s.Width} × {s.Height} cm; {s.Weight} {s.WeightUnit}");
        b.AppendLine($"Insurance selection: {s.Insurance} (recorded only)");
        b.AppendLine($"Special request: {s.Request}");
        b.AppendLine(); b.AppendLine("COST BREAKDOWN");
        foreach (var line in q.Lines) b.AppendLine($"{line.Description}: {s.Currency}{line.Amount:F2}");
        b.AppendLine($"Subtotal: {s.Currency}{q.Subtotal:F2}");
        b.AppendLine($"VAT (demo 20%): {s.Currency}{q.Vat:F2}");
        b.AppendLine($"Discount: −{s.Currency}{q.Discount:F2}");
        b.AppendLine($"TOTAL: {s.Currency}{q.Total:F2}");
        return b.ToString();
    }
}

public sealed class DemoLogin
{
    public int Attempts { get; private set; }
    public DateTime LockedUntil { get; private set; }
    public bool TryLogin(string username, string password, DateTime now)
    {
        if (now < LockedUntil) return false;
        if (Attempts >= 3) Attempts = 0;
        if (username == "JamieP" && password == "password123") { Attempts = 0; return true; }
        if (++Attempts >= 3) LockedUntil = now.AddSeconds(30);
        return false;
    }
}
