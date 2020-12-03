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
        /// <param name="fileDropRootPath">Absolute path of the root folder of the File Drop.</param>
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

        public static FileDropOperationResult RemoveDirectory(string canonicalPath,
                                                              string fileDropName,
                                                              string fileDropRootPath,
                                                              Guid? fileDropId,
                                                              SftpAccount account,
                                                              ApplicationUser user,
                                                              bool? beforeExec = null,
                                                              int sftpStatus = 0)
        {
            FileDropOperationResult returnVal = (FileDropOperationResult)sftpStatus;

            using (var db = NewMapDbContext)
            {
                List<FileDropDirectory> allDirectoryRecordsForFileDrop = db.FileDropDirectory.Where(d => d.FileDropId == fileDropId).ToList();
                FileDropDirectory requestedDirectory = allDirectoryRecordsForFileDrop.SingleOrDefault(d => d.CanonicalFileDropPath == canonicalPath);
                string requestedAbsolutePath = Path.Combine(fileDropRootPath, canonicalPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                switch (beforeExec)
                {
                    case true:
                        if (!Directory.Exists(requestedAbsolutePath) || requestedDirectory == null)
                        {
                            Log.Warning($"OnDirRemove: Requested directory {canonicalPath} at absolute path {requestedAbsolutePath} is not found (database or file system)");
                            returnVal = FileDropOperationResult.NO_SUCH_PATH;
                        }
                        break;

                    case null:
                    case false:
                        try
                        {
                            if (beforeExec == null) {
                                FileSystemUtil.DeleteDirectoryWithRetry(requestedAbsolutePath, true);
                            }

                            List<FileDropDirectory> directoriesToDelete = allDirectoryRecordsForFileDrop.Where(d => EF.Functions.Like(d.CanonicalFileDropPath, canonicalPath + "%")).ToList();

                            var deleteInventory = new FileDropDirectoryInventoryModel
                            {
                                Directories = directoriesToDelete.Select(d => (FileDropDirectoryLogModel)d).ToList(),
                                Files = db.FileDropFile
                                          .Where(f => directoriesToDelete.Select(d => d.Id).Contains(f.DirectoryId))
                                          .ToList(),
                            };

                            // Cascade on delete behavior will remove all subfolder and contained file records
                            db.FileDropDirectory.Remove(requestedDirectory);
                            db.SaveChanges();

                            new AuditLogger().Log(AuditEventType.SftpDirectoryRemoved.ToEvent((FileDropDirectoryLogModel)requestedDirectory,
                                                                                              deleteInventory,
                                                                                              new FileDropLogModel { Id = fileDropId.Value, Name = fileDropName },
                                                                                              account,
                                                                                              user), user?.UserName);
                            Log.Information($"OnDirRemove: Requested directory {canonicalPath} at absolute path {requestedAbsolutePath} removed. Deleted inventory is {{@Inventory}}", deleteInventory);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, $"Failed to remove directory record with ID {requestedDirectory.Id} or contained file or directory record(s)");
                            returnVal = FileDropOperationResult.FAILURE;
                        }
                        break;
                }
            }

            return returnVal;
        }

        internal static object CreateDirectoryLockObj = new object();
        public static FileDropOperationResult CreateDirectory(string canonicalPath,
                                                              string fileDropRootPath,
                                                              string fileDropName,
                                                              Guid? fileDropId,
                                                              Guid? clientId,
                                                              string clientName,
                                                              SftpAccount account,
                                                              ApplicationUser user,
                                                              bool? beforeExec = null,
                                                              int sftpStatus = 0)
        {
            string requestedCanonicalPath = FileDropDirectory.ConvertPathToCanonicalPath(canonicalPath);
            if (string.IsNullOrWhiteSpace(canonicalPath))
            {
                Log.Warning($"OnDirCreate event invoked but requested path <{canonicalPath}> is invalid");
                return FileDropOperationResult.INVALID_FILENAME;
            }

            string requestedAbsolutePath = Path.Combine(fileDropRootPath, canonicalPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string parentCanonicalPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(requestedCanonicalPath));
            string parentAbsolutePath = Path.Combine(fileDropRootPath, parentCanonicalPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            FileDropOperationResult returnVal = (FileDropOperationResult)sftpStatus;

            using (var db = FileDropOperations.NewMapDbContext)
            {
                switch (beforeExec)
                {
                    case true:
                    // Validation:
                        // 1. Requested directory must not already exist
                        if (Directory.Exists(requestedAbsolutePath) ||
                            db.FileDropDirectory.Any(d => d.FileDropId == fileDropId
                                                       && EF.Functions.ILike(d.CanonicalFileDropPath, requestedCanonicalPath)))
                        {
                            Log.Warning($"CreateDirectory invoked but requested path <{canonicalPath}> already exists");
                            return FileDropOperationResult.FILE_ALREADY_EXISTS;
                        }

                        // 2. The parent of the requested directory must already exist, including in the database
                        if (!Directory.Exists(parentAbsolutePath) ||
                            (!db.FileDropDirectory.Any(d => d.FileDropId == fileDropId 
                                                         && EF.Functions.ILike(d.CanonicalFileDropPath, parentCanonicalPath))))
                        {
                            Log.Warning($"CreateDirectory invoked but parent directory of requested path <{canonicalPath}> does not exist");
                            return FileDropOperationResult.NO_SUCH_PATH;
                        }
                        break;

                    case false:
                        // Validation:
                        if (!Directory.Exists(requestedAbsolutePath))
                        {
                            Log.Warning($"CreateDirectory invoked, the requested directory does not appear to have been created");
                            return FileDropOperationResult.FAILURE;
                        }

                        FileDropDirectory parentRecord = db.FileDropDirectory.SingleOrDefault(d => d.FileDropId == fileDropId && EF.Functions.ILike(parentCanonicalPath, d.CanonicalFileDropPath));
                        FileDropDirectory newDirRecord = new FileDropDirectory
                        {
                            CanonicalFileDropPath = requestedCanonicalPath,
                            FileDropId = fileDropId.Value,
                            ParentDirectoryId = parentRecord?.Id,
                            Description = string.Empty,
                        };
                        db.FileDropDirectory.Add(newDirRecord);
                        db.SaveChanges();

                        new AuditLogger().Log(AuditEventType.SftpDirectoryCreated.ToEvent(newDirRecord,
                                                                                          new FileDropLogModel { Id = fileDropId.Value, Name = fileDropName },
                                                                                          account,
                                                                                          new Client { Id = clientId.HasValue ? clientId.Value : Guid.Empty, Name = clientName },
                                                                                          user), user?.UserName);
                        Log.Information($"CreateDirectory invoked, directory {requestedCanonicalPath} created relative to absolute file drop root {fileDropRootPath}");
                        break;
                }
            }

            return returnVal;
        }

        public static FileDropOperationResult RenameFile(string oldPath,
                                                         string newPath,
                                                         string fileDropRootPath,
                                                         string fileDropName,
                                                         Guid? fileDropId,
                                                         Guid? clientId,
                                                         string clientName,
                                                         SftpAccount account,
                                                         ApplicationUser user,
                                                         bool? beforeExec = null,
                                                         int sftpStatus = 0)
        {
            switch (beforeExec)
            {
                case true:
                    break;

                case false:
                    break;

                default:
                    break;
            }

            return 0;
        }

        public static FileDropOperationResult RenameDirectory(string oldPath,
                                                              string newPath,
                                                              string fileDropRootPath,
                                                              string fileDropName,
                                                              Guid? fileDropId,
                                                              Guid? clientId,
                                                              string clientName,
                                                              SftpAccount account,
                                                              ApplicationUser user,
                                                              bool? beforeExec = null,
                                                              int sftpStatus = 0)
        {
            using (var db = NewMapDbContext)
            switch (beforeExec)
            {
                case true:
                    string recordNameString = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetFullPath(oldPath).Replace(Path.GetFullPath(fileDropRootPath), ""));
                    if (recordNameString == "/")
                    {
                        Log.Warning($"Request to rename {recordNameString} in FileDrop <{fileDropName}> (Id {fileDropId}) cannot be performed.  Root directory cannot be renamed.  Account {account?.UserName} (Id {account?.Id})");
                        return FileDropOperationResult.FAILURE; 
                    }

                    // confirm db connectivity and that the source record exists in the db
                    bool sourceRecordFound = db.FileDropDirectory.Any(d => d.FileDropId == fileDropId && EF.Functions.ILike(d.CanonicalFileDropPath, recordNameString));
                    if (!sourceRecordFound)
                    {
                        Log.Warning($"Request to rename {recordNameString} in FileDrop <{fileDropName}> (Id {fileDropId}) cannot be performed.  Corresponding database record not found.  Account {account?.UserName} (Id {account?.Id})");
                        return FileDropOperationResult.FAILURE;
                    }
                    break;

                case null:
                case false:
                    if (beforeExec == null)
                    {
                        if (!Directory.Exists(oldPath))
                        {
                                return FileDropOperationResult.NO_SUCH_PATH;
                        }
                        if (Directory.Exists(newPath))
                        {
                                return FileDropOperationResult.FILE_ALREADY_EXISTS;
                        }
                        try
                        {
                            FileSystemUtil.MoveDirectoryWithRetry(oldPath, newPath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Failed to move directory {oldPath} to {newPath}");
                            return FileDropOperationResult.FAILURE;
                        }
                    }

                    if (sftpStatus == 0)
                    {
                        string canonicalOldPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.Combine("/", Path.GetRelativePath(fileDropRootPath, oldPath)));
                        string canonicalNewPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.Combine("/", Path.GetRelativePath(fileDropRootPath, newPath)));

                        List<FileDropDirectory> allDirectoriesInThisFileDrop = db.FileDropDirectory
                                                                                 .Where(d => d.FileDropId == fileDropId)
                                                                                 .Include(d => d.ParentDirectory)
                                                                                 .Include(d => d.ChildDirectories)
                                                                                 .ToList();

                        FileDropDirectory directoryRecord = allDirectoriesInThisFileDrop.SingleOrDefault(d => EF.Functions.ILike(d.CanonicalFileDropPath, canonicalOldPath));
                        if (directoryRecord == null)
                        {
                            return FileDropOperationResult.FAILURE;
                        }

                        RepathDirectoryRecord(directoryRecord, canonicalNewPath, true);

                        string newCanonicalParentPath = FileDropDirectory.ConvertPathToCanonicalPath(Path.GetDirectoryName(canonicalNewPath));
                        if (!newCanonicalParentPath.Equals(directoryRecord.ParentDirectory.CanonicalFileDropPath, StringComparison.InvariantCultureIgnoreCase))
                        { // This move involves a change in parent directory
                            FileDropDirectory newParentDirectoryRecord = allDirectoriesInThisFileDrop
                                                                           .Where(d => d.FileDropId == fileDropId)
                                                                           .SingleOrDefault(d => EF.Functions.ILike(d.CanonicalFileDropPath, newCanonicalParentPath));
                            if (newParentDirectoryRecord == null)
                            {
                                return FileDropOperationResult.FAILURE;
                            }

                            directoryRecord.ParentDirectory = newParentDirectoryRecord;
                        }

                        db.SaveChanges();

                        Log.Information($"Renamed {oldPath} to {newPath} in FileDrop <{fileDropName}> (Id {fileDropId}).  Account {account?.UserName} (Id {account?.Id})");
                        new AuditLogger().Log(AuditEventType.SftpRename.ToEvent(new SftpRenameLogModel
                        {
                            From = oldPath,
                            To = newPath,
                            IsDirectory = true,
                            FileDrop = new FileDropLogModel { Id = fileDropId.Value, Name = fileDropName },
                            Account = account,
                            User = user,
                        }), user?.UserName);
                    }
                    break;
            }

            return FileDropOperationResult.OK;
        }

        /// <summary>
        /// Alter the FileDropPath field value(s) in one or a tree of nested directory records
        /// </summary>
        /// <param name="directoryRecord">Recursion only occurs on child directory records contained in the ChildDirectories navigation property</param>
        /// <param name="toPath">Must meet requirements to be converted to a canonical path as described in <see cref="FileDropDirectory.CanonicalFileDropPath"/></param>
        /// <param name="recurseOnChildren"></param>
        private static void RepathDirectoryRecord(FileDropDirectory directoryRecord, string toPath, bool recurseOnChildren)
        {
            directoryRecord.FileDropPath = toPath;

            if (recurseOnChildren)
            {
                foreach (FileDropDirectory child in directoryRecord.ChildDirectories)
                {
                    RepathDirectoryRecord(child, Path.Combine(toPath, Path.GetFileName(child.CanonicalFileDropPath)), recurseOnChildren);
                }
            }
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

}
