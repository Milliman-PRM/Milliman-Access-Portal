using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milliman.ReductionEngine.QMSAPI;

namespace Milliman.ReductionEngine {
    public class QMSWrapper {
        public static DocumentTask GetTask() {
            DocumentTask task = new DocumentTask();
            task.Scope = DocumentTaskScope.General;
            task.General = new DocumentTask.TaskGeneral();
            task.General.TaskName = "Empty Task";
            task.General.Enabled = true;

            return task;
        }
    }
}
