using FitMyBike.Model_Class.MTAStructure.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.MTAStructure.WorkerThreads
{
    public abstract class WorkerThreadBaseClass
    {
        private Thread Thread;
        private CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private CancellationToken Token;

        public WorkerThreadBaseClass(string threadName) 
        {
            Token = CancellationTokenSource.Token;

            Thread = new Thread(WorkerThreadMethod);
            Thread.IsBackground = true;
            Thread.Name = threadName;
            Thread.Start();
        }

        public virtual void WorkerThreadMethod()
        {
            while (!Token.IsCancellationRequested)
            {
                if (ThrereIsAnyCommandInTheQueue())
                {
                    CommandBaseClass commandBaseClass = null;
                    try
                    {
                        commandBaseClass = GetNextCommandFromTheQueue();
                        commandBaseClass?.DoAction();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                    finally
                    {
                        commandBaseClass?.Dispose();
                    }
                }

                Thread.Sleep(10);
            }
        }

        public abstract bool ThrereIsAnyCommandInTheQueue();

        public abstract CommandBaseClass GetNextCommandFromTheQueue();

        public void KillThisWorkerThread()
        {
            CancellationTokenSource.Cancel();
            Thread.Join();
        }

        public abstract void AddCommandToTheQueue(CommandBaseClass command);
    }
}
