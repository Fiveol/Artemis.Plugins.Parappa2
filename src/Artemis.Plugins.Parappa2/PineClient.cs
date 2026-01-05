using System;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace Artemis.Plugins.Parappa2
{
    public class PineClient : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly ILogger _logger;

        private enum IPCCommand : byte
        {
            MsgRead8 = 0,
            MsgTitle = 0xB,
            MsgID = 0xC,
            MsgStatus = 0xF
        }

        public PineClient(ILogger logger, string host = "127.0.0.1", int port = 28011)
        {
            _logger = logger;
            _tcpClient = new TcpClient();
            _tcpClient.Connect(host, port);
            _stream = _tcpClient.GetStream();
            _logger.Information("Connected to PINE at {Host}:{Port}", host, port);
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
                // Format: [len][opcode][address]
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

            _logger.Debug("Sending {Command} ({Length} bytes): {Hex}",
                command, buffer.Length, BitConverter.ToString(buffer));

            _stream.Write(buffer, 0, buffer.Length);

            byte[] header = new byte[4];
            ReadExact(header, 0, 4);
            int replyLength = BitConverter.ToInt32(header, 0);

            if (replyLength <= 0)
                throw new Exception($"Invalid reply length {replyLength} from PINE");

            byte[] reply = new byte[replyLength];
            Buffer.BlockCopy(header, 0, reply, 0, 4);
            ReadExact(reply, 4, replyLength - 4);

            _logger.Debug("Received ({Length} bytes): {Hex}",
                reply.Length, BitConverter.ToString(reply));

            return reply;
        }

        public string GetGameId()
        {
            var reply = SendCommand(IPCCommand.MsgID);
            int size = BitConverter.ToInt32(reply, 5);
            string id = Encoding.UTF8.GetString(reply, 9, size).Trim().Trim('\0');
            _logger.Information("Parsed GameId: {GameId}", id);
            return id;
        }

        public string GetTitle()
        {
            var reply = SendCommand(IPCCommand.MsgTitle);
            int size = BitConverter.ToInt32(reply, 5);
            string title = Encoding.UTF8.GetString(reply, 9, size).Trim().Trim('\0');
            _logger.Information("Parsed Title: {Title}", title);
            return title;
        }

        public int GetStatus()
        {
            var reply = SendCommand(IPCCommand.MsgStatus);
            int status = BitConverter.ToInt32(reply, 5);
            _logger.Information("Parsed Status: {Status}", status);
            return status;
        }

        public byte Read8(uint address)
        {
            var reply = SendCommand(IPCCommand.MsgRead8, address);
            byte value = reply[5]; // first byte after header+opcode
            _logger.Information("Read8 at {Address:X}: {Value:X2}", address, value);
            return value;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _tcpClient?.Close();
            _logger.Information("Disconnected from PINE");
        }
    }
}