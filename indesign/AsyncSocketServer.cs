/******************************************************************************
 * Copyright (C) 2016-2017 0ics srls <mail{at}0ics.it>
 * 
 * This file is part of xwcs libraries
 * xwcs libraries and all his part can not be copied 
 * and/or distributed without the express permission 
 * of 0ics srls
 *
 ******************************************************************************/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using xwcs.core.evt;

namespace xwcs.indesign
{
    public class OnMessageEventArgs : EventArgs
    {
        public OnMessageEventArgs(js.json.Message m)
        {
            Message = m;
            Result = null;
        }
        public js.json.Message Message { get; private set; }
        public object Result { get; set; }
    }

    public class AsyncSocketService : IDisposable
    {
        private static xwcs.core.manager.ILogger _logger = core.manager.SLogManager.getInstance().getClassLogger(typeof(AsyncSocketService));

        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();

        private bool _running = false;

        readonly object _gate = new object();

        private TcpListener _listener;

        private WeakEventSource<OnMessageEventArgs> _wes_OnMessage = null;
        public event EventHandler<OnMessageEventArgs> OnMessage
        {
            add
            {
                if (_wes_OnMessage == null)
                {
                    _wes_OnMessage = new WeakEventSource<OnMessageEventArgs>();
                }
                _wes_OnMessage.Subscribe(value);
            }
            remove
            {
                _wes_OnMessage?.Unsubscribe(value);
            }
        }

        private IPAddress ipAddress;
        private int port;
        public AsyncSocketService(int port)
        {
            this.port = port;
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            this.ipAddress = null;
            for (int i = 0; i < ipHostInfo.AddressList.Length; ++i)
            {
                if (ipHostInfo.AddressList[i].AddressFamily ==
                  AddressFamily.InterNetwork)
                {
                    this.ipAddress = ipHostInfo.AddressList[i];
                    break;
                }
            }
            if (this.ipAddress == null)
                throw new Exception("No IPv4 address for server");
        }

        public string Url
        {
            get
            {
                return string.Format("{0}:{1}", this.ipAddress, this.port);
            }
        }

       
        public async void RunAsync()
        {
            if (_running) return;
            _running = true;

            _listener = new TcpListener(this.ipAddress, this.port);
            _listener.Start();
            try
            {
                _logger.Debug($"Service is now running on port :{this.port}");
                while (true)
                {
                    try
                    {
                        TcpClient tcpClient = await _listener.AcceptTcpClientAsync();

                        await Process(tcpClient);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.ToString());
                    }
                }
            }
            finally
            {
                _listener.Stop();
                _logger.Debug($"Service on port :{this.port} Stopped!");
            }
        }

        

        public void Stop()
        {
            _cancelTokenSource.Cancel();
            _listener.Stop();
        }

        private async Task Process(TcpClient tcpClient)
        {
            string clientEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
            _logger.Debug($"Received connection request from {clientEndPoint}");
            try
            {
                NetworkStream networkStream = tcpClient.GetStream();

                // Buffer for reading data
                byte[] lenB = new byte[11];
                byte[] bytes = new byte[100000];
                string data = null;

                int i;

                using(_cancelTokenSource.Token.Register(() => networkStream.Close()))
                {
                    // Loop to receive all the data sent by the client.
                    bool done = false;
                    while (!done)
                    {
                        if((i = await networkStream.ReadAsync(lenB, 0, 10, _cancelTokenSource.Token)) != 10)
                        {
                            if (i == 0)
                            {
                                done = true;
                                continue;
                            }
                            else
                            {
                                throw new ApplicationException("Wrong data on socket!");
                            }
                        }
                        // now read len bytes
                        int len = int.Parse(Encoding.ASCII.GetString(lenB, 0, i));
                        if(len > bytes.Length)
                        {
                            bytes = new byte[len];
                        }
                        // now read data
                        if ((i = await networkStream.ReadAsync(bytes, 0, len, _cancelTokenSource.Token)) != len)
                        {
                            if(i == 0) {
                                done = true;
                                continue;
                            }
                            else
                            {
                                throw new ApplicationException("Wrong data on socket!");
                            }                            
                        }

                        // Translate data bytes to a ASCII string.
                        data = Encoding.ASCII.GetString(bytes, 0, i);

#if DEBUG_TRACE_LOG_ON
                    _logger.Debug($"Received: {data}");
#endif

                        OnMessageEventArgs args = new OnMessageEventArgs(JsonConvert.DeserializeObject<xwcs.indesign.js.json.Message>(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(data))));

                        _wes_OnMessage.Raise(this, args);

                        dynamic resp = new System.Dynamic.ExpandoObject();
                        resp.status = "ok";
                        if (!ReferenceEquals(args.Result, null))
                        {
                            resp.data = args.Result;
                        }
                        data = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resp)));

                        IEnumerable<byte> msg = Encoding.ASCII.GetBytes(data);
                        // write data, first size padded 10 char stringed number
                        int msglen = msg.Count();
                        string lstr = msglen.ToString("0000000000");
                        await networkStream.WriteAsync(Encoding.ASCII.GetBytes(lstr).Concat(msg).ToArray(), 0, msglen + 10);
                      
#if DEBUG_TRACE_LOG_ON
                        _logger.Debug($"Sent: [{lstr}] -> {data}");
#endif
                    }
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                if (tcpClient.Connected)
                    tcpClient.Close();
            }
        }

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                _cancelTokenSource.Cancel();
               
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AsyncSocketService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
#endregion
    }
}
