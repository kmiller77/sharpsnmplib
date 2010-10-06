﻿using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// A placeholder.
    /// </summary>
    internal sealed class NullMessageHandler : IMessageHandler
    {
        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="store">The object store.</param>
        /// <returns></returns>
        public ResponseData Handle(SnmpContext context, ObjectStore store)
        {
            return new ResponseData(null, ErrorCode.NoError, 0);
        }
    }
}