# Windows UI checks

Checked on 4 November 2026 using the Windows x64 application at the laptop's display scaling.

- Length and Width labels and input fields are fully visible, with no overlap from the category panel.
- Demo login opens the delivery form.
- Auto Fill populates customer, driver, package and shipping details.
- EOS Invoice generates the sample total of 91.20.
- Clear removes the inputs and invoice. Restore reinstates both.
- Removing the customer first name clears the stale total and prevents invoice generation with a clear validation message.
- Re-entering the name permits invoice generation again.
- Print preview shows the complete sample invoice on one page. No printer job was submitted.

The automated suite also checks the package layout in full-size and smaller windows, alongside calculation and validation cases. All 24 checks pass.
