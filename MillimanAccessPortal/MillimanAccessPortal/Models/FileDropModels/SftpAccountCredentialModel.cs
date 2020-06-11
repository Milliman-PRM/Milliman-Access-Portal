/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Contains properties representing the outcome of an account credential update
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class SftpAccountCredentialModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
