using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetIndustrialScale
{
    public class ScaleData
    {
        public ScaleStatus Stability { get; set; }
        public TareStatus GrossOrNet { get; set; }
        public decimal Weight { get; set; }
        public string Unit { get; set; }

        public ScaleData()
        {
            Unit = "";
        }
    }

    public enum ScaleStatus
    {
        Stable,
        Unstable,
        Overload
    }

    public enum TareStatus
    {
        Gross,
        Net
    }

    /// <summary>
    /// Kendi projende kullandığın orijinal ScaleHandler sınıfının .NET 4.5 / 4.7.2 (C# 5.0) uyumlu halidir.
    /// </summary>
    public class ScaleHandler : IDisposable
    {
        public event Action<ScaleData> OnWeightChanged;
        public event Action<bool> OnConnectionStatusChanged;

        private decimal _lastFiredWeight = -1;
        private ScaleStatus _lastFiredStatus = (ScaleStatus)(-1);

        public string IPAddress { get; set; }
        public int Port { get; set; }

        // İş parçacığını yönetmek için
        private CancellationTokenSource _cts;
        private TcpClient _tcpClient;
        private Task _workerTask;

        // Thread-safe veri saklama
        private ScaleData _lastData;
        private readonly object _lockObj = new object();

        // Constructor  - C# 5.0 Uyumlu
        public ScaleHandler()
        {
            this.IPAddress = "";
            this.Port = 0;
            this._lastData = null;
        }

        public ScaleData LastData
        {
            get
            {
                lock (this._lockObj)
                {
                    return this._lastData;
                }
            }
            private set
            {
                lock (this._lockObj)
                {
                    this._lastData = value;
                }
            }
        }

        // "Başlatıldı mı?" sorusunun cevabı
        public bool IsRunning
        {
            get
            {
                return this._cts != null && !this._cts.IsCancellationRequested;
            }
        }

        // "Şu an bağlı mı?" sorusunun cevabı
        public bool IsConnected
        {
            get
            {
                TcpClient current = this._tcpClient;
                if (current == null) return false;

                try
                {
                    return current.Client != null && current.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<bool> StartAsync()
        {
            if (string.IsNullOrEmpty(this.IPAddress) || this.Port <= 0) return false;

            this.Stop(); // Temizlik

            bool initialConnectSuccess = false;
            try
            {
                this._cts = new CancellationTokenSource();
                this._tcpClient = new TcpClient();

                // Timeout kontrolü için Task yapısı
                Task connectTask = this._tcpClient.ConnectAsync(this.IPAddress, this.Port);
                Task completedTask = await Task.WhenAny(connectTask, Task.Delay(3000));

                if (completedTask == connectTask)
                {
                    // Bağlantı hatası varsa fırlatması için await
                    await connectTask;
                    initialConnectSuccess = true;
                }
                else
                {
                    // Timeout oldu veya iptal edildi
                    try { this._tcpClient.Close(); }
                    catch { }
                    this._tcpClient = null;
                }
            }
            catch (Exception)
            {
                try { if (this._tcpClient != null) this._tcpClient.Close(); }
                catch { }
                this._tcpClient = null;
            }

            // İlk bağlantı başarılı veya başarısız olsun, arka plan döngüsünü (Reconnection/Self-Healing) kesinlikle başlatıyoruz.
            // Böylece uygulama açılırken terazi kapalı dahi olsa, daha sonra açıldığı an bağlantı otomatik kurulacaktır.
            if (this._cts != null && !this._cts.IsCancellationRequested)
            {
                CancellationToken token = this._cts.Token;
                this._workerTask = Task.Run(new Action(() => this.ReadScaleLoop(token)));
            }

            return initialConnectSuccess;
        }

        // Durdurma Metodu
        public void Stop()
        {
            if (this._cts != null)
            {
                this._cts.Cancel();
            }

            try
            {
                if (this._tcpClient != null)
                {
                    this._tcpClient.Close();
                }
            }
            catch { }
        }

        private async void ReadScaleLoop(CancellationToken token)
        {
            byte[] buffer = new byte[1024];
            StringBuilder stringBuffer = new StringBuilder();

            while (!token.IsCancellationRequested)
            {
                bool hataOlustu = false;

                try
                {
                    TcpClient currentClient = this._tcpClient;
                    bool baglantiYok = false;

                    if (currentClient == null)
                    {
                        baglantiYok = true;
                    }
                    else
                    {
                        try
                        {
                            if (!currentClient.Connected)
                            {
                                baglantiYok = true;
                            }
                        }
                        catch
                        {
                            baglantiYok = true;
                        }
                    }

                    // Eğer bağlantı yoksa yeniden bağlan
                    if (baglantiYok)
                    {
                        if (currentClient != null)
                        {
                            try { currentClient.Close(); }
                            catch { }
                        }

                        this._tcpClient = new TcpClient();
                        Task connectTask = this._tcpClient.ConnectAsync(this.IPAddress, this.Port);
                        Task completedTask = await Task.WhenAny(connectTask, Task.Delay(3000));

                        if (completedTask != connectTask)
                        {
                            try { this._tcpClient.Close(); }
                            catch { }
                            throw new Exception("Timeout: Bağlantı kurulamadı.");
                        }
                        await connectTask;

                        // Bağlantı sağlandı haberi ver
                        if (this.OnConnectionStatusChanged != null)
                        {
                            this.OnConnectionStatusChanged(true);
                        }
                    }

                    if (this._tcpClient == null) continue;

                    using (NetworkStream stream = this._tcpClient.GetStream())
                    {
                        DateTime sonVeriZamani = DateTime.Now;

                        while (!token.IsCancellationRequested)
                        {
                            if (!stream.DataAvailable)
                            {
                                await Task.Delay(50, token);

                                // Fiziksel bağlantı kontrolü
                                if (!this.IsSocketConnected(this._tcpClient.Client))
                                {
                                    throw new Exception("Fiziksel bağlantı koptu.");
                                }

                                // Sessizlik kontrolü (5 sn veri gelmezse kopmuş say)
                                if ((DateTime.Now - sonVeriZamani).TotalSeconds > 5)
                                {
                                    throw new Exception("Zaman aşımı: Veri akışı kesildi.");
                                }
                                continue;
                            }

                            // Veri geldiği an zamanı güncelle
                            sonVeriZamani = DateTime.Now;

                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                            if (bytesRead == 0) throw new Exception("Bağlantı kapandı.");

                            // Veri Ayıklama
                            string chunk = Encoding.GetEncoding("ISO-8859-1").GetString(buffer, 0, bytesRead);
                            stringBuffer.Append(chunk);

                            string content = stringBuffer.ToString();
                            if (content.Contains("\n") || content.Contains("\r"))
                            {
                                string[] separators = new string[] { "\r\n", "\r", "\n" };
                                string[] lines = content.Split(separators, StringSplitOptions.None);

                                int processCount = lines.Length;
                                string remainder = "";
                                bool endsWithNewLine = content.EndsWith("\n") || content.EndsWith("\r");

                                if (!endsWithNewLine)
                                {
                                    processCount--;
                                    remainder = lines[lines.Length - 1];
                                }

                                for (int i = 0; i < processCount; i++)
                                {
                                    if (!string.IsNullOrWhiteSpace(lines[i]))
                                    {
                                        ScaleData parsed = this.ParseData(lines[i]);

                                        if (parsed != null)
                                        {
                                            this.LastData = parsed;

                                            // Sadece ağırlık veya stabilite durumu değiştiğinde event fırlat
                                            if (parsed.Weight != this._lastFiredWeight || parsed.Stability != this._lastFiredStatus)
                                            {
                                                this._lastFiredWeight = parsed.Weight;
                                                this._lastFiredStatus = parsed.Stability;
                                                if (this.OnWeightChanged != null)
                                                {
                                                    this.OnWeightChanged(parsed);
                                                }
                                            }
                                        }
                                    }
                                }
                                stringBuffer.Clear();
                                stringBuffer.Append(remainder);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    hataOlustu = true;
                    try { if (this._tcpClient != null) this._tcpClient.Close(); }
                    catch { }

                    if (this.OnConnectionStatusChanged != null)
                    {
                        this.OnConnectionStatusChanged(false);
                    }
                }

                if (hataOlustu && !token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(2000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            this.Stop();
        }

        private bool IsSocketConnected(Socket s)
        {
            try
            {
                return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
            }
            catch
            {
                return false;
            }
        }

        private ScaleData ParseData(string rawLine)
        {
            // TİP 1: 20 Karakterlik Terazi (CAS Tarzı Virgüllü Şemalar)
            if (rawLine.Length >= 18)
            {
                try
                {
                    string[] parts = rawLine.Split(',');
                    if (parts.Length >= 3)
                    {
                        ScaleData data = new ScaleData();

                        // Durum
                        if (parts[0] == "ST") data.Stability = ScaleStatus.Stable;
                        else if (parts[0] == "US") data.Stability = ScaleStatus.Unstable;
                        else if (parts[0] == "OL") data.Stability = ScaleStatus.Overload;
                        else data.Stability = ScaleStatus.Unstable;

                        // Dara
                        if (parts[1] == "GS") data.GrossOrNet = TareStatus.Gross;
                        else if (parts[1] == "NT") data.GrossOrNet = TareStatus.Net;

                        // Ağırlık ayıklama
                        string weightPart = (parts.Length > 3) ? parts[3] : parts[parts.Length - 1];
                        string[] weightInfo = weightPart.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (weightInfo.Length > 0)
                        {
                            string wStr = weightInfo[0].Replace(",", ".");
                            decimal w;
                            if (decimal.TryParse(wStr, NumberStyles.Any, CultureInfo.InvariantCulture, out w))
                            {
                                data.Weight = w;
                            }
                        }
                        if (weightInfo.Length > 1)
                        {
                            data.Unit = weightInfo[1].ToLower();
                        }

                        return data;
                    }
                }
                catch { return null; }
            }
            // TİP 2: 8 Karakterlik Basit Terazi
            else if (rawLine.Length > 2 && rawLine.Length < 15)
            {
                try
                {
                    ScaleData data = new ScaleData();
                    data.Stability = ScaleStatus.Stable;
                    data.GrossOrNet = TareStatus.Gross;

                    string cleanLine = rawLine.Replace(",", ".");
                    string numberPart = "";
                    string unitPart = "";

                    foreach (char c in cleanLine)
                    {
                        if (char.IsDigit(c) || c == '.' || c == '-' || c == '+')
                            numberPart += c.ToString();
                        else
                            unitPart += c.ToString();
                    }

                    decimal w;
                    if (decimal.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out w))
                    {
                        data.Weight = w;
                        data.Unit = unitPart.Trim().ToLower();
                        return data;
                    }
                }
                catch { return null; }
            }

            return null;
        }

        public void Dispose()
        {
            this.Stop();
        }
    }
}
