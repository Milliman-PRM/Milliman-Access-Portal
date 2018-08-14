using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using MapDbContextLib.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MillimanAccessPortal.Services
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder AddRecentPasswordCountValidator<TUser>(this IdentityBuilder builder, int numberOfPasswordsArg)
            where TUser : ApplicationUser
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }

            var validator = new PasswordRecentNumberValidator<TUser>();
            validator.numberOfPasswords = numberOfPasswordsArg;

            builder.Services.AddSingleton(typeof(IPasswordValidator<>).MakeGenericType(builder.UserType), validator);

            return builder;
        }

        public static IdentityBuilder AddRecentPasswordInDaysValidator<TUser>(this IdentityBuilder builder, int numberOfDaysArg)
            where TUser : ApplicationUser
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }

            var validator = new PasswordRecentDaysValidator<TUser>();
            validator.numberOfDays = numberOfDaysArg;

            builder.Services.AddSingleton(typeof(IPasswordValidator<>).MakeGenericType(builder.UserType), validator);

            return builder;
        }

        public static IdentityBuilder AddPasswordEverUsedValidator<TUser>(this IdentityBuilder builder)
            where TUser : ApplicationUser
        {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }

            return builder.AddPasswordValidator<PasswordHistoryValidator<TUser>>();
        }
    }
}
