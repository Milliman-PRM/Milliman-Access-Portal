/*
 * CODE OWNERS: Ben Wyatt, Tom Puckett
 * OBJECTIVE: Create and configure password validators
 * DEVELOPER NOTES: 
 */
 
 using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MillimanAccessPortal.Services
{
    public static class IdentityBuilderExtensions
    {
        /// <summary>
        /// Configure a password validator to check if a password was used within a specified number of recent passwords for the user
        /// </summary>
        /// <typeparam name="TUser"></typeparam>
        /// <param name="builder"></param>
        /// <param name="numberOfPasswordsArg"></param>
        /// <returns></returns>
        public static IdentityBuilder AddRecentPasswordCountValidator<TUser>(this IdentityBuilder builder, int numberOfPasswordsArg)
            where TUser : ApplicationUser
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }

            var validator = new PasswordRecentNumberValidator<TUser> { numberOfPasswords = numberOfPasswordsArg };

            builder.Services.AddSingleton(typeof(IPasswordValidator<>).MakeGenericType(builder.UserType), validator);

            return builder;
        }

        /// <summary>
        /// Configure a password validator to check if a password was used within a specified number of days for the user
        /// </summary>
        /// <typeparam name="TUser"></typeparam>
        /// <param name="builder"></param>
        /// <param name="numberOfDaysArg"></param>
        /// <returns></returns>
        public static IdentityBuilder AddRecentPasswordInDaysValidator<TUser>(this IdentityBuilder builder, int numberOfDaysArg)
            where TUser : ApplicationUser
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }

            var validator = new PasswordRecentDaysValidator<TUser> { numberOfDays = numberOfDaysArg };

            builder.Services.AddSingleton(typeof(IPasswordValidator<>).MakeGenericType(builder.UserType), validator);

            return builder;
        }

        public static IdentityBuilder AddCommonWordsValidator<TUser>(this IdentityBuilder builder, List<string> commonWordsArg)
            where TUser : ApplicationUser
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }

            var validator = new PasswordContainsCommonWordsValidator<TUser> { commonWords = commonWordsArg };

            builder.Services.AddSingleton(typeof(IPasswordValidator<>).MakeGenericType(builder.UserType), validator);


            return builder;
        }
    }
}
