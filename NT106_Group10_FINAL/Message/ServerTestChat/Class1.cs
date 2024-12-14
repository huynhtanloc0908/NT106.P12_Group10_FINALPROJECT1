using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerTestChat
{
    internal class Class1
    {
        public class ClientInfo
        {
            public Socket Socket { get; set; }// Socket của client
            public byte[] AESKey { get; set; }// Khóa AES của client, được sử dụng để mã hóa và giải mã tin nhắn
        }
    }
}
