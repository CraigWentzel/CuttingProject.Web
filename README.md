\# Cutting Project Web



An ASP.Net Core MVC web application that parses steel cutting list PDFs, 

allocates parts to stock bins, and exports optimised cutting schedules.



\## Features



\- Upload and parse cutting list PDFs (BI Projects format)

\- Validate PDF structure and content before processing

\- Allocate parts to stock bins by profile group

\- View bin summary and parts breakdown

\- Export results to PDF

\- Session-based result storage



\## Tech Stack



\- ASP.Net Core MVC (.NET 8)

\- C# / LINQ

\- UglyToad.PdfPig (PDF parsing)

\- Bootstrap 5

\- Session middleware



\## Getting Started



\### Prerequisites

\- .NET 8 SDK

\- Visual Studio 2022 or VS Code



\### Run Locally



```bash

git clone https://github.com/CraigWentzel/CuttingProject.Web.git

cd CuttingProject.Web

dotnet restore

dotnet run

```



Navigate to `https://localhost:5001`



\## Project Structure



Controllers/    — HomeController, CuttingController

Models/         — PartsRecord, StockBin, BinSummary, CuttingSession

Services/       — PDF parsing, validation, bin allocation, export

Views/          — Razor views per controller

wwwroot/        — CSS, JS, Bootstrap



\## Author



Craig Wentzel — Cape Town, South Africa



