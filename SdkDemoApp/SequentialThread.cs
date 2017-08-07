using Android.OS;
using System;
using System.Threading;

namespace Utils
{
    abstract class SequentialThread : HandlerThread
    {
        public enum TaskType
        {
            Quit = 0,
        }

        [Flags]
        public enum TaskProperties
        {
            Normal = 0, // Default.
            Unique = 1, // Only one task with specified type allowed
            Single = 2, // One task with specified type and arguments allowed.
            Urgent = 4, // Perform task as fast as possible
        }

        /// <summary>
        /// 
        /// </summary>
        private readonly object synchro = new object ();

        /// <summary>
        /// Main processing thread handler.
        /// Created in thread context
        /// </summary>
        private Handler workerHandler;


        /// <summary>
        ///  Notify main thread
        /// </summary>
        private readonly Handler notityHandler = new Handler(Looper.MainLooper);

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="name"></param>
        public SequentialThread(string name) : base(name)
        {
        }

        /// <summary>
        /// Some preparation in thread context
        /// </summary>
        protected abstract void OnThreadStarted();

        /// <summary>
        /// Some cleanup in thread context
        /// </summary>
        protected abstract void OnThreadComplete();

        /// <summary>
        /// Main thread work
        /// </summary>
        /// <param name="type"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected abstract Action OnThreadTask(int type, Java.Lang.Object args);

        /// <summary>
        /// Workaround Java anonimous class
        /// </summary>
        class HandlerCallback : Java.Lang.Object, Handler.ICallback
        {
            public delegate bool OnHandleMessage(Message msg);

            OnHandleMessage handleMessage;
            public HandlerCallback(OnHandleMessage handleMessage)
            {
                this.handleMessage = handleMessage;
            }

            public bool HandleMessage(Message msg)
            {
                return handleMessage(msg);
            }
        }

        protected override void OnLooperPrepared()
        {
            base.OnLooperPrepared();
            OnThreadStarted();

            // Prepare worker handler
            Handler handler = new Handler(Looper, new HandlerCallback(delegate (Message msg)
            {
                if (msg.What == (int) TaskType.Quit)
                {
                    // Try to destroy message loop
                    if (!Quit())
                    {
                        // Hardly interrupt thread if something wrong with looper
                        Interrupt();
                    }
                    return true;
                }
                else
                {
                    // Process thread task
                    Action a = OnThreadTask(msg.What, msg.Obj);
                    if (a != null)
                    {
                        notityHandler.Post(a);
                    }
                    return true;
                }
            }));

            // Assign worker handler and notify all pending callers
            lock(synchro)
            {
                workerHandler = handler;
                Monitor.PulseAll(synchro);
            }
        }

        public override void Run()
        {
            try
            {
                base.Run();
            }
            finally
            {
                // No post-looper overrides in HandlerThread :(
                OnThreadComplete();
            }
        }

        public bool IsReady
        {
            get
            {
                lock(synchro)
                {
                    return IsAlive && workerHandler != null;
                }
            }
        }

        /// <summary>
        /// Wait for worker thread started
        /// </summary>
        private void CheckWorkerThread(int type)
        {
            // Check reserved task type
            if (type == (int) TaskType.Quit)
            {
                throw new ArgumentException("Task type cannot be 0");
            }

            lock (synchro)
            {
                if (IsAlive && workerHandler == null)
                {
                    try
                    {
                        Monitor.Wait(synchro);
                    }
                    catch (ThreadInterruptedException)
                    {
                        // Dummy
                    }
                }

                if (workerHandler == null)
                {
                    throw new InvalidOperationException("Thread is not ready yet");
                }
            }
        }

        public void AddThreadTask(int type, Java.Lang.Object args, TaskProperties flags)
        {
            // Safe wait for thread started
            CheckWorkerThread(type);

            lock(synchro)
            {
                // Remove all pending messages if any
                if (flags.HasFlag(TaskProperties.Unique))
                {
                    workerHandler.RemoveMessages(type);
                }
                else if (flags.HasFlag(TaskProperties.Single) && args != null)
                {
                    workerHandler.RemoveMessages(type, args);
                }

                // Create new message
                Message msg = workerHandler.ObtainMessage(type, args);

                // Put on head or tail or queue
                if (flags.HasFlag(TaskProperties.Urgent))
                {
                    workerHandler.SendMessageAtFrontOfQueue(msg);
                }
                else
                {
                    workerHandler.SendMessage(msg);
                }
            }
        }

        public void RemoveThreadTask(int type, Java.Lang.Object args)
        {
            // Safe wait for thread started
            CheckWorkerThread(type);

            lock(synchro)
            {
                workerHandler.RemoveMessages(type, args);
            }
        }

        public void Finish()
        {
            // Just in case.
            if (!IsAlive)
            {
                return;
            }

            lock(synchro)
            {
                // Remove all pending messages if any
                workerHandler.RemoveCallbacksAndMessages(null);

                // Send message to quit
                workerHandler.SendEmptyMessage((int)TaskType.Quit);
            }

            // Wait for thread termination
            try
            {
                Join();
            }
            catch (ThreadInterruptedException)
            {
                // Nothig
            }
        }

    }
}