using MillimanAccessPortal.Models.SystemAdmin;
using MapDbContextLib.Context;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Binders
{
    public class AuthenticationSchemeBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var model = new AllAuthenticationSchemes.AuthenticationScheme();

            #region Fetch the values of the base class fields
            var valueProviderResult = bindingContext.ValueProvider.GetValue("Type");
            bindingContext.ModelState.SetModelValue("Type", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None && 
                Enum.TryParse(valueProviderResult.FirstValue, out AuthenticationType type))
            {
                model.Type = type;
                bindingContext.ModelState.MarkFieldValid("Type");
            }

            valueProviderResult = bindingContext.ValueProvider.GetValue("Name");
            bindingContext.ModelState.SetModelValue("Name", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
                model.Name = valueProviderResult.FirstValue;
                bindingContext.ModelState.MarkFieldValid("Name");
            }

            valueProviderResult = bindingContext.ValueProvider.GetValue("DisplayName");
            bindingContext.ModelState.SetModelValue("DisplayName", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
                model.DisplayName = valueProviderResult.FirstValue;
                bindingContext.ModelState.MarkFieldValid("DisplayName");
            }

            valueProviderResult = bindingContext.ValueProvider.GetValue("DomainList");
            bindingContext.ModelState.SetModelValue("DomainList", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
                model.DomainList = new List<string>(valueProviderResult.Values);
                bindingContext.ModelState.MarkFieldValid("DomainList");
            }
            #endregion

            // Get scheme type dependent properties
            switch (model.Type)
            {
                case AuthenticationType.WsFederation:
                    var properties = new MapDbContextLib.Models.WsFederationSchemeProperties();

                    valueProviderResult = bindingContext.ValueProvider.GetValue("wtrealm");
                    bindingContext.ModelState.SetModelValue("Properties.Wtrealm", valueProviderResult);
                    if (valueProviderResult != ValueProviderResult.None)
                    {
                        properties.Wtrealm = valueProviderResult.FirstValue;
                        bindingContext.ModelState.MarkFieldValid("Properties.Wtrealm");
                    }

                    valueProviderResult = bindingContext.ValueProvider.GetValue("metadataAddress");
                    bindingContext.ModelState.SetModelValue("Properties.MetadataAddress", valueProviderResult);
                    if (valueProviderResult != ValueProviderResult.None)
                    {
                        properties.MetadataAddress = valueProviderResult.FirstValue;
                        bindingContext.ModelState.MarkFieldValid("Properties.MetadataAddress");
                    }

                    model.Properties = properties;
                    break;

                default:
                    break;
            }

            if (bindingContext.ModelState.IsValid)
            {
                bindingContext.Result = ModelBindingResult.Success(model);
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            return Task.CompletedTask;
        }
    }
}
