/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MapDbContextLib.Context
{
    public enum ContentTypeEnum
    {
        [Display(Name = "Unknown")]
        Unknown = 0,
        [Display(Name = "QlikView")]
        Qlikview = 1,
        [Display(Name = "HTML")]
        Html = 2,
        [Display(Name = "PDF")]
        Pdf = 3,
        [Display(Name = "File Download")]
        FileDownload = 4,
        [Display(Name = "Power BI")]
        PowerBi = 5,
    }

    public static class EnumExtensions
    {
        public static bool LiveContentFileStoredInMap(this ContentTypeEnum type)
        {
            return type != ContentTypeEnum.PowerBi;
        }
    }

    public class ContentType
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Convenience property to automatically translate from the persisted Name to corresponding enumeration
        /// </summary>
        [Required]
        public ContentTypeEnum TypeEnum { get; set; }

        [Required]
        public bool CanReduce { get; set; }

        [Required]
        public string DefaultIconName { get; set; }

        [Required]
        public List<string> FileExtensions { get; set; } = new List<string>();

        #region Database Initialization
        /// <summary>
        /// Initialize the database with known content types
        /// </summary>
        /// <param name="ServiceProvider">Application Services provide connectivity to the identity database.</param>
        /// <returns></returns>
        public static async Task InitializeContentTypesAsync(IServiceProvider serviceProvider)
        {
            List<ContentType> AllProposedContentTypes = new List<ContentType>
            {
                new ContentType {
                    TypeEnum = ContentTypeEnum.Qlikview,
                    CanReduce = true,
                    DefaultIconName = "QlikView_Icon.png",
                    FileExtensions = new List<string> { "qvw" },
                },
                new ContentType {
                    TypeEnum = ContentTypeEnum.Html,
                    CanReduce = false,
                    DefaultIconName = "HTML_Icon.png",
                    FileExtensions = new List<string> { "html", "htm" },
                },
                new ContentType {
                    TypeEnum = ContentTypeEnum.Pdf,
                    CanReduce = false,
                    DefaultIconName = "PDF_Icon.png",
                    FileExtensions = new List<string> { "pdf" },
                },
                new ContentType {
                    TypeEnum = ContentTypeEnum.FileDownload,
                    CanReduce = false,
                    DefaultIconName = "FileDownload_Icon.png",
                    FileExtensions = new List<string> { },
                },
                new ContentType {
                    TypeEnum = ContentTypeEnum.PowerBi,
                    CanReduce = false,
                    DefaultIconName = "PowerBI_Icon.png",
                    FileExtensions = new List<string> { "pbix" },
                },
            };

            ApplicationDbContext Db = serviceProvider.GetService<ApplicationDbContext>();

            foreach (ContentType type in AllProposedContentTypes)
            {
                ContentType fromDb = await Db.ContentType.SingleOrDefaultAsync(t => t.TypeEnum == type.TypeEnum);
                if (fromDb == null)
                {
                    Db.ContentType.Add(type);
                }
                else
                {
                    fromDb.CanReduce = type.CanReduce;
                    fromDb.DefaultIconName = type.DefaultIconName;
                    fromDb.FileExtensions = type.FileExtensions;
                }
            }
            await Db.SaveChangesAsync();
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
                return l.Id == r.Id && l.TypeEnum == r.TypeEnum && l.CanReduce == r.CanReduce && r.DefaultIconName == l.DefaultIconName;
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
