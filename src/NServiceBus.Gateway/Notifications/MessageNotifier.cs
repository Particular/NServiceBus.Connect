namespace NServiceBus.Gateway.Notifications
{
    using System;
    using System.Collections.Generic;

    class MessageNotifier : IMessageNotifier
    {
        public event EventHandler<MessageReceivedOnChannelArgs> MessageForwarded = delegate { };

        void IMessageNotifier.RaiseMessageForwarded(string from, string to, byte[] messageBody, Dictionary<string, string> headers)
        {
            MessageForwarded(this, new MessageReceivedOnChannelArgs
            {
                FromChannel = from,
                ToChannel = to,
                Body = messageBody,
                Headers = headers,
            });
        }
    }
}