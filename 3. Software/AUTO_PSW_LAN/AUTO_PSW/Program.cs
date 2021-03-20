using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Sockets;
namespace Aps1102Cli
{
    class MainClass
    {
        public static PSW_Control psw_Control = new PSW_Control();
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit +=
                    new EventHandler(CurrentDomain_ProcessExit);
            //setup
            string[] portList = SerialPort.GetPortNames();
            for (int i = 0; i < portList.Length; i++)
            {
                Console.WriteLine(portList[i]);
            }

            if (File.Exists("config.json"))
            {
                {
                    string configJson = File.ReadAllText("config.json");
                    psw_Control = JsonSerializer.Deserialize<PSW_Control>(configJson);
                    Console.WriteLine(configJson);
                    psw_Control.Init();
                }
            }
            else
            {
                psw_Control = new PSW_Control();
                psw_Control.Init();
                psw_Control.Save();
            }
            //loop

            Thread PSW_Run = new Thread(psw_Control.Run)
            {
                IsBackground = true
            };
            PSW_Run.Start();

            while (true)
            {
                Console.WriteLine("Enter \"END\" to close.");
                string end = Console.ReadLine();
                if (end == "END")
                {
                    break;
                }
            }
            Console.WriteLine("Reciver \"END\". Application close.");
            Environment.Exit(Environment.ExitCode);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }
    }

    public class PSW_Control
    {
        public SerialPort SWport = new SerialPort()
        {
            BaudRate = 9600,
            Parity = Parity.None,
        };


        public string SwitchPort { get; set; } = "COM4";
        public string PSW_SocketIP { get; set; } = "172.16.5.133";
        public bool SocketRefuse = true;

        private string CommandList { get; set; } = null;

        public PSW_Control()
        {
            SWport.DataReceived += OnOffDatareciver;
        }
        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string configJson = JsonSerializer.Serialize(this, options);
            File.WriteAllText("config.json", configJson);
            Console.WriteLine("SAVE :{0}", configJson);
        }
        public void Init()
        {
            SWport.PortName = SwitchPort;
            SWport.DataReceived -= OnOffDatareciver;
            SWport.DataReceived += OnOffDatareciver;

            try
            {
                SWport.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't open {0}.", SWport.PortName);
            }
        }

        public void Run()
        {
            while (true)
            {
                if (this.SocketRefuse)
                {
                    PSW_Client_Comunicate();
                }
                if (!SWport.IsOpen)
                {
                    try
                    {
                        if (SerialPort.GetPortNames().Contains(SwitchPort))
                        {
                            SWport.Open();
                            Console.WriteLine("Switch is reconnected.");
                        }
                    }
                    catch { }
                }
            }
        }
        public void PSW_Client_Comunicate()
        {
            try
            {
                Console.WriteLine("Try connect to GWINTEK POWER SUPPLY. {0}", PSW_SocketIP);
                TcpClient client = new TcpClient()
                {
                    ReceiveTimeout = 500
                };
                // 1. connect
                IAsyncResult resultConnect = client.BeginConnect(PSW_SocketIP, 2236, null, null);
                bool success = resultConnect.AsyncWaitHandle.WaitOne(1000, true);
                //client.Connect(PSW_SocketIP, 2236);
                if (success)
                {

                    SocketRefuse = false;
                    Stream stream = client.GetStream();
                    Console.WriteLine("Connected to GWINTEK POWER SUPPLY.");
                    while (true)
                    {
                        var reader = new StreamReader(stream);
                        var writer = new StreamWriter(stream);
                        writer.AutoFlush = true;
                        string cmd = "";
                        if (CommandList != null)
                        {
                            cmd = CommandList;
                            CommandList = "OUTPut:PROTection:TRIPped?\n";
                            cmd = cmd.Replace("\r\n", "\n");
                            Console.WriteLine(cmd);
                            writer.WriteLine(cmd);
                        }
                        if (cmd.Contains("OUTPut:STATe:IMMediate OFF"))
                        {
                            CommandList = null;
                        }
                        // 2. send
                        
                        // 3. receive
                        try
                        {
                            string str = reader.ReadLine();
                            if (str != null)
                            {
                                if (cmd == "OUTPut:PROTection:TRIPped?\n" && str.Contains("1"))
                                {
                                    CommandList = "OUTPut:PROTection:CLEar\n";
                                }
                                Console.WriteLine(str);
                            }
                            if (CommandList.Contains("OUTPut:PROTection:CLEar"))
                            {
                                CommandList = "OUTPut:STATe:IMMediate OFF\n";
                            }
                        }
                        catch (Exception) {
                        }
                        Thread.Sleep(50);
                    }
                    // 4. close
                    stream.Close();
                    client.Close();
                    SocketRefuse = true;
                }
                else
                {
                    Console.WriteLine("Connect time out.");
                    SocketRefuse = true;
                }
            }
            catch
            {
                Console.WriteLine("Lost connect to GWINTEK POWER SUPPLY.");
                SocketRefuse = true;
            }
        }


        public void OnOffDatareciver(object sender, SerialDataReceivedEventArgs e)
        {
            if (SWport.IsOpen)
            {
                string data = this.SWport.ReadLine();
                Console.WriteLine("Reciver command: {0}", data);
                if (data.Contains(":POWER_ON"))
                {
                    if (!SocketRefuse)
                    {
                        CommandList = "OUTPut:STATe:IMMediate ON\n";
                    }
                }
                else if (data.Contains(":POWER_OFF"))
                {
                    if (!SocketRefuse)
                    {
                        CommandList = "OUTPut:STATe:IMMediate OFF\n";
                    }
                }
            }
            else
            {
                Console.WriteLine("Switch is disconnected.");
            }
        }
    }
}
