#if UNITY_EDITOR || (!UNITY_WEBGL && !UNITY_ANDROID && !UNITY_IOS)
// Two-player prototype transport. Only game JSON crosses this connection.
// Host listens on loopback; cloudflared supplies the public HTTPS endpoint.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public sealed class TejaInternetTransport : IDisposable {
    public const int ProtocolVersion=6;
    public const int MaxInputBytes=65536,MaxSnapshotBytes=131072;
    const int GuestGraceSeconds=18;
    static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
    static readonly Regex PublicHost=new Regex(@"^[a-z0-9-]{1,180}\.trycloudflare\.com$",RegexOptions.CultureInvariant);
    static readonly Regex TunnelUrl=new Regex(@"https://([a-z0-9-]+\.trycloudflare\.com)",RegexOptions.CultureInvariant);
    volatile Session current;

    sealed class Session {
        public readonly object Gate=new object();
        public readonly CancellationTokenSource Cancel=new CancellationTokenSource();
        public readonly Queue<string> Inputs=new Queue<string>(),Snapshots=new Queue<string>();
        public readonly SemaphoreSlim Requests=new SemaphoreSlim(4);
        public bool Host,Local,Registered,WasConnected;
        public volatile string State="Starting",Failure="",Invite="",HostName="",Diagnostic="",Stage="";
        public string Secret,ClientId,GuestId,Snapshot="{}",Input="{}",Endpoint="",TempFolder="";
        public long LastReceive,RateWindow;
        public int RateCount,Port;
        public TcpListener Listener;
        public HttpClient Http;
        public IPAddress SecureAddress;
        public Process Tunnel;
        public Task Loop,Server;
    }

    public bool IsHost {get {var s=current;return s!=null&&s.Host&&!s.Cancel.IsCancellationRequested;}}
    public bool IsGuest {get {var s=current;return s!=null&&!s.Host&&!s.Cancel.IsCancellationRequested;}}
    public bool Connected {get {var s=current;return s!=null&&!s.Cancel.IsCancellationRequested&&Interlocked.Read(ref s.LastReceive)>0&&Age(Interlocked.Read(ref s.LastReceive))<10;}}
    public string InviteCode {get {var s=current;return s==null?"":s.Invite;}}
    public string Error {get {var s=current;return s==null?"":s.Failure;}}
    public string LastDiagnostic {get {var s=current;return s==null?"offline":s.Diagnostic+"; "+s.Stage+"; secureDNS="+(s.SecureAddress!=null);}}
    // An opaque connection generation for resetting gameplay ACK state after a replacement guest.
    // This is not the room secret; callers should still avoid displaying/logging it.
    public string PeerSessionId {get {var s=current;if(s==null)return "";lock(s.Gate)return s.Host?(s.GuestId??""):s.ClientId;}}
    public double LastReceiveAge {get {var s=current;long last=s==null?0:Interlocked.Read(ref s.LastReceive);return last==0?double.PositiveInfinity:Age(last);}}
    public string Status {get {var s=current;if(s==null)return "Offline";if(s.Failure.Length>0)return s.Failure;if(Connected)return s.Host?"Friend connected":"Connected to host";if(s.Host&&s.Invite.Length>0)return "Room open — waiting for your friend";return s.State;}}

    public void StartHost(string cloudflaredExePath){
        Stop();var s=NewSession(true,false);current=s;
        if(string.IsNullOrEmpty(cloudflaredExePath)||!File.Exists(cloudflaredExePath)){Fail(s,"Internet helper is missing. Extract the complete game download and try again.");return;}
        try {StartListener(s);s.State="Opening a secure internet room…";s.Loop=Task.Run(()=>RunTunnel(s,cloudflaredExePath));}
        catch(Exception){Fail(s,"Could not open the local game connection. Close the room and retry.");}
    }
    public void Join(string invitation){
        Stop();string host,secret;
        if(!TryParseInvite(invitation,out host,out secret)){var invalid=NewSession(false,false);current=invalid;Fail(invalid,"That invite code is not valid. Paste the complete TEJA6 code from your friend.");return;}
        StartGuest("https://"+host,secret,false);
    }
    public static bool TryParseInvite(string value,out string host,out string secret){
        host=secret="";if(string.IsNullOrWhiteSpace(value)||value.Length>400)return false;
        var parts=value.Trim().Split('.');if(parts.Length!=3||parts[0]!="TEJA6"||!ValidToken(parts[2]))return false;
        try {host=Utf8.GetString(FromBase64Url(parts[1]));}catch(Exception){return false;}
        if(!PublicHost.IsMatch(host))return false;secret=parts[2];return true;
    }
    public void SetHostSnapshot(string json){var s=current;if(s==null||!s.Host)return;CheckJson(json,MaxSnapshotBytes);lock(s.Gate)s.Snapshot=json;}
    // Publish an application-level end/rejection packet before closing the room.
    // The caller can wait for its own sequence ACK or briefly delay Stop().
    public void EndSession(string finalSnapshot){SetHostSnapshot(finalSnapshot);}
    // Latest state, not a reliable event queue: repeat action IDs until acknowledged.
    public void SetGuestInput(string json){var s=current;if(s==null||s.Host)return;CheckJson(json,MaxInputBytes);lock(s.Gate)s.Input=json;}
    public bool TryGetGuestInput(out string json){var s=current;json=null;if(s==null)return false;lock(s.Gate){if(s.Inputs.Count==0)return false;json=s.Inputs.Dequeue();return true;}}
    public bool TryGetHostSnapshot(out string json){var s=current;json=null;if(s==null)return false;lock(s.Gate){if(s.Snapshots.Count==0)return false;json=s.Snapshots.Dequeue();return true;}}

    // Explicit loopback-only entrypoints used by the automated transport test.
    // Public Join() never accepts these invitations or arbitrary server URLs.
    public string StartHostLocalForTests(){Stop();var s=NewSession(true,true);current=s;StartListener(s);s.Invite="TEJA6LOCAL."+s.Port+"."+s.Secret;s.State="Local transport test";return s.Invite;}
    public void JoinLocalForTests(string code){
        Stop();var p=(code??"").Split('.');int port;
        if(p.Length!=3||p[0]!="TEJA6LOCAL"||!int.TryParse(p[1],out port)||port<1024||port>65535||!ValidToken(p[2]))throw new ArgumentException("Invalid loopback test invitation");
        StartGuest("http://127.0.0.1:"+port,p[2],true);
    }
    public void Stop(){
        var s=current;current=null;if(s==null)return;
        s.Cancel.Cancel();CloseResources(s);
        lock(s.Gate){s.Inputs.Clear();s.Snapshots.Clear();s.Snapshot=s.Input="{}";}
    }
    public void Dispose(){Stop();}

    static Session NewSession(bool host,bool local){return new Session{Host=host,Local=local,Secret=Token(),ClientId=Token(),RateWindow=Now()};}
    static long Now(){return DateTime.UtcNow.Ticks;}
    static double Age(long ticks){return (Now()-ticks)/(double)TimeSpan.TicksPerSecond;}
    static string Token(){var bytes=new byte[16];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(bytes);return Base64Url(bytes);}
    static string Base64Url(byte[] value){return Convert.ToBase64String(value).TrimEnd('=').Replace('+','-').Replace('/','_');}
    static byte[] FromBase64Url(string value){if(value.Length%4==1)throw new FormatException();return Convert.FromBase64String(value.Replace('-','+').Replace('_','/').PadRight((value.Length+3)/4*4,'='));}
    static bool ValidToken(string value){if(value==null||value.Length!=22)return false;foreach(char c in value)if(!(c>='a'&&c<='z'||c>='A'&&c<='Z'||c>='0'&&c<='9'||c=='-'||c=='_'))return false;return true;}
    static bool SameSecret(string a,string b){if(a==null||b==null||a.Length!=b.Length)return false;int difference=0;for(int i=0;i<a.Length;i++)difference|=a[i]^b[i];return difference==0;}
    static void CheckJson(string json,int limit){if(string.IsNullOrEmpty(json)||Utf8.GetByteCount(json)>limit||!json.TrimStart().StartsWith("{")||!json.TrimEnd().EndsWith("}"))throw new ArgumentException("Game packet must be a bounded JSON object");}
    static void Fail(Session s,string reason){if(s.Cancel.IsCancellationRequested)return;s.Failure=reason;s.State=reason;s.Cancel.Cancel();CloseResources(s);}
    static void CloseResources(Session s){
        try{if(s.Listener!=null)s.Listener.Stop();}catch(Exception){}
        try{if(s.Http!=null)s.Http.Dispose();}catch(Exception){}
        Process tunnel;
        lock(s.Gate){tunnel=s.Tunnel;s.Tunnel=null;}
        try{if(tunnel!=null&&!tunnel.HasExited){tunnel.Kill();tunnel.WaitForExit(2000);}}catch(Exception){}
        try{if(tunnel!=null)tunnel.Dispose();}catch(Exception){}
        // Only the configuration created by this session is removed.
        try{if(s.TempFolder.Length>0){File.Delete(Path.Combine(s.TempFolder,"config.yml"));Directory.Delete(s.TempFolder,false);}}catch(Exception){}
    }

    static void StartListener(Session s){
        s.Listener=new TcpListener(IPAddress.Loopback,0);s.Listener.Start(8);s.Port=((IPEndPoint)s.Listener.LocalEndpoint).Port;
        s.Server=Task.Run(()=>AcceptLoop(s));
    }
    static void RunTunnel(Session s,string executable){
        try{
            s.TempFolder=Path.Combine(Path.GetTempPath(),"TejaImpact-Coop-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(s.TempFolder);
            string config=Path.Combine(s.TempFolder,"config.yml");File.WriteAllText(config,"loglevel: info\n",Encoding.ASCII);
            var start=new ProcessStartInfo{FileName=Path.GetFullPath(executable),Arguments="tunnel --config \""+config+"\" --no-autoupdate --protocol http2 --edge-ip-version 4 --url http://127.0.0.1:"+s.Port,
                UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true,RedirectStandardOutput=true,WorkingDirectory=s.TempFolder};
            var process=new Process{StartInfo=start};
            process.OutputDataReceived+=(sender,args)=>TunnelLine(s,args.Data);process.ErrorDataReceived+=(sender,args)=>TunnelLine(s,args.Data);
            lock(s.Gate){
                if(s.Cancel.IsCancellationRequested){process.Dispose();return;}
                if(!process.Start()){process.Dispose();throw new IOException("Helper launch failed");}
                s.Tunnel=process;process.BeginOutputReadLine();process.BeginErrorReadLine();
            }
            long began=Now();
            while(!s.Cancel.IsCancellationRequested){
                if(process.HasExited){Fail(s,"The internet connection helper stopped. Close the room and host again.");return;}
                if(s.Invite.Length==0&&Age(began)>75){Fail(s,"Could not reach the internet relay. Check your connection and try hosting again.");return;}
                s.Cancel.Token.WaitHandle.WaitOne(250);
            }
        }catch(Exception){if(!s.Cancel.IsCancellationRequested)Fail(s,"Could not start the internet room. Check that the game was fully extracted.");}
        finally{CloseResources(s);}
    }
    static void TunnelLine(Session s,string line){
        if(s.Cancel.IsCancellationRequested||string.IsNullOrEmpty(line))return;
        lock(s.Gate){
            var match=TunnelUrl.Match(line);if(match.Success&&PublicHost.IsMatch(match.Groups[1].Value))s.HostName=match.Groups[1].Value;
            if(line.IndexOf("Registered tunnel connection",StringComparison.OrdinalIgnoreCase)>=0)s.Registered=true;
            if(s.HostName.Length>0&&s.Registered){s.Invite="TEJA6."+Base64Url(Utf8.GetBytes(s.HostName))+"."+s.Secret;s.State="Room open — waiting for your friend";}
        }
    }
    static void AcceptLoop(Session s){
        while(!s.Cancel.IsCancellationRequested){
            TcpClient connection=null;
            try{
                connection=s.Listener.AcceptTcpClient();
                if(!s.Requests.Wait(0)){connection.Close();continue;}
                var accepted=connection;Task.Run(()=>{try{Serve(s,accepted);}finally{s.Requests.Release();}});
            }catch(Exception){if(connection!=null)connection.Close();if(!s.Cancel.IsCancellationRequested)Fail(s,"The local game connection stopped. Close the room and retry.");}
        }
    }
    static void Serve(Session s,TcpClient client){
        using(client){
            try{
                client.NoDelay=true;client.ReceiveTimeout=3000;client.SendTimeout=3000;
                using(var stream=client.GetStream()){
                    var header=ReadHeader(stream);if(header==null)return;
                    var lines=header.Split(new[]{"\r\n"},StringSplitOptions.None);var request=lines[0].Split(' ');
                    if(request.Length!=3||request[0]!="POST"||request[2]!="HTTP/1.1"||!(request[1]=="/teja6/join"||request[1]=="/teja6/sync")){Reply(stream,404,"{\"error\":\"session endpoint only\"}");return;}
                    var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                    for(int i=1;i<lines.Length;i++){
                        if(lines[i].Length==0)continue;int colon=lines[i].IndexOf(':');if(colon<1||headers.Count>=40){Reply(stream,400,"{}");return;}
                        string key=lines[i].Substring(0,colon).Trim(),value=lines[i].Substring(colon+1).Trim();if(headers.ContainsKey(key)){Reply(stream,400,"{}");return;}headers.Add(key,value);
                    }
                    string keyValue,clientId,version,lengthValue;int length;
                    if(headers.ContainsKey("Transfer-Encoding")||!headers.TryGetValue("Content-Length",out lengthValue)||!int.TryParse(lengthValue,out length)||length<2||length>MaxInputBytes){Reply(stream,413,"{}");return;}
                    if(!headers.TryGetValue("X-Teja-Protocol",out version)||version!="6"){Reply(stream,426,"{\"error\":\"version\"}");return;}
                    if(!headers.TryGetValue("X-Teja-Key",out keyValue)||!SameSecret(s.Secret,keyValue)||!headers.TryGetValue("X-Teja-Client",out clientId)||!ValidToken(clientId)){Reply(stream,403,"{\"error\":\"invite\"}");return;}
                    var bytes=new byte[length];int read=0;var timer=Stopwatch.StartNew();while(read<length){if(timer.ElapsedMilliseconds>3000)throw new IOException("Body timeout");int amount=stream.Read(bytes,read,length-read);if(amount==0)return;read+=amount;}
                    string body=Utf8.GetString(bytes);CheckJson(body,MaxInputBytes);string snapshot;
                    lock(s.Gate){
                        if(Age(s.RateWindow)>1){s.RateWindow=Now();s.RateCount=0;}if(++s.RateCount>40){Reply(stream,429,"{}");return;}
                        if(s.GuestId!=null&&Age(Interlocked.Read(ref s.LastReceive))>GuestGraceSeconds){s.GuestId=null;s.Inputs.Clear();}
                        if(s.GuestId!=null&&!SameSecret(s.GuestId,clientId)){Reply(stream,409,"{\"error\":\"full\"}");return;}
                        if(s.GuestId==null&&request[1]!="/teja6/join"){Reply(stream,410,"{\"error\":\"rejoin\"}");return;}
                        if(s.GuestId==null)s.GuestId=clientId;
                        Interlocked.Exchange(ref s.LastReceive,Now());
                        if(request[1]=="/teja6/sync"){
                            // Bound backlog if the main thread is temporarily busy.
                            if(s.Inputs.Count>=32)s.Inputs.Dequeue();s.Inputs.Enqueue(body);
                        }
                        snapshot=s.Snapshot;
                    }
                    Reply(stream,200,snapshot);
                }
            }catch(Exception){/* Malformed, timed-out or disconnected request: close only this socket. */}
        }
    }
    static string ReadHeader(NetworkStream stream){
        var bytes=new byte[8192];int count=0;var timer=Stopwatch.StartNew();
        while(count<bytes.Length){if(timer.ElapsedMilliseconds>3000)return null;int b=stream.ReadByte();if(b<0)return null;bytes[count++]=(byte)b;if(count>=4&&bytes[count-4]==13&&bytes[count-3]==10&&bytes[count-2]==13&&bytes[count-1]==10)return Encoding.ASCII.GetString(bytes,0,count-4);}
        return null;
    }
    static void Reply(NetworkStream stream,int code,string body){
        byte[] bytes=Utf8.GetBytes(body);string reason=code==200?"OK":code==403?"Forbidden":code==404?"Not Found":code==409?"Conflict":code==410?"Gone":code==413?"Payload Too Large":code==426?"Upgrade Required":code==429?"Too Many Requests":"Bad Request";
        var header=Encoding.ASCII.GetBytes("HTTP/1.1 "+code+" "+reason+"\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: "+bytes.Length+"\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nConnection: close\r\n\r\n");stream.Write(header,0,header.Length);stream.Write(bytes,0,bytes.Length);stream.Flush();
    }
    void StartGuest(string endpoint,string secret,bool local){
        var s=NewSession(false,local);s.Endpoint=endpoint;s.Secret=secret;s.State="Connecting to your friend…";current=s;
        // Automatic redirects are disabled so an invite cannot send secrets elsewhere.
        var handler=new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false};if(local)handler.UseProxy=false;
        s.Http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(5)};
        s.Loop=Task.Run(()=>GuestLoop(s));
    }
    static async Task GuestLoop(Session s){
        bool joined=false,dnsFailed=false;int failures=0;long began=Now();
        while(!s.Cancel.IsCancellationRequested){
            long pollBegan=Now();
            try{
                string payload;lock(s.Gate)payload=joined?s.Input:"{}";
                using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(s.Cancel.Token)){
                    deadline.CancelAfter(5000);
                    var response=await SendPacket(s,joined?"/teja6/sync":"/teja6/join",payload,deadline.Token).ConfigureAwait(false);
                        int code=response.Code;s.Diagnostic="HTTP "+code+"; bytes="+(response.Body==null?0:Utf8.GetByteCount(response.Body));
                        if(code==409){Fail(s,"That room already has two players. Ask your friend to open a new room.");return;}
                        if(code==403||code==426){Fail(s,code==403?"This invite is no longer valid. Ask your friend for a new code.":"Both players need the same game version.");return;}
                        if(code==410){joined=false;continue;}
                        if(code!=200)throw new IOException("Room unavailable");
                        string snapshot=response.Body;
                        CheckJson(snapshot,MaxSnapshotBytes);
                        lock(s.Gate){while(s.Snapshots.Count>=2)s.Snapshots.Dequeue();s.Snapshots.Enqueue(snapshot);}
                        joined=true;failures=0;dnsFailed=false;s.WasConnected=true;Interlocked.Exchange(ref s.LastReceive,Now());s.State="Connected to host";
                }
            }catch(OperationCanceledException){if(s.Cancel.IsCancellationRequested)return;s.Diagnostic="request deadline";failures++;s.State="Reconnecting to your friend…";}
            catch(Exception ex){
                if(s.Cancel.IsCancellationRequested)return;s.Diagnostic=ExceptionCode(ex);failures++;dnsFailed=IsDnsFailure(ex);
                s.State=dnsFailed?"Your network could not resolve the room address. Retrying…":s.WasConnected?"Reconnecting to your friend…":"Waiting for the host connection…";
            }
            if(dnsFailed&&!s.Local&&s.SecureAddress==null){
                s.State="Resolving the room securely…";
                try{s.SecureAddress=await ResolveSecureAddress(s).ConfigureAwait(false);dnsFailed=false;s.Diagnostic="secure DNS resolved";}catch(Exception ex){s.Diagnostic="secure DNS: "+ExceptionCode(ex);if(s.Cancel.IsCancellationRequested)return;}
            }
            if(s.Cancel.IsCancellationRequested)return;
            if(s.WasConnected?Age(Interlocked.Read(ref s.LastReceive))>22:Age(began)>45){Fail(s,dnsFailed?"Your network cannot resolve the internet room address. Try another network or ask your network administrator to allow trycloudflare.com.":s.WasConnected?"Connection lost. The host may have closed the game. Rejoin with a new code.":"Could not reach that room. The host must keep the game open; check the code and retry.");return;}
            int wait=failures>0?Math.Min(3000,250*(1<<Math.Min(failures,4))):Math.Max(15,125-(int)((Now()-pollBegan)/TimeSpan.TicksPerMillisecond));
            try{await Task.Delay(wait,s.Cancel.Token).ConfigureAwait(false);}catch(OperationCanceledException){return;}
        }
    }
    static bool IsDnsFailure(Exception exception){
        for(var e=exception;e!=null;e=e.InnerException){
            var web=e as WebException;if(web!=null&&(web.Status==WebExceptionStatus.NameResolutionFailure||web.Status==WebExceptionStatus.ProxyNameResolutionFailure))return true;
            var socket=e as SocketException;if(socket!=null&&(socket.SocketErrorCode==SocketError.HostNotFound||socket.SocketErrorCode==SocketError.NoData||socket.SocketErrorCode==SocketError.TryAgain))return true;
        }
        return false;
    }
    static string ExceptionCode(Exception exception){string result="";for(var e=exception;e!=null;e=e.InnerException){if(result.Length>0)result+=" / ";result+=e.GetType().Name+"["+e.HResult.ToString("X")+"]";var web=e as WebException;if(web!=null)result+=" "+web.Status;var socket=e as SocketException;if(socket!=null)result+=" "+socket.SocketErrorCode;
        switch(e.Message){case "Secure DNS unavailable":case "Secure DNS returned no answer":case "Secure DNS returned no public address":case "Invalid HTTP response":case "Invalid response headers":case "Duplicate response header":case "Unsupported response encoding":case "Ambiguous response framing":case "Invalid response chunk":case "Invalid chunk terminator":case "Invalid snapshot length":case "Unbounded response framing":case "Incomplete response headers":case "Invalid line ending":case "Response headers exceed limit":case "Incomplete snapshot":case "Snapshot exceeds limit":result+=" "+e.Message;break;}}
        return result;
    }
    sealed class PacketReply {public int Code;public string Body;}
    static async Task<PacketReply> SendPacket(Session s,string path,string payload,CancellationToken token){
        if(s.SecureAddress!=null)return await SendSecurePacket(s,path,payload,token).ConfigureAwait(false);
        using(var request=new HttpRequestMessage(HttpMethod.Post,s.Endpoint+path)){
            request.Headers.Add("X-Teja-Key",s.Secret);request.Headers.Add("X-Teja-Client",s.ClientId);request.Headers.Add("X-Teja-Protocol","6");request.Headers.ExpectContinue=false;
            request.Content=new StringContent(payload,Utf8,"application/json");
            using(var response=await s.Http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false))
                return new PacketReply{Code=(int)response.StatusCode,Body=response.IsSuccessStatusCode?await ReadSnapshot(response,token).ConfigureAwait(false):"{}"};
        }
    }
    // Some home routers return NXDOMAIN for new temporary tunnel names. Resolve only
    // the already-validated room hostname through Cloudflare's authenticated HTTPS
    // DNS API. No system DNS/hosts settings are changed and no invite token is sent.
    static async Task<IPAddress> ResolveSecureAddress(Session s){
        string host=new Uri(s.Endpoint).Host;if(!PublicHost.IsMatch(host))throw new IOException("Invalid room host");
        using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(s.Cancel.Token))
        using(var request=new HttpRequestMessage(HttpMethod.Get,"https://cloudflare-dns.com/dns-query?name="+Uri.EscapeDataString(host)+"&type=A")){
            deadline.CancelAfter(5000);request.Headers.Add("Accept","application/dns-json");
            s.Stage="secure DNS request";
            using(var response=await s.Http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token).ConfigureAwait(false)){
                s.Stage="secure DNS HTTP "+(int)response.StatusCode;
                if(!response.IsSuccessStatusCode)throw new IOException("Secure DNS unavailable");
                string json=await ReadSnapshot(response,deadline.Token).ConfigureAwait(false);
                var dnsStatus=Regex.Match(json,"\"Status\"\\s*:\\s*([0-9]+)");s.Stage+="; bytes="+json.Length+"; DNS status="+(dnsStatus.Success?dnsStatus.Groups[1].Value:"missing");
                if(json.Length>8192||!Regex.IsMatch(json,"\"Status\"\\s*:\\s*0\\s*[,}]"))throw new IOException("Secure DNS returned no answer");
                foreach(Match match in Regex.Matches(json,"\"data\"\\s*:\\s*\"([0-9.]{7,15})\"")){
                    IPAddress address;if(!IPAddress.TryParse(match.Groups[1].Value,out address)||address.AddressFamily!=AddressFamily.InterNetwork)continue;
                    byte[] b=address.GetAddressBytes();if(b[0]==0||b[0]==10||b[0]==127||b[0]>=224||b[0]==169&&b[1]==254||b[0]==172&&b[1]>=16&&b[1]<=31||b[0]==192&&b[1]==168||b[0]==100&&b[1]>=64&&b[1]<=127)continue;
                    return address;
                }
                throw new IOException("Secure DNS returned no public address");
            }
        }
    }
    // A narrowly scoped HTTP/1.1 TLS client lets a resolved address be used while
    // SslStream still authenticates the ORIGINAL hostname, using its default
    // certificate validation. There is deliberately no certificate callback.
    static async Task<PacketReply> SendSecurePacket(Session s,string path,string payload,CancellationToken token){
        string host=new Uri(s.Endpoint).Host;if(!PublicHost.IsMatch(host))throw new IOException("Invalid room host");
        using(var client=new TcpClient())using(token.Register(()=>client.Close())){
            s.Stage="secure connect";
            await client.ConnectAsync(s.SecureAddress,443).ConfigureAwait(false);
            using(var tls=new SslStream(client.GetStream(),false)){
                s.Stage="TLS authenticate";
                await tls.AuthenticateAsClientAsync(host,null,SslProtocols.Tls12,true).ConfigureAwait(false);
                s.Stage="TLS send";
                byte[] body=Utf8.GetBytes(payload);
                byte[] header=Encoding.ASCII.GetBytes("POST "+path+" HTTP/1.1\r\nHost: "+host+"\r\nContent-Type: application/json; charset=utf-8\r\nAccept: application/json\r\nContent-Length: "+body.Length+"\r\nX-Teja-Protocol: 6\r\nX-Teja-Key: "+s.Secret+"\r\nX-Teja-Client: "+s.ClientId+"\r\nConnection: close\r\n\r\n");
                await tls.WriteAsync(header,0,header.Length,token).ConfigureAwait(false);await tls.WriteAsync(body,0,body.Length,token).ConfigureAwait(false);
                s.Stage="TLS response headers";
                string status=await ReadTlsLine(tls,1024,token).ConfigureAwait(false);var parts=status.Split(' ');int code;
                if(parts.Length<2||!parts[0].StartsWith("HTTP/1.")||!int.TryParse(parts[1],out code))throw new IOException("Invalid HTTP response");
                var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);int total=status.Length;
                while(true){string line=await ReadTlsLine(tls,8192-total,token).ConfigureAwait(false);total+=line.Length+2;if(line.Length==0)break;int colon=line.IndexOf(':');if(colon<1||headers.Count>=40)throw new IOException("Invalid response headers");string name=line.Substring(0,colon).Trim();
                    // Proxies can legitimately repeat Vary, Server-Timing and other
                    // metadata. Only framing/encoding ambiguity must be rejected.
                    if(headers.ContainsKey(name)){if(name.Equals("Content-Length",StringComparison.OrdinalIgnoreCase)||name.Equals("Transfer-Encoding",StringComparison.OrdinalIgnoreCase)||name.Equals("Content-Encoding",StringComparison.OrdinalIgnoreCase))throw new IOException("Duplicate response header");continue;}
                    headers.Add(name,line.Substring(colon+1).Trim());}
                if(code!=200)return new PacketReply{Code=code,Body="{}"};
                s.Stage="TLS response body";
                string length,transfer,encoding;
                if(headers.TryGetValue("Content-Encoding",out encoding)&&encoding!="identity")throw new IOException("Unsupported response encoding");
                using(var data=new MemoryStream()){
                    if(headers.TryGetValue("Transfer-Encoding",out transfer)){
                        if(!transfer.Equals("chunked",StringComparison.OrdinalIgnoreCase)||headers.ContainsKey("Content-Length"))throw new IOException("Ambiguous response framing");
                        while(true){string size=await ReadTlsLine(tls,128,token).ConfigureAwait(false);int semicolon=size.IndexOf(';');if(semicolon>=0)size=size.Substring(0,semicolon);int amount;if(size.Length>8||!int.TryParse(size,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out amount)||amount<0||amount>MaxSnapshotBytes-data.Length)throw new IOException("Invalid response chunk");if(amount==0)break;await CopyTlsBytes(tls,data,amount,token).ConfigureAwait(false);if((await ReadTlsLine(tls,2,token).ConfigureAwait(false)).Length!=0)throw new IOException("Invalid chunk terminator");}
                    }else if(headers.TryGetValue("Content-Length",out length)){
                        int amount;if(!int.TryParse(length,out amount)||amount<2||amount>MaxSnapshotBytes)throw new IOException("Invalid snapshot length");await CopyTlsBytes(tls,data,amount,token).ConfigureAwait(false);
                    }else throw new IOException("Unbounded response framing");
                    return new PacketReply{Code=code,Body=Utf8.GetString(data.ToArray())};
                }
            }
        }
    }
    static async Task<string> ReadTlsLine(Stream stream,int limit,CancellationToken token){
        var data=new List<byte>();var one=new byte[1];while(data.Count<=limit){if(await stream.ReadAsync(one,0,1,token).ConfigureAwait(false)!=1)throw new IOException("Incomplete response headers");if(one[0]==10){if(data.Count==0||data[data.Count-1]!=13)throw new IOException("Invalid line ending");data.RemoveAt(data.Count-1);return Encoding.ASCII.GetString(data.ToArray());}data.Add(one[0]);}throw new IOException("Response headers exceed limit");
    }
    static async Task CopyTlsBytes(Stream stream,MemoryStream output,int length,CancellationToken token){
        var buffer=new byte[4096];while(length>0){int count=await stream.ReadAsync(buffer,0,Math.Min(buffer.Length,length),token).ConfigureAwait(false);if(count==0)throw new IOException("Incomplete snapshot");output.Write(buffer,0,count);length-=count;}
    }
    static async Task<string> ReadSnapshot(HttpResponseMessage response,CancellationToken token){
        if(response.Content.Headers.ContentLength.HasValue&&response.Content.Headers.ContentLength.Value>MaxSnapshotBytes)throw new IOException("Snapshot exceeds limit");
        using(var stream=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))using(var memory=new MemoryStream()){
            var buffer=new byte[4096];int length;
            while((length=await stream.ReadAsync(buffer,0,buffer.Length,token).ConfigureAwait(false))>0){if(memory.Length+length>MaxSnapshotBytes)throw new IOException("Snapshot exceeds limit");memory.Write(buffer,0,length);}
            return Utf8.GetString(memory.ToArray());
        }
    }
}

#else
using System;

// Native socket/Process APIs are deliberately absent from browser and phone players.
public sealed class TejaInternetTransport : IDisposable {
    public const int ProtocolVersion=6,MaxInputBytes=65536,MaxSnapshotBytes=131072;
    public bool IsHost {get{return false;}}
    public bool IsGuest {get{return false;}}
    public bool Connected {get{return false;}}
    public string InviteCode {get{return "";}}
    public string Error {get{return "Online rooms require the Windows edition.";}}
    public string LastDiagnostic {get{return "Unsupported platform";}}
    public string PeerSessionId {get{return "";}}
    public double LastReceiveAge {get{return double.PositiveInfinity;}}
    public string Status {get{return Error;}}
    public void StartHost(string path){}
    public void Join(string code){}
    public static bool TryParseInvite(string value,out string host,out string secret){host=secret=null;return false;}
    public void SetHostSnapshot(string json){}
    public void EndSession(string json){}
    public void SetGuestInput(string json){}
    public bool TryGetGuestInput(out string json){json=null;return false;}
    public bool TryGetHostSnapshot(out string json){json=null;return false;}
    public string StartHostLocalForTests(){return "";}
    public void JoinLocalForTests(string code){}
    public void Stop(){}
    public void Dispose(){}
}
#endif
