using FitMyBike.Model_Class.MTAStructure.WorkerThreads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.MTAStructure.Command
{
    public abstract class CommandBaseClass : IDisposable
    {
        private static Dictionary<Type, WorkerThreadBaseClass> _workerThreads = new Dictionary<Type, WorkerThreadBaseClass>();
        public int ThreadIndex { get; set; }

        public int KeyFrame {  get; set; }

        public abstract void Dispose();

        public abstract void DoAction();

        public void EnqueueThisCommand()
        {
            if (_workerThreads.ContainsKey(this.GetType()))
            {
                _workerThreads[this.GetType()].AddCommandToTheQueue(this);
            }
            else
            {
                throw new Exception("There is no thread registered for this command type");
            }
        }

        public static void RegisterThread(Type commandType, WorkerThreadBaseClass worker)
        {
            if(!_workerThreads.ContainsKey(commandType))
                _workerThreads.Add(commandType, worker);
        }
    }
}
