/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a account connection to a FileDrop on the MAP Sftp server
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Identity;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapDbContextLib.Context
{
    public class SftpConnection
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public DateTime CreatedDateTimeUtc { get; set; }

        [Required]
        public DateTime LastActivityUtc { get; set; }

        [Column(TypeName = "jsonb")]
        public string MetaData { get; set; }

        [ForeignKey("SftpAccount")]
        public Guid SftpAccountId { get; set; }
        public SftpAccount SftpAccount { get; set; }

        [NotMapped]
        public ConnectionMetaData MetaDataObj
        {
            get
            {
                return string.IsNullOrWhiteSpace(MetaData)
                    ? null
                    : JsonConvert.DeserializeObject<ConnectionMetaData>(MetaData);
            }
            set
            {
                MetaData = value != null
                    ? JsonConvert.SerializeObject(value)
                    : string.Empty;
            }
        }
    }

    public class ConnectionMetaData
    {
        public Guid SftpAccountId { get; set; }
        public string SftpAccountName { get; set; }

        public Guid MapUserId { get; set; }
        public string MapUserName { get; set; }

        public Guid FileDropId { get; set; }
        public string FileDropName { get; set; }
        public string FileDropRootPath { get; set; }
    }
}
