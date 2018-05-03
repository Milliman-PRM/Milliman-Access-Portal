import $ = require('jquery');
require('tooltipster');
import shared = require('../shared');
import { ClientCard, RootContentItemCard } from '../card';
import { ClientTree, RootContentItemList, RootContentItemSummary, BasicNode, ClientSummary, RootContentItemDetail } from '../view-models/content-access-admin';



function mapRootContentItemDetail(item: RootContentItemDetail) {
  const formMap = new Map<string, string | number | boolean>();

  formMap.set('Id',item.Id);
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
  item.AvailableContentTypes.forEach((contentType) => {
    $contentTypeDropdown.append(
      new Option(contentType.Name, contentType.Id.toString()));
  });

  const formMap = mapRootContentItemDetail(item);
  formMap.forEach((value, key) => {
    const $element = $rootContentItemForm.find(`#${key}`);
    if (typeof value === 'boolean') {
      $element.prop('checked', value);
    } else {
      $element.val(value.toString());
    }
  });


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
export function renderClientTree(response: ClientTree, clientId?: number) {
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
