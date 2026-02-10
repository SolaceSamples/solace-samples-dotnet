
/*
 * Copyright 2010-2026 Solace Corporation. All rights reserved.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

/// <summary>
/// Solace Systems Messaging API tutorial: PayloadCompression
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// Demonstrates how to use Solace Systems Messaging API for mapping a topic to a queue
    /// </summary>
    class PayloadCompression : IDisposable
    {
        string VPNName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        string PayloadCompressionLevel { get; set; }
        const int DefaultReconnectRetries = 3;
        const int TotalMessages = 1;
        private  AutoResetEvent WaitHandle = new AutoResetEvent(false);
        private ISession session = null;

        void Run(IContext context, string host)
        {
            // Validate parameters
            if (context == null)
            {
                throw new ArgumentException("Solace Systems API context Router must be not null.", "context");
            }
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Solace Messaging Router host name must be non-empty.", "host");
            }
            if (string.IsNullOrWhiteSpace(VPNName))
            {
                throw new InvalidOperationException("VPN name must be non-empty.");
            }
            if (string.IsNullOrWhiteSpace(UserName))
            {
                throw new InvalidOperationException("Client username must be non-empty.");
            }

            // Create session properties
            SessionProperties sessionProps = new SessionProperties()
            {
                Host = host,
                VPNName = VPNName,
                UserName = UserName,
                Password = Password,
                ReconnectRetries = DefaultReconnectRetries,
                TcpNoDelay = true,
                // Set the Payload Compression Property
                PayloadCompressionLevel = int.Parse(PayloadCompressionLevel)
            };

            // Now that payload compression is enabled on the session, any message
            // published on the session with a non-empty binary attachment will be
            // automatically compressed. Any receiver that supports payload compression
            // will automatically decompress the message if it is compressed.            

            // Connect to the Solace messaging router
            Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);
            using(ISession session = context.CreateSession(sessionProps, HandleMessage, null)){
                ReturnCode returnCode = session.Connect();
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected.");
                    SubscribeAndPublishMessage(session);
                }
                else
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
            }
        }

        private void SubscribeAndPublishMessage(ISession session)
        {
            // Create the topic
            ITopic topic = ContextFactory.Instance.CreateTopic("tutorial/payloadCompression");
            
            using (IMessage message = ContextFactory.Instance.CreateMessage())
            {
                // Set the destination and create the message content as a binary attachment
                byte[] m = new byte[1024];
                message.Destination = topic;
                message.BinaryAttachment = m;

                // Subcribe to the same topic
                session.Subscribe(topic, true);

                // Publish the message to the topic on the Solace messaging router
                Console.WriteLine("Publishing message...");
                ReturnCode returnCode = session.Send(message);
                WaitHandle.WaitOne();
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                    Console.WriteLine("Done.");
                else
                    Console.WriteLine("Publishing failed, return code: {0}", returnCode);
                
                // Console.WriteLine("Payload Compression Level: " + level);
                Console.WriteLine("Original message size: " + m.Length);
                Console.WriteLine("Total Bytes got sent: " + session.GetTxStats()[Stats_Tx.TotalDataBytes]);
                Console.WriteLine("Total Bytes got received: " + session.GetRxStats()[Stats_Rx.TotalDataBytes]);
            }
        }

        /// <summary>
        /// This event handler is invoked by Solace Systems Messaging API when a message arrives
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void HandleMessage(object source, MessageEventArgs args)
        {
            Console.WriteLine("Received published message.");
            // Received a message
            using (IMessage message = args.Message)
            {
                WaitHandle.Set();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (session != null)
                    {
                        session.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Main
        static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: PayloadCompression <host> <username>@<vpnname> <password> <payloadCompressionLevel>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: PayloadCompression <host> <username>@<vpnname> <password> <payloadCompressionLevel>");
                Environment.Exit(1);
            }

            string host = args[0]; // Solace messaging router host name or IP address
            string username = split[0];
            string vpnname = split[1];
            string password = args[2];
            string payloadCompressionLevel = args[3];

            // Initialize Solace Systems Messaging API with logging to console at Warning level
            ContextFactoryProperties cfp = new ContextFactoryProperties()
            {
                SolClientLogLevel = SolLogLevel.Warning
            };
            cfp.LogToConsoleError();
            ContextFactory.Instance.Init(cfp);

            try
            {
                // Context must be created first
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                // Create the application
                using (PayloadCompression payloadCompression = new PayloadCompression()
                {
                    VPNName = vpnname,
                    UserName = username,
                    Password = password,
                    PayloadCompressionLevel = payloadCompressionLevel
                })
                {
                    // Run the application within the context and against the host
                    payloadCompression.Run(context, host);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown: {0}", ex.Message);
            }
            finally
            {
                // Dispose Solace Systems Messaging API
                ContextFactory.Instance.Cleanup();
            }
            Console.WriteLine("Finished.");
        }
        #endregion
    }

}
