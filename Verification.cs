using System.Text.Json;

namespace EosDelivery;

internal static class Verification
{
    public static void Run(string output)
    {
        Directory.CreateDirectory(output); List<string> checks = [];
        void Check(bool passed, string message) { if (!passed) throw new Exception(message); checks.Add(message); }
        var now = new DateTime(2026, 11, 4); var s = Shipment.Example;
        var q = Quote.Calculate(s, now);
        Check(q.Subtotal == 95 && q.Vat == 19 && q.Discount == 22.80m && q.Total == 91.20m, "Category, size, package and shipping charges reconcile with VAT and discount");
        Check(Quote.Calculate(s with { DiscountCode = "wrong" }, now).Total == 114, "Unknown discount is not applied");
        Check(Quote.Calculate(s with { DiscountCode = " DISCOUNT " }, now).Total == 91.20m, "Discount normalises whitespace and case");
        Check(Quote.Calculate(s with { SignedFor = true }, now).Subtotal == 135, "Signed-for charge is applied once");
        Check(Quote.Validate(s with { Weight = 101 }, now).Length > 0, "Overweight package rejected");
        Check(Quote.Validate(s with { Weight = 100 }, now).Length == 0, "100 kg boundary accepted");
        Check(Quote.Validate(s with { Weight = 100001, WeightUnit = "g" }, now).Length > 0, "Weight limit applies after unit conversion");
        Check(Quote.Validate(s with { Count = 0 }, now).Length > 0 && Quote.Validate(s with { Count = 16 }, now).Length > 0, "Package count boundaries enforced");
        Check(Quote.Validate(s with { Count = 15 }, now).Length == 0, "15 package boundary accepted");
        Check(Quote.Validate(s with { Expected = s.Shipped.AddDays(-1) }, now).Length > 0, "Reversed dates rejected");
        Check(Quote.Validate(s with { Birthday = now.AddYears(-18).AddDays(1) }, now).Length > 0, "Under-18 customer rejected");
        Check(Quote.Validate(s with { Birthday = now.AddYears(-18) }, now).Length == 0, "18th birthday accepted");
        Check(Quote.Validate(s with { Email = "not-an-email" }, now).Length > 0, "Invalid email rejected");
        Check(Quote.Calculate(s with { Currency = "€" }, now).Total == q.Total, "Currency changes symbol only");
        Check(Quote.DeliveryType(s with { Expected = s.Shipped.AddDays(2) }) == "1-2 day delivery", "Two-day delivery boundary");
        Check(Quote.DeliveryType(s with { Expected = s.Shipped.AddDays(5) }) == "3-5 day delivery", "Five-day delivery boundary");
        var login = new DemoLogin(); for (int i = 0; i < 3; i++) login.TryLogin("wrong", "wrong", now);
        Check(!login.TryLogin("JamieP", "password123", now.AddSeconds(29)), "Login is locked for 30 seconds");
        Check(login.TryLogin("JamieP", "password123", now.AddSeconds(30)), "Login unlocks after interval");
        using var form = new DeliveryForm(); form.Set(s); Check(form.TryGenerate(), "Valid form generates invoice");
        string before = form.InvoiceText; form.Clear(); Check(form.Read().FirstName == "" && form.InvoiceText == "", "Clear removes entries and invoice");
        form.Restore(); Check(form.Read().FirstName == s.FirstName && form.InvoiceText == before, "Restore recovers entries and invoice");
        form.Set(s with { FirstName = "Changed" }); Check(form.InvoiceText == "", "Editing invalidates stale invoice");
        form.Set(s); form.TryGenerate(); form.ClientSize = new(1480, 890); form.Opacity = 0; form.Show(); form.PerformLayout(); Application.DoEvents();
        Check(form.PackageInputsAreVisible(), "Package controls remain visible without overlapping category panel");
        form.ClientSize = new(1000, 680); form.PerformLayout(); Application.DoEvents();
        Check(form.PackageInputsAreVisible(), "Package controls remain unclipped in smaller window");
        form.ClientSize = new(1480, 890); form.PerformLayout(); Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size)); bitmap.Save(Path.Combine(output, "application.png")); form.Hide();
        File.WriteAllText(Path.Combine(output, "example-invoice.txt"), form.InvoiceText);
        File.WriteAllText(Path.Combine(output, "verification.json"), JsonSerializer.Serialize(new { passed = checks.Count, checks }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
