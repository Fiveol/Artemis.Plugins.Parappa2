using System.Net.Sockets;
using System.Text;

namespace Artemis.Plugins.Parappa2
{
    public class PineClient : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;

        private enum IPCCommand : byte
        {
            MsgRead8 = 0,
            MsgID = 0xC
        }

        public PineClient(string host = "127.0.0.1", int port = 28011)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(host, port);
            _stream = _tcpClient.GetStream();
        }

        private void ReadExact(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = _stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read <= 0)
                    throw new Exception("Connection closed while reading from PINE");
                totalRead += read;
            }
        }

        private byte[] SendCommand(IPCCommand command, uint address = 0)
        {
            byte[] buffer;
            if (command == IPCCommand.MsgRead8)
            {
                buffer = new byte[9];
                BitConverter.GetBytes(9).CopyTo(buffer, 0);
                buffer[4] = (byte)command;
                BitConverter.GetBytes(address).CopyTo(buffer, 5);
            }
            else
            {
                buffer = new byte[5];
                BitConverter.GetBytes(5).CopyTo(buffer, 0);
                buffer[4] = (byte)command;
            }

            _stream.Write(buffer, 0, buffer.Length);

            byte[] header = new byte[4];
            ReadExact(header, 0, 4);
            int replyLength = BitConverter.ToInt32(header, 0);

            byte[] reply = new byte[replyLength];
            Buffer.BlockCopy(header, 0, reply, 0, 4);
            ReadExact(reply, 4, replyLength - 4);

            return reply;
        }

        public string GetGameId()
        {
            var reply = SendCommand(IPCCommand.MsgID);
            int size = BitConverter.ToInt32(reply, 5);
            return Encoding.UTF8.GetString(reply, 9, size).Trim().Trim('\0');
        }

        public byte Read8(uint address)
        {
            var reply = SendCommand(IPCCommand.MsgRead8, address);
            return reply[5];
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _tcpClient?.Close();
        }
    }
}