using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetIndustrialScale;
using NetScaleSimulator;

namespace NetScaleSimulator
{
    class Program
    {
        private static bool _hasConsole = true;

        private static void SafeClear()
        {
            if (!_hasConsole) return;
            try
            {
                Console.Clear();
            }
            catch
            {
                _hasConsole = false;
            }
        }

        private static void SafeWriteLine(string text = "", ConsoleColor? color = null)
        {
            if (!_hasConsole) return;
            try
            {
                if (color.HasValue) Console.ForegroundColor = color.Value;
                Console.WriteLine(text);
                if (color.HasValue) Console.ResetColor();
            }
            catch
            {
                _hasConsole = false;
            }
        }

        private static void SafeWrite(string text, ConsoleColor? color = null)
        {
            if (!_hasConsole) return;
            try
            {
                if (color.HasValue) Console.ForegroundColor = color.Value;
                Console.Write(text);
                if (color.HasValue) Console.ResetColor();
            }
            catch
            {
                _hasConsole = false;
            }
        }

        static void Main(string[] args)
        {
            // İNİ Dosyasından yapılandırmayı yükle (Dosya yoksa varsayılanı otomatik oluşturur)
            string configPath = "config.ini";
            ScaleConfig config = IniReader.Load(configPath);

            SafeClear();
            SafeWriteLine("==========================================================================");
            SafeWriteLine("   ENDÜSTRİYEL TERAZİ ENTEGRASYON VE SİMÜLASYON LABORATUVARI (C# 5.0)     ");
            SafeWriteLine("==========================================================================");
            SafeWriteLine(string.Format(" [İNİ Dış Yapılandırma]: {0} yüklendi.", configPath));
            SafeWriteLine(string.Format("   - IP         : {0}", config.IP));
            SafeWriteLine(string.Format("   - Port       : {0}", config.Port));
            SafeWriteLine(string.Format("   - Başlangıç  : {0} kg", config.InitialWeight));
            SafeWriteLine(string.Format("   - Kapasite   : {0} kg", config.MaxCapacity));
            SafeWriteLine(string.Format("   - Hassasiyet : {0} kg", config.Division));
            SafeWriteLine(string.Format("   - Protokol   : {0}", config.Protocol));
            SafeWriteLine("==========================================================================");

            // 1. ADIM: Yerel Sahte TCP Terazi Sunucusunu Başlat (Donanım Taklidi)
            TcpScaleServer sahteTerazi = new TcpScaleServer(config.Port);
            sahteTerazi.Division = config.Division;
            sahteTerazi.MaxCapacity = config.MaxCapacity;
            sahteTerazi.CurrentWeight = config.InitialWeight;
            sahteTerazi.IsStable = config.IsStable;
            sahteTerazi.Unit = "kg";
            sahteTerazi.ActiveProtocol = config.Protocol;
            sahteTerazi.Start();

            // 2. ADIM: Sağladığınız ScaleHandler Sınıfını Başlat (Kendi sınıfınız)
            ScaleHandler teraziDinleyici = new ScaleHandler();
            teraziDinleyici.IPAddress = config.IP;
            teraziDinleyici.Port = config.Port;

            // 3. ADIM: ScaleHandler'ın Olaylarını (Events) yakala ve ekrana yazdır (C# 5.0 / Anonim Metot Uyumluluğu)
            teraziDinleyici.OnConnectionStatusChanged += delegate(bool isConnected)
            {
                SafeWriteLine(string.Format("\n>>> [SİNYAL HABERİ] Bağlantı Durumu Değişti: {0}", isConnected ? "BAĞLANDI" : "BAĞLANTI KOPTU"), isConnected ? ConsoleColor.Green : ConsoleColor.Red);
            };

            teraziDinleyici.OnWeightChanged += delegate(ScaleData data)
            {
                string stabilityText = "Kararsız (Unstable)";
                if (data.Stability == ScaleStatus.Stable) stabilityText = "Dengede (Stable)";
                else if (data.Stability == ScaleStatus.Overload) stabilityText = "Aşırı Yük (Overload / OL)";

                string tareText = data.GrossOrNet == TareStatus.Gross ? "BRÜT (Gross)" : "NET (Net/Dara)";

                SafeWriteLine(string.Format("\n>>> [SÜRÜCÜ OLAYI] Değer Değişti: {0} {1} | Denge: {2} | Tür: {3}",
                    data.Weight, data.Unit, stabilityText, tareText), ConsoleColor.Cyan);
            };

            // Dinleyiciyi asenkron olarak başlatıp sonucu bekleyelim (.NET 4.5 Task Uyumluluğu)
            SafeWriteLine("[İstemci]: Terazi dinleyici başlatılıyor...");
            bool baslatildi = Task.Run(async () => await teraziDinleyici.StartAsync()).Result;

            if (baslatildi)
            {
                SafeWriteLine("[Sistem]: ScaleHandler başarıyla başlatıldı ve soket dinleniyor.", ConsoleColor.Green);
            }
            else
            {
                SafeWriteLine("[Hata]: Teraziye ilk bağlantı kurulamadı ama recon döngüsü devrede!", ConsoleColor.Red);
            }

            try
            {
                SafeWriteLine("\n[Arayüz]: Grafiksel Terazi Kontrol Paneli (Windows Forms) yükleniyor...", ConsoleColor.Magenta);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(sahteTerazi, teraziDinleyici));
            }
            catch (Exception)
            {
                SafeWriteLine("\n[Not]: Grafiksel ekran (Form) bu sunucu terminal ortamında desteklenmiyor. Konsol moduna düşülüyor...\n", ConsoleColor.Yellow);

                RunConsoleLoop(sahteTerazi, teraziDinleyici, config);
            }

            // Temizlik
            SafeWriteLine("\nKapatılıyor...");
            teraziDinleyici.Stop();
            sahteTerazi.Stop();
        }

        private static void RunConsoleLoop(TcpScaleServer sahteTerazi, ScaleHandler teraziDinleyici, ScaleConfig config)
        {
            // Etkileşimli Menü Döngüsü
            while (true)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("--------------------------------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" AKTİF TERAZİ DURUMU:");
                Console.WriteLine(string.Format("   * Simüle Edilen Ağırlık  : {0} {1}", sahteTerazi.CurrentWeight, sahteTerazi.Unit));
                Console.WriteLine(string.Format("   * Denge / Kararlılık     : {0}", sahteTerazi.IsOverload ? "OVERLOAD (OL - Aşırı Yük)" : (sahteTerazi.IsStable ? "STABLE (Dengede)" : "UNSTABLE (Kararsız)")));
                Console.WriteLine(string.Format("   * Net / Brüt (Dara) Modu : {0}", sahteTerazi.IsNet ? "NET (NT)" : "GROSS (GS - Brüt)"));
                Console.WriteLine(string.Format("   * Çevrimdışı Simülasyon  : {0}", sahteTerazi.IsRunning ? "YAYIN AKTİF (Soket açık)" : "YAYIN KAPALI (Soket koptu)"));
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("--------------------------------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(" [1] Yeni Ağırlık Değeri Gir ve Gönder (Örn: 42.50)");
                Console.WriteLine(" [2] Denge / Kararlılık Durumunu Değiştir (Stable / Unstable)");
                Console.WriteLine(" [3] Net / Brüt Modunu Değiştir (Gross / Net)");
                Console.WriteLine(" [4] Aşırı Yük (Overload / OL) Durumunu Değiştir (Aç / Kapat)");
                Console.WriteLine(" [5] Fiziksel Bağlantı Kopmasını Simüle Et / Bağlantıyı Geri Getir");
                Console.WriteLine(" [6] Çıkış");
                Console.Write("\n Seçiminiz (1-6) ve ENTER: ");
                Console.ResetColor();

                string secim = Console.ReadLine();
                if (secim == null) continue;
                secim = secim.Trim();

                if (secim == "6")
                {
                    break;
                }
                else if (secim == "1")
                {
                    Console.Write("\n Yeni Ağırlık Girin (kg): ");
                    string rawInput = Console.ReadLine();
                    if (string.IsNullOrEmpty(rawInput)) continue;

                    string cleanWInput = rawInput.Replace(",", ".");
                    double yeniAgirlik;
                    if (double.TryParse(cleanWInput, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out yeniAgirlik))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(string.Format("\n[Simülatör]: Yeni hedef ağırlık girildi: {0} {1}", yeniAgirlik, sahteTerazi.Unit));
                        Console.WriteLine("Yük değişimi sebebiyle dalgalanma (dip & overshoot) simülasyonu tetiklendi...");
                        Console.ResetColor();

                        sahteTerazi.CurrentWeight = yeniAgirlik;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n[Hata]: Geçersiz ağırlık değeri girişi!");
                        Console.ResetColor();
                    }
                }
                else if (secim == "2")
                {
                    sahteTerazi.IsStable = !sahteTerazi.IsStable;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(string.Format("\n[Simülatör]: Denge durumu değiştirildi -> {0}", sahteTerazi.IsStable ? "STABLE (Dengede)" : "UNSTABLE (Kararsız)"));
                    Console.ResetColor();
                }
                else if (secim == "3")
                {
                    sahteTerazi.IsNet = !sahteTerazi.IsNet;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(string.Format("\n[Simülatör]: Net/Brüt kipi değiştirildi -> {0}", sahteTerazi.IsNet ? "NET (Dara)" : "GROSS (Brüt)"));
                    Console.ResetColor();
                }
                else if (secim == "4")
                {
                    sahteTerazi.IsOverload = !sahteTerazi.IsOverload;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(string.Format("\n[Simülatör]: Aşırı yük (Overload / OL) durumu değiştirildi -> {0}", sahteTerazi.IsOverload ? "AÇIK (OL gönderiliyor)" : "KAPALI (ST/US gönderiliyor)"));
                    Console.ResetColor();
                }
                else if (secim == "5")
                {
                    if (sahteTerazi.IsRunning)
                    {
                        sahteTerazi.Stop();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n[Simülatör]: Sahte terazi sunucusu DURDURULDU! Fiziksel fişi çekilmiş gibi tüm soketler kapandı.");
                        Console.ResetColor();
                    }
                    else
                    {
                        sahteTerazi.Start();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(string.Format("\n[Simülatör]: Sahte terazi sunucusu BAŞLATILDI! Soket {0} üzerinde tekrar dinleniyor.", config.Port));
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[Sistem]: Geçersiz seçim! Lütfen 1-6 arası bir sayı girin.");
                    Console.ResetColor();
                }
            }
        }
    }
}
