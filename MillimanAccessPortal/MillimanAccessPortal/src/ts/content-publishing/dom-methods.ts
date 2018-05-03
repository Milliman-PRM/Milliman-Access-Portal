import $ = require('jquery');
require('tooltipster');
import shared = require('../shared');
import { ClientCard, RootContentItemCard } from '../card';
import { ClientTree, RootContentItemList, RootContentItemSummary, BasicNode, ClientSummary } from '../view-models/content-access-admin';



function renderRootContentItem(rootContentItem: RootContentItemSummary) {
  const $card = new RootContentItemCard(
    rootContentItem,
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
  response.DetailList.forEach(renderRootContentItem);
  $rootContentItemList.find('.tooltip').tooltipster();

  if (!isNaN(rootContentItemId)) {
    $(`[data-root-content-item-id=${rootContentItemId}]`).click();
  }
}


function renderClientNode(rootClient: BasicNode<ClientSummary>, level: number = 0) {
  const $card = new ClientCard(
    rootClient.Value,
    rootClient.Value.EligibleUserCount,
    rootClient.Value.RootContentItemCount,
    level,
    shared.wrapCardCallback(shared.get(
      'ContentPublishing/RootContentItems',
      [ renderRootContentItemList ],
    )),
  );
  $card.disabled = !rootClient.Value.CanManage;
  $('#client-tree ul.admin-panel-content').append($card.build());

  // Render child nodes
  rootClient.Children.forEach((childNode) => {
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
