using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.IO.Ports;
using System.Linq;
using System.Management;
namespace Aps1102Cli
{
    class MainClass
    {
        public static Auto_power auto_Power;
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit +=
                    new EventHandler(CurrentDomain_ProcessExit);
            //setup
            Console.WriteLine("--------Daeyoung Electronics Vina--------");
            Console.WriteLine("--------Auto Power VR9500 - V 1.1--------");

            Console.WriteLine("\n------------Serial Portlist--------------");
            string[] portList = SerialPort.GetPortNames();
            for (int i = 0; i < portList.Length; i++)
            {
                Console.WriteLine(portList[i]);
            }
            Console.WriteLine("\n------------Serial Port Config--------------");
            if (File.Exists("config.json"))
            {
                {
                    string configJson = File.ReadAllText("config.json");
                    auto_Power = JsonSerializer.Deserialize<Auto_power>(configJson);
                    Console.WriteLine(configJson);
                    auto_Power.Init();
                }
            }
            else
            {
                auto_Power = new Auto_power();
                auto_Power.Save();
            }
            Console.WriteLine("\n---------------Running-----------------");
            //loop
            while (true)
            {
                auto_Power.connect_check();
                auto_Power.checkTrip();
                Thread.Sleep(500);
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            auto_Power.Close();
        }
    }


    public class Auto_power
    {
        public SerialPort SWport = new SerialPort()
        {
            BaudRate = 9600,
            Parity = Parity.None,
        };
        public SerialPort PSWport = new SerialPort()
        {
            BaudRate = 9600,
            Parity = Parity.None,
        };

        public string SW_COM { get; set; } = "COM3";
        public string PSW_COM { get; set; } = "COM7";
        public Auto_power()
        {
            SWport.DataReceived += OnOffDatareciver;
            PSWport.DataReceived += TripdataReciver;
        }
        public void Init()
        {
            SWport.PortName = SW_COM;
            PSWport.PortName = PSW_COM;
            SWport.DataReceived -= OnOffDatareciver;
            SWport.DataReceived += OnOffDatareciver;

            PSWport.DataReceived -= TripdataReciver;
            PSWport.DataReceived += TripdataReciver;

            try
            {
                PSWport.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't open {0}.", PSWport.PortName);
            }
            try
            {
                SWport.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't open {0}.", SWport.PortName);
            }
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

        public void connect_check()
        {
            string[] portList = SerialPort.GetPortNames();
            if (!SWport.IsOpen)
            {
                Console.WriteLine("SW port disconected.");
                if (portList.Contains(SW_COM))
                {
                    try
                    {
                        SWport.Dispose();
                        SWport = new SerialPort()
                        {
                            PortName = SW_COM,
                            BaudRate = 9600,
                            Parity = Parity.None,
                        };
                        SWport.DataReceived -= OnOffDatareciver;
                        SWport.DataReceived += OnOffDatareciver;
                        SWport.Open();
                        Console.WriteLine("SW port reconected.");
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("SW Port: {0}", err.Message);
                    }
                }
            }

            if (!PSWport.IsOpen)
            {
                Console.WriteLine("PSW port disconected.");
                if (portList.Contains(PSW_COM))
                {
                    try
                    {
                        PSWport.Dispose();
                        PSWport = new SerialPort()
                        {
                            PortName = PSW_COM,
                            BaudRate = 9600,
                            Parity = Parity.None,
                        };
                        PSWport.Open();
                        PSWport.DataReceived -= TripdataReciver;
                        PSWport.DataReceived += TripdataReciver;
                        PSWport.Write("OUTPut:STATe:IMMediate OFF\r\n");
                        Console.WriteLine("PSW port reconected.");
                    }
                    catch (Exception err){
                        Console.WriteLine("PSW Port: {0}", err.Message);
                    }
                }
            }
            else
            {
                PSWport.Write("OUTPut:PROTection:TRIPped?\r\n");
            }
        }
        public void Close()
        {
            this.Save();
            if (SWport.IsOpen)
            {
                SWport.Close();
            }
            if (PSWport.IsOpen)
            {
                PSWport.Close();
            }
        }

        public void checkTrip()
        {
            try
            {
                PSWport.Write("OUTPut:PROTection:TRIPped?\r\n");
            }
            catch { }
        }

        public void OnOffDatareciver(object sender, SerialDataReceivedEventArgs e)
        {
            if (SWport.IsOpen)
            {
                string data = this.SWport.ReadLine();
                Console.WriteLine("Reciver a command: {0}", data);
                if (data.Contains(":POWER_ON"))
                {
                    if (PSWport.IsOpen)
                    {
                        try
                        {
                            PSWport.Write("OUTPut:STATe:IMMediate ON \r\n");
                            Console.WriteLine("OUTPut:STATe:IMMediate ON\r\n");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("PSW port disconnected.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("PSW port disconnected.");
                    }
                }
                else if (data.Contains(":POWER_OFF"))
                {
                    if (PSWport.IsOpen)
                    {
                        try
                        {
                            PSWport.Write("OUTPut:STATe:IMMediate OFF\r\n");
                            Console.WriteLine("OUTPut:STATe:IMMediate OFF\r\n");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("PSW port disconnected.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("PSW port disconnected.");
                    }
                }
            }
        }

        public void TripdataReciver(object sender, SerialDataReceivedEventArgs e)
        {
            if (PSWport.IsOpen)
            {
                string dataTrip = PSWport.ReadLine();
                if (dataTrip.Contains("1"))
                {
                    PSWport.Write("OUTPut:PROTection:CLEar\r\n");
                    Thread.Sleep(100);
                    PSWport.Write("OUTPut:STATe:IMMediate OFF\r\n");
                }
            }
        }
    }
}
