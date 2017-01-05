// Copyright (c) 2014-16 Mark Pearce
// http://opensource.org/licenses/MIT

using System;
using System.Diagnostics;
using System.Threading;
using System.Configuration;
using System.Globalization;

namespace SleekSoftware
{
    public sealed class Log : IDisposable
    {
        // Event log properties stored in APP.CONFIG.
        private const string CONFIG_EVENT_SOURCE_NAME = "EventSource";
        private const string CONFIG_EVENT_LOG_NAME = "EventLog";

        private const string THREADPOOL_THREAD = "Threadpool";
        private const string BACKGROUND_THREAD = "Background";
        private const string UNKNOWN_THREAD = "Unknown";
        private const Int32 CALLING_METHOD_DEPTH = 1;

        /// <summary>
        /// Has this class been disposed already?
        /// </summary>
        public bool Disposed { get; private set; }

        // Used to log application messages to custom event log.
        private System.Diagnostics.EventLog AppEventLog { get; set; }
        private static readonly object LockEventLog = new object();

        // Store the time of the last event write.
        private DateTime LastEventWriteTime { get; set; }

        // Note that because the EventLog timestmap only has a granularity of 1 second, 
        // we increment this property where an event occurs in the same second as the 
        // previous event.
        // This makes it easier to see the real order of events in the Windows event log. 
        private Int32 EventOrder { get; set; }

        // Constructor - setup logging.
        public Log()
        {
            this.AlreadyDisposedCheck(); 
            this.AppEventLog = new EventLog(ConfigurationManager.AppSettings[CONFIG_EVENT_LOG_NAME], ".", ConfigurationManager.AppSettings[CONFIG_EVENT_SOURCE_NAME]);
        }

        // Write information message to event log, guessing at calling method name.
        public void Information(string message)
        {
            this.AlreadyDisposedCheck();

            // This isn't reliable, just a best guess at the calling method name.
            // StackTrace contains the return address, which may not be the caller address.
            // http://stackoverflow.com/a/15368508/13118
            // https://msdn.microsoft.com/en-us/magazine/jj891052.aspx
            StackTrace stackTrace = new StackTrace();
            string methodName = stackTrace.GetFrame(CALLING_METHOD_DEPTH).GetMethod().Name;

            this.LogMessage(message, EventLogEntryType.Information, methodName);
        }

        // Write information message to event log.
        public void Information(string message, string methodName)
        {
            this.AlreadyDisposedCheck();
            this.LogMessage(message, EventLogEntryType.Information, methodName);
        }

        // Write warning message to event log, guessing at calling method name.
        public void Warning(string message)
        {
            this.AlreadyDisposedCheck();

            // This isn't reliable, just a best guess at the calling method name.
            StackTrace stackTrace = new StackTrace();
            string methodName = stackTrace.GetFrame(CALLING_METHOD_DEPTH).GetMethod().Name;

            this.LogMessage(message, EventLogEntryType.Warning, methodName);
        }

        // Write warning message to event log.
        public void Warning(string message, string methodName)
        {
            this.AlreadyDisposedCheck();
            this.LogMessage(message, EventLogEntryType.Warning, methodName);
        }

        // Write error message to event log, guessing at calling method name.
        public void Error(string message)
        {
            this.AlreadyDisposedCheck();

            // This isn't reliable, just a best guess at the calling method name.
            StackTrace stackTrace = new StackTrace();
            string methodName = stackTrace.GetFrame(CALLING_METHOD_DEPTH).GetMethod().Name;

            this.LogMessage(message, EventLogEntryType.Error, methodName);
        }

        // Write error message to event log.
        public void Error(string message, string methodName)
        {
            this.AlreadyDisposedCheck();
            this.LogMessage(message, EventLogEntryType.Error, methodName);
        }

        // Write message to event log.
        private void LogMessage(string message, EventLogEntryType messageType, string methodName)
        {
            // Extract name of thread that triggered this message.
            string threadName = Thread.CurrentThread.Name;
            if (threadName == null)
            {
                if (Thread.CurrentThread.IsThreadPoolThread)
                {
                    threadName = THREADPOOL_THREAD;
                }
                else if (Thread.CurrentThread.IsBackground)
                {
                    threadName = BACKGROUND_THREAD;
                }
                else 
                {
                    threadName = UNKNOWN_THREAD;
                }
            }

            // Support developers might want to know this.
            threadName += " (id " + Thread.CurrentThread.ManagedThreadId.ToString() + ")";

            // Setup full message and then write to event log.
            string fullMessage = string.Concat("Thread: ", threadName, Environment.NewLine, 
                                                                        "Method: ", methodName, Environment.NewLine, 
                                                                        "Message: ", message);

            // Note the assumption that no external call inside this protected
            // block will lock something in such a way as to cause a deadlock.
            lock (LockEventLog)
            {
                // Because EventLog's timestamp only has a granularity of 1 second, 
                // we populate the EventId field with an incrementing integer wherever  
                // an event occurs in the same second as the previous event.
                // The combination of Timestamp and EventId makes it easier to view
                // the real order of events in the Windows event log. 
                if (Math.Abs((DateTime.Now - this.LastEventWriteTime).TotalSeconds) < 1)
                {
                    this.EventOrder++;
                }
                else
                {
                    this.EventOrder = 1;
                }
                this.LastEventWriteTime = DateTime.Now;
                this.AppEventLog.WriteEntry(fullMessage, messageType, this.EventOrder);
            }
        }
    
        // Invoke at the start of every public method.
        private void AlreadyDisposedCheck()
        {
            if (this.Disposed)
            {
                throw new ObjectDisposedException(String.Format(CultureInfo.InvariantCulture, "This class instance has already been disposed!"));
            }
        }

        /// <summary>
        /// Implements IDisposable.
        /// </summary>
        /// <remarks>
        /// Don't make this method virtual. A derived class should 
        /// not be able to override this method.
        /// Because this class only disposes managed resources, it 
        /// don't need a finaliser. A finaliser isn't allowed to 
        /// dispose managed resources.
        /// Without a finaliser, this class doesn't need an internal 
        /// implementation of Dispose() and doesn't need to suppress 
        /// finalisation to avoid race conditions. So the full 
        /// IDisposable code pattern isn't required.
        /// </remarks>
        public void Dispose()
        {
            if (!this.Disposed)
            {
                this.Disposed = true;
                this.AppEventLog.Dispose();
            }
        }
    }
}