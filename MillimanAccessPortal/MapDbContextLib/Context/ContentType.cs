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
    public class ContentType
    {
        public long Id { get; set; }

        /// <summary>
        /// Name should match the enumeration value label
        /// </summary>
        [Required]
        public string Name { get; set; }

        [Required]
        public bool CanReduce { get; set; }

        // IMPORTANT: If any fields are added or changed, change AreSameContentType() to match

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
                new ContentType { Id = 1, Name = "Qlikview", CanReduce = true },
                //new ContentType { Id = 2, Name = "AnotherType", CanReduce = true },
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
