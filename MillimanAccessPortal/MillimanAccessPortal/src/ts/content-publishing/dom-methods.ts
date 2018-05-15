import $ = require('jquery');
require('tooltipster');
import * as shared from '../shared';
import { ClientCard, RootContentItemCard, AddRootContentItemActionCard } from '../card';
import { FormBase } from '../form/form-base';
import { AccessMode } from '../form/form-modes';
import { ClientTree, RootContentItemList, RootContentItemSummary, BasicNode, ClientSummary, RootContentItemDetail, ContentType } from '../view-models/content-publishing';
import { setUnloadAlert } from '../unload-alerts';
import { DeleteRootContentItemDialog } from '../dialog';
import { SubmissionGroup } from '../form/form-submission';


export namespace ContentPublishingDOMMethods {
  const forms = new Map<number, FormBase>();
  let currentForm: FormBase;
  let currentFormId: number;

  function mapRootContentItemDetail(item: RootContentItemDetail) {
    const formMap = new Map<string, string | number | boolean>();

    formMap.set('Id',item.Id);
    formMap.set('ClientId',item.ClientId);
    formMap.set('ContentName',item.ContentName);
    formMap.set('ContentTypeId',item.ContentTypeId);
    formMap.set('DoesReduce', item.DoesReduce);
    formMap.set('Description',item.Description);
    formMap.set('Notes',item.Notes);

    return formMap;
  }

  function renderRootContentItemForm(item: RootContentItemDetail) {
    const $panel = $('#content-publishing-form');
    const $rootContentItemForm = $panel.find('form.admin-panel-content');
    shared.clearForm($panel);

    const formMap = mapRootContentItemDetail(item);
    formMap.forEach((value, key) => {
      $rootContentItemForm.find(`#${key}`).val(value.toString());
    });

    const $doesReduceToggle = $rootContentItemForm.find(`#DoesReduce`);
    $doesReduceToggle.prop('checked', item.DoesReduce);



    const createContentGroup = new SubmissionGroup<RootContentItemDetail>(
      [
        'root-content-item-info',
        'root-content-item-description',
      ],
      'ContentPublishing/CreateRootContentItem',
      'POST',
      (response) => { },
    );
    const updateContentGroup = new SubmissionGroup<RootContentItemDetail>(
      [
        'root-content-item-info',
        'root-content-item-description',
      ],
      'ContentPublishing/UpdateRootContentItem',
      'POST',
      (response) => { },
    );
    const submitPublication = new SubmissionGroup<any>(
      [
        'publication-files',
      ],
      'ContentPublishing/Publish',
      'POST',
      (response) => { },
    );

    // First unbind existing form if it exists
    if (currentForm) {
      currentForm.unbindFromDOM();
      forms.set(currentFormId, currentForm);
    }

    // Create/retrieve and bind the new form
    if (!forms.has(item.Id)) {
      currentForm = new FormBase();
      currentForm.bindToDOM($rootContentItemForm[0]);
      currentForm.configure(
        [
          {
            group: createContentGroup.chain(submitPublication),
            name: 'new',
          },
          {
            group: updateContentGroup,
            name: 'edit',
          },
          {
            group: submitPublication,
            name: 'republish',
          },
        ],
      );
    } else {
      currentForm = forms.get(item.Id);
      currentForm.bindToDOM($rootContentItemForm[0]);
    }
    
    currentForm.accessMode = AccessMode.Read;
    currentFormId = item.Id;
  }


  function rootContentItemPublishClickHandler() {
    var $clickedCard = $(this).closest('.card-container');
    var rootContentItemId = $clickedCard.data().rootContentItemId;
    event.stopPropagation();

    const form = forms.get(rootContentItemId);
    form.accessMode = AccessMode.Write;
    form.submissionMode = 'republish';
    $('#content-publishing-form').show();
  }
  function rootContentItemEditClickHandler() {
    var $clickedCard = $(this).closest('.card-container');
    var rootContentItemId = $clickedCard.data().rootContentItemId;
    event.stopPropagation();

    const form = forms.get(rootContentItemId);
    form.accessMode = AccessMode.Write;
    form.submissionMode = 'edit';
    $('#content-publishing-form').show();
  }
  function deleteRootContentItem(rootContentItemId: string, rootContentItemName: string, password: string, callback: () => void) {
    // TODO: AJAX request to delete root content item
  }
  function rootContentItemDeleteClickHandler() {
    var $clickedCard = $(this).closest('.card-container');
    var rootContentItemId = $clickedCard.data().rootContentItemId;
    var rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
    event.stopPropagation();
    new DeleteRootContentItemDialog(
      rootContentItemName,
      rootContentItemId,
      function (data, callback) {
        if (data.password) {
          shared.showButtonSpinner($('.vex-first'), 'Deleting');
          $('.vex-dialog-button').attr('disabled', '');
          deleteRootContentItem(rootContentItemId, rootContentItemName, data.password, callback);
        } else if (data.password === '') {
          toastr.warning('Please enter your password to proceed');
          return false;
        } else {
          toastr.info('Deletion was canceled');
        }
        return true;
      },
    ).open();
  }
  function newRootContentItemClickHandler() {
    shared.wrapCardCallback(() => {
      renderRootContentItemForm({
        ClientId: 1,
        ContentName: '',
        ContentTypeId: 0,
        Description: '',
        DoesReduce: false,
        Id: 0,
        Notes: '',
      });
    })();
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
      rootContentItemPublishClickHandler,
      rootContentItemEditClickHandler,
      rootContentItemDeleteClickHandler,
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

  function populateAvailableContentTypes(contentTypes: Array<ContentType>) {
    const $panel = $('#content-publishing-form');
    const $rootContentItemForm = $panel.find('form.admin-panel-content');

    const $contentTypeDropdown = $rootContentItemForm.find('#ContentTypeId');
    $contentTypeDropdown.children(':not(option[value = ""])').remove();

    contentTypes.forEach((contentType) => {
      const option = new Option(contentType.Name, contentType.Id.toString());
      $(option).data(contentType);
      $contentTypeDropdown.append(option);
    });

    $contentTypeDropdown.change(); // trigger change event
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

    $('.action-icon-expand').click(shared.expandAllListener);
    $('.action-icon-collapse').click(shared.collapseAllListener);
    $('.admin-panel-searchbar-tree').keyup(shared.filterTreeListener);
    $('.admin-panel-searchbar-form').keyup(shared.filterFormListener);

    $('#root-content-items ul.admin-panel-content-action')
      .append(new AddRootContentItemActionCard(newRootContentItemClickHandler).build());

    $('.admin-panel-toolbar .action-icon-edit').click(() => {
      currentForm.submissionMode = 'edit';
      currentForm.accessMode = AccessMode.Defer;
    });
    $('.admin-panel-toolbar .action-icon-cancel').click(() => {
      currentForm.accessMode = AccessMode.Read;
    });
    $('.admin-panel-toolbar .action-icon-file-upload').click(() => {
      currentForm.submissionMode = 'republish';
      currentForm.accessMode = AccessMode.Defer;
    });

    setUnloadAlert(() => currentForm && currentForm.modified);

    $('.tooltip').tooltipster();

    shared.get(
      'ContentPublishing/AvailableContentTypes',
      [ populateAvailableContentTypes ],
    )();
    shared.get(
      'ContentPublishing/Clients',
      [ renderClientTree ],
    )();
  }
}
