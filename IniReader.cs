using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace NetScaleSimulator
{
    public class ScaleConfig
    {
        public string IP { get; set; }
        public int Port { get; set; }
        public double InitialWeight { get; set; }
        public bool IsStable { get; set; }
        public double MaxCapacity { get; set; }
        public double Division { get; set; }
        public string Protocol { get; set; }

        public ScaleConfig()
        {
            // Varsayılan güvenli başlangıç değerleri
            IP = "127.0.0.1";
            Port = 1001;
            InitialWeight = 12.45;
            IsStable = true;
            MaxCapacity = 150.0;
            Division = 0.01;
            Protocol = "CAS";
        }
    }

    public static class IniReader
    {
        public static ScaleConfig Load(string filePath)
        {
            ScaleConfig config = new ScaleConfig();
            if (!File.Exists(filePath))
            {
                CreateDefaultIni(filePath);
                return config;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    // Boş satırları, yorum satırlarını veya grup başlıklarını atla
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#") || trimmed.StartsWith("["))
                        continue;

                    int equalsIdx = trimmed.IndexOf('=');
                    if (equalsIdx > 0)
                    {
                        string key = trimmed.Substring(0, equalsIdx).Trim().ToLowerInvariant();
                        string val = trimmed.Substring(equalsIdx + 1).Trim();

                        switch (key)
                        {
                            case "ip":
                                config.IP = val;
                                break;
                            case "port":
                                int port;
                                if (int.TryParse(val, out port)) config.Port = port;
                                break;
                            case "initialweight":
                                double iw;
                                if (double.TryParse(val.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out iw))
                                    config.InitialWeight = iw;
                                break;
                            case "isstable":
                                bool isStable;
                                if (bool.TryParse(val, out isStable)) config.IsStable = isStable;
                                break;
                            case "maxcapacity":
                                double mc;
                                if (double.TryParse(val.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out mc))
                                    config.MaxCapacity = mc;
                                break;
                            case "division":
                                double div;
                                if (double.TryParse(val.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out div))
                                    config.Division = div;
                                break;
                            case "protocol":
                                config.Protocol = val.ToUpperInvariant();
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Hata]: Config.ini yüklenirken hata oluştu, varsayılanlar devrede: " + ex.Message);
                Console.ResetColor();
            }

            return config;
        }

        private static void CreateDefaultIni(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("; ==========================================================================");
                    writer.WriteLine(";   ENDÜSTRİYEL TERAZİ SİMÜLATÖRÜ CONFIGURATION FILE (INI)");
                    writer.WriteLine("; ==========================================================================");
                    writer.WriteLine("[Scale]");
                    writer.WriteLine("; Bağlantı kurulacak yerel IP");
                    writer.WriteLine("IP=127.0.0.1");
                    writer.WriteLine();
                    writer.WriteLine("; TCP Soket Dinleme Portu");
                    writer.WriteLine("Port=1001");
                    writer.WriteLine();
                    writer.WriteLine("; Başlangıçta simüle edilecek ağırlık");
                    writer.WriteLine("InitialWeight=12.45");
                    writer.WriteLine();
                    writer.WriteLine("; Başlangıç kararlılık durumu (True = Dengede, False = Sarsıntılı/US)");
                    writer.WriteLine("IsStable=True");
                    writer.WriteLine();
                    writer.WriteLine("; Terazinin maksimum ölçüm kapasitesi (Overload eşiği)");
                    writer.WriteLine("MaxCapacity=150.0");
                    writer.WriteLine();
                    writer.WriteLine("; Terazinin hassasiyeti / taksimatı (Örn: 0.001 = 1g, 0.002 = 2g, 0.005 = 5g, 0.01 = 10g)");
                    writer.WriteLine("Division=0.01");
                    writer.WriteLine();
                    writer.WriteLine("; Aktif Haberleşme Protokolü (CAS veya TOLEDO)");
                    writer.WriteLine("Protocol=CAS");
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("[Sistem]: {0} bulunamadı, varsayılan şablon ile otomatik oluşturuldu.", filePath));
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[Hata]: Varsayılan config.ini dosyası oluşturulamadı: " + ex.Message);
                Console.ResetColor();
            }
        }
    }
}
