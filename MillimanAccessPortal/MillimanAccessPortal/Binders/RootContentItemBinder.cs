/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using MapDbContextLib.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using MillimanAccessPortal.Controllers;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MillimanAccessPortal.Binders
{
    public class RootContentItemBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }
            ControllerActionDescriptor actionDescriptor = bindingContext.ActionContext.ActionDescriptor as ControllerActionDescriptor;

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
            ContentTypeEnum requestedContentType = ContentTypeEnum.Unknown;
            valueProviderResult = bindingContext.ValueProvider.GetValue("ContentTypeId");
            bindingContext.ModelState.SetModelValue("ContentTypeId", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None &&
                Guid.TryParse(valueProviderResult.FirstValue, out Guid contentTypeIdVal))
            {
                model.ContentTypeId = contentTypeIdVal;
                bindingContext.ModelState.MarkFieldValid("ContentTypeId");

                using (IServiceScope scope = bindingContext.HttpContext.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                    requestedContentType = dbContext.ContentType.SingleOrDefault(ct => ct.Id == model.ContentTypeId)?.TypeEnum ?? ContentTypeEnum.Unknown;
                }
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

            #region Description field
            valueProviderResult = bindingContext.ValueProvider.GetValue("Description");
            bindingContext.ModelState.SetModelValue("Description", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
                model.Description = valueProviderResult.FirstValue;
                bindingContext.ModelState.MarkFieldValid("Description");
            }
            else
            {
                bindingContext.ModelState.AddModelError("Description", $"Description could not be bound");
            }
            #endregion

            #region Notes field
            valueProviderResult = bindingContext.ValueProvider.GetValue("Notes");
            bindingContext.ModelState.SetModelValue("Notes", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
                model.Notes = valueProviderResult.FirstValue;
                bindingContext.ModelState.MarkFieldValid("Notes");
            }
            else
            {
                bindingContext.ModelState.AddModelError("Notes", $"Notes could not be bound");
            }
            #endregion

            #region IsSuspended field
            // Not expected in Create action
            valueProviderResult = bindingContext.ValueProvider.GetValue("IsSuspended");
            bindingContext.ModelState.SetModelValue("IsSuspended", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None &&
                bool.TryParse(valueProviderResult.FirstValue, out bool IsSuspendedVal))
            {
                model.IsSuspended = IsSuspendedVal;
                bindingContext.ModelState.MarkFieldValid("IsSuspended");
            }
            else
            {
                bindingContext.ModelState.MarkFieldSkipped("IsSuspended");
            }
            #endregion

            #region ContentDisclaimer field
            valueProviderResult = bindingContext.ValueProvider.GetValue("ContentDisclaimer");
            bindingContext.ModelState.SetModelValue("ContentDisclaimer", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
                model.ContentDisclaimer = valueProviderResult.FirstValue;
                bindingContext.ModelState.MarkFieldValid("ContentDisclaimer");
            }
            else
            {
                bindingContext.ModelState.AddModelError("ContentDisclaimer", $"ContentDisclaimer could not be bound");
            }
            #endregion

            #region ContentFiles field
            valueProviderResult = bindingContext.ValueProvider.GetValue("ContentFilesList");
            bindingContext.ModelState.SetModelValue("ContentFilesList", valueProviderResult);
            if (valueProviderResult != ValueProviderResult.None)
            {
               // model.ContentFilesList = valueProviderResult.FirstValue;
                bindingContext.ModelState.MarkFieldValid("ContentFilesList");
            }
            else
            {
                bindingContext.ModelState.MarkFieldSkipped("ContentFilesList");
                //bindingContext.ModelState.AddModelError("ContentFilesList", $"ContentFilesList could not be bound");
            }
            #endregion
            #endregion

            #region Fetch the values of type specific properties
            switch (requestedContentType)
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
                        properties.FilterPaneEnabled = false;
                        bindingContext.ModelState.MarkFieldValid("TypeSpecificDetailObject.FilterPaneEnabled");
                    }

                    valueProviderResult = bindingContext.ValueProvider.GetValue("NavigationPaneEnabled");
                    bindingContext.ModelState.SetModelValue("TypeSpecificDetailObject.NavigationPaneEnabled", valueProviderResult);
                    if (valueProviderResult != ValueProviderResult.None &&
                        bool.TryParse(valueProviderResult.FirstValue, out bool navContentPaneEnabledVal))
                    {
                        properties.NavigationPaneEnabled = navContentPaneEnabledVal;
                        bindingContext.ModelState.MarkFieldValid("TypeSpecificDetailObject.NavigationPaneEnabled");
                    }
                    else
                    {
                        properties.NavigationPaneEnabled = false;
                        bindingContext.ModelState.MarkFieldValid("TypeSpecificDetailObject.NavigationPaneEnabled");
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
            #endregion

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


