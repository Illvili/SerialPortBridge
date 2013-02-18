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
        static NamedPipeServerStream npipe;
        static SerialPort comport;

        static Queue<byte> data = new Queue<byte>(1024);

        static DateTime last_read_time;

        static void Main(string[] args)
        {
            comport = new SerialPort("COM1");

            Thread t_NamedPipeReadThread = new Thread(NamedPipeReadThread);
            Thread t_SerialPortWriteThread = new Thread(SerialPortWriteThread);

            t_NamedPipeReadThread.Start();
            t_SerialPortWriteThread.Start();
        }

        static void NamedPipeReadThread()
        {
            byte[] np_buffer = new byte[1024];
            int np_buffer_len = 0;

            while (true)
            {
                npipe = new NamedPipeServerStream("spb", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Console.WriteLine("Waiting for connection...");
                npipe.WaitForConnection();
                Console.WriteLine("NP connected!");

                while (true)
                {
                    while (npipe.CanRead)
                    {
                        IAsyncResult result = npipe.BeginRead(np_buffer, 0, np_buffer.Length, null, null);
                        np_buffer_len = npipe.EndRead(result);
                        if (0 == np_buffer_len) break;

                        lock (data)
                        {
                            last_read_time = DateTime.Now;

                            for (int i = 0; i < np_buffer_len; i++)
                            {
                                data.Enqueue((byte)np_buffer[i]);
                                Console.WriteLine("<<< {0:X2}", (byte)np_buffer[i]);
                            }
                        }
                    }

                    if (!npipe.IsConnected) break;
                }

                Console.WriteLine("Connection aborted!\n\n");
                npipe.Close();
            }
        }

        static void SerialPortWriteThread()
        {
            byte[] sp_buffer = new byte[1024];
            int sp_buffer_len = 0;

            while (true)
            {
                while (0 == data.Count)
                {
                    if (0 == data.Count)
                    {
                        Thread.Sleep(500);
                    }
                    else if (((DateTime.Now - last_read_time).TotalMilliseconds < 50))
                    {
                        Thread.Sleep(50);
                    }
                    else
                    {
                        break;
                    }
                }

                lock (comport)
                {
                    Console.WriteLine("Write to serial port...");
                    comport.Open();
                    comport.Write(data.ToArray(), 0, data.Count);
                    Console.WriteLine("Write {0} bytes!", data.Count);
                    data.Clear();
                }
                Console.WriteLine("Read from serial port...");
                int c;
                sp_buffer_len = 0;
                comport.ReadTimeout = 500;
                try
                {
                    while (true)
                    {
                        c = comport.ReadByte();
                        sp_buffer[sp_buffer_len++] = (byte)c;
                        Console.WriteLine(">>> {0:X2}", (byte)c);
                    }
                }
                catch (TimeoutException) { }
                comport.Close();

                // Console.Write(">>> ");
                // for (int i = 0; i < sp_buffer_len; i++)
                // {
                //     Console.Write("{0:X2} ", (byte)sp_buffer[i]);
                // }
                // Console.WriteLine();
                // 
                IAsyncResult result = npipe.BeginWrite(sp_buffer, 0, sp_buffer_len, null, null);
                npipe.EndWrite(result);
                npipe.WaitForPipeDrain();
            }
        }
    }
}
