/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: A distinct exception type to be caught for application specific handling throughout the solution
 * DEVELOPER NOTES: It's ok to derive specialized exceptions from this if the occasion arises. 
 */

using System;

namespace MapCommonLib
{
    /// <summary>
    /// Can be used in all the same ways that a System.Exception can be
    /// </summary>
    public class MapException : Exception
    {
        public MapException() : base() {}
        public MapException(string Message) : base(Message) {}
        public MapException(string Message, Exception InnerException) : base(Message, InnerException) {}
    }
}
