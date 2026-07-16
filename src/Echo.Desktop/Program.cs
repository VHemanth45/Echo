using System.IO.Pipes;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
using System.Drawing;
using Echo.Contracts;

namespace Echo.Desktop;
static class Program {
 [STAThread] static void Main() { using var mutex=new Mutex(true,"Echo.Mvp.SingleInstance",out var first); if(!first){try{using var c=new NamedPipeClientStream(".","Echo.Activate",PipeDirection.Out);c.Connect(300);}catch{}return;} ApplicationConfiguration.Initialize();var form=new EchoForm();_=Listen(form);Application.Run(form); }
 static async Task Listen(EchoForm form) { while(!form.IsDisposed){try{using var server=new NamedPipeServerStream("Echo.Activate",PipeDirection.In);await server.WaitForConnectionAsync();if(!form.IsDisposed)form.BeginInvoke(form.ShowWindow);}catch{}} }
}
public sealed class ProfileStore {
 readonly string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Echo","profile.dat");
 public (bool consent,VoiceProfile? profile) Read(){if(!File.Exists(path))return(false,null);try{var raw=ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser);return JsonSerializer.Deserialize<Stored>(raw) is {} s?(s.Consent,s.Profile):(false,null);}catch{return(false,null);}}
 public void Save(VoiceProfile p){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(path,ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(new Stored(true,p)),null,DataProtectionScope.CurrentUser));}
 sealed record Stored(bool Consent,VoiceProfile Profile);
}
public sealed class EchoForm : Form {
 readonly ProfileStore store=new();
 readonly TextBox samples=new(){Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",11),PlaceholderText="Paste a writing sample…\r\n\r\nSeparate each sample with a line containing ---"};
 readonly Label status=new(){AutoSize=false,Font=new Font("Segoe UI",10),ForeColor=Color.FromArgb(91,103,128)};
 readonly Label title=new(){AutoSize=true,Font=new Font("Segoe UI Semibold",23),ForeColor=Color.FromArgb(25,33,59)};
 readonly Label eyebrow=new(){AutoSize=true,Font=new Font("Segoe UI Semibold",9),ForeColor=Color.FromArgb(109,74,255)};
 readonly CheckBox consentBox=new(){AutoSize=true,Text="I consent to sending these samples to my configured cloud gateway.",Font=new Font("Segoe UI",9)};
 readonly Button finish=new(){Text="Create my personality",FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(109,74,255),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),Height=42};
 readonly Button minimize=new(){Text="Minimize Echo",FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(109,74,255),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),Height=42,Width=150};
 VoiceProfile? profile;bool consent;const int HOTKEY=0xE001;
 public EchoForm(){
  Text="Echo — Rewrite in My Voice";Width=720;Height=570;MinimumSize=new Size(650,500);BackColor=Color.FromArgb(251,251,255);Padding=new Padding(30);StartPosition=FormStartPosition.CenterScreen;
  var menu=new MenuStrip{BackColor=BackColor};var file=new ToolStripMenuItem("&File");file.DropDownItems.Add("E&xit",null,(_,_)=>Application.Exit());menu.Items.Add(file);MainMenuStrip=menu;Controls.Add(menu);
  eyebrow.Text="YOUR WRITING VOICE";title.Text="Sound more like yourself.";status.Text="Paste 3–10 short samples that feel like you. We’ll turn them into a writing personality — then Echo can stay out of your way.";
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,Padding=new Padding(8,44,8,8)};layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,18));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  layout.Controls.Add(eyebrow,0,0);layout.Controls.Add(title,0,1);layout.Controls.Add(status,0,3);layout.Controls.Add(samples,0,4);layout.Controls.Add(consentBox,0,5);layout.Controls.Add(finish,0,6);Controls.Add(layout);
  finish.FlatAppearance.BorderSize=0;minimize.FlatAppearance.BorderSize=0;finish.Click+=async (_,_)=>await Onboard();minimize.Click+=(_,_)=>Hide();Load+=(_,_)=>{(consent,profile)=store.Read();Render();RegisterHotKey(Handle,HOTKEY,3,(uint)Keys.E);};FormClosing+=(s,e)=>{if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
 }
 public void ShowWindow(){Show();WindowState=FormWindowState.Normal;Activate();}
 void Render(){if(profile is null)return;eyebrow.Text="PERSONALITY CREATED";title.Text="Echo is ready when you are.";status.Text=$"Minimize this window and select text anywhere. Press Ctrl + Alt + E and Echo will rewrite it in your voice.\r\n\r\nYour profile: {profile.Tone}; {profile.Formality}.";samples.Visible=false;consentBox.Visible=false;finish.Visible=false;if(!Controls.Contains(minimize))Controls.Add(minimize);minimize.Location=new Point(48,Height-105);minimize.BringToFront();}
 async Task Onboard(){var list=ParseSamples(samples.Text);if(!Validation.ValidSamples(list)){MessageBox.Show("Please provide 3 to 10 non-empty samples separated by a line containing --- .");return;}if(!consentBox.Checked){MessageBox.Show("Please confirm your consent before creating a personality.","Consent required");return;}finish.Enabled=false;finish.Text="Creating your personality…";try{using var h=new HttpClient{BaseAddress=new Uri(Environment.GetEnvironmentVariable("ECHO_GATEWAY")??"http://localhost:5000"),Timeout=TimeSpan.FromSeconds(30)};var r=await h.PostAsJsonAsync("/api/profile",new ProfileRequest(true,list));var x=await ReadResult<VoiceProfile>(r);if(x.Success!=true||x.Value is null)throw new Exception(x.Error??"Gateway unavailable");profile=x.Value;consent=true;store.Save(profile);Render();}catch(Exception ex){MessageBox.Show("Profile creation failed. No samples were stored locally.\n"+ex.Message);}finally{finish.Enabled=true;finish.Text="Create my personality";}}
 protected override void WndProc(ref Message m){if(m.Msg==0x0312&&m.WParam.ToInt32()==HOTKEY)_=RewriteSelection();base.WndProc(ref m);}
 async Task RewriteSelection(){if(!consent||profile is null){Notify("Complete onboarding first.");return;}IDataObject? original=null;try{original=Clipboard.GetDataObject();if(!await WaitForHotkeyRelease()){Notify("Release Ctrl and Alt, then try again.");return;}var sequence=GetClipboardSequenceNumber();SendKeys.SendWait("^c");var text=await WaitForCopiedText(sequence);if(string.IsNullOrWhiteSpace(text)){Notify("No supported text selection found. Select editable text, then press Ctrl+Alt+E.");return;}using var h=new HttpClient{BaseAddress=new Uri(Environment.GetEnvironmentVariable("ECHO_GATEWAY")??"http://localhost:5000"),Timeout=TimeSpan.FromSeconds(30)};var response=await h.PostAsJsonAsync("/api/rewrite",new RewriteRequest(true,text,profile));var result=await ReadResult<string>(response);if(result.Success!=true||string.IsNullOrEmpty(result.Value)){Notify(result.Error??"Rewrite failed.");return;}Clipboard.SetText(result.Value);SendKeys.SendWait("^v");await Task.Delay(100);Notify("Rewritten in your voice.");}catch{Notify("Rewrite failed; selected text was not changed.");}finally{if(original is not null)try{Clipboard.SetDataObject(original,true);}catch{}}}
 static async Task<bool> WaitForHotkeyRelease(){for(var i=0;i<50;i++){if(!IsKeyDown(Keys.ControlKey)&&!IsKeyDown(Keys.Menu))return true;await Task.Delay(20);}return false;}
 static async Task<string> WaitForCopiedText(uint previousSequence){for(var i=0;i<60;i++){await Task.Delay(25);if(GetClipboardSequenceNumber()!=previousSequence&&Clipboard.ContainsText())return Clipboard.GetText();}return "";}
 static bool IsKeyDown(Keys key)=>(GetAsyncKeyState((int)key)&0x8000)!=0;
 static string[] ParseSamples(string text){var parts=new List<string>();var current=new StringBuilder();foreach(var raw in text.Replace("\r\n","\n").Split('\n')){var line=raw.Trim();if(line.Length>=3&&line.All(c=>c=='-')){AddPart();continue;}current.AppendLine(raw);}AddPart();return parts.ToArray();void AddPart(){var sample=current.ToString().Trim();if(sample.Length>0)parts.Add(sample);current.Clear();}}
 static async Task<ApiResult<T>> ReadResult<T>(HttpResponseMessage response){var body=await response.Content.ReadAsStringAsync();if(string.IsNullOrWhiteSpace(body))return ApiResult<T>.Fail(response.IsSuccessStatusCode?"Gateway returned an empty response.":"Gateway returned no error details.");try{var result=JsonSerializer.Deserialize<ApiResult<T>>(body,new JsonSerializerOptions{PropertyNameCaseInsensitive=true});return result??ApiResult<T>.Fail("Gateway returned an unreadable response.");}catch{return ApiResult<T>.Fail(response.IsSuccessStatusCode?"Gateway returned invalid JSON.":"Gateway returned an invalid error response.");}}
 void Notify(string text){BeginInvoke(()=>status.Text=text+"\r\n"+status.Text);}
 [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr hWnd,int id,uint fsModifiers,uint vk);[DllImport("user32.dll")]static extern short GetAsyncKeyState(int vKey);[DllImport("user32.dll")]static extern uint GetClipboardSequenceNumber();[DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr hWnd,int id);
 protected override void Dispose(bool disposing){if(disposing)UnregisterHotKey(Handle,HOTKEY);base.Dispose(disposing);}
}
