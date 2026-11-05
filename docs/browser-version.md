# Browser version

[Open it](https://jamiep-205.github.io/eos-delivery-system/browser/) · [Windows app](https://github.com/JamieP-205/eos-delivery-system/releases/latest)

This is the EOS quotation and invoice workflow rebuilt as a web page, so it can be tried without downloading anything. The Windows Forms app is unchanged and remains the original.

- **Same logic.** `browser/EosDelivery.Web` compiles the Windows app's own `Quote.cs` (prices, validation, invoice text and the demo login lockout) to WebAssembly with Blazor. Only the form is new: it mirrors the Windows sections, buttons and messages.
- **Nothing leaves the page.** There's no server. Save invoice downloads a text file, and Print uses the browser's print dialog in place of the Windows print preview.
- Browser version added on 5 November 2026.

## Checked against the original

On 5 November 2026 the Windows app's own `--verify` run (24 checks) passed and produced its sample invoice. The same `Quote.cs`, run on desktop .NET, gave identical text for that sample. Then, in Chromium, the browser version was given these cases and its invoice text compared with the desktop output:

| Case | Expected (desktop .NET) | Browser |
| --- | --- | --- |
| Auto Fill sample, code `discount` | £91.20 | identical invoice |
| Sample + Signed for | £129.60 | identical invoice |
| Sample, code `wrong` | £114.00 | identical invoice |
| Sample, currency € | €91.20 (symbol only) | identical invoice |
| Fragile + Flammable, Large, 7 packages, £12.50 extra, no code | £129.00 | identical invoice |
| 101 kg | weight error | same error |
| 100,001 g | weight error after conversion | same error |
| Expected date before dispatch | date error | same error |
| 16 packages | package-count error | same error |

Also checked: three wrong logins lock sign-in for 30 seconds; editing any field clears the invoice; Clear and Restore bring back the entries and invoice; no horizontal scrolling at 375 px wide.

## Build it

```powershell
dotnet publish browser/EosDelivery.Web -c Release -o browser/publish
python -m http.server 8765 -d browser/publish/wwwroot
```

The GitHub Actions workflow `browser.yml` publishes it to GitHub Pages on every push to `main`.
