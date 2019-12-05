﻿using DTLS;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace SharpSnmpLib.DTLS
{
    public static class SecureMessageExtensions
    {
        private static IEnumerable<object> ba;

        public static ISnmpMessage GetSecureResponse(this ISnmpMessage request, int timeout, IPEndPoint receiver, Client client)
        {
            var registry = new UserRegistry();
            //if (request.Version == VersionCode.V3)
            //{
            //    registry.Add(request.Parameters.UserName, request.Privacy);
            //}

            return request.GetSecureResponse(timeout, receiver, client, registry);
        }

        /// <summary>
        /// Sends an  <see cref="ISnmpMessage"/> and handles the response from agent.
        /// </summary>
        /// <param name="request">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="receiver">Agent.</param>
        /// <param name="udpSocket">The UDP <see cref="Socket"/> to use to send/receive.</param>
        /// <param name="registry">The user registry.</param>
        /// <returns></returns>
        public static ISnmpMessage GetSecureResponse(this ISnmpMessage request, int timeout, IPEndPoint receiver, Client client, UserRegistry registry)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            var requestCode = request.TypeCode();
            if (requestCode == SnmpType.TrapV1Pdu || requestCode == SnmpType.TrapV2Pdu || requestCode == SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "not a request message: {0}", requestCode));
            }

            var bytes = request.ToBytes();
            client.ConnectToServer(receiver);

            byte[] reply = null;
            var manualReset = new ManualResetEvent(false);
            manualReset.Reset();
            client.DataReceived += (server, buffer) =>
            {
                reply = buffer;
                manualReset.Set();
            };
            client.Send(bytes);
            if (!manualReset.WaitOne(timeout))
            {
                client.Stop();
                throw new Lextm.SharpSnmpLib.Messaging.TimeoutException();
            }

            client.Stop();

            // Passing 'count' is not necessary because ParseMessages should ignore it, but it offer extra safety (and would avoid an issue if parsing >1 response).
            var response = MessageFactory.ParseMessages(reply, 0, reply.Length, registry)[0];
            var responseCode = response.TypeCode();
            if (responseCode == SnmpType.ResponsePdu || responseCode == SnmpType.ReportPdu)
            {
                var requestId = request.MessageId();
                var responseId = response.MessageId();
                if (responseId != requestId)
                {
                    throw OperationException.Create(string.Format(CultureInfo.InvariantCulture, "wrong response sequence: expected {0}, received {1}", requestId, responseId), receiver.Address);
                }

                return response;
            }

            throw OperationException.Create(string.Format(CultureInfo.InvariantCulture, "wrong response type: {0}", responseCode), receiver.Address);
        }
    }
}
