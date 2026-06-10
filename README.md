# NetScaleSimulator ⚖️

[![.NET Resolution](https://img.shields.io/badge/.NET-4.5%20%7C%204.7.2%20%7C%204.8-blueviolet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23%205.0%20%2F%20Legacy%20Safe-blue)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

Fiziksel bir endüstriyel teraziye (indikatör/kantar) ihtiyaç duymadan, yerel ağ veya yerel bilgisayarınız üzerinden TCP/IP soketleri aracılığıyla tartım donanımını taklit eden ve gelen verileri çözümleyen **hepsi bir arada** entegrasyon ve simülasyon çözümüdür.

Bu proje, C# 5.0 ve .NET Framework 4.5+ uyumluluğu sayesinde **Visual Studio 2012'den Visual Studio 2026'ya kadar** tüm geliştirme ortamlarında harici hiçbir kütüphane bağımlılığı olmaksızın (native) çalışır.

---

## 🔥 Temel Özellikler

- **Çift Modlu Çalışma Altyapısı**: Terminal ortamında (Sunucu CLI) otomatik olarak etkileşimli konsol menüsüne düşer; grafiksel masaüstü ortamında ise modern ve pürüzsüz bir dille tasarlanmış **Windows Forms Grafik Arabirimiyle** açılır.
- **Yerleşik TCP/IP Sunucusu ve İstemcisi**:
  - `TcpScaleServer`: Belirtilen portta (varsayılan 1001) gerçek bir donanım gibi sürekli veri akışı sağlar.
  - `ScaleHandler`: Sürekli yeniden bağlanma (Auto-reconnect) mekanizmasıyla donatılmış, asenkron (`async/await`) çalışan entegrasyon sürücüsüdür.
- **Endüstriyel Protokol Desteği**:
  - **CAS CI-2001A (22-Byte / 19-Byte)** Standart Tartım Protokolü.
  - **TOLEDO** Endüstriyel İndikatör Protokolü.
- **Gelişmiş Fiziksel Durum Simülatörleri**:
  - **Otomatik Dozajlama ve Akış Simülatörü**: Belirlenen hedef kütleye, belirlenen debi (kg/sn) ile dolum simülasyonu uygular. Akış esnasında otomatik olarak teraziyi "kararsız" konuma çeker, akış bitiminde stabiliteye ulaştırır.
  - **Kararsızlık (Jitter) Titreşimi**: Gerçek endüstriyel ortamlardaki titreşim, rüzgar ve sarsıntı faktörlerini test etmeniz için ayarlanabilir mikro-sapma filtresi sunar.
  - **Aşırı Yük (Overload - OL)** koruma ve hata kodu testi.
- **Dışsal Yapılandırma Desteği**: `config.ini` dosyası üzerinden port, limit, varsayılan ağırlık ve hassasiyet değerlerini otomatik okur ve her açılışta günceller.

---

## 🛠️ Klasör Yapısı ve Sınıflar

Proje, kargaşadan uzak, her biri tek bir amaca hizmet eden şu sınıflardan oluşur:

| Sınıf / Dosya | Görevi |
| :--- | :--- |
| **`Program.cs`** | Uygulamanın giriş noktası. Grafiksel çalışma ortamını test edip karara göre GUI veya CLI modunu başlatır. |
| **`MainForm.cs`** | Windows Forms tabanlı, canlı LED göstergeli, sarsıntı barlı ve dolum butonlu görsel kontrol paneli. |
| **`ScaleHandler.cs`** | TCP/IP üzerinden teraziyi dinleyen, veri ayrıştırıp `OnWeightChanged` olaylarını tetikleyen sürücünüz. |
| **`TcpScaleServer.cs`** | Arka planda asenkron soket açarak CAS veya Toledo formatında veri yayınlayan sahte donanım sunucusu. |
| **`IniReader.cs`** | Proje dizinindeki `config.ini` değerlerini güvenli bir şekilde işleyen yardımcı yapılandırma sınıfı. |

---

## 🚀 Başlangıç ve Visual Studio Entegrasyonu

Geliştirdiğiniz otomasyon programına bu simülatörü entegre etmek veya doğrudan çalıştırmak oldukça kolaydır:

### 1. Adım: Projeyi Bilgisayarınızda Derleme
1. Bu depoyu klonlayın veya ZIP olarak indirin.
2. Bilgisayarınızdaki **Visual Studio**'yu (herhangi bir sürüm) çalıştırın ve projenizi açın.
3. Proje Özelliklerinden (Properties) çıktı tipini isteğinize göre **Console Application** veya **Windows Application** yapabilirsiniz. `Program.cs` içindeki `SafeClear()` ve `IOxception` kontrol mekanizması sayesinde iki türlü de sorunsuz açılacaktır.

### 2. Adım: Yapılandırma (`config.ini`)
Programın çalışacağı dizinde (örneğin `bin/Debug` altında) bir `config.ini` oluşturarak varsayılan değerleri tanımlayabilirsiniz:
```ini
[Scale]
IP=127.0.0.1
Port=1001
InitialWeight=12.45
MaxCapacity=150.0
Division=0.01
Protocol=CAS
IsStable=true
