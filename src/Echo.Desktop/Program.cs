using System.IO.Pipes;
using System.IO;
using System.Net.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Echo.Contracts;

namespace Echo.Desktop;
static class Program {
 [STAThread] static void Main() { using var mutex = new Mutex(true, "Echo.Mvp.SingleInstance", out var first); if (!first) { try { using var c=new NamedPipeClientStream(".","Echo.Activate",PipeDirection.Out); c.Connect(300); } catch {} return; } ApplicationConfiguration.Initialize(); var form=new EchoForm(); _=Listen(form); Application.Run(form); }
 static async Task Listen(EchoForm form) { while (!form.IsDisposed) { try { using var server=new NamedPipeServerStream("Echo.Activate",PipeDirection.In); await server.WaitForConnectionAsync(); if (!form.IsDisposed) form.BeginInvoke(form.ShowWindow); } catch { } } }
}
public sealed class ProfileStore {
 readonly string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Echo","profile.dat");
 public (bool consent, VoiceProfile? profile) Read() { if(!File.Exists(path)) return (false,null); try { var raw=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser); return JsonSerializer.Deserialize<Stored>(raw) is { } s ? (s.Consent,s.Profile) : (false,null); } catch { return(false,null); } }
 public void Save(VoiceProfile p) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path,ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(new Stored(true,p)),null,DataProtectionScope.CurrentUser)); }
 sealed record Stored(bool Consent, VoiceProfile Profile);
}
public sealed class EchoForm : Form {
 readonly ProfileStore store=new(); readonly TextBox samples=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill}; readonly Label status=new(){Dock=DockStyle.Top,Height=46}; readonly Button finish=new(){Text="Create profile",Dock=DockStyle.Bottom}; VoiceProfile? profile; bool consent; const int HOTKEY=0xE001;
 public EchoForm() { Text="Echo – Rewrite in My Voice"; Width=660; Height=460; Controls.Add(samples); Controls.Add(finish); Controls.Add(status); finish.Click+=async (_,_)=>await Onboard(); Load+=(_,_)=> { (consent,profile)=store.Read(); Render(); RegisterHotKey(Handle,HOTKEY,3,(uint)Keys.E); }; FormClosing+=(s,e)=> { if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();} }; }
 public void ShowWindow(){ Show(); WindowState=FormWindowState.Normal; Activate(); }
 void Render() { if(profile is not null) { samples.Visible=false; finish.Visible=false; status.Text=$"Echo is ready in the background. Press Ctrl+Alt+E to rewrite selected text.\r\nVoice: {profile.Tone}; {profile.Formality}. Use File → Exit to stop Echo."; } else status.Text="Paste 3–10 samples below, separated by a line containing --- . By continuing, you consent to sending these samples to your configured cloud gateway. Samples are not retained."; }
 async Task Onboard(){ var list=samples.Text.Split("\n---",StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries); if(!Validation.ValidSamples(list)){MessageBox.Show("Please provide 3 to 10 non-empty samples.");return;} if(MessageBox.Show("Send these samples to the cloud gateway to generate your profile?","Cloud consent",MessageBoxButtons.YesNo)!=DialogResult.Yes)return; try { using var h=new HttpClient{BaseAddress=new Uri(Environment.GetEnvironmentVariable("ECHO_GATEWAY")??"http://localhost:5000")}; var r=await h.PostAsJsonAsync("/api/profile",new ProfileRequest(true,list)); var x=await r.Content.ReadFromJsonAsync<ApiResult<VoiceProfile>>(); if(x?.Success!=true||x.Value is null) throw new Exception(x?.Error??"Gateway unavailable"); profile=x.Value; consent=true; store.Save(profile); Render(); } catch(Exception ex){MessageBox.Show("Profile creation failed. No samples were stored locally.\n"+ex.Message);} }
 protected override void WndProc(ref Message m){ if(m.Msg==0x0312&&m.WParam.ToInt32()==HOTKEY) _=RewriteSelection(); base.WndProc(ref m); }
 async Task RewriteSelection(){ if(!consent||profile is null){Notify("Complete onboarding first.");return;} IDataObject? original=null; try { original=Clipboard.GetDataObject(); SendKeys.SendWait("^c"); await Task.Delay(160); var text=Clipboard.ContainsText()?Clipboard.GetText():""; if(string.IsNullOrWhiteSpace(text)){Notify("No supported text selection found.");return;} using var h=new HttpClient{BaseAddress=new Uri(Environment.GetEnvironmentVariable("ECHO_GATEWAY")??"http://localhost:5000"),Timeout=TimeSpan.FromSeconds(5)}; var response=await h.PostAsJsonAsync("/api/rewrite",new RewriteRequest(true,text,profile)); var result=await response.Content.ReadFromJsonAsync<ApiResult<string>>(); if(result?.Success!=true||string.IsNullOrEmpty(result.Value)){Notify(result?.Error??"Rewrite failed.");return;} Clipboard.SetText(result.Value); SendKeys.SendWait("^v"); Notify("Rewritten in your voice."); } catch { Notify("Rewrite failed; selected text was not changed."); } finally { if(original is not null) try { Clipboard.SetDataObject(original,true); } catch {} } }
 void Notify(string text){ BeginInvoke(()=>status.Text=text+"\r\n"+status.Text); }
 [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd,int id,uint fsModifiers,uint vk);
 protected override void Dispose(bool disposing){if(disposing) UnregisterHotKey(Handle,HOTKEY);base.Dispose(disposing);} [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd,int id);
}
