/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Models.FileDropModels
{
    public class FileDropsModel
    {
        public ClientCardModel ClientCard { get; set; }

        public Dictionary<Guid, FileDropCardModel> FileDrops { get; set; } = new Dictionary<Guid, FileDropCardModel>();
    }


    public class FileDropCardModel
    {
        public FileDropCardModel(FileDrop fileDrop)
        {
            Id = fileDrop.Id;
            Name = fileDrop.Name;
            Description = fileDrop.Description;
            ClientId = fileDrop.ClientId;
            IsSuspended = fileDrop.IsSuspended;
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int? UserCount { get; set; }

        public bool IsSuspended { get; set; }

        public Guid ClientId { get; set; }
    }
}
