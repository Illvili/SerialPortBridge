using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Threading;

namespace SerialPortBridge
{
    class Program
    {
        static void Main(string[] args)
        {
            SerialPort comport = new SerialPort("COM1");

            byte[] buffer = new byte[1024];
            int buffer_len = 0;
            int buffer_read = 0;
            int buffer_read_times = 0;

            while (true)
            {
                NamedPipeServerStream npipe = new NamedPipeServerStream("spb", PipeDirection.InOut);
                Console.Write("Waiting for connection...");
                npipe.WaitForConnection();
                Console.WriteLine("Ok");

                while (true)
                {
                    while (npipe.CanRead)
                    {
                        buffer_len = 0;
                        buffer_read = 0;
                        buffer_read_times = 0;
                        Console.WriteLine("Waiting for data...");

                        //do
                        //{
                        //    buffer_read = npipe.Read(buffer, buffer_len, buffer.Length - buffer_len);
                        //    buffer_len += buffer_read;
                        //} while (0 != buffer_read);
                        //if (0 == buffer_len) break;

                        buffer_len = npipe.Read(buffer, 0, buffer.Length);
                        if (0 == buffer_len) break;

                        Console.Write("<<< ");
                        for (int i = 0; i < buffer_len; i++)
                        {
                            Console.Write(((byte)buffer[i]).ToString("X2"));
                        }

                        comport.Open();
                        comport.Write(buffer, 0, buffer_len);
                        Console.WriteLine();

                        int c;
                        buffer_len = 0;
                        comport.ReadTimeout = 10;
                        Console.Write(">>> ");
                        try
                        {
                            while (true)
                            {
                                c = comport.ReadByte();
                                buffer[buffer_len++] = (byte)c;
                                Console.Write(c.ToString("X2"));
                            }
                        }
                        catch (TimeoutException) { }

                        npipe.Write(buffer, 0, buffer_len);
                        Console.WriteLine("\n");
                        comport.Close();

                        npipe.WaitForPipeDrain();
                    }
                    if (!npipe.IsConnected) break;
                }
                Console.WriteLine("Connection aborted!\n\n");
                npipe.Close();
            }
        }
    }
}
