using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace WcfAbstraction.ServiceModel
{
    public static class CommunicationUtility
    {
        /// <summary>
        /// Determines whether the specified address has TCP connnection.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds.</param>
        /// <returns>
        /// 	<c>true</c> if [has TCP connnection] [the specified address]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasTcpConnnection(Uri address, int timeoutInMilliseconds)
        {
            try
            {
                ManualResetEvent asyncWait = new ManualResetEvent(false);
                TcpClient tcpClient = new TcpClient();
                bool isConnected = false;

                tcpClient.BeginConnect(address.Host, address.Port,
                    state =>
                    {
                        try
                        {
                            tcpClient.EndConnect(state);

                            isConnected = tcpClient.Connected;

                            tcpClient.Close();
                        }
                        catch (SocketException) { }
                        finally
                        {
                            try
                            {
                                asyncWait.Set();
                            }
                            catch (ObjectDisposedException) { }
                        }
                    }, null);

                asyncWait.WaitOne(timeoutInMilliseconds);
                asyncWait.Close();

                return isConnected;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
