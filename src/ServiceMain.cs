// Copyright (c) 2014-16 Mark Pearce
// http://opensource.org/licenses/MIT

using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace SleekSoftware
{
    public sealed partial class ServiceMain : ServiceBase
    {
        // Service startup modes.
        private const string DEBUG = @"/d";			
        private const string INSTALL = @"/i";		
        private const string UNINSTALL = @"/u";		

        // Control how long to wait for environment to stabilise 
        // before restarting the worker thread after it has crashed.
        // This delay is in milliseconds.
        private const string CONFIG_WORKER_RESTART_DELAY = "WorkerThreadRestartDelay";
        private const Int32 DEFAULT_WORKER_RESTART_DELAY = 3000;

        // Names for application-level threads.
        private const string THREAD_NAME_CONTROLLER = "Controller";

        // Backing fields for the ServiceStopRequested property.
        private static readonly object m_LockFlag = new object();
        private bool m_ServiceStopRequested = false;

        public ServiceMain()
        {
            InitializeComponent();
        }

        // Log information, warnings, and errors.
        private Log AppLog { get; set; }

        // The controller thread has the following responsibilities:
        // Start the real work.
        // Restart the real work after an unhandled exception.
        // Stop the service if requested by the SCM.
        private Thread ThreadController { get; set; }

        // Set by an SCM thread when it wants this service 
        // to stop, and read by the controller and work threads.
        // Protected by a lock, which is simpler to understand
        // than using the more fancy concept of a volatile field.
        // http://www.albahari.com/threading/part4.aspx#_The_volatile_keyword
        private bool ServiceStopRequested
        {
            get
            {
                lock (m_LockFlag)
                {
                    return m_ServiceStopRequested;
                }
            }
            set
            {
                lock (m_LockFlag)
                {
                    m_ServiceStopRequested = value;
                }
            }
        }

        // Number of milliseconds to delay worker thread restart.
        private Int32 RestartDelay
        {
            get
            {
                Int32 restartDelay;
                if ( !Int32.TryParse(ConfigurationManager.AppSettings[CONFIG_WORKER_RESTART_DELAY], out restartDelay) )
                {
                    restartDelay = DEFAULT_WORKER_RESTART_DELAY;
                }
                return restartDelay;
            }
        }

        // This is the entry point for this service. 
        // This method runs on a thread provided by the SCM.
        public static void Main(string[] args)
        {
            if (Environment.UserInteractive && args.Length > 0)
            {
                switch (args[0])
                {
                    // Debug the service as a normal app, presumably within Visual Studio.
                    case DEBUG:
                        ServiceMain DebugService = new ServiceMain();
                        DebugService.OnStart(null);
                        break;
                    // Install the service programatically.
                    case INSTALL:
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    // Un-install the service programatically.
                    case UNINSTALL:
                        ManagedInstallerClass.InstallHelper(new string[] { UNINSTALL, Assembly.GetExecutingAssembly().Location });
                        break;
                    // We don't understand this request!
                    default:
                        string message = string.Concat(DEBUG, " to debug service in VS.", Environment.NewLine);
                        message += string.Concat(INSTALL, " to install service.", Environment.NewLine);
                        message += string.Concat(UNINSTALL, " to un-install service.", Environment.NewLine);
                        message += string.Concat("Do not understand the command-line parameter ", args[0]);
                        throw new System.NotImplementedException(message);
                }
            }
            // If no startup mode specified, start the service normally.
            else
            {
                ServiceBase[] ServicesToRun = new ServiceBase[] { new ServiceMain() };
                ServiceBase.Run(ServicesToRun);
            }
        }

        // SCM requests service start using its own thread.
        // This method must complete within 10 seconds of it
        // starting. Otherwise the SCM diagnoses a hang.
        protected override void OnStart(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.UnhandledExceptionFilter);

            this.AppLog = new Log();
            AppLog.Information("SCM requested service start.", "ServiceMain.OnStart");

            this.ThreadController = new Thread(new ThreadStart(ControllerThreadRunning));
            this.ThreadController.Name = THREAD_NAME_CONTROLLER;
            this.ThreadController.Start();
            base.OnStart(args);
        }

        // Invoked when the controller thread starts. 
        private void ControllerThreadRunning()
        {
            this.AppLog.Information("Controller thread has started.", "ServiceMain.ControllerThreadRunning");

             // And we're on our way.
            while ( !this.ServiceStopRequested )
            {
                // Start real work and then block until that finishes or crashes.
                var realWork = this.LaunchWorkAsync();
                realWork.Wait();
                // If SCM didn't request a service stop, the assumption is that 
                // the real work crashed and needs to be restarted.
                if ( !this.ServiceStopRequested )
                {
                    this.PauseControllerThread("Pause before restarting work cycle.", this.RestartDelay);
                }
            }

            // This service will now stop.
            this.AppLog.Information("Service stopping at SCM request.", "ServiceMain.ControllerThreadRunning");
            this.Cleanup();
        }

        // This method handles all ceremony around the real work of this service.
        private async Task LaunchWorkAsync()
        {
            try 
            {
                // Setup progress reporting.
                var progressReport = new Progress<string>
                    (progressInfo => { this.AppLog.Information(progressInfo, "ServiceMain.DoWork"); });
                var progress = progressReport as IProgress<string>;
                // Launch time.
                await Task.Factory.StartNew( () => this.DoWork(progress), TaskCreationOptions.LongRunning );
            }

            // Report any exception raised during the work cycle.
            catch (Exception ex)
            {
                this.AppLog.Error(string.Concat("Work cycle crashed", Environment.NewLine,
                                                ex.GetType().FullName, Environment.NewLine,
                                                ex.Message, Environment.NewLine,
                                                ex.StackTrace));
            }

            return;
        }

        // This is where this service's real work is done.
        // The work cycles continuously until it's asked to stop.
        // If the work crashes with an unhandled exception, the 
        // controller thread will restart it after an appropriate delay.
        private void DoWork(IProgress<string> progress)
        {
            this.AppLog.Information("Work has started.", "ServiceMain.DoWork");

            while (!this.ServiceStopRequested)
            {
                Thread.Sleep(3000);     // Simulated work cycle.
                progress.Report("completed work cycle.");
            }
        }

        // Pause for the specified number of milliseconds.
        private void PauseControllerThread(string message, Int32 waitMilliseconds)
        {
            this.AppLog.Information(message, "ServiceMain.PauseControllerThread");

            // This approach is better than Thread.Sleep.
            // http://msmvps.com/blogs/peterritchie/archive/2007/04/26/thread-sleep-is-a-sign-of-a-poorly-designed-program.aspx 
            using (var pauseControllerThread = new ManualResetEventSlim(initialState: false))
            {
                pauseControllerThread.Wait(waitMilliseconds);
            }
        }

        // SCM requests service stop using its own thread.
        protected override void OnStop()
        {
            this.AppLog.Information("SCM requested service stop.", "ServiceMain.OnStop");
            this.ServiceStopRequested = true;
            base.OnStop();
        }

        // SCM requests service stop (via machine shutdown) using its own thread.
        protected override void OnShutdown()
        {
            this.AppLog.Information("SCM requested service stop due to machine shutdown.", "ServiceMain.OnShutdown");
            this.ServiceStopRequested = true;
            base.OnShutdown();
        }

        private void UnhandledExceptionFilter(object sender, UnhandledExceptionEventArgs e)
        {
        }
        
        // Normal exception, for testing service recovery.
        private void ThrowNormalException()
        {
            Int32 test = 0;
            test = test / test;
        }

        // Process corruption exception, for testing service recovery.
        // From CLR v4 (ie .NET Framework 4.0)  onwards, 
        // no managed code can run after this exception.
        private void ThrowAccessViolationException()
        {
            IntPtr ptr = new IntPtr(1000);
            Marshal.StructureToPtr(1000, ptr, true);
        }

        // Cleanup everything.
        private void Cleanup()
        {
            this.AppLog.Dispose();
              base.Dispose(true);
        }
    }
}