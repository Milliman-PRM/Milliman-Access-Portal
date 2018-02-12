using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Milliman.Reduction.ReductionEngine {

    [ServiceContract]
    public interface IReductionService {

        [OperationContract]
        void EnqueueReductionFolder(string folderPath);
        [OperationContract]
        void Init();
    }
}
