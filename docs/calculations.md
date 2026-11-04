# Calculation notes

The demo uses decimal arithmetic and rounds monetary values to two decimal places, with midpoint values rounded away from zero.

| Item | Charge |
| --- | ---: |
| Normal | 5 |
| Electronic | 10 |
| Fragile | 15 |
| Explosive | 20 |
| Liquids | 25 |
| Flammable | 30 |
| Expensive | 35 |
| Signed for | 40 |
| Small / medium / large size | 5 / 10 / 15 |
| Per package | 5 |

Selected category charges, size, quantity, signed-for service and the entered extra shipping charge form the subtotal. The demo adds 20% VAT. The code `discount`, ignoring case and surrounding spaces, applies 20% off the tax-inclusive amount.

The sample has a subtotal of 95.00, VAT of 19.00 and a discount of 22.80, giving a total of 91.20. Currency selection changes the displayed symbol without an exchange-rate calculation.

Weight must be positive and no more than 100 kg or its equivalent in grams, pounds or ounces. Package count must be between one and fifteen. Dimensions must be positive. Customers must be at least eighteen, and expected delivery cannot precede dispatch.

The date selector calculates the actual duration and offers dispatch dates seven, fourteen and twenty-one days ahead. The delivery-band labels are 1-2, 3-5 and 5-7 days. Durations exceeding seven days are explicitly flagged in the invoice.

The demo login pauses for thirty seconds after three failures. Clear stores a temporary form snapshot; Restore reinstates it. Editing inputs invalidates the displayed invoice so stale totals cannot be exported.
