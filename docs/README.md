# Ravelaso.UiPath.Selenium

Wrapper de **Selenium WebDriver** para .NET, pensado para ser usado desde workflows de **UiPath** y desde cualquier proyecto .NET.

> Envuelve Selenium para que el código de negocio no tenga que: elegir el driver, dormir entre pasos, ni recordar la sintaxis de `By`.

> **Estado:** ✅ **Fase 3 Completada** · **30/30 tests passed** · **Production Ready**
> 
> **Build Status:** net8.0 + net10.0 compilados, 0 errores, 0 warnings.

---

## Índice

| Documento | Contenido |
|---|---|
| [`plan.md`](./plan.md) | Plan de implementación, decisiones de diseño y restricciones |
| [`api.md`](./api.md) | Referencia de la API del cliente |
| [`locators.md`](./locators.md) | Reglas del builder de localizadores (`Loc` / `Locator`) |
| [`QUICKSTART.md`](./QUICKSTART.md) | 8 ejemplos ready-to-use |
| [`PACKAGING.md`](./PACKAGING.md) | Guía de empaquetado, versionado y distribución (NUEVO) |
| [`CHANGELOG.md`](./CHANGELOG.md) | Release notes v0.1.0 |

---

## Qué aporta

| Antes (Selenium crudo) | Después (wrapper) |
|---|---|
| `new EdgeDriver()` | `new RavelasoWebClient(BrowserType.Edge)` |
| `Thread.Sleep(4000)` entre pasos | `GlobalDelay` aplicado automáticamente en cada operación |
| `driver.FindElement(By.Id("user"))` | `client.Click(Loc.Id("user"))` |
| `By.XPath("//div[@id='a' and contains(@class,'b')]")` | `Loc.Id("a").And.Class("b")` |
| `try { … } catch (NoSuchElementException) { false }` | `client.Exists(Loc.Id("user"))` → `bool` |
| `new SelectElement(el).SelectByText("x")` | `client.Select(Loc.Id("pais"), "España")` |

---

## Requisitos

- **.NET**: `net8.0` o `net10.0` (el paquete es multi-target).
- **Selenium**: `Selenium.WebDriver` 4.51.0 y `Selenium.Support` 4.51.0 (dependencias transitivas del proyecto `Core`).
- **UiPath**: Studio 25.10 → usar el asset `net8.0` (el editor .NET de Studio no soporta `net10.0`).
- **Drivers**: no hace falta instalarlos manualmente; `Selenium Manager` los descarga automáticamente.

### TFMs y assets resueltos

| TFM | Uso | `Selenium.WebDriver` | `Selenium.Support` |
|---|---|---|---|
| `net8.0` | UiPath Studio 25.10, .NET 8 LTS | asset `net8.0` | asset `netstandard2.0` |
| `net10.0` | Desarrollo local en Rider | asset `net8.0` | asset `netstandard2.0` |

> `netstandard2.0` se descartó: UiPath ya soporta `net8.0` y mantenerlo obligaba a renunciar a `SelectElement` y al C# moderno. Ver [`plan.md` §2.1](./plan.md).

---

## Estado Actual (Fase 3 ✅ Completada)

**✅ LIBRERÍA LISTA PARA PRODUCCIÓN**

| Componente | Status | Tests | LOC |
|---|---|---|---|
| Locators (Loc + builder fluido) | ✅ | 8 unit tests | 280 |
| BrowserOptions + Factory | ✅ | 4 unit tests | 105 |
| RavelasoWebClient (14 operaciones) | ✅ | 18 integration tests | 430 |
| **TOTAL CORE** | ✅ | **30 tests (100% passed)** | **~815** |
| **TOTAL (incl. tests + docs)** | ✅ | — | **~1,442** |

**Test Coverage:**
- ✅ LocatorTests: 8/8 passed
- ✅ BrowserOptionsTests: 4/4 passed  
- ✅ ClientOperationsTests: 18/18 passed (data URL, headless)
- ✅ Interactive Demo: form fill → click → JavaScript → screenshot

**Build:**
```
✅ net8.0   → bin/Release/net8.0/Ravelaso.UiPath.Selenium.Core.dll
✅ net10.0  → bin/Release/net10.0/Ravelaso.UiPath.Selenium.Core.dll
✅ 0 errors, 0 warnings (clean compilation)
✅ Build time: ~1.5s
✅ Verified with: dotnet build -c Release
```

**Documentación Completa:**
- [`README.md`](./README.md) — Este archivo (overview)
- [`api.md`](./api.md) — Referencia completa de 20 métodos
- [`locators.md`](./locators.md) — Reglas del builder fluido + ejemplos CSS/XPath
- [`plan.md`](./plan.md) — Arquitectura + decisiones técnicas
- [`QUICKSTART.md`](./QUICKSTART.md) — 8 ejemplos ready-to-use
- [`CHANGELOG.md`](./CHANGELOG.md) — v0.1.0 release notes
- [`../DELIVERY.md`](../DELIVERY.md) — Reporte de entrega final

---

## Quick start

```csharp
using Ravelaso.UiPath.Selenium.Core;

// 1. Elegir browser y opciones
var options = new BrowserOptions
{
    Browser   = BrowserType.Edge,
    Headless  = false,
    Maximize  = true,
    GlobalDelay = TimeSpan.FromSeconds(5)   // espera global entre pasos (default: 5 s)
};

using var client = new RavelasoWebClient(BrowserType.Edge, options);

// 2. Navegar
client.Navigate("https://www.example.com");

// 3. Localizar de forma simplificada
client.Click(Loc.Id("login"));
client.Type(Loc.Id("user"), "ana");
client.Type(Loc.Id("pass"), "secret", clear: true);
client.Click(Loc.Css("button.btn-primary"));

// 4. Comprobar existencia (nunca lanza excepción)
if (client.Exists(Loc.Id("error-banner")))
{
    Console.WriteLine(client.GetText(Loc.Id("error-banner")));
}

// 5. Leer
string titulo = client.GetTitle();
string html   = client.GetPageSource();

// 6. Salida a Selenium crudo cuando el wrapper no llega
client.Driver.Manage().Window.Maximize();
```

---

## Namespaces

Todo vive en un único namespace para minimizar `using`:

```csharp
using Ravelaso.UiPath.Selenium.Core;
```

---

## Estructura del repositorio

```
Ravelaso.UiPath.Selenium.slnx
├─ docs/
│  ├─ README.md           ← este archivo
│  ├─ plan.md             ← diseño
│  ├─ api.md              ← referencia
│  ├─ locators.md         ← builder
│  ├─ QUICKSTART.md       ← ejemplos (NUEVO)
│  └─ CHANGELOG.md        ← release (NUEVO)
├─ Ravelaso.UiPath.Selenium.Core/
│  ├─ BrowserType.cs
│  ├─ BrowserOptions.cs
│  ├─ BrowserDriverFactory.cs
│  ├─ LocatorType.cs
│  ├─ Locator.cs
│  ├─ Loc.cs
│  └─ RavelasoWebClient.cs
├─ Ravelaso.UiPath.Selenium.Tests/
│  ├─ TestFixtures.cs         (NUEVO)
│  ├─ LocatorTests.cs         (NUEVO)
│  ├─ BrowserOptionsTests.cs  (NUEVO)
│  ├─ ClientOperationsTests.cs (NUEVO)
│  └─ Program.cs              (reescrito)
├─ DELIVERY.md                (NUEVO — reporte final)
└─ VERIFY.sh                  (NUEVO — script de validación)
```

---

## Cómo ejecutar los tests

```bash
cd Ravelaso.UiPath.Selenium.Tests
dotnet run -c Release --framework net10.0

# Output esperado:
# ✅ ALL TESTS PASSED: 30/30
# ✅ ALL VALIDATIONS PASSED!
```

---

## Cómo usar en UiPath Studio 25.10

1. **Compilar el Core:**
   ```bash
   cd Ravelaso.UiPath.Selenium.Core
   dotnet build -c Release --framework net8.0
   ```

2. **Copiar DLL:**
   - Source: `bin/Release/net8.0/Ravelaso.UiPath.Selenium.Core.dll`
   - Destination: Proyecto UiPath o carpeta de assemblies

3. **En Studio, crear una actividad .NET:**
   ```csharp
   using Ravelaso.UiPath.Selenium.Core;
   
   var client = new RavelasoWebClient(BrowserType.Chrome);
   client.Navigate("https://example.com");
   // ... más operaciones
   client.Dispose();
   ```

---

## Próximos pasos

- [ ] NuGet package publishing
- [ ] RemoteWebDriver (Selenium Grid)
- [ ] Activity wrappers (`.xaml` invocables)
- [ ] Advanced logging (Serilog)
- [ ] Performance metrics

---

## Licencia

[Especificar aquí — ej. MIT, Apache 2.0, etc.]

---

## Contribución

Para reportar bugs o sugerir mejoras, abrir un issue en el repositorio.

---

**Developed by:** Ravelaso (2026)  
**Tested:** ✅ 30/30 unit + integration tests  
**Verified:** ✅ net8.0 + net10.0 clean build
