using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curs1
{
    internal class Transport<T>
    {
        const int bufferSize = 65536;
        const string ackText = "ACK\n";
        private static bool useStream = false;

        public static void Send(Stream stream, T v, bool ack=true)
        {
            if (useStream)
                SendStream(stream, v, ack);
            else
                SendBuf(stream, v, ack);
        }

        public static T Receive(Stream stream) {
            if (useStream)
                return ReceiveStream(stream);
            return ReceiveBuf(stream);
        }

        public static void SendBuf(Stream stream, T v, bool ack = true)
        {
            var json = JsonConvert.SerializeObject(v);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            PrintAction("Send", json);
            stream.Write(jsonBytes);
            stream.WriteByte((byte)'\n');
            stream.Flush();
            if (ack)
                ReceiveAck(stream);
        }

        public static T ReceiveBuf(Stream stream, bool ack = true)
        {
            string json;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int bytesRead;

                var end = false;
                while (!end)
                {
                    bytesRead = stream.Read(buffer, 0, bufferSize);
//                    Console.WriteLine($"Buf read({bytesRead}): {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");
                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == (byte)'\n')
                        {
                            end = true;
                            break;



                        }
                        memoryStream.WriteByte(buffer[i]);
                    }
                    if (bytesRead < 1)
                    {
                        Thread.Sleep(100);
                    }
                }
                json = Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            PrintAction("Recv", json);
            var v = JsonConvert.DeserializeObject<T>(json);
            if (ack)
                SendAck(stream);
            return v!;
        }

        private static void PrintAction(string action, string json)
        {
            if (json.Length <= 40)
            {
                Console.WriteLine($"{action}({json.Length}): '{json}'");
            }
            else
            {
                Console.WriteLine($"{action}({json.Length}): '{json.Substring(0, 20)}...{json.Substring(json.Length - 20)}'");
            }
        }

        public static void SendStream(Stream stream, T v, bool ack = true)
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);
            using var jsonWriter = new JsonTextWriter(writer);

            JsonSerializer serializer = new JsonSerializer();
            Console.WriteLine("Start writing to stream");
            serializer.Serialize(jsonWriter, v);
            Console.WriteLine("End writing to stream");
            writer.Flush();
            Console.WriteLine("Flushed");
            if (ack)
                ReceiveAck(stream);
        }

        public static T ReceiveStream(Stream stream, bool ack = true)
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            using var jsonReader = new JsonTextReader(reader);

            JsonSerializer serializer = new JsonSerializer();
            Console.WriteLine("Start reading from stream");
            var res = serializer.Deserialize<T>(jsonReader)!;
            Console.WriteLine("End reading from stream");
            if (ack)
                SendAck(stream);
            return res;
        }

        private static void SendAck(Stream stream)
        {
            Console.WriteLine("Sending ack");
            stream.Write(Encoding.UTF8.GetBytes(ackText));
            stream.Flush();
            Console.WriteLine("Ack sent");
        }

        private static void ReceiveAck(Stream stream)
        {
            byte[] buffer = new byte[ackText.Length];
            int bytesRead = 0;
            Console.WriteLine("Waiting for ack");
            while ((bytesRead = stream.Read(buffer, 0, ackText.Length)) < 1)
            {
                var recvAck = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (recvAck != ackText)
                {
                    throw new Exception($"Invalid ack: {recvAck}");
                }
            }
            Console.WriteLine("Ack received");
        }
    }
}
