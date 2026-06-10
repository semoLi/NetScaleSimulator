using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetScaleSimulator
{
    /// <summary>
    /// .NET Framework 4.7.2, 4.5 ve 4.0 ile %100 uyumlu yerel sahte endüstriyel terazi sunucusu.
    /// String Interpolation ($""), expression-bodied üyeler veya ?. gibi C# 6+ özellikleri içermez.
    /// </summary>
    public class TcpScaleServer
    {
        private TcpListener _listener;
        private Thread _listenThread;
        private bool _isRunning;
        private readonly int _port;

        private double _currentDisplayWeight;
        private double _targetWeight;
        private bool _isTransitioning;
        private DateTime _transitionStartTime;
        private double _startWeight;
        private Thread _simThread;
        private readonly object _weightLock = new object();

        public double Division { get; set; }
        public double MaxCapacity { get; set; }

        private double RoundToDivision(double value, double division)
        {
            if (division <= 0) return value;
            return Math.Round(value / division) * division;
        }

        private void CheckOverload()
        {
            if (this.MaxCapacity > 0 && this._currentDisplayWeight > this.MaxCapacity)
            {
                this.IsOverload = true;
            }
            else
            {
                this.IsOverload = false;
            }
        }

        private int GetDecimalPlaces(double division)
        {
            string divStr = division.ToString(System.Globalization.CultureInfo.InvariantCulture);
            int decimalIdx = divStr.IndexOf('.');
            if (decimalIdx < 0) return 0;
            return divStr.Length - decimalIdx - 1;
        }

        // Klasik Properties (C# 3.0 desteği) ve Kapsüllenmiş Ağırlık Yönetimi
        public double CurrentWeight
        {
            get
            {
                lock (this._weightLock)
                {
                    return RoundToDivision(this._currentDisplayWeight, this.Division);
                }
            }
            set
            {
                lock (this._weightLock)
                {
                    if (Math.Abs(this._targetWeight - value) < 0.001 && !this._isTransitioning)
                    {
                        this._currentDisplayWeight = value;
                        this._targetWeight = value;
                        this.CheckOverload();
                        return;
                    }

                    this._startWeight = this._currentDisplayWeight;
                    this._targetWeight = value;
                    this._transitionStartTime = DateTime.Now;
                    this._isTransitioning = true;
                    this.IsStable = false; // Geçiş sırasında sarsıntı olur (unstable)
                    this.CheckOverload();
                }
            }
        }

        public bool IsStable { get; set; }
        public string Unit { get; set; }
        public string ActiveProtocol { get; set; }
        public bool IsOverload { get; set; }
        public bool IsNet { get; set; }

        public bool IsRunning
        {
            get { return this._isRunning; }
        }

        public TcpScaleServer(int port)
        {
            this._port = port;
            this._currentDisplayWeight = 12.45;
            this._targetWeight = 12.45;
            this.IsStable = true;
            this.Unit = "kg";
            this.ActiveProtocol = "CAS"; // "CAS" veya "TOLEDO"
            this.IsOverload = false;
            this.IsNet = false;
            this.Division = 0.01;
            this.MaxCapacity = 150.0;
        }

        /// <summary>
        /// Sunucuyu başlatır ve istemci bağlantılarını dinlemeye koyulur.
        /// </summary>
        public void Start()
        {
            if (this._isRunning) return;
            this._isRunning = true;

            this._listener = new TcpListener(IPAddress.Any, this._port);
            this._listener.Start();

            this._listenThread = new Thread(new ThreadStart(ListenForClients));
            this._listenThread.IsBackground = true;
            this._listenThread.Start();

            this._simThread = new Thread(new ThreadStart(RunSimulationLoop));
            this._simThread.IsBackground = true;
            this._simThread.Start();

            Console.WriteLine(string.Format("[Sahte Terazi Sunucusu] Başlatıldı. Port: {0} yerel soket dinleniyor...", this._port));
        }

        private void RunSimulationLoop()
        {
            Random rand = new Random();
            while (this._isRunning)
            {
                try
                {
                    lock (this._weightLock)
                    {
                        if (this._isTransitioning)
                        {
                            double elapsed = (DateTime.Now - this._transitionStartTime).TotalSeconds;
                            double totalDuration = 2.5; // Hedefe 2.5 saniyede ulaşsın

                            if (elapsed >= totalDuration)
                            {
                                this._currentDisplayWeight = this._targetWeight;
                                this._isTransitioning = false;
                                this.IsStable = true; // Hedefe varınca otomatik dengelensin
                            }
                            else
                            {
                                double t = elapsed / totalDuration;
                                double factor;

                                // "Değer girdiğimde önce az bir değer azalıp, ardından fazlalaşıp bir kaç saniye içinde verdiğim değere ulaşmalı."
                                // Dip (U-curve altı) -> Overshoot (aşma) -> Settle (oturma)
                                if (t < 0.25)
                                {
                                    // Dip safhası: 0'dan -0.15'e iner
                                    double phaseT = t / 0.25;
                                    factor = -0.15 * phaseT;
                                }
                                else if (t < 0.70)
                                {
                                    // Yükseliş ve Aşım (Overshoot) safhası: -0.15'ten +1.15'e fırlar
                                    double phaseT = (t - 0.25) / (0.70 - 0.25);
                                    factor = -0.15 + (1.15 - (-0.15)) * phaseT;
                                }
                                else
                                {
                                    // Dengelenme safhası: +1.15'ten 1.00 değerine yavaşça oturur
                                    double phaseT = (t - 0.70) / (1.00 - 0.70);
                                    factor = 1.15 + (1.00 - 1.15) * phaseT;
                                }

                                this._currentDisplayWeight = this._startWeight + (this._targetWeight - this._startWeight) * factor;

                                // Kararsız olduğundan dolayı her tick'te milimetrik gürültü ekleyelim (micro-jitter)
                                double noise = (rand.NextDouble() - 0.5) * 0.05;
                                this._currentDisplayWeight += noise;
                            }
                            this.CheckOverload();
                        }
                        else if (!this.IsStable && !this.IsOverload)
                        {
                            // "unstable durumunu seçersem, rastgele aşağı veya yukarı şekilde dalgalanmalı"
                            // Hedef ağırlık etrafında +/- 0.5 kg aralığında dalgalanma (jitter) yapalım
                            double fluctuation = (rand.NextDouble() - 0.5) * 1.0;
                            this._currentDisplayWeight = this._targetWeight + fluctuation;
                            this.CheckOverload();
                        }
                        else
                        {
                            this.CheckOverload();
                        }
                    }
                }
                catch { }

                Thread.Sleep(50); // Pürüzsüz dalgalanma hissi için 50 ms yenileme
            }
        }

        private void ListenForClients()
        {
            while (this._isRunning)
            {
                try
                {
                    TcpClient client = this._listener.AcceptTcpClient();
                    Console.WriteLine(string.Format("[Sunucu] Yeni C# istemcisi bağlandı: {0}", client.Client.RemoteEndPoint));

                    // Her bağlantı için .NET 4.0 uyumlu klasik ThreadStart ayağa kaldır
                    Thread clientThread = new Thread(new ParameterizedThreadStart(BroadcastCallback));
                    clientThread.IsBackground = true;
                    clientThread.Start(client);
                }
                catch
                {
                    break;
                }
            }
        }

        private void BroadcastCallback(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            try
            {
                using (client)
                {
                    using (NetworkStream stream = client.GetStream())
                    {
                        while (this._isRunning && client.Connected)
                        {
                            try
                            {
                                string status = "ST";
                                if (this.IsOverload)
                                {
                                    status = "OL";
                                }
                                else if (!this.IsStable)
                                {
                                    status = "US";
                                }

                                string mode = this.IsNet ? "NT" : "GS";
                                string sign = this.CurrentWeight >= 0 ? "+" : "-";
                                double absWeight = Math.Abs(this.CurrentWeight);
                                string message = "";

                                int decimals = GetDecimalPlaces(this.Division);
                                string formatStr = "F" + decimals;
                                string rawWeightStr = absWeight.ToString(formatStr, System.Globalization.CultureInfo.InvariantCulture);

                                if (this.ActiveProtocol == "CAS")
                                {
                                    // Standart CAS 22-Byte format katarı: "ST,GS,      +12.45 kg\r\n"
                                    // Ağırlık ve işareti birleştirip sola boşluk dolgusu yapıyoruz (böylece işaret boşluktan sonra gelir)
                                    string numberStr = string.Format("{0}{1}", sign, rawWeightStr);
                                    string paddedWeight = numberStr.PadLeft(12, ' ');
                                    message = string.Format("{0},{1},{2} {3}\r\n", status, mode, paddedWeight, this.Unit);
                                }
                                else
                                {
                                    // Standart Mettler Toledo format katarı (8-Byte veya 17-Byte): "S S +000012.45 kg\r\n"
                                    // Kullanıcının TİP 1 veya TİP 2'sine uygun şekilde sade format basıyoruz
                                    string numberStr = string.Format("{0}{1}", sign, rawWeightStr);
                                    message = string.Format("{0}\r\n", numberStr);
                                }

                                byte[] data = Encoding.ASCII.GetBytes(message);
                                stream.Write(data, 0, data.Length);

                                // Ölçümlü terazi yenileme aralığı (200 ms)
                                Thread.Sleep(200);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Bağlantı kesilmesi durumunda thread'in sessizce sonlanmasını sağla
            }
            Console.WriteLine("[Sunucu] İstemci ayrıldı.");
        }

        /// <summary>
        /// Sunucuyu ve dinleyici soketini durdurur.
        /// </summary>
        public void Stop()
        {
            this._isRunning = false;
            if (this._listener != null)
            {
                try
                {
                    this._listener.Stop();
                }
                catch { }
            }
            Console.WriteLine("[Sunucu] Durduruldu.");
        }
    }
}
