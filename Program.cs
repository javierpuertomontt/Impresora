using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace ImpresoraPortable {
  static class Program {
    [STAThread] static void Main(){ Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
  }
  public class MainForm:Form {
    TabControl tabs=new TabControl{Dock=DockStyle.Fill};
    RichTextBox editor=new RichTextBox{Dock=DockStyle.Fill,Font=new Font("Segoe UI",14)};
    TextBox ip=new TextBox{Text="172.16.67.137",Width=125}; NumericUpDown port=new NumericUpDown{Minimum=1,Maximum=65535,Value=9100,Width=70};
    TextBox code=new TextBox{MaxLength=12,Font=new Font("Segoe UI",18),Width=220}; PictureBox preview=new PictureBox{Width=560,Height=230,SizeMode=PictureBoxSizeMode.CenterImage,BorderStyle=BorderStyle.FixedSingle};
    Bitmap barcode;
    public MainForm(){Text="Impresora";Width=720;Height=570;StartPosition=FormStartPosition.CenterScreen;
      var top=new FlowLayoutPanel{Dock=DockStyle.Top,Height=38}; top.Controls.AddRange(new Control[]{new Label{Text="Impresora:",AutoSize=true,Padding=new Padding(0,8,0,0)},ip,new Label{Text="Puerto:",AutoSize=true,Padding=new Padding(0,8,0,0)},port,Btn("Probar conexión",Test)});
      Controls.Add(tabs);Controls.Add(top); TextTab(); BarcodeTab();
    }
    Button Btn(string t,EventHandler e){var b=new Button{Text=t,AutoSize=true};b.Click+=e;return b;}
    void TextTab(){var p=new TabPage("Texto");var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42};
      bar.Controls.AddRange(new Control[]{Btn("B",(s,e)=>Style(FontStyle.Bold)),Btn("I",(s,e)=>Style(FontStyle.Italic)),Btn("U",(s,e)=>Style(FontStyle.Underline)),Btn("A-",(s,e)=>SizeSel(-2)),Btn("A+",(s,e)=>SizeSel(2)),Btn("Izquierda",(s,e)=>Align(HorizontalAlignment.Left)),Btn("Centro",(s,e)=>Align(HorizontalAlignment.Center)),Btn("Derecha",(s,e)=>Align(HorizontalAlignment.Right)),Btn("IMPRIMIR",(s,e)=>PrintText())});
      p.Controls.Add(editor);p.Controls.Add(bar);tabs.TabPages.Add(p);}
    void BarcodeTab(){var p=new TabPage("Código de barras");var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(18),WrapContents=false};
      panel.Controls.Add(new Label{Text="Código UPC-A (12 dígitos)",AutoSize=true});panel.Controls.Add(code);panel.Controls.Add(Btn("Generar",(s,e)=>Generate()));panel.Controls.Add(preview);panel.Controls.Add(Btn("IMPRIMIR CÓDIGO",(s,e)=>PrintBarcode()));p.Controls.Add(panel);tabs.TabPages.Add(p);}
    void Style(FontStyle f){if(editor.SelectionLength==0)return;var x=editor.SelectionFont??editor.Font;editor.SelectionFont=new Font(x,x.Style^f);}
    void SizeSel(float d){if(editor.SelectionLength==0)return;var x=editor.SelectionFont??editor.Font;editor.SelectionFont=new Font(x,Math.Max(6,x.Size+d),x.Style);}
    void Align(HorizontalAlignment a){editor.SelectionAlignment=a;}
    void Test(object s,EventArgs e){try{using(var c=new TcpClient()){var r=c.BeginConnect(ip.Text,(int)port.Value,null,null);if(!r.AsyncWaitHandle.WaitOne(1500))throw new Exception("Sin respuesta");c.EndConnect(r);}MessageBox.Show("Conexión correcta. No se imprimió ni modificó nada.");}catch(Exception ex){MessageBox.Show("No se pudo conectar: "+ex.Message);}}
    void Send(byte[] data){using(var c=new TcpClient()){c.Connect(ip.Text,(int)port.Value);using(var st=c.GetStream())st.Write(data,0,data.Length);}}
    void PrintText(){try{var enc=Encoding.GetEncoding(437);var bytes=enc.GetBytes(editor.Text+"\n\n\n");Send(bytes);MessageBox.Show("Enviado a la impresora.");}catch(Exception ex){MessageBox.Show(ex.Message);}}
    bool Valid(){if(code.Text.Length!=12||!long.TryParse(code.Text,out _)){MessageBox.Show("Ingresa exactamente 12 dígitos.");return false;}int sum=0;for(int i=0;i<11;i++)sum+=(code.Text[i]-'0')*(i%2==0?3:1);int check=(10-(sum%10))%10;if(check!=code.Text[11]-'0'){MessageBox.Show("El dígito verificador UPC-A no es válido.");return false;}return true;}
    void Generate(){if(!Valid())return; barcode=UPC(code.Text,520,210);preview.Image=barcode;}
    Bitmap UPC(string s,int w,int h){string[] L={"0001101","0011001","0010011","0111101","0100011","0110001","0101111","0111011","0110111","0001011"},R={"1110010","1100110","1101100","1000010","1011100","1001110","1010000","1000100","1001000","1110100"};string bits="101";for(int i=0;i<6;i++)bits+=L[s[i]-'0'];bits+="01010";for(int i=6;i<12;i++)bits+=R[s[i]-'0'];bits+="101";var b=new Bitmap(w,h);using(var g=Graphics.FromImage(b)){g.Clear(Color.White);int module=Math.Max(2,(w-40)/95),x=(w-module*95)/2;for(int i=0;i<bits.Length;i++)if(bits[i]=='1')g.FillRectangle(Brushes.Black,x+i*module,8,module,150);using(var f=new Font("Arial",18)) {var z=g.MeasureString(s,f);g.DrawString(s,f,Brushes.Black,(w-z.Width)/2,165);}}return b;}
    byte[] Raster(Bitmap src){int width=src.Width, wb=(width+7)/8, height=src.Height;var data=new byte[8+wb*height+3];int k=0;data[k++]=0x1D;data[k++]=0x76;data[k++]=0x30;data[k++]=0;data[k++]=(byte)(wb&255);data[k++]=(byte)(wb>>8);data[k++]=(byte)(height&255);data[k++]=(byte)(height>>8);for(int y=0;y<height;y++)for(int xb=0;xb<wb;xb++){byte v=0;for(int bit=0;bit<8;bit++){int x=xb*8+bit;if(x<width&&src.GetPixel(x,y).GetBrightness()<0.5)v|=(byte)(0x80>>bit);}data[k++]=v;}data[k++]=10;data[k++]=10;data[k++]=10;return data;}
    void PrintBarcode(){if(barcode==null)Generate();if(barcode==null)return;try{Send(Raster(barcode));MessageBox.Show("Código enviado a la impresora.");}catch(Exception ex){MessageBox.Show(ex.Message);}}
  }
}