using System.Drawing.Printing;
using System.Globalization;

namespace EosDelivery;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
        ApplicationConfiguration.Initialize();
        if (args.Contains("--verify"))
        {
            try { Verification.Run(args.Last()); return 0; }
            catch (Exception ex) { Directory.CreateDirectory(args.Last()); File.WriteAllText(Path.Combine(args.Last(), "verification-error.txt"), ex.ToString()); return 1; }
        }
        Application.Run(new LoginForm());
        return 0;
    }
}

public sealed class LoginForm : Form
{
    readonly DemoLogin login = new();
    readonly TextBox username = new() { Width = 260 };
    readonly TextBox password = new() { Width = 260, UseSystemPasswordChar = true };
    readonly Label status = new() { AutoSize = true, ForeColor = Color.DarkRed };
    public LoginForm()
    {
        Text = "EOS Login"; ClientSize = new(520, 350); StartPosition = FormStartPosition.CenterScreen;
        Font = new("Segoe UI", 10); BackColor = Color.WhiteSmoke;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new(36), AutoScroll = true };
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "EOS Login", Font = new("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.Red, AutoSize = true });
        layout.Controls.Add(new Label { Text = "Coursework demo", AutoSize = true });
        layout.Controls.Add(new Label { Text = "Username", AutoSize = true }); layout.Controls.Add(username);
        layout.Controls.Add(new Label { Text = "Password", AutoSize = true }); layout.Controls.Add(password);
        var show = new CheckBox { Text = "Show password", AutoSize = true };
        show.CheckedChanged += (_, _) => password.UseSystemPasswordChar = !show.Checked;
        layout.Controls.Add(show);
        var actions = new FlowLayoutPanel { Width = 420, Height = 42 };
        var submit = new Button { Text = "Login", Width = 160, Height = 32, BackColor = Color.Red, ForeColor = Color.White };
        var help = new Button { Text = "Help", Width = 100, Height = 32, BackColor = Color.Cyan };
        help.Click += (_, _) => MessageBox.Show("Demo username: JamieP\nDemo password: password123\n\nThree failed attempts trigger a 30-second pause. These are fixed demo details, not a real user account.", "Login help");
        submit.Click += (_, _) =>
        {
            if (login.TryLogin(username.Text, password.Text, DateTime.Now))
            { Hide(); using var main = new DeliveryForm(); main.ShowDialog(); Close(); }
            else status.Text = DateTime.Now < login.LockedUntil ? "Too many attempts. Wait 30 seconds." : $"Invalid details. Attempts remaining: {3 - login.Attempts}";
        };
        actions.Controls.AddRange([submit, help]); layout.Controls.Add(actions); layout.Controls.Add(status); AcceptButton = submit;
    }
}

public sealed class DeliveryForm : Form
{
    readonly Dictionary<string, Control> fields = [];
    readonly Dictionary<string, CheckBox> categories = [];
    readonly RichTextBox invoice = new() { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.White, Font = new("Consolas", 9), WordWrap = true };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 45, Padding = new(8), BackColor = Color.FromArgb(255, 241, 221), ForeColor = Color.Black };
    Shipment? previous;
    string previousInvoice = "";
    bool loading;
    readonly Color orange = Color.FromArgb(244, 167, 94);

    public DeliveryForm()
    {
        var available = Screen.PrimaryScreen?.WorkingArea.Size ?? new Size(1520, 970);
        Text = "EOS Delivery System"; ClientSize = new(Math.Min(1480, available.Width - 40), Math.Min(890, available.Height - 80)); MinimumSize = new(720, 480);
        StartPosition = FormStartPosition.CenterScreen; Font = new("Segoe UI", 9); BackColor = orange; AutoScaleMode = AutoScaleMode.None;
        var canvas = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var root = new TableLayoutPanel { Location = new(0, 0), Size = new(1460, 820), ColumnCount = 3, RowCount = 1, BackColor = orange, Padding = new(10) };
        root.ColumnStyles.Add(new(SizeType.Absolute, 680)); root.ColumnStyles.Add(new(SizeType.Absolute, 390)); root.ColumnStyles.Add(new(SizeType.Percent, 100));
        canvas.Controls.Add(root); Controls.Add(canvas); Controls.Add(status);
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        left.RowStyles.Add(new(SizeType.Absolute, 250)); left.RowStyles.Add(new(SizeType.Absolute, 360)); left.RowStyles.Add(new(SizeType.Percent, 100)); root.Controls.Add(left, 0, 0);
        var package = Section("Package Details"); left.Controls.Add(package, 0, 0);
        var packageLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        packageLayout.RowStyles.Add(new(SizeType.Absolute, 56));
        packageLayout.RowStyles.Add(new(SizeType.Percent, 100));
        package.Controls.Add(packageLayout);
        var checks = new FlowLayoutPanel { Name = "PackageCategories", Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new(8), Font = new("Segoe UI", 9), ForeColor = Color.Black };
        foreach (var name in Quote.CategoryPrices.Keys)
        { var cb = new CheckBox { Text = name, AutoSize = true, Margin = new(6) }; categories.Add(name, cb); cb.CheckedChanged += Changed; checks.Controls.Add(cb); }
        var packageGrid = Grid(4); packageGrid.Name = "PackageDimensions"; packageGrid.Margin = Padding.Empty;
        Num(packageGrid, "Length", "Length (cm)", 0, 10000, 0, 0); Num(packageGrid, "Width", "Width (cm)", 0, 10000, 0, 1);
        Num(packageGrid, "Height", "Height (cm)", 0, 10000, 1, 0); Choice(packageGrid, "WeightUnit", "Measurement", Quote.Units, 1, 1);
        Num(packageGrid, "Weight", "Package weight", 0, 100000, 2, 0); Choice(packageGrid, "Size", "Package size", Quote.SizePrices.Keys.ToArray(), 2, 1);
        Num(packageGrid, "Count", "Number of packages", 0, 15, 3, 0, 0);
        packageLayout.Controls.Add(checks, 0, 0); packageLayout.Controls.Add(packageGrid, 0, 1);
        var people = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        people.ColumnStyles.Add(new(SizeType.Percent, 50)); people.ColumnStyles.Add(new(SizeType.Percent, 50)); left.Controls.Add(people, 0, 1);
        var customer = Section("Customer Details"); var driver = Section("Delivery Driver"); people.Controls.Add(customer, 0, 0); people.Controls.Add(driver, 1, 0);
        var cg = Grid(7); customer.Controls.Add(cg); var dg = Grid(6); driver.Controls.Add(dg);
        Choice(cg, "CustomerTitle", "Title", ["Mr", "Mrs", "Ms", "Mx"], 0); Choice(dg, "DriverTitle", "Title", ["Mr", "Mrs", "Ms", "Mx"], 0);
        string[] customerKeys = ["FirstName", "MiddleName", "Surname", "Phone", "Email"];
        string[] driverKeys = ["DriverFirst", "DriverMiddle", "DriverSurname", "DriverPhone", "DriverEmail"];
        string[] labels = ["First name", "Middle name", "Surname", "Phone number", "Email"];
        for (int i = 0; i < 5; i++) { Entry(cg, customerKeys[i], labels[i], i + 1); Entry(dg, driverKeys[i], labels[i], i + 1); }
        Date(cg, "Birthday", "Birthday", 6);
        var buttons = Section("Controls"); left.Controls.Add(buttons, 0, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(12), WrapContents = true }; buttons.Controls.Add(actions);
        AddButton(actions, "Restore", Restore); AddButton(actions, "Clear", Clear); AddButton(actions, "EOS Invoice", Generate);
        AddButton(actions, "Help", Help, Color.Cyan, Color.Black); AddButton(actions, "Auto Fill (for testing)", () => Set(Shipment.Example), Color.Black, Color.Red); AddButton(actions, "Close Form", Close);

        var delivery = Section("Delivery Details"); root.Controls.Add(delivery, 1, 0); var g = Grid(23); delivery.Controls.Add(g);
        Date(g, "Shipped", "Date shipped", 0); Date(g, "Expected", "Expected delivery", 1);
        Entry(g, "Days", "Days until delivered", 2, true); Entry(g, "Future1", "Next dispatch +7d", 3, true); Entry(g, "Future2", "Then +14d", 4, true); Entry(g, "Future3", "Then +21d", 5, true);
        string[] countries = ["Ireland", "England", "Wales", "Scotland", "Northern Ireland"];
        Choice(g, "CountryTo", "Country to", countries, 6); Entry(g, "Street", "Street address", 7); Entry(g, "Street2", "Street address 2", 8);
        Entry(g, "City", "City", 9); Entry(g, "Region", "State / province", 10); Entry(g, "Postcode", "Post / zip code", 11);
        Choice(g, "CountryFrom", "Country from", countries, 12); Entry(g, "DeliveryType", "Delivery type", 13, true);
        Entry(g, "Request", "Special request", 14); Choice(g, "Insurance", "Insurance (record only)", ["1st Class", "2nd Class", "None"], 15);
        Choice(g, "Currency", "Currency (symbol only)", Quote.Currencies, 16); Num(g, "AdditionalCharge", "Extra shipping charge", 0, 100, 17);
        Entry(g, "DiscountCode", "Discount code", 18); Entry(g, "Vat", "VAT (demo 20%)", 19, true); Entry(g, "Total", "Total cost", 20, true);
        var signed = new CheckBox { Text = "Signed for (+40)", AutoSize = true, Dock = DockStyle.Fill }; fields.Add("SignedFor", signed); signed.CheckedChanged += Changed; g.Controls.Add(signed, 1, 21);
        var note = new Label { Text = "Enter additional weight, route or insurance charges in the extra shipping field.", Dock = DockStyle.Fill, AutoSize = true, ForeColor = Color.Black }; g.Controls.Add(note, 0, 22); g.SetColumnSpan(note, 2);
        var inv = Section("Invoice"); root.Controls.Add(inv, 2, 0); var printActions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 88 };
        AddButton(printActions, "Print / preview", Print); AddButton(printActions, "Save invoice", SaveInvoice);
        inv.Controls.Add(invoice); inv.Controls.Add(printActions); Set(new Shipment());
    }
    GroupBox Section(string text) => new() { Text = text, Dock = DockStyle.Fill, Padding = new(10, 30, 10, 10), Font = new("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.DarkRed };
    TableLayoutPanel Grid(int rows) { var g = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = rows, ColumnCount = 2, Font = new("Segoe UI", 9), ForeColor = Color.Black, Padding = new(4) }; g.ColumnStyles.Add(new(SizeType.Percent, 43)); g.ColumnStyles.Add(new(SizeType.Percent, 57)); for (int i = 0; i < rows; i++) g.RowStyles.Add(new(SizeType.Percent, 100f / rows)); return g; }
    void Field(TableLayoutPanel g, string key, string label, Control control, int row, int pair = 0)
    {
        if (pair > 0 && g.ColumnCount == 2) { g.ColumnCount = 4; g.ColumnStyles.Clear(); for (int i = 0; i < 4; i++) g.ColumnStyles.Add(new(SizeType.Percent, 25)); }
        control.Dock = DockStyle.Fill; control.Margin = new(3); control.Font = new("Segoe UI", 9); control.ForeColor = Color.Black;
        control.AccessibleName = label; control.Name = key; fields.Add(key, control);
        g.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true }, pair * 2, row);
        g.Controls.Add(control, pair * 2 + 1, row);
    }
    void Entry(TableLayoutPanel g, string key, string label, int row, bool readOnly = false) { var t = new TextBox { ReadOnly = readOnly, MaxLength = 160 }; Field(g, key, label, t, row); if (!readOnly) t.TextChanged += Changed; }
    void Choice(TableLayoutPanel g, string key, string label, string[] options, int row, int pair = 0) { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList }; c.Items.AddRange(options); c.SelectedIndex = 0; c.SelectedIndexChanged += Changed; Field(g, key, label, c, row, pair); }
    void Date(TableLayoutPanel g, string key, string label, int row) { var d = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd MMM yyyy" }; d.ValueChanged += Changed; Field(g, key, label, d, row); }
    void Num(TableLayoutPanel g, string key, string label, decimal min, decimal max, int row, int pair = 0, int decimals = 2) { var n = new NumericUpDown { Minimum = min, Maximum = max, DecimalPlaces = decimals }; n.ValueChanged += Changed; Field(g, key, label, n, row, pair); }
    void AddButton(Control panel, string text, Action action, Color? back = null, Color? fore = null) { var b = new Button { Text = text, Size = new(185, 40), Margin = new(5), BackColor = back ?? Color.Red, ForeColor = fore ?? Color.White, Font = new("Segoe UI", 10, FontStyle.Bold), UseVisualStyleBackColor = false }; b.Click += (_, _) => action(); panel.Controls.Add(b); }
    string T(string key) => fields[key].Text;
    decimal N(string key) => ((NumericUpDown)fields[key]).Value;
    DateTime D(string key) => ((DateTimePicker)fields[key]).Value.Date;
    public Shipment Read() => new()
    {
        CustomerTitle = T("CustomerTitle"), FirstName = T("FirstName"), MiddleName = T("MiddleName"), Surname = T("Surname"), Phone = T("Phone"), Email = T("Email"), Birthday = D("Birthday"),
        DriverTitle = T("DriverTitle"), DriverFirst = T("DriverFirst"), DriverMiddle = T("DriverMiddle"), DriverSurname = T("DriverSurname"), DriverPhone = T("DriverPhone"), DriverEmail = T("DriverEmail"),
        Categories = categories.Where(x => x.Value.Checked).Select(x => x.Key).ToArray(), Size = T("Size"), Length = N("Length"), Width = N("Width"), Height = N("Height"), Weight = N("Weight"), WeightUnit = T("WeightUnit"), Count = (int)N("Count"),
        Shipped = D("Shipped"), Expected = D("Expected"), CountryTo = T("CountryTo"), CountryFrom = T("CountryFrom"), Street = T("Street"), Street2 = T("Street2"), City = T("City"), Region = T("Region"), Postcode = T("Postcode"), Insurance = T("Insurance"), Request = T("Request"), Currency = T("Currency"), AdditionalCharge = N("AdditionalCharge"), DiscountCode = T("DiscountCode"), SignedFor = ((CheckBox)fields["SignedFor"]).Checked
    };
    public void Set(Shipment s)
    {
        loading = true;
        foreach (var property in typeof(Shipment).GetProperties().Where(p => p.Name != "Example" && p.Name != "Categories"))
        {
            if (!fields.TryGetValue(property.Name, out var c)) continue;
            var value = property.GetValue(s);
            if (c is NumericUpDown n) n.Value = Convert.ToDecimal(value);
            else if (c is DateTimePicker d) d.Value = (DateTime)value!;
            else if (c is CheckBox cb) cb.Checked = (bool)value!;
            else c.Text = value?.ToString() ?? "";
        }
        foreach (var (key, cb) in categories) cb.Checked = s.Categories.Contains(key);
        loading = false; Changed(null, EventArgs.Empty);
    }
    void Changed(object? sender, EventArgs e)
    {
        if (loading || !fields.ContainsKey("SignedFor")) return;
        invoice.Clear(); fields["Total"].Text = ""; fields["Vat"].Text = "";
        var s = Read(); fields["Days"].Text = Quote.Days(s).ToString(); fields["DeliveryType"].Text = Quote.DeliveryType(s);
        for (int i = 1; i <= 3; i++) fields["Future" + i].Text = s.Shipped.AddDays(i * 7).ToString("dd MMM yyyy");
        status.Text = "Ready. Use Auto Fill to try the sample details, then EOS Invoice. Currency changes the symbol only.";
    }
    public bool TryGenerate()
    {
        var s = Read(); var errors = Quote.Validate(s, DateTime.Today);
        if (errors.Length > 0) { status.Text = errors[0]; invoice.Text = "PLEASE CHECK\n\n" + string.Join("\n", errors); return false; }
        var q = Quote.Calculate(s, DateTime.Today); invoice.Text = Quote.Invoice(s, q); fields["Vat"].Text = q.Vat.ToString("F2"); fields["Total"].Text = q.Total.ToString("F2");
        status.Text = q.Discount > 0 ? "Invoice ready. 20% discount applied." : "Invoice ready. No discount applied."; return true;
    }
    void Generate() => TryGenerate();
    public void Clear() { previous = Read(); previousInvoice = invoice.Text; Set(new Shipment()); status.Text = "Form cleared. Restore brings back the previous entries."; }
    public void Restore() { if (previous == null) { status.Text = "There is no cleared form to restore."; return; } Set(previous); if (previousInvoice.Length > 0 && !previousInvoice.StartsWith("PLEASE CHECK")) TryGenerate(); status.Text = "Previous form restored."; }
    void Help() => MessageBox.Show("1. Enter customer, driver, package and delivery details.\n2. Select all applicable categories and a size.\n3. Enter any extra shipping charge. Include any weight, route or insurance fees here.\n4. Enter 'discount' for 20% off.\n5. Select EOS Invoice, then save or preview/print it.\n\nClear saves a temporary snapshot for Restore. Auto Fill uses fictional test details. Nothing is uploaded or saved automatically. Weight must be entered manually. Insurance is recorded only. Currency changes only the displayed symbol.", "EOS help");
    void SaveInvoice() { if (!TryGenerate()) return; using var dialog = new SaveFileDialog { Filter = "Text invoice|*.txt", FileName = "EOS-invoice.txt" }; if (dialog.ShowDialog() == DialogResult.OK) File.WriteAllText(dialog.FileName, invoice.Text); }
    void Print()
    {
        if (!TryGenerate()) return;
        using var document = new PrintDocument { DocumentName = "EOS coursework invoice" };
        using var font = new Font("Consolas", 10);
        string remaining = invoice.Text;
        document.BeginPrint += (_, _) => remaining = invoice.Text;
        document.PrintPage += (_, e) =>
        {
            if (e.Graphics == null) return;
            var bounds = new RectangleF(e.MarginBounds.X, e.MarginBounds.Y, e.MarginBounds.Width, e.MarginBounds.Height);
            e.Graphics.MeasureString(remaining, font, bounds.Size, StringFormat.GenericTypographic, out int count, out int linesFilled);
            if (count <= 0) throw new InvalidOperationException("Printer margins leave no space for invoice text.");
            e.Graphics.DrawString(remaining[..count], font, Brushes.Black, bounds, StringFormat.GenericTypographic);
            remaining = remaining[count..]; e.HasMorePages = remaining.Length > 0;
        };
        using var preview = new PrintPreviewDialog { Document = document, Width = 1000, Height = 800 };
        try { preview.ShowDialog(); } catch (InvalidPrinterException) { MessageBox.Show("No printer is available. Use Save invoice instead.", "Printing"); }
    }
    public string InvoiceText => invoice.Text;
    internal bool PackageInputsAreVisible()
    {
        var categoriesPanel = Controls.Find("PackageCategories", true).Single();
        var dimensions = Controls.Find("PackageDimensions", true).Single();
        if (categoriesPanel.Bounds.IntersectsWith(dimensions.Bounds)) return false;
        foreach (string key in new[] { "Length", "Width", "Height", "Weight", "WeightUnit", "Size", "Count" })
        {
            Control control = fields[key];
            Rectangle bounds = control.Bounds;
            if (control.Parent == null || !control.Parent.ClientRectangle.Contains(bounds)) return false;
            if (control.Height < control.PreferredSize.Height) return false;
        }
        return dimensions.Height >= 4 * 28;
    }
}
