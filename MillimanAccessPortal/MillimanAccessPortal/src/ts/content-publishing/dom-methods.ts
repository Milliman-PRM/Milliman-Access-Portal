import $ = require('jquery');
require('tooltipster');
import * as shared from '../shared';
import { ClientCard, RootContentItemCard } from '../card';
import { EntityForm } from '../entity-form/entity-form';
import { SubmissionMode, AccessMode } from '../entity-form/form-modes';
import { ClientTree, RootContentItemList, RootContentItemSummary, BasicNode, ClientSummary, RootContentItemDetail, ContentType } from '../view-models/content-publishing';
import { setUnloadAlert } from '../unload-alerts';


export namespace ContentPublishingDOMMethods {
  let form: EntityForm;

  function mapRootContentItemDetail(item: RootContentItemDetail) {
    const formMap = new Map<string, string | number | boolean>();

    formMap.set('Id',item.Id);
    formMap.set('ClientId',item.ClientId);
    formMap.set('ContentName',item.ContentName);
    formMap.set('ContentTypeId',item.ContentType.Id);
    formMap.set('DoesReduce', item.DoesReduce);
    formMap.set('Description',item.Description);
    formMap.set('Notes',item.Notes);

    return formMap;
  }

  function renderRootContentItemForm(item: RootContentItemDetail) {
    const $panel = $('#content-publishing-form');
    const $rootContentItemForm = $panel.find('form.admin-panel-content');
    shared.clearForm($panel);

    const $contentTypeDropdown = $rootContentItemForm.find('#ContentTypeId');
    $contentTypeDropdown.children(':not(option[value = ""])').remove();
    item.AvailableContentTypes.forEach((contentType) => {
      const option = new Option(contentType.Name, contentType.Id.toString());
      $(option).data(contentType);
      $contentTypeDropdown.append(option);
    });

    const formMap = mapRootContentItemDetail(item);
    formMap.forEach((value, key) => {
      $rootContentItemForm.find(`#${key}`).val(value.toString());
    });

    const $doesReduceToggle = $rootContentItemForm.find(`#DoesReduce`);
    $doesReduceToggle.prop('checked', item.DoesReduce);

    $contentTypeDropdown.change(); // trigger change event

    form = new EntityForm();
    form.bind($rootContentItemForm[0]);
    form.configure(
      [
        {
          url: 'ContentPublishing/CreateRootContentItem',
          sections: [
            'root-content-item-info',
            'root-content-item-description',
          ],
        },
        {
          url: 'ContentPublishing/Publish',
          sections: [
            'publication-files',
          ],
        },
      ],
      SubmissionMode.Update,
    );
  }


  function renderRootContentItem(item: RootContentItemSummary) {
    const $card = new RootContentItemCard(
      item,
      item.GroupCount,
      item.EligibleUserCount,
      shared.wrapCardCallback(shared.get(
        'ContentPublishing/RootContentItemDetail',
        [ renderRootContentItemForm ],
      )),
    ).build();
    shared.updateCardStatus($card, item.PublicationDetails);
    $('#root-content-items ul.admin-panel-content').append($card);
  }
  function renderRootContentItemList(response: RootContentItemList, rootContentItemId?: number) {
    const $rootContentItemList = $('#root-content-items ul.admin-panel-content');
    $rootContentItemList.empty();
    response.DetailList.forEach(renderRootContentItem);
    $rootContentItemList.find('.tooltip').tooltipster();

    if (!isNaN(rootContentItemId)) {
      $(`[data-root-content-item-id=${rootContentItemId}]`).click();
    }
  }


  function renderClientNode(client: BasicNode<ClientSummary>, level: number = 0) {
    const $card = new ClientCard(
      client.Value,
      client.Value.EligibleUserCount,
      client.Value.RootContentItemCount,
      level,
      shared.wrapCardCallback(shared.get(
        'ContentPublishing/RootContentItems',
        [ renderRootContentItemList ],
      )),
    );
    $card.disabled = !client.Value.CanManage;
    $('#client-tree ul.admin-panel-content').append($card.build());

    // Render child nodes
    client.Children.forEach((childNode) => {
      renderClientNode(childNode, level + 1);
    });
  }
  function renderClientTree(response: ClientTree, clientId?: number) {
    const $clientTreeList = $('#client-tree ul.admin-panel-content');
    $clientTreeList.empty();
    response.Root.Children.forEach((rootClient) => {
      renderClientNode(rootClient);
      $clientTreeList.append('<li class="hr width-100pct"></li>');
    });
    $clientTreeList.find('.hr').last().remove();
    $clientTreeList.find('.tooltip').tooltipster();

    if (!isNaN(clientId)) {
      $(`[data-client-id=${clientId}]`).click();
    }
  }

  export function setup() {
    const $contentTypeDropdown = $('#ContentTypeId');
    $contentTypeDropdown.change(() => {
      const $doesReduceToggle = $('#DoesReduce');
      const contentType = $contentTypeDropdown
        .find(`option[value="${$contentTypeDropdown.val()}"]`)
        .data() as ContentType;
      if (!contentType.CanReduce) {
        $doesReduceToggle.attr('disabled', '');
        $doesReduceToggle.prop('checked', false);
      } else {
        $doesReduceToggle.removeAttr('disabled');
      }
    });

    $('#content-publishing-form .new-form-button-container button.submit-button').click(() => {
      const formData = $('#content-publishing-form form').serialize();
      $.post({
        url: 'ContentPublishing/CreateRootContentItem',
        data: formData,
      }).done((response) => {
      }).fail((response) => {
      });
    });

    $('.action-icon-expand').click(shared.expandAllListener);
    $('.action-icon-collapse').click(shared.collapseAllListener);
    $('.admin-panel-searchbar-tree').keyup(shared.filterTreeListener);
    $('.admin-panel-searchbar-form').keyup(shared.filterFormListener);

    $('.action-icon-edit').click(() => {
      form.mode = AccessMode.Write;
    });
    $('.action-icon-cancel').click(() => {
      form.mode = AccessMode.Read;
    });

    setUnloadAlert(() => form && form.modified);

    $('.tooltip').tooltipster();

    shared.get(
      'ContentPublishing/Clients',
      [ renderClientTree ],
    )();
  }
}
