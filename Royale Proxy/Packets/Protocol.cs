using System;
using System.Linq;

namespace Royale_Proxy
{
    public class Protocol
    {
        public static void ReceiveCallback(IAsyncResult Result)
        {
            try
            {
                var state = Result.AsyncState as State;
                var socket = state.Socket;

                int bytesAvailable, bytesNeeded, bytesRead = 0, bytesReceived = socket.EndReceive(Result);

                while (bytesRead < bytesReceived)
                {
                    bytesAvailable = bytesReceived - bytesRead;

                    if (bytesReceived > 0 && state.Packet.Length >= 7)
                    {
                        var payloadLength =
                            BitConverter.ToInt32(new byte[1].Concat(state.Packet.Skip(2).Take(3)).Reverse().ToArray(),
                                0);

                        bytesNeeded = payloadLength - (state.Packet.Length - 7);
                        if (bytesAvailable >= bytesNeeded)
                        {
                            state.Packet = state.Packet.Concat(state.Buffer.Skip(bytesRead).Take(bytesNeeded))
                                .ToArray();
                            bytesRead += bytesNeeded;
                            bytesAvailable -= bytesNeeded;

                            if (state.GetType() == typeof(ClientState))
                                ClientCrypto.DecryptPacket(state as ClientState, state.Packet);
                            else if (state.GetType() == typeof(ServerState))
                                ServerCrypto.DecryptPacket(state as ServerState, state.Packet);

                            state.Packet = new byte[0];
                        }
                        else
                        {
                            state.Packet = state.Packet.Concat(state.Buffer.Skip(bytesRead).Take(bytesAvailable))
                                .ToArray();
                            bytesRead = bytesReceived;
                            bytesAvailable = 0;
                        }
                    }
                    else if (bytesAvailable >= 7)
                    {
                        state.Packet = state.Packet.Concat(state.Buffer.Skip(bytesRead).Take(7)).ToArray();
                        bytesRead += 7;
                        bytesAvailable -= 7;
                    }
                    else
                    {
                        state.Packet = state.Packet.Concat(state.Buffer.Skip(bytesRead).Take(bytesAvailable)).ToArray();
                        bytesRead = bytesReceived;
                        bytesAvailable = 0;
                    }
                }

                socket.BeginReceive(state.Buffer, 0, 2048, 0, ReceiveCallback, state);
            }
            catch (Exception)
            {
            }
        }

        public static void SendCallback(IAsyncResult Result)
        {
            try
            {
                (Result.AsyncState as State).Socket.EndSend(Result);
            }
            catch (Exception)
            {
            }
        }
    }
}