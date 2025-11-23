# Sample Document Template Structure

This document describes the structure of a Word document template that works with the Ouderschapsplan Document Generator.

## Example Document Structure

```
DIVORCE AGREEMENT

1. CHILDREN AND CUSTODY
   [Content Control: ^]
   
   1.1 Children names:
   [Content Control: John and Jane Doe]
   
   1.2 Custody arrangement:
   [Content Control: Shared custody, alternating weeks]

2. ASSET DIVISION
   [Content Control: Assets will be divided as follows:]
   
   2.1 House:
   [Content Control: Sold, proceeds divided 50/50]
   
   2.2 Car:
   [Content Control: #]
   
   2.3 Bank accounts:
   [Content Control: Each party keeps their own accounts]

3. PENSION RIGHTS
   [Content Control: No pension division applicable]
   
   3.1 Retirement accounts:
   [Content Control: Each party maintains their own]

4. SPOUSAL SUPPORT
   [Content Control: ^]
   
   4.1 Monthly amount:
   [Content Control: Not applicable]
   
   4.2 Duration:
   [Content Control: Not applicable]

5. FINAL PROVISIONS
   [Content Control: Both parties agree to these terms]
```

## How it Works

1. **Content Controls with `^`**: The entire article will be removed
   - In the example, articles 1 (CHILDREN AND CUSTODY) and 4 (SPOUSAL SUPPORT) will be completely removed

2. **Content Controls with `#`**: Only that specific paragraph will be removed
   - In the example, only paragraph 2.2 (Car) will be removed

3. **After Processing**: The document will be renumbered:
   - Article 2 becomes Article 1
   - Article 3 becomes Article 2
   - Article 5 becomes Article 3
   - Sub-article 2.3 becomes 1.2

## Final Output

```
DIVORCE AGREEMENT

1. ASSET DIVISION
   Assets will be divided as follows:
   
   1.1 House:
   Sold, proceeds divided 50/50
   
   1.2 Bank accounts:
   Each party keeps their own accounts

2. PENSION RIGHTS
   No pension division applicable
   
   2.1 Retirement accounts:
   Each party maintains their own

3. FINAL PROVISIONS
   Both parties agree to these terms
```

## Creating Your Template

1. Use Word's Developer tab to insert Content Controls
2. Place `^` in content controls where entire articles should be removed
3. Place `#` in content controls where only that paragraph should be removed
4. Use Power Automate to populate other content controls with actual data
5. Send to the ProcessDocument function for final processing