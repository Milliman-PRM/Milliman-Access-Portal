using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using MapDbContextLib.Context;

namespace ContentPublishingLib.JobRunners
{
    public class MapDbPublishRunner
    {
        public PublishJobDetail JobDetail { get; set; } = new PublishJobDetail();
        public async Task<PublishJobDetail> Execute(CancellationToken cancellationToken)
        {
            // TODO Handle the publication life cycle
            return new PublishJobDetail();
        }
    }
}
