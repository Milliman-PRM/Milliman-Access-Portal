/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Binders
{
    public class RootContentItemBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            ValueProviderResult valueProviderResult = default;
            var model = new RootContentItem();

            #region Fetch the values of non type specific fields
            #region Id field
            valueProviderResult = bindingContext.ValueProvider.GetValue("Id");
            bindingContext.ModelState.SetModelValue("Id", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None &&
                Guid.TryParse(valueProviderResult.FirstValue, out Guid idVal))
            {
                model.Id = idVal;
                bindingContext.ModelState.MarkFieldValid("Id");
            }
            else
            {
                // Id field is not required for action to create new content item. 
                bindingContext.ModelState.MarkFieldSkipped("Id");
            }
            #endregion

            #region ContentName field
            valueProviderResult = bindingContext.ValueProvider.GetValue("ContentName");
            bindingContext.ModelState.SetModelValue("ContentName", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
                model.ContentName = valueProviderResult.FirstValue;
                bindingContext.ModelState.MarkFieldValid("ContentName");
            }
            else
            {
                bindingContext.ModelState.AddModelError("ContentName", $"ContentName could not be bound");
            }
            #endregion

            #region ContentTypeId field
            valueProviderResult = bindingContext.ValueProvider.GetValue("ContentTypeId");
            bindingContext.ModelState.SetModelValue("ContentTypeId", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None &&
                Guid.TryParse(valueProviderResult.FirstValue, out Guid contentTypeIdVal))
            {
                model.ContentTypeId = contentTypeIdVal;
                bindingContext.ModelState.MarkFieldValid("ContentTypeId");
            }
            else
            {
                bindingContext.ModelState.AddModelError("ContentTypeId", $"ContentTypeId not found or value <{valueProviderResult.FirstValue}> could not be parsed as a Guid");
            }
            #endregion

            #region ClientId field
            valueProviderResult = bindingContext.ValueProvider.GetValue("ClientId");
            bindingContext.ModelState.SetModelValue("ClientId", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None &&
                Guid.TryParse(valueProviderResult.FirstValue, out Guid clientIdVal))
            {
                model.ClientId = clientIdVal;
                bindingContext.ModelState.MarkFieldValid("ClientId");
            }
            else
            {
                bindingContext.ModelState.AddModelError("ClientId", $"ClientId not found or value <{valueProviderResult.FirstValue}> could not be parsed as a Guid");
            }
            #endregion

            #region DoesReduce field
            valueProviderResult = bindingContext.ValueProvider.GetValue("DoesReduce");
            bindingContext.ModelState.SetModelValue("DoesReduce", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None &&
                bool.TryParse(valueProviderResult.FirstValue, out bool doesReduceVal))
            {
                model.DoesReduce = doesReduceVal;
                bindingContext.ModelState.MarkFieldValid("DoesReduce");
            }
            else
            {
                bindingContext.ModelState.AddModelError("DoesReduce", $"DoesReduce not found or value <{valueProviderResult.FirstValue}> could not be parsed as a bool");
            }
            #endregion
            #endregion

            // Find the corresponding content type enumeration value
            ContentType contentType = default;
            using (IServiceScope scope = bindingContext.HttpContext.RequestServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                contentType = dbContext.ContentType.Find(model.ContentTypeId);
            }

            if (contentType != null)
            #region Fetch the values of type specific properties
            {
                switch (contentType.TypeEnum)
                {
                    case ContentTypeEnum.PowerBi:
                        var properties = new PowerBiContentItemProperties();

                        valueProviderResult = bindingContext.ValueProvider.GetValue("FilterPaneEnabled");
                        bindingContext.ModelState.SetModelValue("TypeSpecificDetailObject.FilterPaneEnabled", valueProviderResult);
                        if (valueProviderResult != ValueProviderResult.None &&
                            bool.TryParse(valueProviderResult.FirstValue, out bool filterPaneEnabledVal))
                        {
                            properties.FilterPaneEnabled = filterPaneEnabledVal;
                            bindingContext.ModelState.MarkFieldValid("TypeSpecificDetailObject.FilterPaneEnabled");
                        }
                        else
                        {
                            bindingContext.ModelState.AddModelError("TypeSpecificDetailObject.FilterPaneEnabled", $"TypeSpecificDetailObject.FilterPaneEnabled property not found or value <{valueProviderResult.FirstValue}> could not be parsed as a bool");
                        }

                        valueProviderResult = bindingContext.ValueProvider.GetValue("navContentPaneEnabled");
                        bindingContext.ModelState.SetModelValue("TypeSpecificDetailObject.NavContentPaneEnabled", valueProviderResult);
                        if (valueProviderResult != ValueProviderResult.None &&
                            bool.TryParse(valueProviderResult.FirstValue, out bool navContentPaneEnabledVal))
                        {
                            properties.FilterPaneEnabled = navContentPaneEnabledVal;
                            bindingContext.ModelState.MarkFieldValid("TypeSpecificDetailObject.NavContentPaneEnabled");
                        }
                        else
                        {
                            bindingContext.ModelState.AddModelError("TypeSpecificDetailObject.NavContentPaneEnabled", $"TypeSpecificDetailObject.NavContentPaneEnabled property not found or value <{valueProviderResult.FirstValue}> could not be parsed as a bool");
                        }

                        model.TypeSpecificDetail = JsonConvert.SerializeObject(properties);
                        break;

                    case ContentTypeEnum.Qlikview:
                    case ContentTypeEnum.Pdf:
                    case ContentTypeEnum.Html:
                    case ContentTypeEnum.FileDownload:
                    default:
                        break;
                }
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


