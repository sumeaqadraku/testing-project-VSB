# 2. Analiza e Kërkesave dhe Modelimi

## 2.1 Përshkrimi i Sistemit

**VehicleServiceBooking** është një sistem web për menaxhimin e rezervimeve të servisit të automjeteve. Sistemi mundëson klientët të regjistrojnë automjetet e tyre, të rezervojnë termine për servisim në qendra të ndryshme servisi, dhe të ndjekin procesin e plotë nga rezervimi deri te pagesa.

Sistemi përbëhet nga dy pjesë kryesore:
- **Backend (ASP.NET Core Web API)** — ofron REST API me autentikim JWT, menaxhim rolesh me ASP.NET Identity, dhe bazë të dhënash SQL Server.
- **Frontend (React + Vite)** — aplikacion SPA me dashboard-e të ndara sipas rolit (Manager, Mekanik, Klient), i stilizuar me TailwindCSS.

**Rrjedha kryesore e procesit:**
1. Klienti regjistrohet dhe shton automjetin e tij.
2. Klienti krijon një rezervim duke zgjedhur qendrën e servisit, tipin e shërbimit, datën dhe orën.
3. Menaxheri shqyrton rezervimin, cakton mekanikun, dhe konfirmon rezervimin (krijohet automatikisht një WorkOrder).
4. Mekaniku fillon punën, shënon kohën, shton pjesë të përdorura, dhe përfundon WorkOrder-in.
5. Menaxheri gjeneron faturën (Invoice) bazuar në kostot e punës dhe pjesëve.
6. Klienti ose Menaxheri kryen pagesën; pas pagesës së plotë, booking-u mbyllet.

**Teknologjitë e përdorura:**
- Backend: ASP.NET Core 8, Entity Framework Core, SQL Server, ASP.NET Identity, JWT
- Frontend: React 18, Vite, TailwindCSS, React Router
- API: RESTful, Swagger/OpenAPI

---

## 2.2 Aktorët e Sistemit

| #  | Aktori        | Përshkrimi                                                                                          |
|----|---------------|-----------------------------------------------------------------------------------------------------|
| 1  | **Klient**    | Përdoruesi i regjistruar që regjistron automjete, krijon rezervime, shikon statusin, dhe kryen pagesa. |
| 2  | **Mekanik**   | Punonjës i servisit i caktuar nga menaxheri, kryen punën teknike, përditëson WorkOrder-in me shënime dhe kohë. |
| 3  | **Menaxher**  | Administratori kryesor i sistemit që menaxhon qendrat e servisit, tipet e shërbimeve, mekanikët, rezervimet, faturat, dhe pagesat. |
| 4  | **Sistem**    | Aktori jo-njerëzor që kryen veprime automatike si: gjenerim JWT token, krijim automatik WorkOrder, llogaritje kostosh, dhe validime biznesi. |

---

## 2.3 Use Cases (Rastet e Përdorimit)

### UC-01: Regjistrimi i Klientit
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Klient                                                        |
| **Parakusht**  | Përdoruesi nuk ka llogari ekzistuese                          |
| **Përshkrimi** | Klienti plotëson formularin e regjistrimit me emër, mbiemër, email, dhe fjalëkalim. Sistemi krijon llogarinë, i cakton rolin "Client", dhe kthen JWT token. |
| **Paskusht**   | Klienti ka llogari aktive dhe mund të kyçet                   |
| **Rrjedha alternative** | Nëse email-i ekziston, sistemi kthen gabim validimi. |

### UC-02: Kyçja në Sistem (Login)
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Klient, Mekanik, Menaxher                                     |
| **Parakusht**  | Përdoruesi ka llogari aktive                                  |
| **Përshkrimi** | Përdoruesi fut email-in dhe fjalëkalimin. Sistemi verifikon kredencialet, gjeneron JWT token, dhe ridrejton përdoruesin në dashboard-in përkatës sipas rolit. |
| **Paskusht**   | Përdoruesi është i autentikuar dhe ka akses në funksionet e rolit të tij |
| **Rrjedha alternative** | Kredenciale të gabuara → mesazh gabimi "Invalid email or password." |

### UC-03: Regjistrimi i Automjetit
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Klient                                                        |
| **Parakusht**  | Klienti është i kyçur në sistem                               |
| **Përshkrimi** | Klienti shton një automjet të ri duke specifikuar markën, modelin, targën, vitin, VIN, dhe ngjyrën. Automjeti lidhet me llogarinë e klientit. |
| **Paskusht**   | Automjeti është i regjistruar dhe i disponueshëm për rezervime |
| **Rrjedha alternative** | Validimi dështon (p.sh. targë boshe) → gabim validimi. |

### UC-04: Krijimi i Rezervimit (Booking)
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Klient, Menaxher                                              |
| **Parakusht**  | Klienti ka të paktën një automjet të regjistruar               |
| **Përshkrimi** | Klienti zgjedh automjetin, qendrën e servisit, tipin e shërbimit, datën dhe orën. Sistemi verifikon disponueshmërinë e slotit kohor, krijon rezervimin me statusin "Pending". |
| **Paskusht**   | Rezervimi ekziston me status Pending                          |
| **Rrjedha alternative** | Data në të kaluarën → gabim. Slot i zënë → gabim disponueshmërie. |

### UC-05: Caktimi i Mekanikut dhe Konfirmimi i Rezervimit
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Menaxher                                                      |
| **Parakusht**  | Ekziston një rezervim me status Pending                       |
| **Përshkrimi** | Menaxheri zgjedh një mekanik të disponueshëm dhe ia cakton rezervimit. Sistemi ndryshon statusin nga Pending → Confirmed dhe krijon automatikisht një WorkOrder me statusin "Scheduled". |
| **Paskusht**   | Rezervimi është Confirmed, WorkOrder është krijuar            |
| **Rrjedha alternative** | Mekaniku nuk është i disponueshëm → mesazh gabimi.   |

### UC-06: Ekzekutimi i Punës (WorkOrder)
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Mekanik                                                       |
| **Parakusht**  | WorkOrder ekziston me status Scheduled                        |
| **Përshkrimi** | Mekaniku fillon punën (statusi → InProgress, regjistrohet StartedAt), shton shënime teknike, regjistron kohën aktuale, dhe përfundon punën (statusi → Completed, regjistrohet CompletedAt). Sistemi llogarit automatikisht koston e punës bazuar në orët × tarifën e mekanikut. |
| **Paskusht**   | WorkOrder është Completed me kosto të llogaritura             |
| **Rrjedha alternative** | Mekaniku nuk ka HourlyRate → kosto pune nuk llogaritet automatikisht. |

### UC-07: Gjenerimi i Faturës (Invoice)
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Menaxher                                                      |
| **Parakusht**  | WorkOrder është Completed ose ReadyForPayment                 |
| **Përshkrimi** | Menaxheri gjeneron faturën duke specifikuar numrin e faturës. Sistemi llogarit SubTotal (kosto pune + kosto pjesësh), TaxAmount, dhe TotalAmount. Fatura lidhet me WorkOrder-in. |
| **Paskusht**   | Fatura ekziston me shumën totale të llogaritur                |
| **Rrjedha alternative** | WorkOrder pa kosto → fatura me shumë zero.            |

### UC-08: Kryerja e Pagesës
| Fushat         | Detajet                                                      |
|----------------|--------------------------------------------------------------|
| **Aktori**     | Klient, Menaxher                                              |
| **Parakusht**  | Fatura ekziston për WorkOrder-in; WorkOrder është ReadyForPayment ose Completed |
| **Përshkrimi** | Përdoruesi zgjedh metodën e pagesës (Cash, CreditCard, DebitCard, BankTransfer, Online), fut shumën, dhe kryen pagesën. Sistemi verifikon që shuma nuk tejkalon balancën e mbetur. Nëse pagesa e plotë arrihet, WorkOrder kalon në Closed dhe Booking në Completed. |
| **Paskusht**   | Pagesa është regjistruar; nëse e plotë, booking-u është mbyllur |
| **Rrjedha alternative** | Shuma tejkalon balancën → gabim. Invoice nuk ekziston → gabim. |

---

## 2.4 Use Case Diagram

```
@startuml use-case-diagram

skinparam actorStyle awesome
skinparam packageStyle rectangle
left to right direction

actor "Klient" as Client
actor "Mekanik" as Mechanic
actor "Menaxher" as Manager
actor "Sistem" as System

rectangle "VehicleServiceBooking" {

  usecase "UC-01: Regjistrimi\ni Klientit" as UC01
  usecase "UC-02: Kyçja\nnë Sistem" as UC02
  usecase "UC-03: Regjistrimi\ni Automjetit" as UC03
  usecase "UC-04: Krijimi i\nRezervimit" as UC04
  usecase "UC-05: Caktimi i Mekanikut\ndhe Konfirmimi" as UC05
  usecase "UC-06: Ekzekutimi\ni Punës" as UC06
  usecase "UC-07: Gjenerimi\ni Faturës" as UC07
  usecase "UC-08: Kryerja\ne Pagesës" as UC08

  usecase "Gjenerim JWT Token" as SYS01
  usecase "Krijim automatik\nWorkOrder" as SYS02
  usecase "Llogaritje\nautomatike kostosh" as SYS03
}

Client --> UC01
Client --> UC02
Client --> UC03
Client --> UC04
Client --> UC08

Mechanic --> UC02
Mechanic --> UC06

Manager --> UC02
Manager --> UC04
Manager --> UC05
Manager --> UC07
Manager --> UC08

UC01 ..> SYS01 : <<include>>
UC02 ..> SYS01 : <<include>>
UC05 ..> SYS02 : <<include>>
UC06 ..> SYS03 : <<include>>

System --> SYS01
System --> SYS02
System --> SYS03

@enduml
```

**Si ta vizatoni:** Kopjoni kodin PlantUML më sipër në [plantuml.com/plantuml](https://www.plantuml.com/plantuml/uml/) ose në një plugin PlantUML në IDE për të gjeneruar diagramin vizual.

---

## 2.5 Sequence Diagram — Procesi i Plotë i Rezervimit deri te Pagesa

```
@startuml sequence-booking-to-payment

skinparam sequenceMessageAlign center
skinparam maxMessageSize 200

actor "Klient" as C
actor "Menaxher" as M
actor "Mekanik" as Mech
participant "Frontend\n(React)" as FE
participant "API\n(ASP.NET Core)" as API
database "Database\n(SQL Server)" as DB

== 1. Regjistrimi dhe Kyçja ==
C -> FE : Plotëson formularin e regjistrimit
FE -> API : POST /api/Auth/register-client
API -> DB : Krijo ApplicationUser + roli "Client"
API --> FE : JWT Token + user info
FE --> C : Ridrejton në ClientDashboard

== 2. Regjistrimi i Automjetit ==
C -> FE : Shton automjet të ri
FE -> API : POST /api/VehiclesApi
API -> DB : Ruaj Vehicle (clientId)
API --> FE : Vehicle i krijuar
FE --> C : Automjeti shfaqet në listë

== 3. Krijimi i Rezervimit ==
C -> FE : Zgjedh automjet, qendër, shërbim, datë/orë
FE -> API : POST /api/BookingsApi
API -> API : Validon datën (jo në të kaluarën)\nValidon slot-in (jo i zënë)
API -> DB : Krijo Booking (status=Pending)
API --> FE : Booking i krijuar
FE --> C : Konfirmim rezervimi

== 4. Caktimi i Mekanikut ==
M -> FE : Shqyrton rezervimet Pending
FE -> API : GET /api/BookingsApi
API -> DB : Merr rezervimet
API --> FE : Lista e rezervimeve
M -> FE : Cakton mekanikun
FE -> API : PUT /api/BookingsApi/{id}
API -> DB : Përditëso Booking (status=Confirmed)
API -> DB : Krijo WorkOrder (status=Scheduled)
API --> FE : 204 No Content
FE --> M : Rezervimi konfirmuar

== 5. Ekzekutimi i Punës ==
Mech -> FE : Shikon WorkOrder-at e caktuara
FE -> API : GET /api/WorkOrdersApi
API --> FE : Lista e WorkOrder-ave
Mech -> FE : Fillon punën
FE -> API : PUT /api/WorkOrdersApi/{id}\n(status=InProgress)
API -> DB : Përditëso WorkOrder (StartedAt=now)
API --> FE : 204 No Content

Mech -> FE : Përfundon punën (shton kohën, shënime)
FE -> API : PUT /api/WorkOrdersApi/{id}\n(status=Completed, actualDuration)
API -> API : LaborCost = (actualMinutes/60) × hourlyRate
API -> DB : Përditëso WorkOrder (CompletedAt=now, LaborCost)
API --> FE : 204 No Content

== 6. Gjenerimi i Faturës ==
M -> FE : Gjeneron faturë për WorkOrder
FE -> API : POST /api/InvoicesApi
API -> DB : Krijo Invoice (SubTotal, Tax, Total)
API --> FE : Invoice e krijuar

== 7. Pagesa ==
C -> FE : Zgjedh metodën e pagesës, fut shumën
FE -> API : POST /api/PaymentsApi
API -> API : Verifikon: Invoice ekziston?\nShuma ≤ balanca e mbetur?
API -> DB : Krijo Payment (status=Pending)
API -> API : totalPaid ≥ invoiceTotal?
API -> DB : WorkOrder.Status = Closed\nBooking.Status = Completed\nPayment.Status = Completed
API --> FE : Payment i krijuar
FE --> C : Pagesa e suksesshme, rezervimi i mbyllur

@enduml
```

**Si ta vizatoni:** Kopjoni kodin PlantUML më sipër në [plantuml.com/plantuml](https://www.plantuml.com/plantuml/uml/) ose në një plugin PlantUML në IDE për të gjeneruar diagramin vizual.

---

## 2.6 Funksionalitetet Kritike

Funksionalitetet kritike janë ato që ndikojnë drejtpërdrejt në korrektësinë, sigurinë, dhe integritetin e sistemit. Këto duhet testuar me përparësi të lartë:

| #  | Funksionaliteti                          | Arsyeja e Kriticitetit                                                                                   |
|----|------------------------------------------|----------------------------------------------------------------------------------------------------------|
| 1  | **Autentikimi dhe Autorizimi (JWT)**     | Çdo endpoint i mbrojtur varet nga JWT. Gabime këtu lejojnë akses të paautorizuar. Rolet (Manager, Mechanic, Client) kontrollojnë çfarë sheh dhe bën çdo përdorues. |
| 2  | **Krijimi i Rezervimit + Validimi**      | Validimi i datës (jo në të kaluarën) dhe slot-it kohor (jo i zënë) parandalon konflikte në orar. Gabime këtu çojnë në mbingarkesë mekanikësh. |
| 3  | **Krijimi automatik i WorkOrder**        | Kur menaxheri cakton mekanikun, WorkOrder krijohet automatikisht. Nëse kjo dështon, procesi i servisimit ngec. |
| 4  | **Llogaritja e Kostove (LaborCost)**     | Kosto e punës llogaritet si `(actualMinutes / 60) × hourlyRate`. Gabime matematikore ndikojnë drejtpërdrejt në faturim. |
| 5  | **Tranzicioni i Statuseve**              | Booking: Pending→Confirmed→InProgress→Completed→Closed. WorkOrder: Scheduled→InProgress→Completed→ReadyForPayment→Closed. Tranzicione të gabuara prishin rrjedhën e procesit. |
| 6  | **Validimi i Pagesës**                   | Pagesa duhet: (a) të ketë Invoice, (b) shuma të mos tejkalojë balancën, (c) pas pagesës së plotë të mbyllë WorkOrder+Booking automatikisht. Gabime këtu çojnë në mbipagesë ose booking-e të pambylla. |
| 7  | **Kontrolli i Aksesit sipas Rolit**      | Klienti sheh vetëm të dhënat e veta; mekaniku sheh vetëm punët e tij; menaxheri sheh gjithçka. Shkelja e kësaj rregullen është cenueshmëri sigurie. |
| 8  | **Anulimi i Rezervimit (24h rregull)**   | Klienti mund të anulojë vetëm nëse ka ≥24 orë deri në termin. Menaxheri mund të anulojë kurdoherë. Gabime këtu çojnë në anulime të padrejta. |
