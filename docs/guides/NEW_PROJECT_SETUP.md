# Nytt projekt – steg för steg (generell mall)

> Målet är ett repo som både människor och AI trivs i: strikt struktur, .NET 9 Aspire som standard, och tydliga regler för körning, loggar, tester och dokument.

## 0) Förutsättningar
- .NET SDK 9 installerad
- (Valfritt) GitHub Actions kopplat till repo
- (Valfritt) Azure CLI om du ska deploya senare

## 1) Skapa grundstruktur

```bash
mkdir -p .augment docs/guides src tests scripts config infra

Lägg i repo-roten:

README.md (översikt + länkar)
NuGet.config (paketkällor; gäller hela repot)
Directory.Build.props (warnings as errors, nullable, etc.)
Directory.Packages.props (central versionsstyrning)
.gitignore, .editorconfig (valfritt men rekommenderas)

2) AI-rutan (.augment)
Filer:
.augment/system-prompt.md
Innehåller: “Minimal och skarp”, STATUS-protokoll, ASPIRE DEFAULTS (kör alltid AppHost), uppdatera state.json.
.augment/rules.md
Innehåller: icke-förhandlingsbara regler (warnings as errors, cache-busting, FakeItEasy, strukturkrav, inga nya .md i roten).
.augment/GPT.md, .augment/Claude.md
Stil/regler per agent.
.augment/state.json
Hålls uppdaterad av AI (mode, phase, result, lastOutput, next, updatedUtc).

3) Dokument (docs)
docs/INSTRUCTIONS.md – SSOT för arbetssätt
Feature-baserad struktur, testregler, DoD, “Run & Logs”-konvention (Aspire).
docs/INTEGRATION.md – SSOT för integration/konfig
Endpoints, externa tjänster, env vars, telemetri.
docs/adr-0001.md – första beslutet (struktur + SWA/statisk webb m.m. vid behov)
docs/prd.md – produktkrav (om aktuellt)
docs/guides/ – guider (som den här)

4) Sätt upp .NET 9 Aspire-lösningen
Målet: projektnamn exakt AppHost och ServiceDefaults.
Variant A – via mall (rekommenderad om tillgänglig i din SDK/IDE):
Skapa en Aspire-lösning (CLI/VS-mall). Säkerställ att projekten heter:
src/AppHost/AppHost.csproj
src/ServiceDefaults/ServiceDefaults.csproj
Variant B – manuellt (om mall saknas):
Skapa solution:
dotnet new sln -n Repo
Skapa AppHost-projekt (web/console beroende på mall du utgår från) och lägg till Aspire-hostning (paket/Program.cs bootstrap).
Skapa ServiceDefaults som klassbibliotek med delade policies (HTTP/timeouts/serilog/etc).
Lägg till dina tjänster under src/<TjänstNamn>/... allteftersom.
Run & Logs (standard):
Kör alltid:
dotnet run --project src/AppHost/AppHost.csproj
(eller dotnet watch --project src/AppHost/AppHost.csproj)
Loggar: 1) Aspire Dashboard (om aktiv), 2) fil-loggar: ./logs/<ProjectName>/yyyyMMdd.log
Sätt gärna env: LOG_ROOT=./logs

5) Strikt struktur för kod & tester
Produktionskod: src/<Name>/...
Tester: tests/<Name>.Tests/... (matcha mappstruktur)
Förbjudet: tester i roten eller under /src, nya .md i roten (utom whitelist)
Lägg till CI-spärrar (se punkt 7)

6) Feature-baserad mappning (i varje projekt)
I t.ex. src/MyApi lägger du feature-mappar direkt i projektroten (inte en global Features/):
/<FeatureName>
├── Dtos/
├── Entities/
├── Services/
└── Endpoints/
Ex:
/Projects
├── Dtos/ProjectCreateDto.cs
├── Entities/Project.cs
├── Services/ProjectService.cs
└── Endpoints/CreateProjectEndpoint.cs

7) Scripts & CI (blockera slarv)
Skapa:
scripts/validate-structure.sh – validera att:
tester endast ligger i /tests/*
varje src/<Name> har tests/<Name>.Tests
inga nya .md i roten (utom whitelist)
GitHub Action: .github/workflows/validate-structure.yml som kör scriptet på push/PR.
CI för build+test (se docs/guides/CI-CD_AUGMENT.md)

8) NuGet/Build-props
NuGet.config i repo-roten (feeds & credentials via CI-secrets)
Directory.Build.props (warnings as errors m.m.)
Directory.Packages.props (centrala versioner, t.ex. FakeItEasy, FluentAssertions)

9) Första commit & testkörning
git add .
git commit -m "chore: init repo structure (+ Aspire defaults)"
dotnet run --project src/AppHost/AppHost.csproj

10) STATUS-kommandot (alltid)
“status” i chatten → AI svarar enligt protokollet (körfas eller utvecklingsfas) och uppdaterar .augment/state.json.
Definition of Done – init
dotnet build + dotnet test grönt och utan varningar
Strukturvalidering OK i CI
AppHost startar lokalt, loggar hittas enligt konvention

---

# `docs/guides/FEATURES.md`

```md
# Nya features – var och hur du skriver

> En feature är en slutet område av domänlogik (t.ex. Projects, Users, Invoices). All kod för en feature ligger **inom projektet** som äger den – inte i globala mappar.

## 1) Placering i källkod
- I respektive projekt (t.ex. `src/MyApi`) skapar du en mapp **per feature**:
/<FeatureName>
├── Dtos/
├── Entities/
├── Services/
└── Endpoints/
Exempel:
src/MyApi/
└── Projects/
├── Dtos/ProjectCreateDto.cs
├── Entities/Project.cs
├── Services/ProjectService.cs
└── Endpoints/CreateProjectEndpoint.cs

**Undvik** globala mappar som `Models`, `Services`, `Controllers`, `Endpoints`.  
Allt som rör featuren hör hemma **i dess mapp**.

## 2) Tester (måste)
- Spegla strukturen i `tests/<ProjectName>.Tests/...`  
  Ex:
tests/MyApi.Tests/Projects/ProjectServiceTests.cs
tests/MyApi.Tests/Projects/CreateProjectEndpointTests.cs
- Minst en testklass per produktionsklass.  
- Använd **FakeItEasy** (inte Moq).

## 3) När du lägger till en ny feature – checklista
1. **Behövs ADR?**  
 - Om featuren introducerar ett arkitekturbeslut (ny datakälla, caching, auth, större mönster) → skapa/uppdatera `docs/adr-xxxx.md`.
2. **Uppdatera docs**  
 - Nya/ändrade endpoints, env vars, externa beroenden → `docs/INTEGRATION.md`.
 - Produktkrav/acceptanskriterier (om ni kör det) → `docs/prd.md`.
3. **Skapa struktur i projektet**  
 - Lägg mapp `<FeatureName>` och underkataloger `Dtos/ Entities/ Services/ Endpoints/`.
4. **Skriv testerna parallellt**  
 - Skapa speglande filer i `tests/<ProjectName>.Tests/<FeatureName>/...`
5. **Cache-busting** (om statiska tillgångar påverkas)  
 - Byt filnamn eller lägg versionsquery (ex: `app.v2.css` eller `?v=20250909`).
6. **STATUS**  
 - Skriv “status” i chatten när du gjort delsteg; AI uppdaterar `.augment/state.json`.

## 4) Namngivning och mönster (exempel)
- Dtos: `XyzCreateDto`, `XyzUpdateDto`, `XyzResponseDto`
- Entities: domänmodeller (utan UI/transport-bagage)
- Services: applikationslogik (tunn controller/endpoint)
- Endpoints: en liten klass per operation, t.ex. `CreateProjectEndpoint`

## 5) Pull request – krav
- Kod + tester i rätt mappar
- Dokument uppdaterade (`INTEGRATION.md`, ADR, PRD vid behov)
- `scripts/validate-structure.sh` passerar lokalt
- CI grönt (build+test utan varningar)

## 6) Definition of Done – feature
- Alla tester gröna och relevanta
- Endpoints dokumenterade i `docs/INTEGRATION.md`
- ADR uppdaterad om arkitektur berörts
- AppHost kör och loggar visar featurens flöden/ev. fel

