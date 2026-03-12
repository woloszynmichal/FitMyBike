using FitMyBike.Model_Class.MTAStructure.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.MTAStructure.WorkerThreads
{
    internal class WorkerThread<T> : WorkerThreadBaseClass where T : CommandBaseClass
    {
        private readonly object _lock = new object();
        private Queue<CommandBaseClass> commandQueue = new Queue<CommandBaseClass>();

        public WorkerThread(string threadName) : base(threadName)
        {
            CommandBaseClass.RegisterThread(typeof(T), this);
        }

        public override CommandBaseClass GetNextCommandFromTheQueue()
        {
            lock (_lock)
            {
                if (commandQueue.Any())
                {
                    return commandQueue.Dequeue();
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool ThrereIsAnyCommandInTheQueue()
        {
            lock(_lock)
            {
                return commandQueue.Any();
            }
        }

        public override void AddCommandToTheQueue(CommandBaseClass command)
        {
            lock (_lock)
            {
                commandQueue.Enqueue(command);
            }
        }
    }
}
