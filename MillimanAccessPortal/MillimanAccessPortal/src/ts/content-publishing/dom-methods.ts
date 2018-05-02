import $ = require('jquery');
require('tooltipster');
import shared = require('../shared');
import { ClientCard, RootContentItemCard } from '../card';
import { ClientTree, ClientWithChildren, RootContentItemList, RootContentItemDetail } from '../view-models/content-access-admin';



function renderRootContentItem(rootContentItem: RootContentItemDetail) {
  const $card = new RootContentItemCard(
    rootContentItem.RootContentItemEntity,
    rootContentItem.GroupCount,
    rootContentItem.EligibleUserCount,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/SelectionGroups',
      [ ],
    )),
  ).build();
  shared.updateCardStatus($card, rootContentItem.PublicationDetails);
  $('#root-content-items ul.admin-panel-content').append($card);
}
function renderRootContentItemList(response: RootContentItemList, rootContentItemId?: number) {
  const $rootContentItemList = $('#root-content-items ul.admin-panel-content');
  $rootContentItemList.empty();
  response.RootContentItemList.forEach(renderRootContentItem);
  $rootContentItemList.find('.tooltip').tooltipster();

  if (!isNaN(rootContentItemId)) {
    $(`[data-root-content-item-id=${rootContentItemId}]`).click();
  }
}


function renderClientNode(rootClient: ClientWithChildren, level: number = 0) {
  const $card = new ClientCard(
    rootClient.ClientDetailModel.ClientEntity,
    rootClient.ClientDetailModel.EligibleUserCount,
    rootClient.ClientDetailModel.RootContentItemCount,
    level,
    shared.wrapCardCallback(shared.get(
      'ContentAccessAdmin/RootContentItems',
      [ renderRootContentItemList ],
    )),
  );
  $card.disabled = !rootClient.ClientDetailModel.CanManage;
  $('#client-tree ul.admin-panel-content').append($card.build());

  // Render child nodes
  rootClient.ChildClientModels.forEach((childNode) => {
    renderClientNode(childNode, level + 1);
  });
}
export function renderClientTree(response: ClientTree, clientId?: number) {
  const $clientTreeList = $('#client-tree ul.admin-panel-content');
  $clientTreeList.empty();
  response.ClientTreeList.forEach((rootClient) => {
    renderClientNode(rootClient);
    $clientTreeList.append('<li class="hr width-100pct"></li>');
  });
  $clientTreeList.find('.hr').last().remove();
  $clientTreeList.find('.tooltip').tooltipster();

  if (!isNaN(clientId)) {
    $(`[data-client-id=${clientId}]`).click();
  }
}
