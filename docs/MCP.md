# MCP Server — Browser Discovery for AI Agents

The `Ravelaso.UiPath.Selenium.Mcp` project is a Model Context Protocol (MCP) server that lets AI agents (OpenCode, VS Code, Claude Desktop, etc.) **explore live web pages** before writing automation code. The agent can open a browser, inspect the DOM, discover stable locators, and then emit C# code using the `RavelasoWebClient` + `Loc` API.

This complements `docs/QUICKSTART.md` — QuickStart shows the library API; this doc shows how an agent discovers the right locators to use with that API.

---

## What it provides

### Tools (stdio transport)

| Category | Tools |
|---|---|
| **Lifecycle** | `browser_start`, `browser_stop`, `browser_status` |
| **Navigation** | `browser_navigate`, `browser_get_url`, `browser_get_title`, `browser_get_page_source`, `browser_screenshot` |
| **Discovery** | `browser_find_elements`, `browser_get_element`, `browser_list_inputs`, `browser_list_buttons`, `browser_list_interactive`, `browser_exists` |
| **Interaction** | `browser_click`, `browser_type`, `browser_get_text`, `browser_get_attribute`, `browser_wait_for`, `browser_wait_for_text` |

### Key capabilities

- **Shadow DOM traversal** — finds elements inside web components (e.g. `<alusta-navigation>`). All discovery tools recurse into open shadow roots.
- **Ranked locator suggestions** — each element returns `suggestedLocators` (ordered by stability), each with `matchCount` and `unique` flags. Only verified (matchCount ≥ 1) suggestions are included.
- **Full attribute dump** — every element's attributes are returned (not a fixed list), so framework-specific hooks (`data-t-id`, `data-cy`, `aria-label`, `formcontrolname`, etc.) are visible.
- **No XPath in suggestions** — suggestions use `Loc.Id`, `Loc.Name`, `Loc.Attr`, `Loc.Tag().Class()`, `Loc.Tag().Text()`. Raw HTML is also provided so the agent can craft its own combos.
- **Text search excludes `<script>`/`<style>`** — so it matches the visible anchor, not the config JSON.

### Resources

The server exposes the library README and QuickStart as MCP resources so the agent has the API in context.

---

## Installation

The MCP server is a standalone console application. It does **not** require the `Ravelaso.UiPath.Selenium` NuGet package — it references the Core library directly via project reference and bundles all dependencies in its output folder.

### Option 1: Published self-contained exe (recommended for end users)

Download the latest release from the GitHub Releases page (or build it yourself). The published exe includes the .NET runtime — no SDK or NuGet needed on the target machine.

**Build it yourself:**

```bash
cd Ravelaso.UiPath.Selenium.Mcp
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

This produces a folder with `Ravelaso.UiPath.Selenium.Mcp.exe` plus all dependencies. Copy the folder anywhere (e.g. `C:\Tools\RavelasoMcp\`).

**OpenCode config** (`opencode.json` or `~/.config/opencode/opencode.json`):

```json
{
  "mcp": {
    "ravelaso-selenium": {
      "type": "local",
      "command": ["C:\\Tools\\RavelasoMcp\\Ravelaso.UiPath.Selenium.Mcp.exe"],
      "enabled": true
    }
  }
}
```

### Option 2: Build from source (for development)

Clone the repo, build, and point OpenCode at the debug exe:

```bash
cd Ravelaso.UiPath.Selenium
dotnet build -c Debug
```

**OpenCode config:**

```json
{
  "mcp": {
    "ravelaso-selenium": {
      "type": "local",
      "command": ["C:\\Data\\Dev\\Ravelaso.UiPath.Selenium\\Ravelaso.UiPath.Selenium.Mcp\\bin\\Debug\\net10.0\\Ravelaso.UiPath.Selenium.Mcp.exe"],
      "enabled": true
    }
  }
}
```

### Option 3: dotnet run (no pre-build)

```json
{
  "mcp": {
    "ravelaso-selenium": {
      "type": "local",
      "command": ["dotnet", "run", "--project", "C:\\Data\\Dev\\Ravelaso.UiPath.Selenium\\Ravelaso.UiPath.Selenium.Mcp\\Ravelaso.UiPath.Selenium.Mcp.csproj", "--no-build"],
      "enabled": true
    }
  }
}
```

> **Note**: with `--no-build`, you must build first (`dotnet build`). Without `--no-build`, `dotnet run` builds automatically but is slower on startup.

### VS Code

Add to `.vscode/mcp.json`:

```json
{
  "servers": {
    "ravelaso-selenium": {
      "type": "stdio",
      "command": "C:\\Tools\\RavelasoMcp\\Ravelaso.UiPath.Selenium.Mcp.exe"
    }
  }
}
```

### Claude Desktop

Add to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "ravelaso-selenium": {
      "command": "C:\\Tools\\RavelasoMcp\\Ravelaso.UiPath.Selenium.Mcp.exe"
    }
  }
}
```

### Rebuild after code changes

```bash
cd Ravelaso.UiPath.Selenium\Ravelaso.UiPath.Selenium.Mcp
dotnet build
```

If the MCP server is running, stop it first (the exe is locked by the process). OpenCode will respawn it automatically on the next tool call.

---

## Agent workflow example

1. **Start + navigate**: `browser_start(Edge)` → `browser_navigate(url)`
2. **Discover**: `browser_list_inputs()` finds `txtLogin`, `txtPassword` with `Loc.Id("txtLogin")` / `Loc.Id("txtPassword")`
3. **Find submit**: `browser_list_buttons()` finds the submit button with `Loc.Tag("button").Class("radius")` or `Loc.Id("btnLogin")`
4. **Verify**: type credentials and click to confirm the flow works
5. **Emit code**: the agent writes the C# automation using the discovered locators


## Locator suggestion priority

Suggestions are ranked by stability (most stable first):

1. **Id** — `Loc.Id("x")` (if unique)
2. **Name** — `Loc.Name("x")` (if unique)
3. **Testing attributes** — `Loc.Attr("data-t-id", "x")`, `Loc.Attr("data-testid", "x")`, `Loc.Attr("data-cy", "x")`, `Loc.Attr("aria-label", "x")`, etc.
4. **Text** — `Loc.Tag("button").Text("Login")` (only if direct text matches and is unique)
5. **Class combo** — `Loc.Tag("div").Class("error-panel")` (only if unique)
6. **CSS path** — `Loc.Css("html > body > ...")` (last resort, fragile)

Only suggestions with `matchCount ≥ 1` are included. The agent also receives `outerHtml` and the full `attributes` dump to craft its own combos when needed.

---

## Security notes

- Stdio transport only — no network exposure.
- Browser runs visible by default (headless opt-in).
- Credentials are never stored — the agent emits placeholders (`Username`, `Password`) for the user to fill in.
- Screenshot paths are returned for the host to read; no image data is sent over the wire.