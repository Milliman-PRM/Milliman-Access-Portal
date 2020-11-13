/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using AuditLogLib;
using AuditLogLib.Event;
using AuditLogLib.Models;
using MapDbContextLib.Context;
using MapCommonLib;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MapDbContextLib.Identity;

namespace FileDropLib
{
    public class FileDropOperations
    {
        /// <summary>
        /// Sets the connection string to be used when constructing instances of ApplicationDbContext
        /// </summary>
        public static string MapDbConnectionString
        {
            set
            {
                DbContextOptionsBuilder<ApplicationDbContext> ContextBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                ContextBuilder.UseNpgsql(value);
                MapDbContextOptions = ContextBuilder.Options;
            }
        }
        private static DbContextOptions<ApplicationDbContext> MapDbContextOptions = null;

        /// <summary>
        /// Provides a newly constructed instance of ApplicationDbContext based on a previously assigned connection string
        /// </summary>
        public static ApplicationDbContext NewMapDbContext
        {
            get
            {
                if (MapDbContextOptions == null)
                {
                    throw new ApplicationException("Attempt to create an instance of ApplicationDbContext without assigning a connection string");
                }

                return new ApplicationDbContext(MapDbContextOptions);
            }
        }

        public enum FileDropOperationResult
        {
            OK = 0,
            EOF = 1,
            NO_SUCH_FILE = 2,
            PERMISSION_DENIED = 3,
            FAILURE = 4,
            BAD_MESSAGE = 5,
            NO_CONNECTION = 6,
            CONNECTION_LOST = 7,
            OP_UNSUPPORTED = 8,
            INVALID_HANDLE = 9,
            NO_SUCH_PATH = 10,
            FILE_ALREADY_EXISTS = 11,
            WRITE_PROTECT = 12,
            NO_MEDIA = 13,
            NO_SPACE_ON_FILESYSTEM = 14,
            QUOTA_EXCEEDED = 15,
            UNKNOWN_PRINCIPAL = 16,
            LOCK_CONFLICT = 17,
            DIR_NOT_EMPTY = 18,
            NOT_A_DIRECTORY = 19,
            INVALID_FILENAME = 20,
            LINK_LOOP = 21,
            CANNOT_DELETE = 22,
            INVALID_PARAMETER = 23,
            FILE_IS_A_DIRECTORY = 24,
            BYTE_RANGE_LOCK_CONFLICT = 25,
            BYTE_RANGE_LOCK_REFUSED = 26,
            DELETE_PENDING = 27,
            FILE_CORRUPT = 28,
            OWNER_INVALID = 29,
            GROUP_INVALID = 30,
            NO_MATCHING_BYTE_RANGE_LOCK = 31
        }

        /// <summary>
        /// Execute the common portions of operations to remove a file from the file system and db
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileDropName"></param>
        /// <param name="fileDropRootPath"></param>
        /// <param name="fileDropId"></param>
        /// <param name="account"></param>
        /// <param name="user"></param>
        /// <param name="BeforeExec">Only for use when the caller is sftp server</param>
        /// <param name="sftpStatus">Only for use when the caller is sftp server</param>
        /// <returns></returns>
        public static FileDropOperationResult RemoveFile(string path, 
                                                         string fileDropName, 
                                                         string fileDropRootPath, 
                                                         Guid? fileDropId, 
                                                         SftpAccount account, 
                                                         ApplicationUser user, 
                                                         bool? BeforeExec = null, 
                                                         int sftpStatus = 0)
        {
            FileDropOperationResult returnVal = (FileDropOperationResult)sftpStatus;

            string requestedFileName = Path.GetFileName(path);
            string requestedDirectoryCanonicalPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(path));
            string requestedAbsolutePath = Path.Combine(fileDropRootPath, path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            using (ApplicationDbContext db = NewMapDbContext)
            {
                FileDropFile fileRecord = db.FileDropFile
                                            .Include(f => f.Directory)
                                            .Where(f => EF.Functions.ILike(requestedDirectoryCanonicalPath, f.Directory.CanonicalFileDropPath))
                                            .Where(f => f.Directory.FileDropId == fileDropId)
                                            .SingleOrDefault(f => EF.Functions.ILike(requestedFileName, f.FileName));

                switch (BeforeExec)
                {
                    case null: // caller is MAP
                        if (!File.Exists(requestedAbsolutePath))
                        {
                            Log.Warning($"RemoveFile: Requested file {path} at absolute path {requestedAbsolutePath} is not found in the file system, FileDrop ID {fileDropId}, named {fileDropName}");
                            returnVal = FileDropOperationResult.NO_SUCH_PATH;  // SSH_FX_NO_SUCH_PATH 10
                        }
                        else if (fileRecord == null)
                        {
                            Log.Warning($"RemoveFile: Requested file {path} at absolute path {requestedAbsolutePath} is not found in the database, FileDrop ID {fileDropId}, named {fileDropName}");
                            returnVal = FileDropOperationResult.NO_SUCH_PATH;  // SSH_FX_NO_SUCH_PATH 10
                        }
                        else
                        {
                            FileSystemUtil.DeleteFileWithRetry(requestedAbsolutePath);

                            db.FileDropFile.Remove(fileRecord);
                            db.SaveChanges();
                        }
                        break;

                    case true:
                        bool failValidation = false;
                        if (fileRecord == null)
                        {
                            Log.Warning($"OnFileRemove: Requested file {path} at absolute path {requestedAbsolutePath} is not found in the database, FileDrop ID {fileDropId}, named {fileDropName}");
                            failValidation = true;
                        }
                        else if (!File.Exists(requestedAbsolutePath))
                        {
                            Log.Warning($"OnFileRemove: Requested file {path} at absolute path {requestedAbsolutePath} is not found in the file system), FileDrop ID {fileDropId}, named {fileDropName}");
                            failValidation = true;
                        }
                        if (failValidation)
                        {
                            returnVal = FileDropOperationResult.NO_SUCH_PATH;  // SSH_FX_NO_SUCH_PATH 10
                        }
                        break;

                    case false:
                        if (returnVal == FileDropOperationResult.OK)
                        {
                            db.FileDropFile.Remove(fileRecord);
                            db.SaveChanges();

                            new AuditLogger().Log(AuditEventType.SftpFileRemoved.ToEvent((FileDropFileLogModel)fileRecord,
                                                                                         (FileDropDirectoryLogModel)fileRecord.Directory,
                                                                                              new FileDropLogModel { Id = fileDropId.Value, Name = fileDropName },
                                                                                              account,
                                                                                              user), user?.UserName);
                            Log.Information($"OnFileRemove: Requested file {path} at absolute path {requestedAbsolutePath} removed, FileDrop ID {fileDropId}, named {fileDropName}");
                        }
                        else
                        {
                            Log.Error($"OnFileRemove: Requested file {path} at absolute path {requestedAbsolutePath} failed to be removed, event status {(int)returnVal}, FileDrop ID {fileDropId}, named {fileDropName}");
                        }
                        break;
                }
            }

            return returnVal;
        }

        public static FileDropOperationResult RemoveDirectory(string path,
                                                              string fileDropName,
                                                              string fileDropRootPath,
                                                              Guid? fileDropId,
                                                              SftpAccount account,
                                                              ApplicationUser user,
                                                              bool? BeforeExec = null,
                                                              int sftpStatus = 0)
        {
            FileDropOperationResult returnVal = (FileDropOperationResult)sftpStatus;

            

            return returnVal;
        }
    }

    public enum RequiredAccess
    {
        NoRequirement,
        AnyOneOrMore,
        Read,
        Write,
        Delete
    }

    public enum AuthorizationResult
    {
        ConnectionNotFound,
        NotAuthorized,
        Authorized,
    }

}
