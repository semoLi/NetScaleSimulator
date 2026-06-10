# NetScaleSimulator ⚖️

[![.NET Resolution](https://img.shields.io/badge/.NET-4.5%20%7C%204.7.2%20%7C%204.8%20%7C%208.0%20%2B-blueviolet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%205.0%20%2F%20Legacy%20Safe-blue)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

Fiziksel bir endüstriyel teraziye (indikatör/kantar) ihtiyaç duymadan, yerel ağ veya yerel bilgisayarınız üzerinden TCP/IP soketleri aracılığıyla tartım donanımını taklit eden ve gelen verileri çözümleyen **hepsi bir arada** entegrasyon ve simülasyon çözümüdür.

Bu proje, C# 5.0 ve .NET Framework 4.5+ uyumluluğu sayesinde **Visual Studio 2012'den Visual Studio 2026'ya kadar** (ve .NET Core / .NET 5/6/7/8/9 ortamlarında) tüm geliştirme ortamlarında harici hiçbir üçüncü parti kütüphane bağımlılığı olmaksızın (tamamen native) çalışır.

---

## 🔥 Temel Özellikler

- **Çift Modlu Çalışma Altyapısı**: Grafiksel ekranı olmayan sunucu terminal ortamlarında (CLI) otomatik olarak etkileşimli konsol menüsüne düşer; grafiksel masaüstü ortamında ise modern ve pürüzsüz bir dille tasarlanmış **Windows Forms Grafik Arabirimiyle** açılır.
- **Yerleşik TCP/IP Sunucusu ve İstemcisi**:
  - `TcpScaleServer`: Belirtilen portta (varsayılan 1001) gerçek bir donanım gibi sürekli veri akışı sağlayan sanal aygıttır.
  - `ScaleHandler`: Sürekli yeniden bağlanma (Auto-reconnect / Self-healing) mekanizmasıyla donatılmış, asenkron (`async/await`) çalışan entegrasyon sürücüsüdür.
- **Endüstriyel Protokol Desteği**:
  - **CAS CI-2001A (22-Byte)** Standart Tartım Protokolü (`ST,GS,      +12.45 kg\r\n`).
  - **TOLEDO** Endüstriyel İndikatör Protokolü (`+12.45\r\n`).
- **Gelişmiş Fiziksel Durum Simülatörleri**:
  - **Otomatik Dozajlama ve Akış Simülatörü**: Belirlenen hedef kütleye, belirlenen debi (kg/sn) ile dolum simülasyonu uygular. Akış esnasında teraziyi otomatik olarak "kararsız (unstable)" konuma çeker, dolum bittiğinde ise aslına uygun olarak hafifçe aşarak ("overshoot") ardından dengeler.
  - **Kararsızlık (Jitter) Titreşimi**: Gerçek endüstriyel sahalardaki rüzgar, sarsıntı ve bant hareketlerini test etmek amacıyla ayarlanabilir mikro-sapma gürültüsü sunar.
  - **Aşırı Yük (Overload - OL)** koruma sınır testi.
- **Dışsal Yapılandırma Desteği**: `config.ini` dosyası üzerinden port, limit, varsayılan ağırlık ve hassasiyet değerlerini otomatik okur ve her açılışta günceller.

---

## 🛠️ Klasör Yapısı ve Düzen

Proje, temiz ve modüler bir organizasyon için dosyaları ayrıştırır. Sürücü ve kütüphane dosyaları `src/` klasöründe yer alırken, çalıştırılabilir Windows uygulaması `NetScaleSimulator.App/` dizininde konumlandırılmıştır:

```text
csharp/
├── LICENSE
├── README.md
├── src/                         # Paylaşılabilir Çekirdek Sınıflar (Kütüphane)
│   ├── IniReader.cs             # config.ini yapılandırma okuyucusu
│   ├── ScaleHandler.cs          # Sizin programınıza eklenecek Entegrasyon Sürücüsü
│   └── TcpScaleServer.cs        # Donanımı taklit eden Sahte TCP/IP Sunucusu
└── NetScaleSimulator.App/       # Windows Forms & Konsol Test Uygulaması
    ├── NetScaleSimulator.csproj # .NET Proje Dosyası (src/ altındaki sınıflara referans içerir)
    ├── Program.cs               # Giriş noktası (GUI / CLI algılama mekanizması)
    ├── MainForm.cs              # Grafik ekran kodları
    └── MainForm.Designer.cs     # Grafik ekran tasarım nesneleri
```

`.csproj` proje dosyası, `src/` klasöründeki ortak dosyaları bağımsız halde tutmak ve her yere kolayca taşınabilmesini sağlamak amacıyla bağımlılıkları şu şekilde dinamik olarak üst dizinden derler:
```xml
<ItemGroup>
  <Compile Include="..\src\IniReader.cs" />
  <Compile Include="..\src\ScaleHandler.cs" />
  <Compile Include="..\src\TcpScaleServer.cs" />
  <Compile Include="MainForm.cs" />
  ...
</ItemGroup>
```

---

## 🚀 Kendi Programınıza Nasıl Entegre Edersiniz?

NetScaleSimulator'ın temel amacı, geliştirdiğiniz **fabrika otomasyonu, kantar otomasyonu veya ERP/MES yazılımlarına** terazileri sıfır hata ile entegre etmenizi sağlamaktır.

Entegrasyonu gerçekleştirmek için sadece **`ScaleHandler.cs`** sınıfını kendi projenize dahil etmeniz yeterlidir. Sürücü sınıfı tamamen asenkron, olay (event) tabanlı ve iş parçacığı güvenlidir (Thread-safe).

### Entegrasyon Adımları:

1. **`ScaleHandler.cs`** dosyasını kopyalayın ve kendi projenizin içerisine yapıştırın. (Namespace adını dilerseniz projenize göre güncelleyebilirsiniz).
2. Sınıfı asenkron olarak ayağa kaldırın ve `OnWeightChanged` ile `OnConnectionStatusChanged` olaylarına abone olun.

### 🟢 Pratik Kod Örneği (Konsol veya Servis Uygulamaları İçin):

Aşağıdaki kod parçası, kendi uygulamanızın giriş noktasında veya teraziyi yönetecek ana sınıfınızda kullanabileceğiniz en sade, kararlı ve test edilmiş örnektir:

```csharp
using System;
using System.Threading.Tasks;
using NetIndustrialScale; // ScaleHandler'ın namespace'i

namespace KendiOtomasyonProjeniz
{
    class Program
    {
        private static ScaleHandler _scaleDriver;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Otomasyon Sistemi Başlatıldı. Teraziye bağlanılıyor...");

            // 1. Sürücüyü oluşturun ve IP / Port tanımlayın
            _scaleDriver = new ScaleHandler();
            _scaleDriver.IPAddress = "127.0.0.1"; // Varsa gerçek terazinin IP adresi veya localhost simülatör
            _scaleDriver.Port = 1001;              // Terazi portu

            // 2. Olaylara (Events) abone olun
            _scaleDriver.OnConnectionStatusChanged += Scale_OnConnectionStatusChanged;
            _scaleDriver.OnWeightChanged += Scale_OnWeightChanged;

            // 3. Sürücüyü asenkron olarak dinlemeye başlatın
            // (StartAsync bağlantı denemesini yapar, başarısız olsa dahi arka planda otomatik yeniden bağlanma döngüsünü sürdürür)
            bool ilkBaglantiBasarili = await _scaleDriver.StartAsync();

            if (ilkBaglantiBasarili)
            {
                Console.WriteLine("Terazi donanımıyla ilk TCP/IP bağlantısı başarıyla kuruldu.");
            }
            else
            {
                Console.WriteLine("Terazi şu an kapalı veya ulaşılamıyor. Endişelenmeyin, bağlandığı an otomatik canlı akış başlayacaktır (Auto-Reconnect).");
            }

            Console.WriteLine("İzlemeyi durdurmak ve çıkış yapmak için ENTER tuşuna basın.");
            Console.ReadLine();

            // 4. Uygulama kapatılırken bağlantıyı güvenli bir şekilde kapatın
            _scaleDriver.Stop();
        }

        // Terazi Ağırlığı veya Stabilite durumu her değiştiğinde bu olay tetiklenir
        private static void Scale_OnWeightChanged(ScaleData data)
        {
            string kararlilikDurumu = "Kararsız/Dalgalanıyor";
            if (data.Stability == ScaleStatus.Stable) kararlilikDurumu = "Dengede (Kararlı)";
            else if (data.Stability == ScaleStatus.Overload) kararlilikDurumu = "⚠️ AŞIRI YÜK LİMİT AŞILDI (O.L)";

            string tartimTipi = data.GrossOrNet == TareStatus.Gross ? "Brüt" : "Net";

            // Sadece kararlı ağırlığı işlemek veya veri tabanına kaydetmek istiyorsanız:
            if (data.Stability == ScaleStatus.Stable)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("[KANTAR]: Kararlı Ölçüm Alındı: {0} {1} ({2})", data.Weight, data.Unit, tartimTipi));
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("[Sinyal]: Ağırlık Değişimi Var -> {0} {1} ({2})", data.Weight, data.Unit, kararlilikDurumu));
                Console.ResetColor();
            }
        }

        // Cihazın elektrik kesintisi, kablo kopması veya geri açılması durumlarında tetiklenir
        private static void Scale_OnConnectionStatusChanged(bool isConnected)
        {
            if (isConnected)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(">>> [DONANIM]: Terazi donanımı ile iletişim tekrar aktifleştirildi!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(">>> [HATA / SİNYAL KOPTU]: Terazi fiziksel bağlantısı koptu! Sistem otomatik kurtarma modunda (Saniyede bir yeniden deniyor)...");
                Console.ResetColor();
            }
        }
    }
}
```

### 🔵 Windows Forms Uygulamalarınızda Kullanım İpucu:

UI nesnelerini (TextBox, Label vb.) başka bir thread'den gelen `Scale_OnWeightChanged` ile güncellerken kilitlenme (Cross-thread Exception) yaşamamak için Windows Forms içerisinde `Invoke` kullanmayı unutmayın:

```csharp
private void Scale_OnWeightChanged(ScaleData data)
{
    if (this.InvokeRequired)
    {
        this.BeginInvoke(new Action<ScaleData>(Scale_OnWeightChanged), data);
        return;
    }
    
    // UI nesnelerini güvenle güncelleyin
    lblWeight.Text = data.Weight.ToString("F2") + " " + data.Unit;
    ledIndicator.BackColor = data.Stability == ScaleStatus.Stable ? Color.Green : Color.Red;
}
```

---

## 🛠️ Nasıl Çalıştırılır ve Test Edilir?

### 1. Adım: Projeyi Açın
- `NetScaleSimulator.csproj` dosyasını çift tıklayarak **Visual Studio** üzerinde açın.

### 2. Adım: Yapılandırın (`config.ini`)
Programın çalışacağı derleme dizininde (`bin/Debug` veya `bin/Release` altında) bir `config.ini` dosyası yoksa program bunu ilk açılışta otomatik oluşturur. Değerleri değiştirebilirsiniz:
```ini
[Scale]
IP=127.0.0.1
Port=1001
InitialWeight=12.45
MaxCapacity=150.0
Division=0.01
Protocol=CAS
IsStable=True
```

### 3. Adım: Çalıştırın
- Projeyi derleyip **F5** (Start) tuşuna basın.
- Grafiksel arayüz açıldığında **Dozajlama/Dolum** veya sürgülü **Sarsıntı/Titreşim** ayarını değiştirerek sunucunun (terazi) anlık veri yayınlarını değiştirebilir, bu sırada pencerenin alt tarafında konumlanmış `ScaleHandler` olay izleme logs bölümünden entegrasyon sürücünüzün gelen verileri nasıl kesintisiz ve hatasız ayrıştırıp çözdüğünü canlı olarak deneyimleyebilirsiniz!
