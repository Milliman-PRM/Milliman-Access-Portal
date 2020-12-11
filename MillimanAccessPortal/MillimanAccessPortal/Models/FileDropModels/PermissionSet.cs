/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: Permissions that may be managed for a user's access to file drop resources
 * DEVELOPER NOTES: <What future developers need to know.>
 */

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class PermissionSet
    {
        public bool ReadAccess { get; set; }
        public bool WriteAccess { get; set; }
        public bool DeleteAccess { get; set; }
    }
}
