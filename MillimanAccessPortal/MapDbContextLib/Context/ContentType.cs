/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace MapDbContextLib.Context
{
    public enum ContentTypeEnum
    {
        Unknown = 0,
        Qlikview,
        Html,
        Pdf,
        FileDownload,
    }

    public class ContentType
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Convenience property to automatically translate from the persisted Name to corresponding enumeration
        /// </summary>
        [NotMapped]
        public ContentTypeEnum TypeEnum
        {
            set
            {
                Name = value.ToString();
            }
            get
            {
                if (Enum.TryParse(Name, true, out ContentTypeEnum result))
                {
                    return result;
                }
                else
                {
                    return (ContentTypeEnum)int.MaxValue;
                }
            }
        }

        /// <summary>
        /// Name should match the enumeration value label
        /// </summary>
        [Required]
        public string Name { get; set; }

        [Required]
        public bool CanReduce { get; set; }

        [Required]
        public string DefaultIconName { get; set; }

        [Required]
        public string[] FileExtensions { get; set; } = new string[0];

        #region Database Initialization
        /// <summary>
        /// Initialize the database with known content types
        /// </summary>
        /// <param name="ServiceProvider">Application Services provide connectivity to the identity database.</param>
        /// <returns></returns>
        internal static void InitializeContentTypes(IServiceProvider serviceProvider)
        {
            List<ContentType> AllProposedContentTypes = new List<ContentType>
            {
                new ContentType {
                    TypeEnum = ContentTypeEnum.Qlikview,
                    CanReduce = true,
                    DefaultIconName = "QlikView_Icon.png",
                    FileExtensions = new string[] { "qvw" },
                },
                new ContentType {
                    TypeEnum = ContentTypeEnum.Html,
                    CanReduce = false,
                    DefaultIconName = "HTML_Icon.png",
                    FileExtensions = new string[] { "html", "htm" },
                },
                new ContentType {
                    TypeEnum = ContentTypeEnum.Pdf,
                    CanReduce = false,
                    DefaultIconName = "PDF_Icon.png",
                    FileExtensions = new string[] { "pdf" },
                },
                new ContentType {
                    TypeEnum = ContentTypeEnum.FileDownload,
                    CanReduce = false,
                    DefaultIconName = "FileDownload_Icon.png",
                    FileExtensions = new string[] { },
                },
            };

            ApplicationDbContext Db = serviceProvider.GetService<Context.ApplicationDbContext>();

            foreach (ContentType type in AllProposedContentTypes)
            {
                ContentType fromDb = Db.ContentType.SingleOrDefault(t => t.TypeEnum == type.TypeEnum);
                if (fromDb == null)
                {
                    Db.ContentType.Add(type);
                }
                else
                {
                    fromDb.Name = type.Name;
                    fromDb.CanReduce = type.CanReduce;
                    fromDb.DefaultIconName = type.DefaultIconName;
                    fromDb.FileExtensions = type.FileExtensions;
                    Db.ContentType.Update(fromDb);
                }
            }
            Db.SaveChanges();
        }

        /// <summary>
        /// Similar to operator== override but #DoesntFail and #NotDownARabbitHole if a null is passed in
        /// </summary>
        /// <param name="l"></param>
        /// <param name="r"></param>
        /// <param name="TwoNullsAreSame"></param>
        /// <returns></returns>
        public static bool AreSameContentType(ContentType l, ContentType r, bool TwoNullsAreSame = true)
        {
            if (l != null && r != null)  // Neither one is null
            {
                return l.Id == r.Id && l.Name == r.Name && l.CanReduce == r.CanReduce && r.DefaultIconName == l.DefaultIconName;
            }
            else if (l == null ^ r == null)  // exactly one is null  (^ is logical xor operator)
            {
                return false;  
            }
            else  // Both are null
            {
                return TwoNullsAreSame;
            }
        }
        #endregion

    }
}
