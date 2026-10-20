# Ravelaso.UiPath.Selenium

Wrapper de **Selenium WebDriver** para .NET, pensado para ser usado desde workflows de **UiPath** y desde cualquier proyecto .NET.

> Envuelve Selenium para que el código de negocio no tenga que: elegir el driver, dormir entre pasos, ni recordar la sintaxis de `By`.

> **Estado:** plan aprobado · implementación pendiente.
> Estos documentos describen la **API objetivo**; el código todavía no está escrito. La API pública está definida en [`api.md`](./api.md) y las reglas del builder en [`locators.md`](./locators.md).

---

## Índice

| Documento | Contenido |
|---|---|
| [`plan.md`](./plan.md) | Plan de implementación, decisiones de diseño y restricciones |
| [`api.md`](./api.md) | Referencia de la API del cliente |
| [`locators.md`](./locators.md) | Reglas del builder de localizadores (`Loc` / `Locator`) |

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
└─ Ravelaso.UiPath.Selenium.Core/     ← la librería
   ├─ BrowserType.cs
   ├─ BrowserOptions.cs
   ├─ BrowserDriverFactory.cs
   ├─ LocatorType.cs
   ├─ Locator.cs
   ├─ Loc.cs
   └─ RavelasoWebClient.cs
```
