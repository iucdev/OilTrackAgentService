using NLog;
using NLog.LayoutRenderers;
using System;
using System.Text;
using System.Threading;

namespace Service.Common {
    [LayoutRenderer("CustomAppender")]
    public class CustomAppender : LayoutRenderer {
        public CustomAppender()
        {
        }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var thread = string.IsNullOrEmpty(Thread.CurrentThread.Name) ? Thread.CurrentThread.ManagedThreadId.ToString() : Thread.CurrentThread.Name;

            var text = $"{DateTime.Now.ToString()} [{thread}] {logEvent.Level.Name.ToUpper()} {logEvent.LoggerName} - {logEvent.FormattedMessage}";

            builder.Append(text);
        }
    }
}
