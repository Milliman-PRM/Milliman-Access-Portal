/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: An entity representing a root content item in the persistence data model
 * DEVELOPER NOTES: 
 */

using System;
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
    }

    public class ContentType
    {
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Convenience property to automatically translate between persisted string and enumeration
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
                return Enum.Parse<ContentTypeEnum>(Name, true);
            }
        }

        /// <summary>
        /// Name should match the enumeration value label
        /// </summary>
        [Required]
        public string Name { get; set; }

        [Required]
        public bool CanReduce { get; set; }

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
                new ContentType { Id = 1, TypeEnum=ContentTypeEnum.Qlikview, CanReduce = true },
                //new ContentType { Id = 2, TypeEnum = ContentTypeEnum.AnotherType, CanReduce = trueorfalse },
            };

            ApplicationDbContext Db = serviceProvider.GetService<Context.ApplicationDbContext>();

            // Eliminate any proposed objects already in the database
            AllProposedContentTypes.RemoveAll(t => AreSameContentType(t, Db.ContentType.Find(t.Id)));

            if (AllProposedContentTypes.Count > 0)
            {
                Db.ContentType.AddRange(AllProposedContentTypes);
                Db.SaveChanges();
            }
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
                return l.Id == r.Id && l.Name == r.Name && l.CanReduce == r.CanReduce;
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
