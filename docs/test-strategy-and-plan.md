# 3. Test Strategy & Test Plan

## 3.1 Scope of Testing

### 3.1.1 Në Fushë (In Scope)

Testimi mbulon të dyja shtresat e sistemit **VehicleServiceBooking**:

**Backend (ASP.NET Core Web API):**

| # | Moduli                        | Përshkrimi                                                              |
|----|-------------------------------|-------------------------------------------------------------------------|
| 1  | Autentikimi (Auth)            | Regjistrim klienti, login, gjenerim JWT, endpoint `/me`                 |
| 2  | Menaxhimi i Automjeteve       | CRUD Vehicles me kontroll aksesi sipas rolit                            |
| 3  | Rezervimet (Bookings)         | Krijim, përditësim, anulim (rregulla 24h), filtrim sipas rolit          |
| 4  | Urdhërat e Punës (WorkOrders) | Krijim automatik, tranzicion statusesh, llogaritje LaborCost            |
| 5  | Faturat (Invoices)            | Gjenerim fature, llogaritje SubTotal/Tax/Total                          |
| 6  | Pagesat (Payments)            | Krijim, validim balance, mbyllje automatike Booking+WorkOrder           |
| 7  | Qendrat e Servisit            | CRUD ServiceCenters (vetëm Manager)                                     |
| 8  | Tipet e Shërbimeve            | CRUD ServiceTypes                                                       |
| 9  | Mekanikët                     | Krijim me llogari përdoruesi, fshirje kaskadë                           |
| 10 | Oraret (Schedules)            | Krijim/fshirje oraresh për mekanikë                                     |
| 11 | Pjesët (Parts)                | CRUD Parts                                                              |
| 12 | Klientët                      | Listim, përditësim profili, fshirje nga Manager                         |
| 13 | Middleware                    | Exception handling global                                                |

**Frontend (React + Vite):**

| # | Moduli                  | Përshkrimi                                                    |
|----|------------------------|---------------------------------------------------------------|
| 1  | Autentikim (Login/Register) | Forma, validim, ruajtje token, ridrejtim sipas rolit      |
| 2  | Dashboard-et sipas rolit    | ClientDashboard, MechanicDashboard, ManagerDashboard       |
| 3  | Menaxhimi i automjeteve     | Listim, shtim, editim, fshirje automjetesh                 |
| 4  | Rezervimi                   | Forma e rezervimit, zgjedhje e qendrës/shërbimit/datës    |
| 5  | Navigimi (Routing)          | Mbrojtje rrugësh sipas rolit, ridrejtime                   |

### 3.1.2 Jashtë Fushës (Out of Scope)

- Testimi i performancës / load testing
- Testimi i sigurisë me penetration testing
- Testimi i kompatibilitetit cross-browser (përveç Chrome)
- Testimi i deployment / CI/CD pipeline
- Testimi i email notifications (nuk ekziston aktualisht)
- Testimi i mobile responsiveness

---

## 3.2 Test Levels (Nivelet e Testimit)

### 3.2.1 Unit Testing

| Aspekti            | Detajet                                                                                          |
|--------------------|--------------------------------------------------------------------------------------------------|
| **Objektivi**      | Verifikim i logjikës së izoluar brenda një njësie (metodë, funksion)                             |
| **Teknologjia**    | **xUnit** + **Moq** (Backend), **Vitest** + **React Testing Library** (Frontend)                 |
| **Përgjegjësia**   | Zhvilluesi                                                                                       |
| **Fokusi Backend** | Llogaritje LaborCost, validim datash, tranzicion statusesh, logjikë autorizimi në controllers    |
| **Fokusi Frontend**| Funksione ndihmëse (formatim datash, parsim JWT), komponentë UI të izoluara                      |
| **Shembuj testesh**| `LaborCost = (actualMinutes / 60) × hourlyRate` me vlera të ndryshme; Validim që data në të kaluarën refuzohet |

**Çfarë testohet në nivel Unit:**

```
Backend:
├── AuthController         → Regjistrim me email dublikatë, login me kredenciale të gabuara
├── BookingsApiController  → Validim date, kontroll slot-i, rregulla anulimi 24h
├── WorkOrdersApiController→ Llogaritje LaborCost, tranzicion statusesh
├── PaymentsApiController  → Validim balance, mbyllje automatike
└── InvoicesApiController  → Llogaritje SubTotal, Tax, TotalAmount

Frontend:
├── Formatim datash/orësh
├── Parsim JWT token
└── Validim formash (email, fjalëkalim)
```

### 3.2.2 Integration Testing

| Aspekti            | Detajet                                                                                          |
|--------------------|--------------------------------------------------------------------------------------------------|
| **Objektivi**      | Verifikim i ndërveprimit ndërmjet moduleve (Controller ↔ Database, API ↔ Identity)               |
| **Teknologjia**    | **xUnit** + **WebApplicationFactory** + **EF Core InMemory** (Backend), **Vitest** + **MSW** (Frontend) |
| **Përgjegjësia**   | Zhvilluesi                                                                                       |
| **Fokusi Backend** | API endpoints me databazë InMemory, autentikim JWT real, krijim automatik WorkOrder kur caktohet mekaniku |
| **Fokusi Frontend**| Komunikim komponent ↔ API (me mock server), rrjedha login → dashboard                           |

**Çfarë testohet në nivel Integration:**

```
Backend:
├── POST /api/Auth/register-client → krijon user + rol "Client" në DB
├── POST /api/BookingsApi → krijon Booking → PUT cakton mekanik → WorkOrder krijohet automatikisht
├── PUT /api/WorkOrdersApi/{id} → llogarit LaborCost → POST Invoice → POST Payment → mbyllje
├── Autorizimi: Client nuk ka akses në endpoints të Manager-it
└── Cascade delete: fshirje Mechanic → fshirje User

Frontend:
├── Login → ruajtje token → ridrejtim në dashboard sipas rolit
├── Forma rezervimi → POST API → shfaqje në listë
└── Error handling: API kthen 401 → ridrejtim në login
```

### 3.2.3 System Testing

| Aspekti            | Detajet                                                                                          |
|--------------------|--------------------------------------------------------------------------------------------------|
| **Objektivi**      | Verifikim i sistemit të plotë (Frontend + Backend + Database) si një njësi e vetme               |
| **Teknologjia**    | **Playwright** (E2E browser testing)                                                             |
| **Përgjegjësia**   | Testuesi / QA                                                                                    |
| **Fokusi**         | Rrjedha e plotë e biznesit: Regjistrim → Automjet → Rezervim → Caktim mekaniku → Punë → Faturë → Pagesë → Mbyllje |
| **Mjedisi**        | Backend + SQL Server + Frontend i ndërtuar, mjedis sa më afër prodhimit                          |

**Skenarë kryesorë System Testing:**

| # | Skenari                                           | Aktorët              |
|---|---------------------------------------------------|----------------------|
| 1 | Rrjedha e plotë: Regjistrim → Pagesë → Mbyllje   | Klient + Menaxher + Mekanik |
| 2 | Anulim rezervimi brenda dhe jashtë kufirit 24h    | Klient               |
| 3 | Tentativë aksesi i paautorizuar                   | Klient (tenton endpoint Manager) |
| 4 | Pagesë e pjesshme dhe pastaj pagesë e plotë       | Klient + Menaxher    |
| 5 | Krijim booking me slot të zënë                    | Klient               |

### 3.2.4 Acceptance Testing (UAT)

| Aspekti            | Detajet                                                                                          |
|--------------------|--------------------------------------------------------------------------------------------------|
| **Objektivi**      | Konfirmim që sistemi plotëson kërkesat e biznesit nga perspektiva e përdoruesit                   |
| **Teknologjia**    | Testim manual sipas skriptave UAT                                                                |
| **Përgjegjësia**   | Palë e interesit (Product Owner / përdorues përfundimtar)                                        |
| **Fokusi**         | Verifikim i 8 Use Cases (UC-01 deri UC-08) nga dokumenti i kërkesave                             |
| **Kriteret**       | Çdo Use Case duhet të kalojë me sukses sipas rrjedhës kryesore dhe rrjedhës alternative          |

**Matrica UAT ↔ Use Case:**

| Use Case | Përshkrimi                                   | Kriteri i Pranimit                                           |
|----------|----------------------------------------------|--------------------------------------------------------------|
| UC-01    | Regjistrimi i Klientit                       | Llogaria krijohet, JWT kthehet, roli = Client                |
| UC-02    | Kyçja në Sistem                              | Token i vlefshëm, ridrejtim sipas rolit                      |
| UC-03    | Regjistrimi i Automjetit                     | Automjeti ruhet, shfaqet në listë, lidhet me klientin        |
| UC-04    | Krijimi i Rezervimit                         | Rezervimi krijohet me status Pending, validim datë/slot      |
| UC-05    | Caktimi i Mekanikut                          | Status → Confirmed, WorkOrder krijohet automatikisht         |
| UC-06    | Ekzekutimi i Punës                           | Status InProgress→Completed, LaborCost llogaritet            |
| UC-07    | Gjenerimi i Faturës                          | Invoice me SubTotal, Tax, Total të sakta                     |
| UC-08    | Kryerja e Pagesës                            | Pagesë e regjistruar, mbyllje automatike nëse e plotë        |

---

## 3.3 Test Types (Tipet e Testimit)

| # | Tipi                   | Përshkrimi                                                                                   | Niveli            |
|---|------------------------|----------------------------------------------------------------------------------------------|-------------------|
| 1 | **Functional Testing** | Verifikim që funksionalitetet punojnë sipas specifikimeve (UC-01 deri UC-08)                 | Të gjitha nivelet |
| 2 | **API Testing**        | Testim i drejtpërdrejtë i REST endpoints me request/response validation                     | Integration       |
| 3 | **Authentication & Authorization Testing** | Verifikim JWT, kontroll rolesh, akses i paautorizuar                      | Unit, Integration |
| 4 | **Validation Testing** | Testim i rregullave të validimit (datë jo në kaluarën, slot i lirë, balancë pagese)          | Unit              |
| 5 | **Negative Testing**   | Testim me të dhëna të pavlefshme, request pa autentikim, vlera kufitare                      | Unit, Integration |
| 6 | **Regression Testing** | Ritestim pas çdo ndryshimi kodi për të siguruar që funksionalitete ekzistuese nuk prishen    | Të gjitha nivelet |
| 7 | **Smoke Testing**      | Testim i shpejtë pas deployment-it: login, krijim booking, shfaqje dashboard                 | System            |
| 8 | **Usability Testing**  | Vlerësim i UI/UX: navigim, forma, mesazhe gabimi, ridrejtime                                 | Acceptance        |

---

## 3.4 Risk Analysis (Analiza e Riskut)

| # | Risku                                          | Probabiliteti | Ndikimi  | Prioriteti | Masa Zbutëse                                                           |
|---|-------------------------------------------------|---------------|----------|------------|------------------------------------------------------------------------|
| 1 | **Gabim në llogaritje LaborCost/Invoice**       | Mesatar       | I lartë  | **Kritik** | Unit teste të detajuara me vlera kufitare (0 min, 1 min, 480 min)      |
| 2 | **Bypass autorizimi (akses i paautorizuar)**    | I ulët        | I lartë  | **Kritik** | Teste autorizimi për çdo endpoint × çdo rol                            |
| 3 | **WorkOrder nuk krijohet automatikisht**        | I ulët        | I lartë  | **Kritik** | Integration test: cakto mekanik → verifiko WorkOrder                   |
| 4 | **Pagesë mbi balancën / booking nuk mbyllet**   | Mesatar       | I lartë  | **Kritik** | Teste me pagesa të plota, të pjesshme, dhe mbi limit                   |
| 5 | **Tranzicion i gabuar statusesh**               | Mesatar       | Mesatar  | **I lartë**| Teste për çdo tranzicion valid dhe invalid të Booking/WorkOrder         |
| 6 | **Anulim booking brenda 24h lejohet gabimisht** | I ulët        | Mesatar  | **I lartë**| Unit teste me DateTime mock: 23h, 24h, 25h para terminit               |
| 7 | **JWT token i skaduar lejon akses**             | I ulët        | I lartë  | **I lartë**| Teste me token të skaduar, të pavlefshëm, dhe pa token                 |
| 8 | **Slot i zënë lejohet për booking**             | Mesatar       | Mesatar  | **Mesatar**| Integration teste me booking duplikatë në të njëjtin slot              |
| 9 | **Frontend nuk trajton error-et e API**         | Mesatar       | I ulët   | **Mesatar**| E2E teste me API që kthen 400/401/403/500                              |
| 10| **Cascade delete prish të dhëna**               | I ulët        | I lartë  | **Mesatar**| Integration teste: fshirje mekanik/klient me booking ekzistues         |

---

## 3.5 Entry Criteria (Kriteret e Hyrjes)

Testimi fillon vetëm kur plotësohen këto kushte:

| # | Kriteri                                                                                        |
|---|-----------------------------------------------------------------------------------------------|
| 1 | Kodi burimor i kompletuar është i commit-uar në branch-in përkatës (main ose dev)              |
| 2 | Aplikacioni backend ndërtohet pa gabime (`dotnet build` kalon me sukses)                       |
| 3 | Aplikacioni frontend ndërtohet pa gabime (`npm run build` kalon me sukses)                     |
| 4 | Databaza është e migruar dhe e seeduar me të dhëna fillestare (rolet, menaxheri default)       |
| 5 | Mjedisi i testimit është konfiguruar (SQL Server, portat, JWT secret)                          |
| 6 | Dokumenti i kërkesave (Faza 2) është i rishikuar dhe i aprovuar                                |
| 7 | Test cases janë të shkruara dhe të rishikuara para ekzekutimit                                 |
| 8 | Swagger/OpenAPI dokumentacioni është i aksessueshëm për referencë gjatë testimit                |

---

## 3.6 Exit Criteria (Kriteret e Daljes)

Testimi konsiderohet i përfunduar kur:

| # | Kriteri                                                                                        |
|---|-----------------------------------------------------------------------------------------------|
| 1 | Të gjitha test case-t e planifikuara janë ekzekutuar                                           |
| 2 | ≥ 90% e test case-ve kanë kaluar me sukses (pass rate)                                         |
| 3 | 0 defekte kritike (Critical) dhe 0 defekte të larta (High) të hapura                           |
| 4 | Defektet mesatare (Medium) kanë plan zgjidhje ose janë dokumentuar si "known issues"            |
| 5 | Të 8 Use Cases (UC-01 deri UC-08) kalojnë UAT me sukses                                       |
| 6 | Code coverage ≥ 70% për unit teste (Backend)                                                   |
| 7 | Regression test suite kalon plotësisht pas fix-it të çdo defekti                                |
| 8 | Test summary report është gjeneruar dhe i aprovuar                                              |

---

## 3.7 Test Environment (Mjedisi i Testimit)

### 3.7.1 Konfigurimi Teknik

| Komponenti        | Teknologjia / Versioni                    | Qëllimi                              |
|-------------------|-------------------------------------------|---------------------------------------|
| **Backend**       | ASP.NET Core 9.0 (net9.0)                | REST API server                       |
| **Frontend**      | React 19 + Vite 7 + TailwindCSS 3        | SPA klient                            |
| **Databazë Test** | EF Core InMemory (Unit/Integration)       | Databazë e izoluar për teste          |
| **Databazë System**| SQL Server (LocalDB ose Docker)          | Databazë reale për System/E2E teste   |
| **Autentikim**    | ASP.NET Identity + JWT Bearer             | Autentikim/autorizim                  |
| **Test Frameworks**| xUnit, Moq, WebApplicationFactory       | Backend testing                       |
| **E2E Framework** | Playwright                                | System testing me browser             |
| **API Testing**   | Swagger UI + teste automatike             | Testim i drejtpërdrejtë i endpoints   |
| **OS**            | macOS (zhvillim), Windows/Linux (CI)      | Platformë ekzekutimi                  |

### 3.7.2 Struktura e Projekteve Test

```
VehicleServiceBooking/
├── VehicleServiceBooking/                          # Projekti kryesor (Backend API)
├── VehicleServiceBooking.Tests/                    # Unit + Integration tests (xUnit)
│   ├── Unit/
│   │   ├── Controllers/
│   │   │   ├── AuthControllerTests.cs
│   │   │   ├── BookingsApiControllerTests.cs
│   │   │   ├── WorkOrdersApiControllerTests.cs
│   │   │   ├── PaymentsApiControllerTests.cs
│   │   │   └── InvoicesApiControllerTests.cs
│   │   └── Helpers/
│   │       └── JwtHelperTests.cs
│   ├── Integration/
│   │   ├── AuthIntegrationTests.cs
│   │   ├── BookingFlowIntegrationTests.cs
│   │   └── PaymentFlowIntegrationTests.cs
│   └── Fixtures/
│       └── TestWebApplicationFactory.cs
├── VehicleServiceBooking.E2E/                      # System tests (Playwright)
│   ├── tests/
│   │   ├── full-booking-flow.spec.ts
│   │   ├── auth.spec.ts
│   │   └── role-access.spec.ts
│   └── playwright.config.ts
└── VehicleServiceBooking.Client/
    └── vehicle-service-booking-client/
        └── src/
            └── __tests__/                          # Frontend unit tests (Vitest)
```

### 3.7.3 Të Dhëna Testimi (Test Data)

| Roli     | Email                    | Fjalëkalimi       | Qëllimi                          |
|----------|--------------------------|-------------------|----------------------------------|
| Manager  | manager@test.com         | Test@123456       | Menaxhim i plotë i sistemit       |
| Mechanic | mechanic@test.com        | Test@123456       | Ekzekutim pune, përditësim WO     |
| Client   | client@test.com          | Test@123456       | Rezervime, pagesa, automjete      |
| Client 2 | client2@test.com        | Test@123456       | Teste izolimi (akses i paautorizuar) |

---

## 3.8 Test Schedule (Orari i Testimit)

### 3.8.1 Fazat e Testimit

| Faza | Aktiviteti                            | Kohëzgjatja  | Fillimi      | Mbarimi      | Përgjegjësia |
|------|---------------------------------------|-------------|--------------|--------------|--------------|
| 1    | Përgatitja e mjedisit test            | 1 ditë      | Dita 1       | Dita 1       | Zhvilluesi   |
| 2    | Shkrim Unit Tests (Backend)           | 3 ditë      | Dita 2       | Dita 4       | Zhvilluesi   |
| 3    | Shkrim Integration Tests (Backend)    | 2 ditë      | Dita 5       | Dita 6       | Zhvilluesi   |
| 4    | Ekzekutim Unit + Integration Tests    | 1 ditë      | Dita 7       | Dita 7       | Zhvilluesi   |
| 5    | Shkrim System Tests (E2E)             | 2 ditë      | Dita 8       | Dita 9       | Testuesi     |
| 6    | Ekzekutim System Tests                | 1 ditë      | Dita 10      | Dita 10      | Testuesi     |
| 7    | Acceptance Testing (UAT)              | 2 ditë      | Dita 11      | Dita 12      | PO / Klient  |
| 8    | Rregullim defektesh + Regression      | 2 ditë      | Dita 13      | Dita 14      | Zhvilluesi   |
| 9    | Test Summary Report                   | 1 ditë      | Dita 15      | Dita 15      | Testuesi     |

**Kohëzgjatja totale: ~15 ditë pune**

### 3.8.2 Gantt Chart (Tekstual)

```
Dita:  1    2    3    4    5    6    7    8    9    10   11   12   13   14   15
       |    |    |    |    |    |    |    |    |    |    |    |    |    |    |
Faza 1 ████
Faza 2      ████████████
Faza 3                     ████████
Faza 4                              ████
Faza 5                                   ████████
Faza 6                                            ████
Faza 7                                                 ████████
Faza 8                                                           ████████
Faza 9                                                                    ████
```

### 3.8.3 Prioriteti i Testimit

Testimi fillon me modulet me risk më të lartë (sipas §3.4):

| Prioriteti | Modulet                                                    |
|------------|-----------------------------------------------------------|
| **P1**     | Auth (JWT), Payments (validim balance), WorkOrders (LaborCost) |
| **P2**     | Bookings (krijim, anulim, slot), Invoices (llogaritje)     |
| **P3**     | Vehicles, ServiceCenters, ServiceTypes                     |
| **P4**     | Mechanics, Schedules, Parts, Clients                       |

---

## 3.9 Action Items sipas Personave

Modulet ndahen në 3 grupe të barabarta. Secili person ekzekuton **të gjitha 4 nivelet** (Unit, Integration, System, Acceptance) për modulet e tij.

| Persona | Modulet Backend | Modulet Frontend | Use Cases UAT |
|---------|----------------|-----------------|---------------|
| **Persona 1** | Auth, Bookings, Vehicles | Login, Register, Routing (PrivateRoute) | UC-01, UC-02, UC-03, UC-04 |
| **Persona 2** | WorkOrders, Invoices, ServiceCenters, ServiceTypes | ManagerDashboard (shërbime & WorkOrders) | UC-05, UC-06, UC-07 |
| **Persona 3** | Payments, Mechanics, Schedules, Parts, Clients | ClientDashboard, MechanicDashboard | UC-08 + skenarë negativ |

---

### 👤 Persona 1 — Auth · Bookings · Vehicles

#### Unit Tests
- [ ] `AuthControllerTests.cs`
  - Regjistrim me email dublikatë → `400 Bad Request`
  - Login me fjalëkalim të gabuar → `401 Unauthorized`
  - `GET /api/auth/me` pa token → `401 Unauthorized`
- [ ] `BookingsApiControllerTests.cs`
  - Krijim booking me datë në të kaluarën → refuzohet
  - Anulim 23h para terminit → refuzohet (rregulla 24h)
  - Anulim 25h para terminit → lejohet
  - Client nuk mund të shohë bookings të klientit tjetër
- [ ] `VehiclesApiControllerTests.cs`
  - Client nuk mund të editojë automjetin e klientit tjetër → `403`
  - Fshirje automjeti me booking aktiv → sillet sipas rregullave
- [ ] `JwtHelperTests.cs`
  - Token gjenerohet me claim-et e sakta (role, userId, email)
  - Token skadon sipas konfigurimit (60 min)

#### Integration Tests
- [ ] `AuthIntegrationTests.cs`
  - `POST /api/auth/register-client` → krijon ApplicationUser + rol "Client" në DB
  - `POST /api/auth/login` → kthen JWT të vlefshëm me claims të sakta
  - Token i skaduar → `POST` me të → merr `401`
- [ ] `BookingIntegrationTests.cs`
  - Client krijon booking → shfaqet në `GET /api/bookings` të tij
  - Client tenton `GET /api/bookings` të klientit tjetër → merr vetëm të vetët
  - Booking duplikat në të njëjtin slot/mekanik → `400 Bad Request`

#### System Tests (E2E — Playwright)
- [ ] `auth.spec.ts`
  - Regjistrim klient i ri nga UI → ridrejtim në `/login`
  - Login si Client → ridrejtim në `/client`
  - Login si Manager → ridrejtim në `/manager`
  - Login si Mechanic → ridrejtim në `/mechanic`
  - Token i skaduar → ridrejtim automatik në `/login`
- [ ] `booking-ui.spec.ts`
  - Client krijon booking nga UI → shfaqet në listë me status "Pending"
  - Client tenton të anulojë booking < 24h → shfaqet mesazh gabimi

#### Acceptance Tests (UAT)
| Use Case | Hapat | Kriteri Pranim |
|----------|-------|----------------|
| **UC-01** Regjistrim Klienti | Hap `/register`, plotëso të dhënat, kliko "Regjistrohu" | Llogaria krijohet, ridrejtohet në `/login` |
| **UC-02** Kyçja në Sistem | Login me Client / Manager / Mechanic | Secili ridrejtohet në dashboard-in e vet |
| **UC-03** Regjistrim Automjeti | Si Client: shko te "Automjetet" → "Shto" → plotëso → ruaj | Automjeti shfaqet në listë të klientit |
| **UC-04** Krijim Rezervimi | Si Client: kliko "Rezervo" → zgjidh datë/qendër/shërbim → dërgo | Booking shfaqet me status "Pending" |

---

### 👤 Persona 2 — WorkOrders · Invoices · ServiceCenters · ServiceTypes

#### Unit Tests
- [ ] `WorkOrdersApiControllerTests.cs`
  - `LaborCost = (actualMinutes / 60) × hourlyRate` — vlera: 0 min, 60 min, 90 min, 480 min
  - Mekanik nuk mund të ndryshojë `PartsCost` (vetëm Manager)
  - Tranzicion i pavlefshëm statusi (p.sh. Completed → InProgress) → refuzohet
  - `StartedAt` vendoset automatikisht kur statusi → InProgress
  - `CompletedAt` vendoset automatikisht kur statusi → Completed
- [ ] `InvoicesApiControllerTests.cs`
  - `TaxAmount = SubTotal × 0.18` me vlera të ndryshme
  - `TotalAmount = SubTotal + TaxAmount`
  - `InvoiceNumber` gjenerohet si `INV-{yyyyMMdd}-{WorkOrderId:D4}`
  - Dy fatura për të njëjtin WorkOrder → `400 Bad Request`
- [ ] `ServiceCentersApiControllerTests.cs`
  - `GET /api/servicecenters` aksessohet pa token (public endpoint)
  - `POST` pa rol Manager → `403 Forbidden`
- [ ] `ServiceTypesApiControllerTests.cs`
  - `GET /api/servicetypes` aksessohet pa token (public endpoint)
  - `POST` pa rol Manager → `403 Forbidden`

#### Integration Tests
- [ ] `WorkOrderIntegrationTests.cs`
  - Cakto Mechanic në Booking → verifiko WorkOrder u krijua automatikisht në DB
  - Mechanic përditëson WorkOrder → `LaborCost` llogaritet dhe ruhet
  - Manager krijon WorkOrder manualisht → shfaqet në `GET /api/workorders`
- [ ] `InvoiceIntegrationTests.cs`
  - `POST /api/invoices` me WorkOrder të vlefshëm → Invoice ruhet me numër unik
  - `GET /api/invoices/workorder/{workOrderId}` → kthen Invoice-in e lidhur
  - Manager i vetëm mund të aksesojë `GET /api/invoices` (listim i plotë)

#### System Tests (E2E — Playwright)
- [ ] `workorder-flow.spec.ts`
  - Manager cakton mekanik në booking → WorkOrder shfaqet automatikisht
  - Mechanic hap WorkOrder → kliko "Fillo Punën" → status bëhet InProgress
  - Mechanic kliko "Completo" → LaborCost shfaqet i llogarituar
- [ ] `invoice-ui.spec.ts`
  - Manager hap WorkOrder të kompletuar → kliko "Gjenero Faturë"
  - Invoice shfaqet me SubTotal, Tax 18%, Total të sakta
  - Tentativë e dytë "Gjenero Faturë" → mesazh gabimi "Invoice ekziston"

#### Acceptance Tests (UAT)
| Use Case | Hapat | Kriteri Pranim |
|----------|-------|----------------|
| **UC-05** Caktimi i Mekanikut | Si Manager: hap Booking Pending → cakto Mechanic → ruaj | Status → "Confirmed", WorkOrder ekziston në sistem |
| **UC-06** Ekzekutimi i Punës | Si Mechanic: hap WorkOrder → "Fillo" → "Completo" | LaborCost llogaritet, status "Completed" |
| **UC-07** Gjenerimi i Faturës | Si Manager: hap WorkOrder → "Gjenero Faturë" | Invoice me SubTotal, Tax 18%, Total korrekte |

---

### 👤 Persona 3 — Payments · Mechanics · Schedules · Parts · Clients

#### Unit Tests
- [ ] `PaymentsApiControllerTests.cs`
  - Pagesë me shumë > balancës së mbetur → `400 Bad Request`
  - Pagesë e plotë → WorkOrder.Status = Closed + Booking.Status = Completed
  - Pagesë pa Invoice të krijuar → `400 Bad Request`
  - Pagesë e pjesshme → statuset nuk ndryshojnë
- [ ] `MechanicsApiControllerTests.cs`
  - `POST /api/mechanics` krijon edhe ApplicationUser + rol "Mechanic"
  - `DELETE /api/mechanics/{id}` fshi mekanikun dhe user-in e lidhur
  - Vetëm Manager mund të krijojë/fshijë mekanikë → `403` për të tjerët
- [ ] `PartsApiControllerTests.cs`
  - `GET /api/parts` aksessohet pa token (public)
  - `POST` pa rol Manager → `403 Forbidden`
  - Part me `StockQuantity < MinStockLevel` sillet saktë
- [ ] `ClientsApiControllerTests.cs`
  - Client mund të shohë vetëm profilin e vet → `403` për ID tjetër
  - Manager mund të shohë çdo klient

#### Integration Tests
- [ ] `PaymentFlowIntegrationTests.cs`
  - Krijo WorkOrder → Krijo Invoice → `POST /api/payments` (e plotë) → verifiko WorkOrder.Status = Closed dhe Booking.Status = Completed
  - `POST /api/payments` (e pjesshme) → statuset nuk ndryshojnë, balance zvogëlohet
  - Pagesë e dytë plotëson balancën → mbyllje automatike
- [ ] `MechanicScheduleIntegrationTests.cs`
  - Manager krijon Mechanic → `POST /api/schedules` me emrin e mekanikut → Schedule ruhet
  - `GET /api/schedules` si Mechanic → sheh vetëm oraret e veta
  - `DELETE /api/schedules/{id}` si Manager → fshihet me sukses
- [ ] `ClientsIntegrationTests.cs`
  - `GET /api/clients` si Manager → kthen listën e plotë
  - `GET /api/clients` si Client → `403 Forbidden`
  - `DELETE /api/clients/{id}` si Manager → klienti fshihet

#### System Tests (E2E — Playwright)
- [ ] `payment-ui.spec.ts`
  - Client hap Faturën → kliko "Paguaj" → konfirmo → Pagesë e regjistruar
  - Pagesë e plotë → Booking shfaqet si "Completed" në dashboard
  - Pagesë e pjesshme → Booking mbetet aktiv, balance e re shfaqet
- [ ] `negative-scenarios.spec.ts`
  - Client tenton të hapë `/manager` → ridrejtohet (akses i mohuar)
  - Client tenton të anulojë booking të klientit tjetër → `403`
  - Tentativë pagese mbi balancën nga UI → mesazh gabimi specifik

#### Acceptance Tests (UAT)
| Use Case | Hapat | Kriteri Pranim |
|----------|-------|----------------|
| **UC-08** Kryerja e Pagesës | Si Client: hap Faturën → "Paguaj" → konfirmo shumën → dërgo | Pagesë e regjistruar, Booking = Completed nëse e plotë |
| **Skenar negativ 1** | Client tenton akses në `/manager` route | Ridrejtohet, mesazh "Akses i mohuar" |
| **Skenar negativ 2** | Pagesë mbi balancën e faturës | Mesazh gabimi specifik, transaksioni nuk kryhet |
| **Skenar negativ 3** | Booking pa Invoice → tentativë pagese | Mesazh gabimi "Fatura nuk ekziston" |
