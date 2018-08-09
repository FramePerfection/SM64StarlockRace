using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace ServerBackend
{
    public class ClientDescription
    {
        public TcpClient client;
        public int gameID = -1;

        internal byte[] receiveBuffer = new byte[2];
        internal int receivePosition = -3;
        internal Queue<byte[]> receivedMessages = new Queue<byte[]>();
        

        public ClientDescription(TcpClient client)
        {
            this.client = client;
        }
        
        internal void ReadData(byte[] newBytes)
        {
            int readPosition = 0;
        newMessage:
            while (receivePosition < -1 && readPosition < newBytes.Length)
            {
                receiveBuffer[receivePosition + 3] = newBytes[readPosition++];
                receivePosition++;
            }
            if (receivePosition == -1)
            {
                receivePosition = 0;
                receiveBuffer = new byte[BitConverter.ToInt16(receiveBuffer, 0) + 1];
            }
            if (receivePosition >= 0)
            {
                while (receivePosition >= 0 && readPosition < newBytes.Length)
                {
                    receiveBuffer[receivePosition++] = newBytes[readPosition++];
                    if (receivePosition == receiveBuffer.Length)
                    {
                        receivePosition = -3;
                        receivedMessages.Enqueue(receiveBuffer);
                        receiveBuffer = new byte[2];
                        goto newMessage;
                    }
                }
            }
        }
    }
}
