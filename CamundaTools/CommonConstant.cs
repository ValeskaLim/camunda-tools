using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaTools
{
    public class CommonConstant
    {
        public const string CAMUNDA_PROCESS_DEFINITION_ENDPOINT = "/engine-rest/process-instance?processDefinitionId=";
        public const string CAMUNDA_PROCESS_GET_PROCESS_INSTANCE_ENDPOINT = "/engine-rest/job?processInstanceId=";
        public const string CAMUNDA_PROCESS_RETRY_ENDPOINT = "/engine-rest/job/";
    }
}
